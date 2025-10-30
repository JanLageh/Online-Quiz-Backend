using Microsoft.AspNetCore.Http;

namespace OnlineQuiz.DTOs
{
    public class ImportQuestionsRequest
    {
        public required IFormFile File { get; set; }
        public long QuizId { get; set; }
        public long? UserId { get; set; }
    }
}
