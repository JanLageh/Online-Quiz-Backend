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
using System.Text.RegularExpressions;

namespace OnlineQuiz.Repository
{
    public class ImportExportRepository : IImportExportRepository
    {
        private readonly OnlineQuizDbContext _context;
        private const int MaxFileSize = 10 * 1024 * 1024; // 10MB

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

                // Validate file size
                if (file.Length > MaxFileSize)
                {
                    response.Success = false;
                    response.Message = "File size exceeds maximum allowed size (10MB).";
                    return response;
                }

                // Validate file name (prevent directory traversal)
                if (!Regex.IsMatch(file.FileName, @"^[a-zA-Z0-9_\-\.]+$"))
                {
                    response.Success = false;
                    response.Message = "Invalid file name. Only alphanumeric characters, underscores, hyphens, and dots are allowed.";
                    return response;
                }

                var students = new List<ImportStudentDto>();

                using (var transaction = await _context.Database.BeginTransactionAsync())
                {
                    try
                    {
                        using var stream = new MemoryStream();
                        await file.CopyToAsync(stream);
                        stream.Position = 0;

                        if (extension == ".csv")
                        {
                            students = await ParseCsvStudents(stream, errors);
                        }
                        else if (extension == ".xlsx")
                        {
                            students = await ParseExcelStudentsAsync(stream, errors);
                        }

                        // Bulk check for duplicates
                        var emails = students.Select(s => s.Email).ToList();
                        var studentNumbers = students.Select(s => s.StudentNumber).ToList();

                        var existingEmails = await _context.Users
                            .Where(u => emails.Contains(u.Email))
                            .Select(u => u.Email)
                            .ToListAsync();

                        var existingStudentNumbers = await _context.Students
                            .Where(s => studentNumbers.Contains(s.StudentNumber))
                            .Select(s => s.StudentNumber)
                            .ToListAsync();

                        var usersToAdd = new List<UserModel>();
                        var studentsToAdd = new List<StudentModel>();
                        var userRolesToAdd = new List<UserRoleModel>();

                        foreach (var student in students)
                        {
                            try
                            {
                                // Check for duplicates
                                if (existingEmails.Contains(student.Email))
                                {
                                    errors.Add($"Email already exists: {student.Email}");
                                    continue;
                                }

                                if (existingStudentNumbers.Contains(student.StudentNumber))
                                {
                                    errors.Add($"Student number already exists: {student.StudentNumber}");
                                    continue;
                                }

                                // Create user with secure password
                                var password = GenerateSecurePassword();
                                var user = new UserModel
                                {
                                    Email = student.Email,
                                    FullName = student.FullName,
                                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                                    Status = "Active",
                                    CreatedAt = DateTime.UtcNow,
                                    UpdatedAt = DateTime.UtcNow
                                };

                                usersToAdd.Add(user);

                                // Create student
                                studentsToAdd.Add(new StudentModel
                                {
                                    UserId = user.UserId, // Will be set after save
                                    StudentNumber = student.StudentNumber
                                });

                                // Assign Student role
                                var studentRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "Student");
                                if (studentRole != null)
                                {
                                    userRolesToAdd.Add(new UserRoleModel
                                    {
                                        UserId = user.UserId,
                                        RoleId = studentRole.RoleId
                                    });
                                }
                            }
                            catch (DbUpdateException ex)
                            {
                                errors.Add($"Database error for {student.Email}: {ex.InnerException?.Message}");
                            }
                            catch (FormatException ex)
                            {
                                errors.Add($"Format error for {student.Email}: {ex.Message}");
                            }
                            catch (Exception ex)
                            {
                                errors.Add($"Error processing {student.Email}: {ex.Message}");
                            }
                        }

                        // Batch save
                        if (usersToAdd.Any())
                        {
                            await _context.Users.AddRangeAsync(usersToAdd);
                            await _context.SaveChangesAsync();

                            // Update IDs for related entities
                            for (int i = 0; i < usersToAdd.Count; i++)
                            {
                                var user = usersToAdd[i];
                                studentsToAdd[i].UserId = user.UserId;
                                userRolesToAdd[i].UserId = user.UserId;
                            }

                            await _context.Students.AddRangeAsync(studentsToAdd);
                            await _context.UserRoles.AddRangeAsync(userRolesToAdd);
                            await _context.SaveChangesAsync();

                            response.ImportedCount = usersToAdd.Count;
                        }

                        await transaction.CommitAsync();
                    }
                    catch
                    {
                        await transaction.RollbackAsync();
                        throw;
                    }
                }

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

                // Validate file size
                if (file.Length > MaxFileSize)
                {
                    response.Success = false;
                    response.Message = "File size exceeds maximum allowed size (10MB).";
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

                using (var transaction = await _context.Database.BeginTransactionAsync())
                {
                    try
                    {
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
                            questions = await ParseExcelQuestionsAsync(stream, errors);
                        }

                        // Validate question numbers don't conflict with existing questions
                        var existingQuestionNumbers = await _context.Questions
                            .Where(q => q.QuizId == quizId)
                            .Select(q => q.SortOrder)
                            .ToListAsync();

                        var questionsToAdd = new List<QuestionModel>();

                        foreach (var question in questions)
                        {
                            try
                            {
                                // Check if question number already exists for this quiz
                                if (existingQuestionNumbers.Contains(question.SortOrder))
                                {
                                    errors.Add($"Question with Sort Order {question.SortOrder} already exists in this quiz.");
                                    continue;
                                }

                                questionsToAdd.Add(new QuestionModel
                                {
                                    QuizId = quizId,
                                    Body = question.Body,
                                    Type = question.Type,
                                    Points = question.Points,
                                    SortOrder = question.SortOrder
                                });
                            }
                            catch (DbUpdateException ex)
                            {
                                errors.Add($"Database error for question: {ex.InnerException?.Message}");
                            }
                            catch (Exception ex)
                            {
                                errors.Add($"Error processing question: {ex.Message}");
                            }
                        }

                        // Batch save
                        if (questionsToAdd.Any())
                        {
                            await _context.Questions.AddRangeAsync(questionsToAdd);
                            await _context.SaveChangesAsync();
                            response.ImportedCount = questionsToAdd.Count;
                        }

                        await transaction.CommitAsync();
                    }
                    catch
                    {
                        await transaction.RollbackAsync();
                        throw;
                    }
                }

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
                // More specific error handling
                if (ex is IOException)
                    errors.Add("File access error. Please ensure the file is not open by another program.");
                else if (ex is InvalidDataException)
                    errors.Add("Invalid CSV format. Please ensure the file is not corrupted.");
            }

            return students;
        }

        private async Task<List<ImportStudentDto>> ParseExcelStudentsAsync(Stream stream, List<string> errors)
        {
            var students = new List<ImportStudentDto>();

            try
            {
                using var workbook = new XLWorkbook(stream);
                var ws = workbook.Worksheets.First();
                var rows = ws.RowsUsed().Skip(1); // Skip header

                foreach (var row in rows)
                {
                    try
                    {
                        var student = new ImportStudentDto
                        {
                            FullName = row.Cell(1).GetValue<string>(),
                            Email = row.Cell(2).GetValue<string>(),
                            StudentNumber = row.Cell(3).GetValue<string>()
                        };

                        if (ValidateStudent(student, errors))
                        {
                            students.Add(student);
                        }
                    }
                    catch (Exception rowEx)
                    {
                        errors.Add($"Error processing row {row.RowNumber()}: {rowEx.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                errors.Add($"Excel parsing error: {ex.Message}");
                // More specific error handling
                if (ex is IOException)
                    errors.Add("File access error. Please ensure the file is not open by another program.");
                else if (ex is InvalidDataException)
                    errors.Add("Invalid Excel format. Please ensure the file is not corrupted.");
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

        private async Task<List<ImportQuestionDto>> ParseExcelQuestionsAsync(Stream stream, List<string> errors)
        {
            var questions = new List<ImportQuestionDto>();

            try
            {
                using var workbook = new XLWorkbook(stream);
                var ws = workbook.Worksheets.First();
                var rows = ws.RowsUsed().Skip(1); // Skip header

                foreach (var row in rows)
                {
                    try
                    {
                        var question = new ImportQuestionDto
                        {
                            Body = row.Cell(1).GetValue<string>(),
                            Type = row.Cell(2).GetValue<string>() ?? "Single",
                            Points = row.Cell(3).TryGetValue(out decimal points) ? points : 1,
                            SortOrder = row.Cell(4).TryGetValue(out int sortOrder) ? sortOrder : 1
                        };

                        if (ValidateQuestion(question, errors))
                        {
                            questions.Add(question);
                        }
                    }
                    catch (Exception rowEx)
                    {
                        errors.Add($"Error processing row {row.RowNumber()}: {rowEx.Message}");
                    }
                }
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

        private bool ValidateQuestion(ImportQuestionDto question, List<string> errors, int? rowNumber = null)
        {
            if (string.IsNullOrWhiteSpace(question.Body))
            {
                if (rowNumber.HasValue)
                {
                    errors.Add($"Row {rowNumber.Value}: Question body is required.");
                }
                else
                {
                    errors.Add("Question body is required.");
                }
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

        private string GenerateSecurePassword()
        {
            const string validChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890!@#$%^&*";
            var random = new Random();
            var password = new StringBuilder();
            for (int i = 0; i < 12; i++)
            {
                password.Append(validChars[random.Next(validChars.Length)]);
            }
            return password.ToString();
        }
    }
}