using System.ComponentModel.DataAnnotations;

namespace OnlineQuiz.DTOs
{
    public class AttemptDtos
    {
        // Submit Quiz Attempt DTO
        public class SubmitAttemptDto
        {
            [Required]
            public long QuizId { get; set; }

            [Required]
            public List<SubmitAnswerDto> Answers { get; set; } = new();
        }

        // Individual Answer DTO
        public class SubmitAnswerDto
        {
            [Required]
            public long QuestionId { get; set; }

            // For single choice questions
            public long? ChoiceId { get; set; }

            // For multiple choice questions
            public List<long>? ChoiceIds { get; set; }

            // For text-based answers (if applicable)
            public string? TextAnswer { get; set; }
        }

        // Attempt Result DTO
        public class AttemptResultDto
        {
            public long AttemptId { get; set; }
            public long QuizId { get; set; }
            public string QuizTitle { get; set; } = string.Empty;
            public decimal Score { get; set; }
            public decimal MaxScore { get; set; }
            public double Percentage { get; set; }
            public DateTime SubmittedAt { get; set; }
            public List<QuestionResultDto> QuestionResults { get; set; } = new();
        }

        // Individual Question Result DTO
        public class QuestionResultDto
        {
            public long QuestionId { get; set; }
            public string QuestionBody { get; set; } = string.Empty;
            public decimal Points { get; set; }
            public decimal EarnedPoints { get; set; }
            public bool IsCorrect { get; set; }
            public List<long> CorrectChoiceIds { get; set; } = new();
            public List<long> SelectedChoiceIds { get; set; } = new();
        }

        // Attempt List DTO
        public class AttemptListDto
        {
            public long AttemptId { get; set; }
            public long QuizId { get; set; }
            public string QuizTitle { get; set; } = string.Empty;
            public string StudentName { get; set; } = string.Empty;
            public string StudentEmail { get; set; } = string.Empty;
            public decimal Score { get; set; }
            public DateTime SubmittedAt { get; set; }
        }

        // Attempt Detail DTO
        public class AttemptDetailDto
        {
            public long AttemptId { get; set; }
            public long UserId { get; set; }
            public string UserName { get; set; } = string.Empty;
            public long QuizId { get; set; }
            public string QuizTitle { get; set; } = string.Empty;
            public decimal Score { get; set; }
            public DateTime SubmittedAt { get; set; }
            public List<AttemptAnswerDetailDto> Answers { get; set; } = new();
        }

        // Answer Detail DTO
        public class AttemptAnswerDetailDto
        {
            public long QuestionId { get; set; }
            public string QuestionBody { get; set; } = string.Empty;
            public long? ChoiceId { get; set; }
            public string? ChoiceText { get; set; }
            public bool IsCorrect { get; set; }
            public decimal PointsEarned { get; set; }
        }
    }
}