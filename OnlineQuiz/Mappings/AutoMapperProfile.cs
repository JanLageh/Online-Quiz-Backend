using AutoMapper;
using OnlineQuiz.DTOs;
using OnlineQuiz.Models;

namespace OnlineQuiz.Mappings
{
    public class AutoMapperProfile : Profile
    {
        public AutoMapperProfile()
        {

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


            CreateMap<TeacherModel, TeacherDto>();
            CreateMap<CreateTeacherDto, TeacherModel>();

            CreateMap<StudentModel, StudentDto>();
            CreateMap<CreateStudentDto, StudentModel>();


            CreateMap<RoleModel, string>().ConvertUsing(src => src.Name);
        }
    }
}
