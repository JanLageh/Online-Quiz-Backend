using AutoMapper;
using OnlineQuiz.DTOs;
using OnlineQuiz.Models;

namespace OnlineQuiz.Mappings
{
    public class AutoMapperProfile : Profile
    {
        public AutoMapperProfile()
        {
            // User mappings
            CreateMap<UserModel, UserDto>()
                .ForMember(dest => dest.Roles, opt => opt.MapFrom(src => src.UserRoles.Select(ur => ur.Role.Name)));
            CreateMap<CreateUserDto, UserModel>()
                .ForMember(dest => dest.UserId, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
                .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
                .ForMember(dest => dest.PasswordHash, opt => opt.Ignore())
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => "Active"));
            CreateMap<UpdateUserDto, UserModel>()
                .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
                .ForAllMembers(opt => opt.Condition((src, dest, srcMember) => srcMember != null));


            CreateMap<CourseModel, CourseDTO.CourseDto>()
        .ForMember(dest => dest.InstructorName,
            opt => opt.MapFrom(src =>
                src.Instructor != null && src.Instructor.User != null
                    ? src.Instructor.User.FullName
                    : "N/A"))
         .ForMember(dest => dest.Department,
        opt => opt.MapFrom(src => src.Department)) 
        .ForMember(dest => dest.EnrollmentCount,
            opt => opt.MapFrom(src => src.Enrollments.Count))
        .ForMember(dest => dest.QuizCount,
            opt => opt.MapFrom(src => src.Quizzes.Count))
        .ForMember(dest => dest.EnrolledStudents,
            opt => opt.MapFrom(src => src.Enrollments));

            CreateMap<CourseDTO.CreateCourseDto, CourseModel>()
                .ForMember(dest => dest.CourseId, opt => opt.Ignore())
                .ForMember(dest => dest.Instructor, opt => opt.Ignore());

            CreateMap<CourseDTO.UpdateCourseDto, CourseModel>()
                .ForMember(dest => dest.CourseId, opt => opt.Ignore())
                .ForMember(dest => dest.Instructor, opt => opt.Ignore())
                .ForAllMembers(opt =>
                    opt.Condition((src, dest, srcMember) =>
                        srcMember != null && !string.IsNullOrEmpty(srcMember?.ToString())));

            // Teacher mappings
            CreateMap<TeacherModel, TeacherDto>();
            CreateMap<CreateTeacherDto, TeacherModel>();
            
            // Student mappings
            CreateMap<StudentModel, StudentDto>();
            CreateMap<CreateStudentDto, StudentModel>();

            // Role mappings
            CreateMap<RoleModel, string>().ConvertUsing(src => src.Name);
        }
    }
}