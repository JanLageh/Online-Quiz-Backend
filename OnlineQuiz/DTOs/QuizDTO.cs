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
