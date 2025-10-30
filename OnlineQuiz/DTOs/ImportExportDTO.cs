using System.ComponentModel.DataAnnotations;

namespace OnlineQuiz.DTOs
{
    public class ImportExportDtos
    {
        // Student Import DTO
        public class ImportStudentDto
        {
            [Required]
            [StringLength(100)]
            public string FullName { get; set; } = string.Empty;

            [Required]
            [EmailAddress]
            [StringLength(100)]
            public string Email { get; set; } = string.Empty;

            [Required]
            [StringLength(50)]
            public string StudentNumber { get; set; } = string.Empty;
        }

        // Question Import DTO
        public class ImportQuestionDto
        {
            [Required]
            [StringLength(1000)]
            public string Body { get; set; } = string.Empty;

            [Required]
            [StringLength(20)]
            public string Type { get; set; } = "Single"; // Single, Multiple, TrueFalse

            [Range(0, 100)]
            public decimal Points { get; set; } = 1;

            [Range(1, int.MaxValue)]
            public int SortOrder { get; set; } = 1;
        }

        // Export Result DTO
        public class ExportResultDto
        {
            public string Student { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public decimal Score { get; set; }
            public DateTime? SubmittedAt { get; set; }
        }

        // Export Student DTO
        public class ExportStudentDto
        {
            public string FullName { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public string Status { get; set; } = string.Empty;
            public string CourseName { get; set; } = string.Empty;
            public string CourseCode { get; set; } = string.Empty;
        }

        // Import Response DTO
        public class ImportResponseDto
        {
            public bool Success { get; set; }
            public int ImportedCount { get; set; }
            public int ErrorCount { get; set; }
            public string Message { get; set; } = string.Empty;
            public List<string> Errors { get; set; } = new();
        }

        // Export/Import Log DTO
        public class ExportImportLogDto
        {
            public long LogId { get; set; }
            public long UserId { get; set; }
            public string UserName { get; set; } = string.Empty;
            public string Action { get; set; } = string.Empty;
            public string Entity { get; set; } = string.Empty;
            public string? FileName { get; set; }
            public DateTime CreatedAt { get; set; }
        }

        // Create Export/Import Log DTO
        public class CreateExportImportLogDto
        {
            [Required]
            public long UserId { get; set; }

            [Required]
            [StringLength(20)]
            public string Action { get; set; } = string.Empty;

            [Required]
            [StringLength(60)]
            public string Entity { get; set; } = string.Empty;

            [StringLength(255)]
            public string? FileName { get; set; }
        }
    }

}
