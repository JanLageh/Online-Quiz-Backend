using OnlineQuiz.DTOs;
using OnlineQuiz.Models;
using OnlineQuiz.Models.Response;

namespace OnlineQuiz.IRepository
{
    public interface ICourseRepository
    {
        // Overload without optional parameters — for tests and expression tree safety
        Task<ServiceResponse<IEnumerable<CourseDTO.CourseDto>>> GetAllCoursesAsync();

        // Main version — used by real code
        Task<ServiceResponse<IEnumerable<CourseDTO.CourseDto>>> GetAllCoursesAsync(long? instructorId, string? department);

        Task<ServiceResponse<CourseDTO.CourseDto>> GetCourseByIdAsync(long id);
        Task<ServiceResponse<CourseDTO.CourseDto>> CreateCourseAsync(CourseDTO.CreateCourseDto dto);
        Task<ServiceResponse<CourseDTO.CourseDto>> UpdateCourseAsync(long id, CourseDTO.UpdateCourseDto dto);
        Task<ServiceResponse<bool>> DeleteCourseAsync(long id);
        Task<ServiceResponse<IEnumerable<UserDto>>> GetEnrolledStudentsAsync(long courseId);
        Task<ServiceResponse<IEnumerable<UserDto>>> GetUnenrolledStudentsAsync(long courseId);
        Task<ServiceResponse<TeacherDto>> GetAssignedTeacherAsync(long courseId);
        Task<ServiceResponse<bool>> EnrollStudentAsync(long courseId, long userId);
        Task<ServiceResponse<bool>> UnenrollStudentAsync(long courseId, long userId);
        Task<ServiceResponse<IEnumerable<CourseDTO.CourseDto>>> GetCoursesByStudentAsync(long studentId);
        Task<ServiceResponse<IEnumerable<CourseDTO.CourseDto>>> GetCoursesByTeacherAsync(long teacherId);
    }
}
