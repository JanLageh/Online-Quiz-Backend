using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OnlineQuiz.DTOs;
using OnlineQuiz.IServices;
using static OnlineQuiz.DTOs.AttemptDtos;

namespace OnlineQuiz.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class QuizController : ControllerBase
    {
        private readonly IQuizService _service;

        public QuizController(IQuizService service)
        {
            _service = service;
        }

        private long GetCurrentUserIdOrDefault()
        {
            var idClaim = User.FindFirst("nameid")?.Value;
            if (long.TryParse(idClaim, out var userId))
                return userId;
            return 1; // Default user ID for testing
        }

        //GET /api/quiz
        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] long? courseId = null,
            [FromQuery] long? teacherId = null)
        {
            var response = await _service.GetQuizzesAsync(page, pageSize, courseId, teacherId);
            return response.Success ? Ok(response) : BadRequest(response);
        }

        //GET /api/quiz/{id}
        [HttpGet("{id:long}")]
        public async Task<IActionResult> GetById(long id)
        {
            var response = await _service.GetQuizByIdAsync(id);
            return response.Success ? Ok(response) : NotFound(response);
        }

        //POST /api/quiz
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] QuizDTO.CreateQuizDto dto)
        {
            long userId = GetCurrentUserIdOrDefault();
            var response = await _service.CreateQuizAsync(dto, userId);
            return response.Success ? Ok(response) : BadRequest(response);
        }

        //PUT /api/quiz/{id}
        [HttpPut("{id:long}")]
        public async Task<IActionResult> Update(long id, [FromBody] QuizDTO.UpdateQuizDto dto)
        {
            long userId = GetCurrentUserIdOrDefault();
            var response = await _service.UpdateQuizAsync(id, dto, userId);
            return response.Success ? Ok(response) : BadRequest(response);
        }

        //DELETE /api/quiz/{id}
        [HttpDelete("{id:long}")]
        public async Task<IActionResult> Delete(long id)
        {
            long userId = GetCurrentUserIdOrDefault();
            var response = await _service.DeleteQuizAsync(id, userId);
            return response.Success ? Ok(response) : BadRequest(response);
        }

        //POST /api/quiz/{id}/publish
        [HttpPost("{id:long}/publish")]
        public async Task<IActionResult> Publish(long id)
        {
            long userId = GetCurrentUserIdOrDefault();
            var response = await _service.PublishQuizAsync(id, userId);
            return response.Success ? Ok(response) : BadRequest(response);
        }

        //Add a new question to a quiz
        [HttpPost("{quizId:long}/questions")]
        public async Task<IActionResult> AddQuestion(long quizId, [FromBody] QuizDTO.CreateQuestionDto dto)
        {
            dto.QuizId = quizId;
            var response = await _service.AddQuestionAsync(dto);
            return response.Success ? Ok(response) : BadRequest(response);
        }

        //Get all questions for a quiz
        [HttpGet("{quizId:long}/questions")]
        public async Task<IActionResult> GetQuestions(long quizId)
        {
            var response = await _service.GetQuestionsByQuizIdAsync(quizId);
            return response.Success ? Ok(response) : NotFound(response);
        }

        //Add choices to a question
        [HttpPost("/api/questions/{questionId:long}/choices")]
        public async Task<IActionResult> AddChoices(long questionId, [FromBody] IEnumerable<QuizDTO.CreateChoiceDto> choices)
        {
            var response = await _service.AddChoicesAsync(questionId, choices);
            return response.Success ? Ok(response) : BadRequest(response);
        }

        //Get all choices for a question
        [HttpGet("/api/questions/{questionId:long}/choices")]
        public async Task<IActionResult> GetChoices(long questionId)
        {
            var response = await _service.GetChoicesByQuestionIdAsync(questionId);
            return response.Success ? Ok(response) : NotFound(response);
        }


        /// Submit a quiz attempt
        /// POST /api/quiz/{quizId}/submit
        [HttpPost("{quizId:long}/submit")]
        [AllowAnonymous] // Allow both authenticated and anonymous for testing
        public async Task<IActionResult> SubmitAttempt(long quizId, [FromBody] SubmitAttemptDto dto)
        {
            try
            {
                dto.QuizId = quizId; // Ensure quizId matches the route
                var userId = GetCurrentUserIdOrDefault();
                var result = await _service.SubmitAttemptAsync(dto, userId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// Get all attempts for a specific quiz (Teacher/Admin)
        /// GET /api/quiz/{quizId}/attempts
        [HttpGet("{quizId:long}/attempts")]
        [AllowAnonymous] // Allow both authenticated and anonymous for testing
        public async Task<IActionResult> GetQuizAttempts(long quizId)
        {
            try
            {
                var attempts = await _service.GetQuizAttemptsAsync(quizId);
                return Ok(attempts);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// Get all attempts for the current user
        /// GET /api/quiz/my-attempts
        [HttpGet("my-attempts")]
        [AllowAnonymous] // Allow both authenticated and anonymous for testing
        public async Task<IActionResult> GetMyAttempts([FromQuery] long? userId = null)
        {
            try
            {
                var actualUserId = userId ?? GetCurrentUserIdOrDefault();
                var attempts = await _service.GetUserAttemptsAsync(actualUserId);
                return Ok(attempts);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// Get detailed information about a specific attempt
        /// GET /api/quiz/attempts/{attemptId}
        [HttpGet("attempts/{attemptId:long}")]
        [AllowAnonymous] // Allow both authenticated and anonymous for testing
        public async Task<IActionResult> GetAttemptDetail(long attemptId)
        {
            try
            {
                var attempt = await _service.GetAttemptDetailAsync(attemptId);
                return Ok(attempt);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}