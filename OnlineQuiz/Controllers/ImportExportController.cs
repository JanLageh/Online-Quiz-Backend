using Microsoft.AspNetCore.Mvc;
using OnlineQuiz.DTOs;
using OnlineQuiz.IServices;

namespace OnlineQuiz.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ImportExportController : ControllerBase
    {
        private readonly IImportExportService _importExportService;

        public ImportExportController(IImportExportService importExportService)
        {
            _importExportService = importExportService;
        }

        private long? GetCurrentUserId()
        {
            var idClaim = User.FindFirst("nameid")?.Value;
            return long.TryParse(idClaim, out var userId) ? userId : null;
        }

        [HttpPost("import/students")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> ImportStudents([FromForm] ImportStudentsRequest request)
        {
            var userId = request.UserId ?? GetCurrentUserId();
            var result = await _importExportService.ImportStudentsFromFileAsync(request.File, userId);
            return Ok(result);
        }

        [HttpPost("import/questions")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> ImportQuestions([FromForm] ImportQuestionsRequest request)
        {
            var userId = request.UserId ?? GetCurrentUserId();
            var result = await _importExportService.ImportQuestionsFromFileAsync(request.File, request.QuizId, userId);
            return Ok(result);
        }

        [HttpGet("export/students/{courseId}")]
        public async Task<IActionResult> ExportStudents(long courseId, [FromQuery] string format = "csv")
        {
            var userId = GetCurrentUserId();
            var fileBytes = await _importExportService.ExportStudentsToFileAsync(courseId, format, userId);
            var mimeType = format == "xlsx"
                ? "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
                : "text/csv";
            return File(fileBytes, mimeType, $"students_{courseId}.{format}");
        }

        [HttpGet("export/results/{quizId}")]
        public async Task<IActionResult> ExportResults(long quizId, [FromQuery] string format = "csv")
        {
            var userId = GetCurrentUserId();
            var fileBytes = await _importExportService.ExportQuizResultsToFileAsync(quizId, format, userId);
            var mimeType = format == "xlsx"
                ? "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
                : "text/csv";
            return File(fileBytes, mimeType, $"quiz_results_{quizId}.{format}");
        }
    }
}
