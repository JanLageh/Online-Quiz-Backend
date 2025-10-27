using Microsoft.AspNetCore.Http;

namespace OnlineQuiz.DTOs
{
    public class ImportQuestionsRequest
    {
        public IFormFile File { get; set; }
        public long QuizId { get; set; }
        public long? UserId { get; set; }
    }
}
