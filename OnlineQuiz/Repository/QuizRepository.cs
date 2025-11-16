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


        //QUIZ CRUD

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
                    CreatedAt = q.CreatedAt
                }).ToList();

                response.Data = new QuizDTO.PagedResult<QuizDTO.QuizListItemDto>
                {
                    CurrentPage = page,
                    PageSize = pageSize,
                    TotalItems = totalItems,
                    TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize),
                    Items = quizDtos
                };
                response.Message = "Quizzes retrieved successfully.";
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = $"Error retrieving quizzes: {ex.Message}";
            }

            return response;
        }

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

            response.Data = _mapper.Map<QuizDTO.QuizDetailDto>(quiz);
            response.Message = "Quiz details retrieved successfully.";
            return response;
        }

        public async Task<ServiceResponse<QuizDTO.QuizListItemDto>> CreateQuizAsync(QuizDTO.CreateQuizDto dto, long createdByUserId)
        {
            var response = new ServiceResponse<QuizDTO.QuizListItemDto>();

            var course = await _context.Courses.Include(c => c.Instructor)
                .FirstOrDefaultAsync(c => c.CourseId == dto.CourseId);

            if (course == null)
            {
                response.Success = false;
                response.Message = "Course not found.";
                return response;
            }

            // Only teacher of this course or admin can create quiz
            if (course.InstructorUserId != createdByUserId &&
                !await UserIsAdmin(createdByUserId))
            {
                response.Success = false;
                response.Message = "Only the course instructor or an admin can create quizzes.";
                return response;
            }

            if (await _context.Quizzes.AnyAsync(q => q.Title == dto.Title && q.CourseId == dto.CourseId))
            {
                response.Success = false;
                response.Message = "Quiz with this title already exists.";
                return response;
            }

            var quiz = _mapper.Map<QuizModel>(dto);
            _context.Quizzes.Add(quiz);
            await _context.SaveChangesAsync();

            response.Data = _mapper.Map<QuizDTO.QuizListItemDto>(quiz);
            response.Message = "Quiz created successfully.";
            return response;
        }

        public async Task<ServiceResponse<QuizDTO.QuizListItemDto>> UpdateQuizAsync(long quizId, QuizDTO.UpdateQuizDto dto, long updatedByUserId)
        {
            var response = new ServiceResponse<QuizDTO.QuizListItemDto>();

            var quiz = await _context.Quizzes
                .Include(q => q.Course)
                .FirstOrDefaultAsync(q => q.QuizId == quizId);

            if (quiz == null)
            {
                response.Success = false;
                response.Message = "Quiz not found.";
                return response;
            }

            if (quiz.Course.InstructorUserId != updatedByUserId && !await UserIsAdmin(updatedByUserId))
            {
                response.Success = false;
                response.Message = "You are not authorized to update this quiz.";
                return response;
            }

            if (!string.IsNullOrWhiteSpace(dto.Title))
                quiz.Title = dto.Title;

            if (dto.TimeLimit.HasValue)
                quiz.TimeLimitMinutes = dto.TimeLimit.Value;

            quiz.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            response.Data = _mapper.Map<QuizDTO.QuizListItemDto>(quiz);
            response.Message = "Quiz updated successfully.";
            return response;
        }

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

            if (quiz.Course.InstructorUserId != requestedByUserId && !await UserIsAdmin(requestedByUserId))
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

            if (quiz.Course.InstructorUserId != requestedByUserId && !await UserIsAdmin(requestedByUserId))
            {
                response.Success = false;
                response.Message = "Not authorized to publish.";
                return response;
            }

            if (!quiz.Questions.Any())
            {
                response.Success = false;
                response.Message = "Cannot publish a quiz without questions.";
                return response;
            }

            quiz.IsPublished = true;
            await _context.SaveChangesAsync();

            Console.WriteLine($"[Notify] Quiz '{quiz.Title}' published for course '{quiz.Course.Name}'.");
            response.Data = true;
            response.Message = "Quiz published successfully.";
            return response;
        }

        //QUESTION MANAGEMENT 

        public async Task<ServiceResponse<QuizDTO.QuestionDto>> AddQuestionAsync(QuizDTO.CreateQuestionDto dto)
        {
            var response = new ServiceResponse<QuizDTO.QuestionDto>();

            var quiz = await _context.Quizzes.Include(q => q.Course)
                .FirstOrDefaultAsync(q => q.QuizId == dto.QuizId);

            if (quiz == null)
            {
                response.Success = false;
                response.Message = "Quiz not found.";
                return response;
            }

            var question = new QuestionModel
            {
                QuizId = dto.QuizId,
                Type = dto.Type,
                Body = dto.Body,
                SortOrder = dto.SortOrder,
                Points = dto.Points
            };

            _context.Questions.Add(question);
            await _context.SaveChangesAsync();

            if (dto.Choices != null && dto.Choices.Any())
            {
                foreach (var c in dto.Choices)
                {
                    _context.Choices.Add(new ChoiceModel
                    {
                        QuestionId = question.QuestionId,
                        Body = c.Text,
                        IsCorrect = c.IsCorrect
                    });
                }

                await _context.SaveChangesAsync();
            }

            // Reload with choices
            question = await _context.Questions
                .Include(q => q.Choices)
                .FirstOrDefaultAsync(q => q.QuestionId == question.QuestionId);

            response.Data = _mapper.Map<QuizDTO.QuestionDto>(question);
            response.Message = "Question created successfully.";
            return response;
        }

        public async Task<ServiceResponse<IEnumerable<QuizDTO.QuestionDto>>> GetQuestionsByQuizIdAsync(long quizId)
        {
            var response = new ServiceResponse<IEnumerable<QuizDTO.QuestionDto>>();
            var questions = await _context.Questions
                .Include(q => q.Choices)
                .Where(q => q.QuizId == quizId)
                .OrderBy(q => q.SortOrder)
                .ToListAsync();

            response.Data = _mapper.Map<IEnumerable<QuizDTO.QuestionDto>>(questions);
            response.Message = "Questions retrieved successfully.";
            return response;
        }

        public async Task<ServiceResponse<QuizDTO.QuestionDto>> GetQuestionByIdAsync(long questionId)
        {
            var response = new ServiceResponse<QuizDTO.QuestionDto>();

            var question = await _context.Questions
                .Include(q => q.Choices)
                .FirstOrDefaultAsync(q => q.QuestionId == questionId);

            if (question == null)
            {
                response.Success = false;
                response.Message = "Question not found.";
                return response;
            }

            response.Data = _mapper.Map<QuizDTO.QuestionDto>(question);
            response.Message = "Question retrieved successfully.";
            return response;
        }

        public async Task<ServiceResponse<QuizDTO.QuestionDto>> UpdateQuestionAsync(long questionId, QuizDTO.UpdateQuestionDto dto, long userId)
        {
            var response = new ServiceResponse<QuizDTO.QuestionDto>();

            var question = await _context.Questions
                .Include(q => q.Quiz)
                    .ThenInclude(qz => qz.Course)
                .Include(q => q.Choices)
                .FirstOrDefaultAsync(q => q.QuestionId == questionId);

            if (question == null)
            {
                response.Success = false;
                response.Message = "Question not found.";
                return response;
            }

            // Check authorization
            if (question.Quiz.Course.InstructorUserId != userId && !await UserIsAdmin(userId))
            {
                response.Success = false;
                response.Message = "You are not authorized to update this question.";
                return response;
            }

            // Check if quiz has attempts
            var hasAttempts = await _context.Attempts.AnyAsync(a => a.QuizId == question.QuizId);
            if (hasAttempts)
            {
                response.Success = false;
                response.Message = "Cannot update question. Quiz already has attempts.";
                return response;
            }

            // Update fields
            if (!string.IsNullOrWhiteSpace(dto.Type))
                question.Type = dto.Type;

            if (!string.IsNullOrWhiteSpace(dto.Body))
                question.Body = dto.Body;

            if (dto.Points.HasValue)
                question.Points = dto.Points.Value;

            if (dto.SortOrder.HasValue)
                question.SortOrder = dto.SortOrder.Value;

            // Update choices if provided
            if (dto.Choices != null)
            {
                // Remove existing choices
                _context.Choices.RemoveRange(question.Choices);

                // Add new choices
                foreach (var c in dto.Choices)
                {
                    _context.Choices.Add(new ChoiceModel
                    {
                        QuestionId = question.QuestionId,
                        Body = c.Text,
                        IsCorrect = c.IsCorrect
                    });
                }
            }

            await _context.SaveChangesAsync();

            // Reload with choices
            question = await _context.Questions
                .Include(q => q.Choices)
                .FirstOrDefaultAsync(q => q.QuestionId == questionId);

            response.Data = _mapper.Map<QuizDTO.QuestionDto>(question);
            response.Message = "Question updated successfully.";
            return response;
        }

        public async Task<ServiceResponse<bool>> DeleteQuestionAsync(long questionId, long userId)
        {
            var response = new ServiceResponse<bool>();

            var question = await _context.Questions
                .Include(q => q.Quiz)
                    .ThenInclude(qz => qz.Course)
                .FirstOrDefaultAsync(q => q.QuestionId == questionId);

            if (question == null)
            {
                response.Success = false;
                response.Message = "Question not found.";
                return response;
            }

            // Check authorization
            if (question.Quiz.Course.InstructorUserId != userId && !await UserIsAdmin(userId))
            {
                response.Success = false;
                response.Message = "You are not authorized to delete this question.";
                return response;
            }

            // Check if quiz has attempts
            var hasAttempts = await _context.Attempts.AnyAsync(a => a.QuizId == question.QuizId);
            if (hasAttempts)
            {
                response.Success = false;
                response.Message = "Cannot delete question. Quiz already has attempts.";
                return response;
            }

            _context.Questions.Remove(question);
            await _context.SaveChangesAsync();

            response.Data = true;
            response.Message = "Question deleted successfully.";
            return response;
        }

        //CHOICE MANAGEMENT

        public async Task<ServiceResponse<bool>> AddChoicesAsync(long questionId, IEnumerable<QuizDTO.CreateChoiceDto> choices)
        {
            var response = new ServiceResponse<bool>();

            var question = await _context.Questions.FindAsync(questionId);
            if (question == null)
            {
                response.Success = false;
                response.Message = "Question not found.";
                return response;
            }

            foreach (var c in choices)
            {
                _context.Choices.Add(new ChoiceModel
                {
                    QuestionId = questionId,
                    Body = c.Text,
                    IsCorrect = c.IsCorrect
                });
            }

            await _context.SaveChangesAsync();
            response.Data = true;
            response.Message = "Choices added successfully.";
            return response;
        }

        public async Task<ServiceResponse<IEnumerable<QuizDTO.ChoiceDto>>> GetChoicesByQuestionIdAsync(long questionId)
        {
            var response = new ServiceResponse<IEnumerable<QuizDTO.ChoiceDto>>();

            var choices = await _context.Choices
                .Where(c => c.QuestionId == questionId)
                .ToListAsync();

            response.Data = _mapper.Map<IEnumerable<QuizDTO.ChoiceDto>>(choices);
            response.Message = "Choices retrieved successfully.";
            return response;
        }

        public async Task<ServiceResponse<QuizDTO.ChoiceDto>> UpdateChoiceAsync(long choiceId, QuizDTO.UpdateChoiceDto dto, long userId)
        {
            var response = new ServiceResponse<QuizDTO.ChoiceDto>();

            var choice = await _context.Choices
                .Include(c => c.Question)
                    .ThenInclude(q => q.Quiz)
                        .ThenInclude(qz => qz.Course)
                .FirstOrDefaultAsync(c => c.ChoiceId == choiceId);

            if (choice == null)
            {
                response.Success = false;
                response.Message = "Choice not found.";
                return response;
            }

            // Check authorization
            if (choice.Question.Quiz.Course.InstructorUserId != userId && !await UserIsAdmin(userId))
            {
                response.Success = false;
                response.Message = "You are not authorized to update this choice.";
                return response;
            }

            // Check if quiz has attempts
            var hasAttempts = await _context.Attempts.AnyAsync(a => a.QuizId == choice.Question.QuizId);
            if (hasAttempts)
            {
                response.Success = false;
                response.Message = "Cannot update choice. Quiz already has attempts.";
                return response;
            }

            // Update fields
            if (!string.IsNullOrWhiteSpace(dto.Text))
                choice.Body = dto.Text;

            if (dto.IsCorrect.HasValue)
                choice.IsCorrect = dto.IsCorrect.Value;

            await _context.SaveChangesAsync();

            response.Data = _mapper.Map<QuizDTO.ChoiceDto>(choice);
            response.Message = "Choice updated successfully.";
            return response;
        }

        public async Task<ServiceResponse<bool>> DeleteChoiceAsync(long choiceId, long userId)
        {
            var response = new ServiceResponse<bool>();

            var choice = await _context.Choices
                .Include(c => c.Question)
                    .ThenInclude(q => q.Quiz)
                        .ThenInclude(qz => qz.Course)
                .FirstOrDefaultAsync(c => c.ChoiceId == choiceId);

            if (choice == null)
            {
                response.Success = false;
                response.Message = "Choice not found.";
                return response;
            }

            // Check authorization
            if (choice.Question.Quiz.Course.InstructorUserId != userId && !await UserIsAdmin(userId))
            {
                response.Success = false;
                response.Message = "You are not authorized to delete this choice.";
                return response;
            }

            // Check if quiz has attempts
            var hasAttempts = await _context.Attempts.AnyAsync(a => a.QuizId == choice.Question.QuizId);
            if (hasAttempts)
            {
                response.Success = false;
                response.Message = "Cannot delete choice. Quiz already has attempts.";
                return response;
            }

            // Ensure at least one correct choice remains
            var correctChoicesCount = await _context.Choices
                .Where(c => c.QuestionId == choice.QuestionId && c.IsCorrect)
                .CountAsync();

            if (choice.IsCorrect && correctChoicesCount <= 1)
            {
                response.Success = false;
                response.Message = "Cannot delete the only correct choice. At least one correct choice must remain.";
                return response;
            }

            // Ensure at least one choice remains for the question
            var totalChoicesCount = await _context.Choices
                .Where(c => c.QuestionId == choice.QuestionId)
                .CountAsync();
            if (totalChoicesCount <= 1)
            {
                response.Success = false;
                response.Message = "Cannot delete the only choice. At least one choice must remain for the question.";
                return response;
            }
            _context.Choices.Remove(choice);
            await _context.SaveChangesAsync();

            response.Data = true;
            response.Message = "Choice deleted successfully.";
            return response;
        }

        //HELPER METHODS

        private async Task<bool> UserIsAdmin(long userId)
        {
            return await _context.UserRoles
                .Include(ur => ur.Role)
                .AnyAsync(ur => ur.UserId == userId && ur.Role.Name == "Admin");
        }
    }
}