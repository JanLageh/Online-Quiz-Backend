using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OnlineQuiz.DTOs;
using OnlineQuiz.IServices;

namespace OnlineQuiz.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class CourseController : ControllerBase
    {
        private readonly ICourseService _service;

        public CourseController(ICourseService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] long? instructorId, [FromQuery] string? department)
        {
            // If no filters provided, call the parameterless method
            if (!instructorId.HasValue && string.IsNullOrEmpty(department))
            {
                var result = await _service.GetAllCoursesAsync();
                return Ok(result);
            }

            // Otherwise call the filtered version
            var filteredResult = await _service.GetAllCoursesAsync(instructorId, department);
            return Ok(filteredResult);
        }

        [HttpGet("{id:long}")]
        public async Task<IActionResult> GetById([FromRoute] long id) =>
            Ok(await _service.GetCourseByIdAsync(id));

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CourseDTO.CreateCourseDto dto) =>
            Ok(await _service.CreateCourseAsync(dto));

        [HttpPut("{id:long}")]
        public async Task<IActionResult> Update([FromRoute] long id, [FromBody] CourseDTO.UpdateCourseDto dto) =>
            Ok(await _service.UpdateCourseAsync(id, dto));

        [HttpDelete("{id:long}")]
        public async Task<IActionResult> Delete([FromRoute] long id) =>
            Ok(await _service.DeleteCourseAsync(id));

        [HttpGet("{courseId:long}/students")]
        public async Task<IActionResult> GetEnrolledStudents(long courseId)
        {
            var response = await _service.GetEnrolledStudentsAsync(courseId);
            return Ok(response);
        }

        [HttpGet("{courseId:long}/unenrolled-students")]
        public async Task<IActionResult> GetUnenrolledStudents(long courseId)
        {
            var response = await _service.GetUnenrolledStudentsAsync(courseId);
            return Ok(response);
        }

        [HttpGet("{courseId:long}/teacher")]
        public async Task<IActionResult> GetAssignedTeacher(long courseId)
        {
            var response = await _service.GetAssignedTeacherAsync(courseId);
            return Ok(response);
        }

        [HttpPost("{courseId:long}/enroll/{userId:long}")]
        public async Task<IActionResult> EnrollStudent(long courseId, long userId)
        {
            var response = await _service.EnrollStudentAsync(courseId, userId);
            return Ok(response);
        }

        [HttpDelete("{courseId:long}/unenroll/{userId:long}")]
        public async Task<IActionResult> UnenrollStudent(long courseId, long userId)
        {
            var response = await _service.UnenrollStudentAsync(courseId, userId);
            return Ok(response);
        }

        [HttpGet("student/{studentId:long}/courses")]
        public async Task<IActionResult> GetCoursesByStudent(long studentId)
        {
            var response = await _service.GetCoursesByStudentAsync(studentId);
            return Ok(response);
        }

        [HttpGet("teacher/{teacherId:long}/courses")]
        public async Task<IActionResult> GetCoursesByTeacher(long teacherId)
        {
            var response = await _service.GetCoursesByTeacherAsync(teacherId);
            return Ok(response);
        }
    }
}