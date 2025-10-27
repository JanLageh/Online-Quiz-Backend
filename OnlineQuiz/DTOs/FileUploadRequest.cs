using Microsoft.AspNetCore.Http;

namespace OnlineQuiz.DTOs
{
    public class FileUploadRequest
    {
        public IFormFile File { get; set; }
    }
}
