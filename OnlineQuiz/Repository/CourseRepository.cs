using Microsoft.EntityFrameworkCore;
using AutoMapper;
using OnlineQuiz.Data;
using OnlineQuiz.DTOs;
using OnlineQuiz.IRepository;
using OnlineQuiz.Models;
using OnlineQuiz.Models.Response;

namespace OnlineQuiz.Repository
{
    public class CourseRepository : ICourseRepository
    {
        private readonly OnlineQuizDbContext _context;
        private readonly IMapper _mapper;

        public CourseRepository(OnlineQuizDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<ServiceResponse<IEnumerable<CourseDTO.CourseDto>>> GetAllCoursesAsync()
        {
            return await GetAllCoursesAsync(null, null);
        }

        // Existing method with optional parameters
        public async Task<ServiceResponse<IEnumerable<CourseDTO.CourseDto>>> GetAllCoursesAsync(
            long? instructorId = null, string? department = null)
        {
            var response = new ServiceResponse<IEnumerable<CourseDTO.CourseDto>>();

            try
            {
                var query = _context.Courses
                    .Include(c => c.Instructor)
                        .ThenInclude(t => t.User)
                    .Include(c => c.Enrollments)
                        .ThenInclude(e => e.User)
                    .Include(c => c.Quizzes)
                    .AsQueryable();

                if (instructorId.HasValue)
                    query = query.Where(c => c.InstructorUserId == instructorId.Value);

                if (!string.IsNullOrWhiteSpace(department))
                    query = query.Where(c => c.Department.ToLower() == department.ToLower());

                var courses = await query.ToListAsync();

                response.Data = _mapper.Map<IEnumerable<CourseDTO.CourseDto>>(courses);
                response.Message = "Courses retrieved successfully.";
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = $"Error retrieving courses: {ex.Message}";
            }

            return response;
        }

        public async Task<ServiceResponse<CourseDTO.CourseDto>> GetCourseByIdAsync(long id)
        {
            var response = new ServiceResponse<CourseDTO.CourseDto>();

            try
            {
                var course = await _context.Courses
                    .Include(c => c.Instructor)
                        .ThenInclude(t => t.User)
                    .Include(c => c.Enrollments)
                        .ThenInclude(e => e.User)
                    .Include(c => c.Quizzes)
                    .FirstOrDefaultAsync(c => c.CourseId == id);

                if (course == null)
                {
                    response.Success = false;
                    response.Message = "Course not found.";
                    return response;
                }

                response.Data = _mapper.Map<CourseDTO.CourseDto>(course);
                response.Message = "Course retrieved successfully.";
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = $"Error retrieving course: {ex.Message}";
            }

            return response;
        }

        public async Task<ServiceResponse<CourseDTO.CourseDto>> CreateCourseAsync(CourseDTO.CreateCourseDto dto)
        {
            var response = new ServiceResponse<CourseDTO.CourseDto>();

            try
            {
                var model = _mapper.Map<CourseModel>(dto);
                _context.Courses.Add(model);
                await _context.SaveChangesAsync();

                response.Data = _mapper.Map<CourseDTO.CourseDto>(model);
                response.Message = "Course created successfully.";
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = $"Error creating course: {ex.Message}";
            }

            return response;
        }

        public async Task<ServiceResponse<CourseDTO.CourseDto>> UpdateCourseAsync(long id, CourseDTO.UpdateCourseDto dto)
        {
            var response = new ServiceResponse<CourseDTO.CourseDto>();

            try
            {
                var course = await _context.Courses.FindAsync(id);

                if (course == null)
                {
                    response.Success = false;
                    response.Message = "Course not found.";
                    return response;
                }

                if (!string.IsNullOrWhiteSpace(dto.Code)) course.Code = dto.Code;
                if (!string.IsNullOrWhiteSpace(dto.Name)) course.Name = dto.Name;
                if (dto.InstructorUserId.HasValue) course.InstructorUserId = dto.InstructorUserId.Value;

                await _context.SaveChangesAsync();

                response.Data = _mapper.Map<CourseDTO.CourseDto>(course);
                response.Message = "Course updated successfully.";
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = $"Error updating course: {ex.Message}";
            }

            return response;
        }

        public async Task<ServiceResponse<bool>> DeleteCourseAsync(long id)
        {
            var response = new ServiceResponse<bool>();

            try
            {
                var course = await _context.Courses.FindAsync(id);

                if (course == null)
                {
                    response.Success = false;
                    response.Message = "Course not found.";
                    return response;
                }

                _context.Courses.Remove(course);
                await _context.SaveChangesAsync();

                response.Data = true;
                response.Message = "Course deleted successfully.";
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = $"Error deleting course: {ex.Message}";
            }

            return response;
        }

        public async Task<ServiceResponse<IEnumerable<UserDto>>> GetEnrolledStudentsAsync(long courseId)
        {
            var response = new ServiceResponse<IEnumerable<UserDto>>();

            try
            {
                var enrolledUsers = await _context.Enrollments
                    .Where(e => e.CourseId == courseId)
                    .Include(e => e.User)
                    .Select(e => new UserDto
                    {
                        UserId = e.User.UserId,
                        FullName = e.User.FullName,
                        Email = e.User.Email,
                        Status = e.User.Status
                    })
                    .ToListAsync();

                response.Data = enrolledUsers;
                response.Message = "Enrolled students retrieved successfully.";
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = $"Error retrieving enrolled students: {ex.Message}";
            }

            return response;
        }

        public async Task<ServiceResponse<IEnumerable<UserDto>>> GetUnenrolledStudentsAsync(long courseId)
        {
            var response = new ServiceResponse<IEnumerable<UserDto>>();

            try
            {
                var enrolledIds = await _context.Enrollments
                    .Where(e => e.CourseId == courseId)
                    .Select(e => e.UserId)
                    .ToListAsync();

                var unenrolledUsers = await _context.Users
                    .Where(u => !enrolledIds.Contains(u.UserId))
                    .Select(u => new UserDto
                    {
                        UserId = u.UserId,
                        FullName = u.FullName,
                        Email = u.Email,
                        Status = u.Status
                    })
                    .ToListAsync();

                response.Data = unenrolledUsers;
                response.Message = "Unenrolled students retrieved successfully.";
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = $"Error retrieving unenrolled students: {ex.Message}";
            }

            return response;
        }

        public async Task<ServiceResponse<TeacherDto>> GetAssignedTeacherAsync(long courseId)
        {
            var response = new ServiceResponse<TeacherDto>();

            try
            {
                var teacher = await _context.Courses
                    .Include(c => c.Instructor)
                    .ThenInclude(t => t.User)
                    .Where(c => c.CourseId == courseId)
                    .Select(c => new TeacherDto
                    {
                        UserId = c.Instructor.UserId,
                        Department = c.Instructor.Department
                    })
                    .FirstOrDefaultAsync();

                if (teacher == null)
                {
                    response.Success = false;
                    response.Message = "Assigned teacher not found.";
                    return response;
                }

                response.Data = teacher;
                response.Message = "Assigned teacher retrieved successfully.";
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = $"Error retrieving assigned teacher: {ex.Message}";
            }

            return response;
        }

        public async Task<ServiceResponse<bool>> EnrollStudentAsync(long courseId, long userId)
        {
            var response = new ServiceResponse<bool>();

            try
            {
                var course = await _context.Courses
                    .Include(c => c.Enrollments)
                    .FirstOrDefaultAsync(c => c.CourseId == courseId);

                if (course == null)
                {
                    response.Success = false;
                    response.Message = "Course not found.";
                    return response;
                }

                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    response.Success = false;
                    response.Message = "User not found.";
                    return response;
                }

                if (course.Enrollments.Any(e => e.UserId == userId))
                {
                    response.Success = false;
                    response.Message = "User is already enrolled in this course.";
                    return response;
                }

                _context.Enrollments.Add(new EnrollmentModel
                {
                    CourseId = courseId,
                    UserId = userId
                });

                await _context.SaveChangesAsync();

                response.Data = true;
                response.Message = "User enrolled successfully.";
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = $"Error enrolling user: {ex.Message}";
            }

            return response;
        }

        public async Task<ServiceResponse<bool>> UnenrollStudentAsync(long courseId, long userId)
        {
            var response = new ServiceResponse<bool>();

            try
            {
                var enrollment = await _context.Enrollments
                    .FirstOrDefaultAsync(e => e.CourseId == courseId && e.UserId == userId);

                if (enrollment == null)
                {
                    response.Success = false;
                    response.Message = "Enrollment not found.";
                    return response;
                }

                _context.Enrollments.Remove(enrollment);
                await _context.SaveChangesAsync();

                response.Data = true;
                response.Message = "User unenrolled successfully.";
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = $"Error unenrolling user: {ex.Message}";
            }

            return response;
        }
        public async Task<ServiceResponse<IEnumerable<CourseDTO.CourseDto>>> GetCoursesByStudentAsync(long studentId)
        {
            var response = new ServiceResponse<IEnumerable<CourseDTO.CourseDto>>();
            try
            {
                var courses = await _context.Enrollments
                    .Where(e => e.UserId == studentId)
                    .Include(e => e.Course)
                        .ThenInclude(c => c.Instructor)
                            .ThenInclude(t => t.User)
                    .Select(e => e.Course)
                    .ToListAsync();

                response.Data = _mapper.Map<IEnumerable<CourseDTO.CourseDto>>(courses);
                response.Message = "Courses enrolled by student retrieved successfully.";
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = $"Error retrieving student courses: {ex.Message}";
            }

            return response;
        }

        public async Task<ServiceResponse<IEnumerable<CourseDTO.CourseDto>>> GetCoursesByTeacherAsync(long teacherId)
        {
            var response = new ServiceResponse<IEnumerable<CourseDTO.CourseDto>>();
            try
            {
                var courses = await _context.Courses
                    .Where(c => c.InstructorUserId == teacherId)
                    .Include(c => c.Enrollments)
                        .ThenInclude(e => e.User)
                    .Include(c => c.Quizzes)
                    .ToListAsync();

                response.Data = _mapper.Map<IEnumerable<CourseDTO.CourseDto>>(courses);
                response.Message = "Courses taught by teacher retrieved successfully.";
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = $"Error retrieving teacher courses: {ex.Message}";
            }

            return response;
        }
    }
}
