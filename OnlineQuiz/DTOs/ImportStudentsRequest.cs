using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;
namespace OnlineQuiz.DTOs
{
    public class ImportStudentsRequest
    {
        [Required]
        public IFormFile File { get; set; }
        public long? UserId { get; set; }
    }
}