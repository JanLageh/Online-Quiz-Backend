using Microsoft.AspNetCore.Http;
using OnlineQuiz.IRepository;
using OnlineQuiz.IServices;
using OnlineQuiz.Models.Response;
using System;
using System.Threading.Tasks;

namespace OnlineQuiz.Services
{
    public class ImportExportService : IImportExportService
    {
        private readonly IImportExportRepository _repo;

        public ImportExportService(IImportExportRepository repo)
        {
            _repo = repo;
        }

        //Import Students
        public async Task<ServiceResponse<string>> ImportStudentsAsync(IFormFile file)
        {
            var response = new ServiceResponse<string>();
            try
            {
                if (file == null || file.Length == 0)
                {
                    response.Success = false;
                    response.Message = "Invalid or empty file.";
                    return response;
                }

                var result = await _repo.ImportStudentsFromFileAsync(file);

                response.Data = result;
                response.Message = "Students imported successfully.";
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = $"Error importing students: {ex.Message}";
            }
            return response;
        }

        //Import Questions
        public async Task<ServiceResponse<string>> ImportQuestionsAsync(IFormFile file, long quizId)
        {
            var response = new ServiceResponse<string>();
            try
            {
                if (file == null || file.Length == 0)
                {
                    response.Success = false;
                    response.Message = "Invalid or empty file.";
                    return response;
                }

                var result = await _repo.ImportQuestionsFromFileAsync(file, quizId);
                response.Data = result;
                response.Message = "Questions imported successfully.";
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = $"Error importing questions: {ex.Message}";
            }
            return response;
        }

        //Export Students
        public async Task<ServiceResponse<byte[]>> ExportStudentsAsync(long courseId, string format = "csv")
        {
            var response = new ServiceResponse<byte[]>();
            try
            {
                var result = await _repo.ExportStudentsToFileAsync(courseId, format);
                response.Data = result;
                response.Message = $"Student list exported successfully as {format.ToUpper()}.";
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = $"Error exporting students: {ex.Message}";
            }
            return response;
        }

        //Export Quiz Results
        public async Task<ServiceResponse<byte[]>> ExportQuizResultsAsync(long quizId, string format = "csv")
        {
            var response = new ServiceResponse<byte[]>();
            try
            {
                var result = await _repo.ExportQuizResultsToFileAsync(quizId, format);
                response.Data = result;
                response.Message = $"Quiz results exported successfully as {format.ToUpper()}.";
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = $"Error exporting quiz results: {ex.Message}";
            }
            return response;
        }
    }
}
