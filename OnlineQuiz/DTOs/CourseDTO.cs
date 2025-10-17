using OnlineQuiz.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OnlineQuiz.DTOs
{
    public class CourseDTO
    {
        [Required]
        [StringLength(20)]
        public string Code { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        public string Department { get; set; } = string.Empty;



        [Required]
        [Column("Instructor_UserId")]
        public long InstructorUserId { get; set; }

        [ForeignKey("InstructorUserId")]
        public virtual TeacherModel Instructor { get; set; } = null!;

        public int EnrollmentCount { get; set; }
        public class CreateCourseDto
        {
            [Required]
            [StringLength(20)]
            public string Code { get; set; } = string.Empty;

            [Required]
            [StringLength(200)]
            public string Name { get; set; } = string.Empty;

            [Required]
            [StringLength(100)]
            public string Department { get; set; } = string.Empty;
            [Required]
            public long InstructorUserId { get; set; }
            [Required]
            public string Status { get; set; } = "Active";
            [Required]
            public string Section { get; set; }
            [Required]
            public int CreatedBy { get; set; }
        }

        public class UpdateCourseDto
        {
            [StringLength(20)]
            public string? Code { get; set; }

            [StringLength(200)]
            public string? Name { get; set; }

            [StringLength(100)]
            public string? Department { get; set; }

            public long? InstructorUserId { get; set; }
        }

        public class CourseDto
        {
            public long CourseId { get; set; }
            public string Code { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
            public string Department { get; set; }
            public long InstructorUserId { get; set; }
            public string? InstructorName { get; set; }
            public string Status { get; set; } = string.Empty;
            public string? Section { get; set; }
            public DateTime CreatedAt { get; set; }
            public DateTime UpdatedAt { get; set; }
            public long CreatedBy { get; set; }

            public int EnrollmentCount { get; set; }
            public int QuizCount { get; set; }

            public List<EnrolledStudentDto> EnrolledStudents { get; set; } = new();
        }

        public class EnrolledStudentDto
        {
            public long StudentId { get; set; }
            public string FullName { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public object UserId { get; internal set; }
        }
    }
}
