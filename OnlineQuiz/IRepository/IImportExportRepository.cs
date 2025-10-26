using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace OnlineQuiz.IRepository
{
    public interface IImportExportRepository
    {
        Task<string> ImportStudentsFromFileAsync(IFormFile file);
        Task<string> ImportQuestionsFromFileAsync(IFormFile file, long quizId);

        Task<byte[]> ExportStudentsToFileAsync(long courseId, string format = "csv");
        Task<byte[]> ExportQuizResultsToFileAsync(long quizId, string format = "csv");
    }
}
