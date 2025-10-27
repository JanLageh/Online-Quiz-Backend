using Microsoft.AspNetCore.Http;
using OnlineQuiz.Models.Response;
using System.Threading.Tasks;

namespace OnlineQuiz.IServices
{
    public interface IImportExportService
    {
        Task<ServiceResponse<string>> ImportStudentsAsync(IFormFile file);
        Task<ServiceResponse<string>> ImportQuestionsAsync(IFormFile file, long quizId);

        Task<ServiceResponse<byte[]>> ExportStudentsAsync(long courseId, string format = "csv");
        Task<ServiceResponse<byte[]>> ExportQuizResultsAsync(long quizId, string format = "csv");
    }
}
