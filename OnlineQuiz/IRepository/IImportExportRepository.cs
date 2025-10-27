using Microsoft.AspNetCore.Http;
using OnlineQuiz.DTOs;
using System.Threading.Tasks;
using static OnlineQuiz.DTOs.ImportExportDtos;

namespace OnlineQuiz.IRepository
{
    public interface IImportExportRepository
    {
        Task<ImportResponseDto> ImportStudentsFromFileAsync(IFormFile file, long? userId);
        Task<ImportResponseDto> ImportQuestionsFromFileAsync(IFormFile file, long quizId, long? userId);
        Task<byte[]> ExportStudentsToFileAsync(long courseId, string format, long? userId);
        Task<byte[]> ExportQuizResultsToFileAsync(long quizId, string format, long? userId);
    }
}
