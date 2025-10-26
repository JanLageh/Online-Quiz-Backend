using Microsoft.AspNetCore.Mvc;
using OnlineQuiz.IServices;

namespace OnlineQuiz.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ImportExportController : ControllerBase
    {
        private readonly IImportExportService _service;

        public ImportExportController(IImportExportService service)
        {
            _service = service;
        }

        // POST: /api/importexport/import/students
        [HttpPost("import/students")]
        public async Task<IActionResult> ImportStudents([FromForm] IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded.");

            var result = await _service.ImportStudentsAsync(file);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        // POST: /api/importexport/import/questions
        [HttpPost("import/questions")]
        public async Task<IActionResult> ImportQuestions([FromForm] IFormFile file, [FromQuery] long quizId)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded.");

            var result = await _service.ImportQuestionsAsync(file, quizId);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        // GET: /api/importexport/export/students/{courseId}
        [HttpGet("export/students/{courseId:long}")]
        public async Task<IActionResult> ExportStudents(long courseId, [FromQuery] string format = "csv")
        {
            var fileResult = await _service.ExportStudentsAsync(courseId, format);
            if (!fileResult.Success || fileResult.Data == null)
                return BadRequest(fileResult.Message);

            var fileBytes = fileResult.Data;
            var contentType = format.ToLower() == "xlsx"
                ? "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
                : "text/csv";
            var fileName = $"Students_Course_{courseId}_{DateTime.UtcNow:yyyyMMddHHmmss}.{format}";

            return File(fileBytes, contentType, fileName);
        }

        // GET: /api/importexport/export/results/{quizId}
        [HttpGet("export/results/{quizId:long}")]
        public async Task<IActionResult> ExportQuizResults(long quizId, [FromQuery] string format = "csv")
        {
            var fileResult = await _service.ExportQuizResultsAsync(quizId, format);
            if (!fileResult.Success || fileResult.Data == null)
                return BadRequest(fileResult.Message);

            var fileBytes = fileResult.Data;
            var contentType = format.ToLower() == "xlsx"
                ? "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
                : "text/csv";
            var fileName = $"QuizResults_{quizId}_{DateTime.UtcNow:yyyyMMddHHmmss}.{format}";

            return File(fileBytes, contentType, fileName);
        }
    }
}
