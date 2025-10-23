using OnlineQuiz.DTOs;
using OnlineQuiz.IServices;
using OnlineQuiz.IRepository;
using OnlineQuiz.Models.Response;
using AutoMapper;

namespace OnlineQuiz.Services
{
    public class CourseService : ICourseService
    {
        private readonly ICourseRepository _repo;
        private readonly IMapper? _mapper;

        public CourseService(ICourseRepository repo, IMapper? mapper = null)
        {
            _repo = repo;
            _mapper = mapper;
        }

        //Overload used by tests
        public async Task<ServiceResponse<IEnumerable<CourseDTO.CourseDto>>> GetAllCoursesAsync()
        {
            // Explicitly call the repo with nulls — no optional args
            var result = await _repo.GetAllCoursesAsync(null, null);

            if (_mapper != null)
            {
                return new ServiceResponse<IEnumerable<CourseDTO.CourseDto>>
                {
                    Success = result.Success,
                    Message = result.Message,
                    Data = _mapper.Map<IEnumerable<CourseDTO.CourseDto>>(result.Data)
                };
            }

            return result;
        }
        public async Task<ServiceResponse<IEnumerable<CourseDTO.CourseDto>>> GetAllCoursesAsync(long? instructorId, string? department)
        {
            var result = await _repo.GetAllCoursesAsync(instructorId, department);

            if (_mapper != null)
            {
                return new ServiceResponse<IEnumerable<CourseDTO.CourseDto>>
                {
                    Success = result.Success,
                    Message = result.Message,
                    Data = _mapper.Map<IEnumerable<CourseDTO.CourseDto>>(result.Data)
                };
            }

            return result;
        }

        public async Task<ServiceResponse<CourseDTO.CourseDto>> GetCourseByIdAsync(long id) =>
            await _repo.GetCourseByIdAsync(id);

        public async Task<ServiceResponse<CourseDTO.CourseDto>> CreateCourseAsync(CourseDTO.CreateCourseDto dto) =>
            await _repo.CreateCourseAsync(dto);

        public async Task<ServiceResponse<CourseDTO.CourseDto>> UpdateCourseAsync(long id, CourseDTO.UpdateCourseDto dto) =>
            await _repo.UpdateCourseAsync(id, dto);

        public async Task<ServiceResponse<bool>> DeleteCourseAsync(long id) =>
            await _repo.DeleteCourseAsync(id);

        public async Task<ServiceResponse<IEnumerable<UserDto>>> GetEnrolledStudentsAsync(long courseId)
        {
            var result = await _repo.GetEnrolledStudentsAsync(courseId);
            return new ServiceResponse<IEnumerable<UserDto>>
            {
                Success = result.Success,
                Message = result.Message,
                Data = _mapper?.Map<IEnumerable<UserDto>>(result.Data)
            };
        }

        public async Task<ServiceResponse<IEnumerable<UserDto>>> GetUnenrolledStudentsAsync(long courseId)
        {
            var result = await _repo.GetUnenrolledStudentsAsync(courseId);
            return new ServiceResponse<IEnumerable<UserDto>>
            {
                Success = result.Success,
                Message = result.Message,
                Data = _mapper?.Map<IEnumerable<UserDto>>(result.Data)
            };
        }

        public async Task<ServiceResponse<TeacherDto>> GetAssignedTeacherAsync(long courseId)
        {
            var result = await _repo.GetAssignedTeacherAsync(courseId);
            return new ServiceResponse<TeacherDto>
            {
                Success = result.Success,
                Message = result.Message,
                Data = _mapper?.Map<TeacherDto>(result.Data)
            };
        }

        public Task<ServiceResponse<bool>> EnrollStudentAsync(long courseId, long userId)
            => _repo.EnrollStudentAsync(courseId, userId);

        public Task<ServiceResponse<bool>> UnenrollStudentAsync(long courseId, long userId)
            => _repo.UnenrollStudentAsync(courseId, userId);

        public async Task<ServiceResponse<IEnumerable<CourseDTO.CourseDto>>> GetCoursesByStudentAsync(long studentId) =>
            await _repo.GetCoursesByStudentAsync(studentId);

        public async Task<ServiceResponse<IEnumerable<CourseDTO.CourseDto>>> GetCoursesByTeacherAsync(long teacherId) =>
            await _repo.GetCoursesByTeacherAsync(teacherId);
    }
}