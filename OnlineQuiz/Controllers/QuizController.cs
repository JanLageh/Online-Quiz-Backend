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
    }
}
