using AutoMapper;
using Microsoft.EntityFrameworkCore;
using OnlineQuiz.Data;
using OnlineQuiz.DTOs;
using OnlineQuiz.IRepository;
using OnlineQuiz.Models;
using OnlineQuiz.Models.Response;

namespace OnlineQuiz.Repository
{
    public class QuizRepository : IQuizRepository
    {
        private readonly OnlineQuizDbContext _context;
        private readonly IMapper _mapper;

        public QuizRepository(OnlineQuizDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        //GET /api/quizzes — filter + pagination + stats
        public async Task<ServiceResponse<QuizDTO.PagedResult<QuizDTO.QuizListItemDto>>> GetPagedQuizzesAsync(
            int page = 1, int pageSize = 10, long? courseId = null, long? teacherId = null)
        {
            var response = new ServiceResponse<QuizDTO.PagedResult<QuizDTO.QuizListItemDto>>();

            try
            {
                var query = _context.Quizzes
                    .Include(q => q.Course).ThenInclude(c => c.Instructor).ThenInclude(t => t.User)
                    .Include(q => q.Attempts)
                    .AsQueryable();

                if (courseId.HasValue)
                    query = query.Where(q => q.CourseId == courseId.Value);

                if (teacherId.HasValue)
                    query = query.Where(q => q.Course.InstructorUserId == teacherId.Value);

                var totalItems = await query.CountAsync();

                var quizzes = await query
                    .OrderByDescending(q => q.QuizId)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var quizDtos = quizzes.Select(q => new QuizDTO.QuizListItemDto
                {
                    QuizId = q.QuizId,
                    Title = q.Title,
                    CourseName = q.Course.Name,
                    TeacherName = q.Course.Instructor.User.FullName,
                    TotalAttempts = q.Attempts.Count,
                    AverageScore = q.Attempts.Any() ? (double)q.Attempts.Average(a => a.Score) : 0,
                    CreatedAt = DateTime.UtcNow
                }).ToList();

                var paged = new QuizDTO.PagedResult<QuizDTO.QuizListItemDto>
                {
                    CurrentPage = page,
                    PageSize = pageSize,
                    TotalItems = totalItems,
                    TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize),
                    Items = quizDtos
                };

                response.Data = paged;
                response.Message = "Quizzes retrieved successfully.";
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = $"Error retrieving quizzes: {ex.Message}";
            }

            return response;
        }

        //GET /api/quizzes/{id}
        public async Task<ServiceResponse<QuizDTO.QuizDetailDto>> GetQuizByIdAsync(long quizId)
        {
            var response = new ServiceResponse<QuizDTO.QuizDetailDto>();

            var quiz = await _context.Quizzes
                .Include(q => q.Course).ThenInclude(c => c.Instructor).ThenInclude(t => t.User)
                .Include(q => q.Questions).ThenInclude(qn => qn.Choices)
                .Include(q => q.Attempts)
                .FirstOrDefaultAsync(q => q.QuizId == quizId);

            if (quiz == null)
            {
                response.Success = false;
                response.Message = "Quiz not found.";
                return response;
            }

            var dto = _mapper.Map<QuizDTO.QuizDetailDto>(quiz);
            dto.CourseName = quiz.Course.Name;
            dto.TeacherName = quiz.Course.Instructor.User.FullName;
            dto.TotalAttempts = quiz.Attempts.Count;
            dto.AverageScore = quiz.Attempts.Any() ? (double)quiz.Attempts.Average(a => a.Score) : 0;

            response.Data = dto;
            response.Message = "Quiz details retrieved successfully.";
            return response;
        }

        //POST /api/quizzes
        public async Task<ServiceResponse<QuizDTO.QuizListItemDto>> CreateQuizAsync(QuizDTO.CreateQuizDto dto, long createdByUserId)
        {
            var response = new ServiceResponse<QuizDTO.QuizListItemDto>();

            try
            {
                var course = await _context.Courses.Include(c => c.Instructor)
                    .FirstOrDefaultAsync(c => c.CourseId == dto.CourseId);

                if (course == null)
                {
                    response.Success = false;
                    response.Message = "Course not found.";
                    return response;
                }

                if (course.InstructorUserId != createdByUserId)
                {
                    response.Success = false;
                    response.Message = "Only the course instructor or admin can create a quiz.";
                    return response;
                }

                if (await _context.Quizzes.AnyAsync(q => q.Title == dto.Title && q.CourseId == dto.CourseId))
                {
                    response.Success = false;
                    response.Message = "A quiz with this title already exists in this course.";
                    return response;
                }

                var quiz = _mapper.Map<QuizModel>(dto);
                _context.Quizzes.Add(quiz);
                await _context.SaveChangesAsync();

                response.Data = _mapper.Map<QuizDTO.QuizListItemDto>(quiz);
                response.Message = "Quiz created successfully.";
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = $"Error creating quiz: {ex.Message}";
            }

            return response;
        }

        //PUT /api/quizzes/{id}
        public async Task<ServiceResponse<QuizDTO.QuizListItemDto>> UpdateQuizAsync(long quizId, QuizDTO.UpdateQuizDto dto, long updatedByUserId)
        {
            var response = new ServiceResponse<QuizDTO.QuizListItemDto>();
            var quiz = await _context.Quizzes.Include(q => q.Course).FirstOrDefaultAsync(q => q.QuizId == quizId);

            if (quiz == null)
            {
                response.Success = false;
                response.Message = "Quiz not found.";
                return response;
            }

            if (quiz.Course.InstructorUserId != updatedByUserId)
            {
                response.Success = false;
                response.Message = "You are not allowed to update this quiz.";
                return response;
            }

            quiz.Title = dto.Title ?? quiz.Title;

            await _context.SaveChangesAsync();
            response.Data = _mapper.Map<QuizDTO.QuizListItemDto>(quiz);
            response.Message = "Quiz updated successfully.";
            return response;
        }

        //DELETE /api/quizzes/{id}
        public async Task<ServiceResponse<bool>> DeleteQuizAsync(long quizId, long requestedByUserId)
        {
            var response = new ServiceResponse<bool>();
            var quiz = await _context.Quizzes.Include(q => q.Course).FirstOrDefaultAsync(q => q.QuizId == quizId);

            if (quiz == null)
            {
                response.Success = false;
                response.Message = "Quiz not found.";
                return response;
            }

            if (quiz.Course.InstructorUserId != requestedByUserId)
            {
                response.Success = false;
                response.Message = "You are not authorized to delete this quiz.";
                return response;
            }

            _context.Quizzes.Remove(quiz);
            await _context.SaveChangesAsync();

            response.Data = true;
            response.Message = "Quiz deleted successfully.";
            return response;
        }

        //POST /api/quizzes/{id}/publish
        public async Task<ServiceResponse<bool>> PublishQuizAsync(long quizId, long requestedByUserId)
        {
            var response = new ServiceResponse<bool>();

            var quiz = await _context.Quizzes
                .Include(q => q.Questions)
                .Include(q => q.Course)
                .FirstOrDefaultAsync(q => q.QuizId == quizId);

            if (quiz == null)
            {
                response.Success = false;
                response.Message = "Quiz not found.";
                return response;
            }

            if (quiz.Course.InstructorUserId != requestedByUserId)
            {
                response.Success = false;
                response.Message = "You are not authorized to publish this quiz.";
                return response;
            }

            if (!quiz.Questions.Any())
            {
                response.Success = false;
                response.Message = "Cannot publish a quiz without questions.";
                return response;
            }

            //Simulate publish without model fields
            Console.WriteLine($"[Notify] Quiz '{quiz.Title}' published for course '{quiz.Course.Name}'.");

            response.Data = true;
            response.Message = "Quiz published successfully.";
            return response;
        }
    }
}
