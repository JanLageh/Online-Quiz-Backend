using Microsoft.AspNetCore.Http;
using OnlineQuiz.DTOs;
using static OnlineQuiz.DTOs.ImportExportDtos;

namespace OnlineQuiz.IServices
{
    public interface IImportExportService
    {
        Task<ImportResponseDto> ImportStudentsFromFileAsync(IFormFile file, long? userId);
        Task<ImportResponseDto> ImportQuestionsFromFileAsync(IFormFile file, long quizId, long? userId);
        Task<byte[]> ExportStudentsToFileAsync(long courseId, string format, long? userId);
        Task<byte[]> ExportQuizResultsToFileAsync(long quizId, string format, long? userId);
    }
}