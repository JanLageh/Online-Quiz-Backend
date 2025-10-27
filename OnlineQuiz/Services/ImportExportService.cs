using Microsoft.AspNetCore.Http;
using OnlineQuiz.DTOs;
using OnlineQuiz.IRepository;
using OnlineQuiz.IServices;
using System.Threading.Tasks;
using static OnlineQuiz.DTOs.ImportExportDtos;

namespace OnlineQuiz.Services
{
    public class ImportExportService : IImportExportService
    {
        private readonly IImportExportRepository _repository;

        public ImportExportService(IImportExportRepository repository)
        {
            _repository = repository;
        }

        public Task<ImportResponseDto> ImportStudentsFromFileAsync(IFormFile file, long? userId)
            => _repository.ImportStudentsFromFileAsync(file, userId);

        public Task<ImportResponseDto> ImportQuestionsFromFileAsync(IFormFile file, long quizId, long? userId)
            => _repository.ImportQuestionsFromFileAsync(file, quizId, userId);

        public Task<byte[]> ExportStudentsToFileAsync(long courseId, string format, long? userId)
            => _repository.ExportStudentsToFileAsync(courseId, format, userId);

        public Task<byte[]> ExportQuizResultsToFileAsync(long quizId, string format, long? userId)
            => _repository.ExportQuizResultsToFileAsync(quizId, format, userId);
    }
}
