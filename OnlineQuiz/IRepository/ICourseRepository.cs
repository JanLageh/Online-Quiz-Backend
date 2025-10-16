using OnlineQuiz.DTOs;
using OnlineQuiz.Models.Response;

namespace OnlineQuiz.IRepository
{
    public interface ICourseRepository
    {
        Task<ServiceResponse<IEnumerable<CourseDTO.CourseDto>>> GetAllCoursesAsync(long? instructorId = null, string? department = null);
        Task<ServiceResponse<CourseDTO.CourseDto>> GetCourseByIdAsync(long id);
        Task<ServiceResponse<CourseDTO.CourseDto>> CreateCourseAsync(CourseDTO.CreateCourseDto dto);
        Task<ServiceResponse<CourseDTO.CourseDto>> UpdateCourseAsync(long id, CourseDTO.UpdateCourseDto dto);
        Task<ServiceResponse<bool>> DeleteCourseAsync(long id);
        Task<ServiceResponse<bool>> EnrollStudentAsync(long courseId, long userId);
    }
}
