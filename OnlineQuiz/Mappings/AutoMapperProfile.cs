using AutoMapper;
using OnlineQuiz.DTOs;
using OnlineQuiz.Models;
using static OnlineQuiz.DTOs.ImportExportDtos;
using static OnlineQuiz.DTOs.AttemptDtos;

namespace OnlineQuiz.Mappings
{
    public class AutoMapperProfile : Profile
    {
        public AutoMapperProfile()
        {
            //mapping for UserModel to UserDto
            CreateMap<UserModel, UserDto>()
                .ForMember(dest => dest.Roles, opt => opt.MapFrom(src => src.UserRoles.Select(ur => ur.Role.Name)));

            CreateMap<CreateUserDto, UserModel>()
                .ForMember(dest => dest.UserId, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(_ => DateTime.UtcNow))
                .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(_ => DateTime.UtcNow))
                .ForMember(dest => dest.PasswordHash, opt => opt.Ignore())
                .ForMember(dest => dest.Status, opt => opt.MapFrom(_ => "Active"));

            CreateMap<UpdateUserDto, UserModel>()
                .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(_ => DateTime.UtcNow))
                .ForAllMembers(opt => opt.Condition((src, dest, srcMember) => srcMember != null));

            //mapping for EnrollmentModel to UserDto
            CreateMap<EnrollmentModel, UserDto>()
                .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.User.UserId))
                .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => src.User.FullName))
                .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.User.Email))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.User.Status));

            CreateMap<UserModel, CourseDTO.EnrolledStudentDto>()
                .ForMember(dest => dest.StudentId, opt => opt.MapFrom(src => src.UserId))
                .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.UserId))
                .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => src.FullName))
                .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email));


            //mapping for CourseModel to CourseDTO.CourseDto
            CreateMap<CourseModel, CourseDTO.CourseDto>()
                .ForMember(dest => dest.InstructorName, opt => opt.MapFrom(src =>
                    src.Instructor != null && src.Instructor.User != null
                        ? src.Instructor.User.FullName
                        : "N/A"))
                .ForMember(dest => dest.EnrolledStudents, opt => opt.MapFrom(src =>
                    src.Enrollments.Select(e => e.User)))
                .ForMember(dest => dest.EnrollmentCount, opt => opt.MapFrom(src => src.Enrollments.Count))
                .ForMember(dest => dest.QuizCount, opt => opt.MapFrom(src => src.Quizzes.Count))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status))
                .ForMember(dest => dest.Department, opt => opt.MapFrom(src => src.Department))
                .ForMember(dest => dest.Section, opt => opt.MapFrom(src => src.Section))
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CreatedAt))
                .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => src.UpdatedAt))
                .ForMember(dest => dest.CreatedBy, opt => opt.MapFrom(src => src.CreatedBy));

            CreateMap<CourseDTO.CreateCourseDto, CourseModel>()
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(_ => DateTime.UtcNow))
                .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(_ => DateTime.UtcNow))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status ?? "Active"))
                .ForMember(dest => dest.Department, opt => opt.MapFrom(src => src.Department))
                .ForMember(dest => dest.Section, opt => opt.MapFrom(src => src.Section))
                .ForMember(dest => dest.CreatedBy, opt => opt.MapFrom(src => src.CreatedBy));

            CreateMap<CourseDTO.UpdateCourseDto, CourseModel>()
                .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(_ => DateTime.UtcNow))
                .ForAllMembers(opt => opt.Condition((src, dest, srcMember) => srcMember != null));

            //mapping for QuizModel to QuizDTO
            CreateMap<QuizModel, QuizDTO.QuizListItemDto>()
                .ForMember(dest => dest.CourseName, opt => opt.MapFrom(src => src.Course.Name))
                .ForMember(dest => dest.TeacherName, opt => opt.MapFrom(src => src.Course.Instructor.User.FullName))
                .ForMember(dest => dest.TotalAttempts, opt => opt.MapFrom(src => src.Attempts.Count))
                .ForMember(dest => dest.AverageScore, opt => opt.MapFrom(src =>
                    src.Attempts.Any() ? (double)src.Attempts.Average(a => a.Score) : 0));

            CreateMap<QuizModel, QuizDTO.QuizDetailDto>()
                .ForMember(dest => dest.CourseName, opt => opt.MapFrom(src => src.Course.Name))
                .ForMember(dest => dest.TeacherName, opt => opt.MapFrom(src => src.Course.Instructor.User.FullName))
                .ForMember(dest => dest.Questions, opt => opt.MapFrom(src => src.Questions))
                .ForMember(dest => dest.TotalAttempts, opt => opt.MapFrom(src => src.Attempts.Count))
                .ForMember(dest => dest.AverageScore, opt => opt.MapFrom(src =>
                    src.Attempts.Any() ? (double)src.Attempts.Average(a => a.Score) : 0));

            CreateMap<QuizDTO.CreateQuizDto, QuizModel>()
                .ForMember(dest => dest.Title, opt => opt.MapFrom(src => src.Title))
                .ForMember(dest => dest.CourseId, opt => opt.MapFrom(src => src.CourseId));

            CreateMap<QuizDTO.UpdateQuizDto, QuizModel>()
                .ForAllMembers(opt => opt.Condition((src, dest, srcMember) => srcMember != null));

            CreateMap<QuestionModel, QuizDTO.QuestionDto>()
                .ForMember(dest => dest.Choices, opt => opt.MapFrom(src => src.Choices));

            //mapping for ChoiceModel
            CreateMap<ChoiceModel, QuizDTO.ChoiceDto>()
                .ForMember(dest => dest.Text, opt => opt.MapFrom(src => src.Body));

            CreateMap<QuizDTO.ChoiceDto, ChoiceModel>()
                .ForMember(dest => dest.Body, opt => opt.MapFrom(src => src.Text));

            CreateMap<QuizDTO.CreateChoiceDto, ChoiceModel>()
                .ForMember(dest => dest.Body, opt => opt.MapFrom(src => src.Text));

            // Reverse direction
            CreateMap<QuizDTO.QuestionDto, QuestionModel>()
                .ForMember(dest => dest.Choices, opt => opt.MapFrom(src => src.Choices));

            CreateMap<TeacherModel, TeacherDto>();
            CreateMap<CreateTeacherDto, TeacherModel>();

            CreateMap<StudentModel, StudentDto>();
            CreateMap<CreateStudentDto, StudentModel>();

            CreateMap<RoleModel, string>().ConvertUsing(src => src.Name);

            // ExportImportLog mappings
            CreateMap<ExportImportLogModel, ExportImportLogDto>()
                .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.User.FullName));

            CreateMap<CreateExportImportLogDto, ExportImportLogModel>()
                .ForMember(dest => dest.LogId, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(_ => DateTime.UtcNow));

            // Attempt mappings
            CreateMap<AttemptModel, AttemptListDto>()
                .ForMember(dest => dest.QuizTitle, opt => opt.MapFrom(src => src.Quiz.Title))
                .ForMember(dest => dest.StudentName, opt => opt.MapFrom(src => src.User.FullName))
                .ForMember(dest => dest.StudentEmail, opt => opt.MapFrom(src => src.User.Email))
                .ForMember(dest => dest.SubmittedAt, opt => opt.MapFrom(src => src.SubmittedAt ?? DateTime.UtcNow));

            CreateMap<AttemptModel, AttemptDetailDto>()
                .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.User.FullName))
                .ForMember(dest => dest.QuizTitle, opt => opt.MapFrom(src => src.Quiz.Title))
                .ForMember(dest => dest.SubmittedAt, opt => opt.MapFrom(src => src.SubmittedAt ?? DateTime.UtcNow))
                .ForMember(dest => dest.Answers, opt => opt.MapFrom(src => src.AttemptAnswers));

            CreateMap<AttemptAnswerModel, AttemptAnswerDetailDto>()
                .ForMember(dest => dest.QuestionBody, opt => opt.MapFrom(src => src.Question.Body))
                .ForMember(dest => dest.ChoiceText, opt => opt.MapFrom(src => src.Choice != null ? src.Choice.Body : null))
                .ForMember(dest => dest.IsCorrect, opt => opt.MapFrom(src => src.IsCorrect ?? false))
                .ForMember(dest => dest.PointsEarned, opt => opt.Ignore()); // Calculated in repository
        }
    }
}