using CsvHelper;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using OnlineQuiz.Data;
using OnlineQuiz.IRepository;
using OnlineQuiz.Models;
using System.Globalization;
using System.Text;

namespace OnlineQuiz.Repository
{
    public class ImportExportRepository : IImportExportRepository
    {
        private readonly OnlineQuizDbContext _context;

        public ImportExportRepository(OnlineQuizDbContext context)
        {
            _context = context;
        }

        public async Task<string> ImportStudentsFromFileAsync(IFormFile file)
        {
            var importedCount = 0;
            var errors = new List<string>();

            try
            {
                using var stream = new MemoryStream();
                await file.CopyToAsync(stream);
                stream.Position = 0;

                var extension = Path.GetExtension(file.FileName).ToLower();
                var students = new List<StudentModel>();

                if (extension == ".csv")
                {
                    using var reader = new StreamReader(stream);
                    using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
                    var records = csv.GetRecords<dynamic>().ToList();

                    foreach (var record in records)
                    {
                        var email = record.Email?.ToString() ?? "";
                        bool exists = _context.Users.AsEnumerable().Any(u => u.Email == email);
                        if (string.IsNullOrEmpty(email) || exists)
                        {
                            errors.Add($"Duplicate or invalid email: {email}");
                            continue;
                        }

                        var user = new UserModel
                        {
                            Email = email,
                            FullName = record.FullName,
                            PasswordHash = "Default@123", // placeholder
                            Status = "Active",
                            CreatedAt = DateTime.UtcNow
                        };

                        var student = new StudentModel
                        {
                            User = user,
                            StudentNumber = record.StudentNumber
                        };

                        _context.Users.Add(user);
                        _context.Students.Add(student);
                        importedCount++;
                    }
                }
                else if (extension == ".xlsx")
                {
                    using var workbook = new XLWorkbook(stream);
                    var ws = workbook.Worksheets.First();
                    var rows = ws.RowsUsed().Skip(1);

                    foreach (var row in rows)
                    {
                        var email = row.Cell(2).GetString();
                        if (string.IsNullOrEmpty(email) || _context.Users.Any(u => u.Email == email))
                        {
                            errors.Add($"Duplicate or invalid email: {email}");
                            continue;
                        }

                        var user = new UserModel
                        {
                            Email = email,
                            FullName = row.Cell(1).GetString(),
                            PasswordHash = "Default@123",
                            Status = "Active",
                            CreatedAt = DateTime.UtcNow
                        };

                        var student = new StudentModel
                        {
                            User = user,
                            StudentNumber = row.Cell(3).GetString()
                        };

                        _context.Users.Add(user);
                        _context.Students.Add(student);
                        importedCount++;
                    }
                }

                await _context.SaveChangesAsync();

                await LogOperationAsync("ImportStudents", $"Imported {importedCount} students.");
                return $"Successfully imported {importedCount} students. {errors.Count} errors.";
            }
            catch (Exception ex)
            {
                await LogOperationAsync("ImportStudents", $"Error: {ex.Message}");
                throw;
            }
        }

        public async Task<string> ImportQuestionsFromFileAsync(IFormFile file, long quizId)
        {
            var importedCount = 0;

            try
            {
                using var stream = new MemoryStream();
                await file.CopyToAsync(stream);
                stream.Position = 0;

                var extension = Path.GetExtension(file.FileName).ToLower();
                var quiz = await _context.Quizzes.Include(q => q.Questions)
                                                 .FirstOrDefaultAsync(q => q.QuizId == quizId);
                if (quiz == null) throw new Exception("Quiz not found.");

                if (extension == ".csv")
                {
                    using var reader = new StreamReader(stream);
                    using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
                    var records = csv.GetRecords<dynamic>().ToList();

                    foreach (var record in records)
                    {
                        var question = new QuestionModel
                        {
                            QuizId = quizId,
                            Body = record.Body,
                            Type = record.Type ?? "Single",
                            Points = Convert.ToDecimal(record.Points ?? 1),
                            SortOrder = Convert.ToInt32(record.SortOrder ?? 1)
                        };
                        _context.Questions.Add(question);
                        importedCount++;
                    }
                }
                else if (extension == ".xlsx")
                {
                    using var workbook = new XLWorkbook(stream);
                    var ws = workbook.Worksheets.First();
                    var rows = ws.RowsUsed().Skip(1);

                    foreach (var row in rows)
                    {
                        var question = new QuestionModel
                        {
                            QuizId = quizId,
                            Body = row.Cell(1).GetString(),
                            Type = row.Cell(2).GetString() ?? "Single",
                            Points = row.Cell(3).GetValue<decimal>(),
                            SortOrder = row.Cell(4).GetValue<int>()
                        };
                        _context.Questions.Add(question);
                        importedCount++;
                    }
                }

                await _context.SaveChangesAsync();

                await LogOperationAsync("ImportQuestions", $"Imported {importedCount} questions for quiz {quizId}.");
                return $"Successfully imported {importedCount} questions.";
            }
            catch (Exception ex)
            {
                await LogOperationAsync("ImportQuestions", $"Error: {ex.Message}");
                throw;
            }
        }

        public async Task<byte[]> ExportStudentsToFileAsync(long courseId, string format = "csv")
        {
            var enrollments = await _context.Enrollments
                .Include(e => e.User)
                .Include(e => e.Course)
                .Where(e => e.CourseId == courseId)
                .ToListAsync();

            var data = enrollments.Select(e => new
            {
                e.User.FullName,
                e.User.Email,
                e.User.Status,
                e.Course.Name,
                e.Course.Code
            }).ToList();

            await LogOperationAsync("ExportStudents", $"Exported {data.Count} students from course {courseId}.");

            return format.ToLower() switch
            {
                "xlsx" => GenerateExcel(data),
                _ => GenerateCsv(data)
            };
        }


        public async Task<byte[]> ExportQuizResultsToFileAsync(long quizId, string format = "csv")
        {
            var quiz = await _context.Quizzes
                .Include(q => q.Attempts).ThenInclude(a => a.User)
                .FirstOrDefaultAsync(q => q.QuizId == quizId);

            if (quiz == null)
                throw new Exception("Quiz not found.");

            var results = quiz.Attempts.Select(a => new
            {
                Student = a.User.FullName,
                Email = a.User.Email,
                a.Score,
                a.SubmittedAt
            }).ToList();

            await LogOperationAsync("ExportResults", $"Exported results for quiz {quizId}.");

            return format.ToLower() switch
            {
                "xlsx" => GenerateExcel(results),
                _ => GenerateCsv(results)
            };
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

        private async Task LogOperationAsync(string action, string message)
        {
            var log = new ExportImportLogModel
            {
                Action = action,
            };

            var detailsProp = typeof(ExportImportLogModel).GetProperty("Details") ??
                              typeof(ExportImportLogModel).GetProperty("Description");

            if (detailsProp != null)
                detailsProp.SetValue(log, message);

            _context.ExportImportLogs.Add(log);
            await _context.SaveChangesAsync();
        }
    }
}
