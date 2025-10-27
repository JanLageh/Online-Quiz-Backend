using Microsoft.AspNetCore.Http;

namespace OnlineQuiz.DTOs
{
    public class ImportStudentsRequest
    {
        public IFormFile File { get; set; }
        public long? UserId { get; set; }
    }
}