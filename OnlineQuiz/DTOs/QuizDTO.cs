using System.ComponentModel.DataAnnotations;

namespace OnlineQuiz.DTOs
{
    public class QuizDTO
    {
        //Used for listing quizzes
        public class QuizListItemDto
        {
            public long QuizId { get; set; }
            public string Title { get; set; } = string.Empty;
            public string CourseName { get; set; } = string.Empty;
            public string TeacherName { get; set; } = string.Empty;
            public int TotalAttempts { get; set; }
            public double AverageScore { get; set; }
            public DateTime CreatedAt { get; set; }
        }

        //Used for showing detailed quiz info
        public class QuizDetailDto
        {
            public long QuizId { get; set; }
            public string Title { get; set; } = string.Empty;
            public string CourseName { get; set; } = string.Empty;
            public string TeacherName { get; set; } = string.Empty;
            public int TotalAttempts { get; set; }
            public double AverageScore { get; set; }
            public int TimeLimit { get; set; }
            public bool IsPublished { get; set; }
            public DateTime? AvailableFrom { get; set; }
            public DateTime? AvailableUntil { get; set; }

            public List<QuestionDto> Questions { get; set; } = new();
        }

        //Used when creating a new quiz
        public class CreateQuizDto
        {
            [Required]
            public string Title { get; set; } = string.Empty;

            [Required]
            public long CourseId { get; set; }

            public int? TimeLimit { get; set; }
        }

        //Used when updating an existing quiz
        public class UpdateQuizDto
        {
            public string? Title { get; set; }
            public int? TimeLimit { get; set; }
        }

        //Nested DTO for questions and choices
        public class QuestionDto
        {
            public long QuestionId { get; set; }
            public string Text { get; set; } = string.Empty;
            public List<ChoiceDto> Choices { get; set; } = new();
        }

        public class ChoiceDto
        {
            public long ChoiceId { get; set; }
            public string Text { get; set; } = string.Empty;
            public bool IsCorrect { get; set; }
        }

        // Used when creating a question under a quiz
        public class CreateQuestionDto
        {
            public long QuizId { get; set; }
            public string Type { get; set; } = "Single"; // Single, Multiple, Text
            public string Body { get; set; } = string.Empty;
            public string Text { get => Body; set => Body = value; }
            public decimal Points { get; set; } = 1;
            public int SortOrder { get; set; } = 1;

            // optional: allow creating choices when adding question
            public List<CreateChoiceDto> Choices { get; set; } = new();
        }

        // Used when updating a question
        public class UpdateQuestionDto
        {
            public string? Type { get; set; }
            public string? Body { get; set; }
            public decimal? Points { get; set; }
            public int? SortOrder { get; set; }

            // optional: provide choices to replace existing ones
            public List<CreateChoiceDto>? Choices { get; set; }
        }

        // Used when adding choices under a question
        public class CreateChoiceDto
        {
            [Required]
            [StringLength(300)]
            public string Text { get; set; } = string.Empty;

            [Required]
            public bool IsCorrect { get; set; }
        }

        // Used when updating a choice
        public class UpdateChoiceDto
        {
            [StringLength(300)]
            public string? Text { get; set; }

            public bool? IsCorrect { get; set; }
        }

        //Generic pagination wrapper
        public class PagedResult<T>
        {
            public int CurrentPage { get; set; }
            public int PageSize { get; set; }
            public int TotalItems { get; set; }
            public int TotalPages { get; set; }
            public IEnumerable<T> Items { get; set; } = new List<T>();
        }
    }
}