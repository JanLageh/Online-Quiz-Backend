using CsvHelper;
using CsvHelper.Configuration;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using OnlineQuiz.Data;
using OnlineQuiz.DTOs;
using OnlineQuiz.IRepository;
using OnlineQuiz.Models;
using System.Globalization;
using System.Text;
using static OnlineQuiz.DTOs.ImportExportDtos;
using AutoMapper;

namespace OnlineQuiz.Repository
{
    public class ImportExportRepository : IImportExportRepository
    {
        private readonly OnlineQuizDbContext _context;

        public ImportExportRepository(OnlineQuizDbContext context)
        {
            _context = context;
        }

        public async Task<ImportResponseDto> ImportStudentsFromFileAsync(IFormFile file, long? userId)
        {
            var response = new ImportResponseDto();
            var errors = new List<string>();

            try
            {
                // Validate file
                if (file == null || file.Length == 0)
                {
                    response.Success = false;
                    response.Message = "No file provided or file is empty.";
                    return response;
                }

                var extension = Path.GetExtension(file.FileName).ToLower();
                if (extension != ".csv" && extension != ".xlsx")
                {
                    response.Success = false;
                    response.Message = "Invalid file format. Only CSV and XLSX are supported.";
                    return response;
                }

                using var stream = new MemoryStream();
                await file.CopyToAsync(stream);
                stream.Position = 0;

                var students = new List<ImportStudentDto>();

                if (extension == ".csv")
                {
                    students = await ParseCsvStudents(stream, errors);
                }
                else if (extension == ".xlsx")
                {
                    students = ParseExcelStudents(stream, errors);
                }

                // Import students to database
                foreach (var student in students)
                {
                    try
                    {
                        // Check if user already exists
                        var existingUser = await _context.Users
                            .FirstOrDefaultAsync(u => u.Email == student.Email);

                        if (existingUser != null)
                        {
                            errors.Add($"Email already exists: {student.Email}");
                            continue;
                        }

                        // Check if student number already exists
                        var existingStudent = await _context.Students
                            .FirstOrDefaultAsync(s => s.StudentNumber == student.StudentNumber);

                        if (existingStudent != null)
                        {
                            errors.Add($"Student number already exists: {student.StudentNumber}");
                            continue;
                        }

                        // Create user
                        var user = new UserModel
                        {
                            Email = student.Email,
                            FullName = student.FullName,
                            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Default@123"),
                            Status = "Active",
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        };

                        _context.Users.Add(user);
                        await _context.SaveChangesAsync(); // Save to get UserId

                        // Create student
                        var studentModel = new StudentModel
                        {
                            UserId = user.UserId,
                            StudentNumber = student.StudentNumber
                        };

                        _context.Students.Add(studentModel);

                        // Assign Student role
                        var studentRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "Student");
                        if (studentRole != null)
                        {
                            _context.UserRoles.Add(new UserRoleModel
                            {
                                UserId = user.UserId,
                                RoleId = studentRole.RoleId
                            });
                        }
                        response.ImportedCount++;
                    }
                    catch (Exception ex) // Only catch non-critical exceptions
                    {
                        if (ex is OutOfMemoryException || ex is StackOverflowException || ex is ThreadAbortException)

                            errors.Add($"Error importing {student.Email}: {ex.Message}");
                    }
                }

                await _context.SaveChangesAsync();

                response.Success = true;
                response.ErrorCount = errors.Count;
                response.Errors = errors;
                response.Message = $"Successfully imported {response.ImportedCount} students. {errors.Count} errors.";

                await LogOperationAsync("Import", "Students", file.FileName, userId);

                return response;
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = $"Import failed: {ex.Message}";
                response.Errors = errors;

                await LogOperationAsync("Import", "Students", file.FileName, userId);

                return response;
            }
        }

        public async Task<ImportResponseDto> ImportQuestionsFromFileAsync(IFormFile file, long quizId, long? userId)
        {
            var response = new ImportResponseDto();
            var errors = new List<string>();

            try
            {
                // Validate file
                if (file == null || file.Length == 0)
                {
                    response.Success = false;
                    response.Message = "No file provided or file is empty.";
                    return response;
                }

                var extension = Path.GetExtension(file.FileName).ToLower();
                if (extension != ".csv" && extension != ".xlsx")
                {
                    response.Success = false;
                    response.Message = "Invalid file format. Only CSV and XLSX are supported.";
                    return response;
                }

                // Validate quiz exists
                var quiz = await _context.Quizzes
                    .Include(q => q.Questions)
                    .FirstOrDefaultAsync(q => q.QuizId == quizId);

                if (quiz == null)
                {
                    response.Success = false;
                    response.Message = "Quiz not found.";
                    return response;
                }

                using var stream = new MemoryStream();
                await file.CopyToAsync(stream);
                stream.Position = 0;

                var questions = new List<ImportQuestionDto>();

                if (extension == ".csv")
                {
                    questions = await ParseCsvQuestions(stream, errors);
                }
                else if (extension == ".xlsx")
                {
                    questions = ParseExcelQuestions(stream, errors);
                }

                // Import questions to database
                foreach (var question in questions)
                {
                    try
                    {
                        var questionModel = new QuestionModel
                        {
                            QuizId = quizId,
                            Body = question.Body,
                            Type = question.Type,
                            Points = question.Points,
                            SortOrder = question.SortOrder
                        };

                        _context.Questions.Add(questionModel);
                        response.ImportedCount++;
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"Error importing question: {ex.Message}");
                    }
                }

                await _context.SaveChangesAsync();

                response.Success = true;
                response.ErrorCount = errors.Count;
                response.Errors = errors;
                response.Message = $"Successfully imported {response.ImportedCount} questions. {errors.Count} errors.";

                await LogOperationAsync("Import", "Questions", file.FileName, userId);

                return response;
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = $"Import failed: {ex.Message}";
                response.Errors = errors;

                await LogOperationAsync("Import", "Questions", file.FileName, userId);

                return response;
            }
        }

        public async Task<byte[]> ExportStudentsToFileAsync(long courseId, string format, long? userId)
        {
            var enrollments = await _context.Enrollments
                .Include(e => e.User)
                .Include(e => e.Course)
                .Where(e => e.CourseId == courseId)
                .ToListAsync();

            var data = enrollments.Select(e => new ExportStudentDto
            {
                FullName = e.User.FullName,
                Email = e.User.Email,
                Status = e.User.Status,
                CourseName = e.Course.Name,
                CourseCode = e.Course.Code
            }).ToList();

            await LogOperationAsync("Export", "Students", $"students_{courseId}.{format}", userId);

            return format.ToLower() switch
            {
                "xlsx" => GenerateExcel(data),
                _ => GenerateCsv(data)
            };
        }

        public async Task<byte[]> ExportQuizResultsToFileAsync(long quizId, string format, long? userId)
        {
            var quiz = await _context.Quizzes
                .Include(q => q.Attempts)
                    .ThenInclude(a => a.User)
                .FirstOrDefaultAsync(q => q.QuizId == quizId);

            if (quiz == null)
                throw new Exception("Quiz not found.");

            var results = quiz.Attempts.Select(a => new ExportResultDto
            {
                Student = a.User.FullName,
                Email = a.User.Email,
                Score = a.Score,
                SubmittedAt = a.SubmittedAt
            }).ToList();

            await LogOperationAsync("Export", "Results", $"quiz_results_{quizId}.{format}", userId);

            return format.ToLower() switch
            {
                "xlsx" => GenerateExcel(results),
                _ => GenerateCsv(results)
            };
        }

        // Helper Methods
        private async Task<List<ImportStudentDto>> ParseCsvStudents(Stream stream, List<string> errors)
        {
            var students = new List<ImportStudentDto>();

            try
            {
                using var reader = new StreamReader(stream);
                var config = new CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    HasHeaderRecord = true,
                    MissingFieldFound = null,
                    HeaderValidated = null
                };

                using var csv = new CsvReader(reader, config);

                await foreach (var record in csv.GetRecordsAsync<ImportStudentDto>())
                {
                    if (ValidateStudent(record, errors))
                    {
                        students.Add(record);
                    }
                }
            }
            catch (Exception ex)
            {
                errors.Add($"CSV parsing error: {ex.Message}");
            }

            return students;
        }

        private List<ImportStudentDto> ParseExcelStudents(Stream stream, List<string> errors)
        {
            var students = new List<ImportStudentDto>();

            try
            {
                using var workbook = new XLWorkbook(stream);
                var ws = workbook.Worksheets.First();
                var rows = ws.RowsUsed().Skip(1); // Skip header

                students = rows
                    .Select(row => new ImportStudentDto
                    {
                        FullName = row.Cell(1).GetString(),
                        Email = row.Cell(2).GetString(),
                        StudentNumber = row.Cell(3).GetString()
                    })
                    .Where(student => ValidateStudent(student, errors))
                    .ToList();
            }
            catch (FormatException ex)
            {
                errors.Add($"Excel parsing error: {ex.Message}");
            }
            catch (IOException ex)
            {
                errors.Add($"Excel parsing error: {ex.Message}");
            }
            catch (InvalidDataException ex)
            {
                errors.Add($"Excel parsing error: {ex.Message}");
            }
            catch (ArgumentException ex)
            {
                errors.Add($"Excel parsing error: {ex.Message}");
            }

            return students;
        }

        private async Task<List<ImportQuestionDto>> ParseCsvQuestions(Stream stream, List<string> errors)
        {
            var questions = new List<ImportQuestionDto>();

            try
            {
                using var reader = new StreamReader(stream);
                var config = new CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    HasHeaderRecord = true,
                    MissingFieldFound = null,
                    HeaderValidated = null
                };

                using var csv = new CsvReader(reader, config);

                await foreach (var record in csv.GetRecordsAsync<ImportQuestionDto>())
                {
                    if (ValidateQuestion(record, errors))
                    {
                        questions.Add(record);
                    }
                }
            }
            catch (Exception ex)
            {
                errors.Add($"CSV parsing error: {ex.Message}");
            }

            return questions;
        }

        private List<ImportQuestionDto> ParseExcelQuestions(Stream stream, List<string> errors)
        {
            var questions = new List<ImportQuestionDto>();

            try
            {
                using var workbook = new XLWorkbook(stream);
                var ws = workbook.Worksheets.First();
                var rows = ws.RowsUsed().Skip(1); // Skip header

                questions = rows
                    .Select(row => new ImportQuestionDto
                    {
                        Body = row.Cell(1).GetString(),
                        Type = row.Cell(2).GetString() ?? "Single",
                        Points = row.Cell(3).TryGetValue(out decimal points) ? points : 1,
                        SortOrder = row.Cell(4).TryGetValue(out int sortOrder) ? sortOrder : 1
                    })
                    .Where(question => ValidateQuestion(question, errors))
                    .ToList();
            }
            catch (Exception ex)
            {
                errors.Add($"Excel parsing error: {ex.Message}");
            }

            return questions;
        }

        private bool ValidateStudent(ImportStudentDto student, List<string> errors)
        {
            if (string.IsNullOrWhiteSpace(student.FullName))
            {
                errors.Add("Full name is required.");
                return false;
            }

            if (string.IsNullOrWhiteSpace(student.Email) || !IsValidEmail(student.Email))
            {
                errors.Add($"Invalid email: {student.Email}");
                return false;
            }

            if (string.IsNullOrWhiteSpace(student.StudentNumber))
            {
                errors.Add($"Student number is required for {student.Email}");
                return false;
            }

            return true;
        }

        private bool ValidateQuestion(ImportQuestionDto question, List<string> errors)
        {
            if (string.IsNullOrWhiteSpace(question.Body))
            {
                errors.Add("Question body is required.");
                return false;
            }

            var validTypes = new[] { "Single", "Multiple", "TrueFalse" };
            if (!validTypes.Contains(question.Type))
            {
                errors.Add($"Invalid question type: {question.Type}");
                return false;
            }

            if (question.Points < 0 || question.Points > 100)
            {
                errors.Add($"Points must be between 0 and 100.");
                return false;
            }

            return true;
        }


        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static byte[] GenerateCsv<T>(IEnumerable<T> data)
        {
            using var ms = new MemoryStream();
            using var writer = new StreamWriter(ms, Encoding.UTF8);
            using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
            csv.WriteRecords(data);
            writer.Flush();
            return ms.ToArray();
        }

        private static byte[] GenerateExcel<T>(IEnumerable<T> data)
        {
            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Sheet1");
            ws.Cell(1, 1).InsertTable(data);
            using var ms = new MemoryStream();
            workbook.SaveAs(ms);
            return ms.ToArray();
        }

        private async Task LogOperationAsync(string action, string entity, string? fileName, long? userId)
        {
            try
            {
                var log = new ExportImportLogModel
                {
                    UserId = userId ?? 1, // Default to system user if not provided
                    Action = action,
                    Entity = entity,
                    FileName = fileName,
                    CreatedAt = DateTime.UtcNow
                };

                _context.ExportImportLogs.Add(log);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                // Log operation should not break the main flow
                Console.Error.WriteLine($"[LogOperationAsync] Logging failed: {ex}");
            }
        }
    }
}