using Microsoft.AspNetCore.Mvc;
using OnlineQuiz.DTOs;
using OnlineQuiz.IServices;

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

        //GET /api/quizzes
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

        //GET /api/quizzes/{id}
        [HttpGet("{id:long}")]
        public async Task<IActionResult> GetById(long id)
        {
            var response = await _service.GetQuizByIdAsync(id);
            return response.Success ? Ok(response) : NotFound(response);
        }

        //POST /api/quizzes
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] QuizDTO.CreateQuizDto dto)
        {
            long userId = 1; // TODO: Replace with real logged-in user context
            var response = await _service.CreateQuizAsync(dto, userId);
            return response.Success ? Ok(response) : BadRequest(response);
        }

        //PUT /api/quizzes/{id}
        [HttpPut("{id:long}")]
        public async Task<IActionResult> Update(long id, [FromBody] QuizDTO.UpdateQuizDto dto)
        {
            long userId = 1;
            var response = await _service.UpdateQuizAsync(id, dto, userId);
            return response.Success ? Ok(response) : BadRequest(response);
        }

        //DELETE /api/quizzes/{id}
        [HttpDelete("{id:long}")]
        public async Task<IActionResult> Delete(long id)
        {
            long userId = 1;
            var response = await _service.DeleteQuizAsync(id, userId);
            return response.Success ? Ok(response) : BadRequest(response);
        }

        //POST /api/quizzes/{id}/publish
        [HttpPost("{id:long}/publish")]
        public async Task<IActionResult> Publish(long id)
        {
            long userId = 1;
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
    }
}
