using AutoMapper;
using OnlineQuiz.DTOs;
using OnlineQuiz.IRepository;
using OnlineQuiz.IServices;
using OnlineQuiz.Models.Response;

namespace OnlineQuiz.Services
{
    public class QuizService : IQuizService
    {
        private readonly IQuizRepository _repo;
        private readonly IMapper _mapper;

        public QuizService(IQuizRepository repo, IMapper mapper)
        {
            _repo = repo;
            _mapper = mapper;
        }

        public Task<ServiceResponse<QuizDTO.PagedResult<QuizDTO.QuizListItemDto>>> GetQuizzesAsync(
            int page = 1, int pageSize = 10, long? courseId = null, long? teacherId = null)
            => _repo.GetPagedQuizzesAsync(page, pageSize, courseId, teacherId);

        public Task<ServiceResponse<QuizDTO.QuizDetailDto>> GetQuizByIdAsync(long quizId)
            => _repo.GetQuizByIdAsync(quizId);

        public Task<ServiceResponse<QuizDTO.QuizListItemDto>> CreateQuizAsync(QuizDTO.CreateQuizDto dto, long createdByUserId)
            => _repo.CreateQuizAsync(dto, createdByUserId);

        public Task<ServiceResponse<QuizDTO.QuizListItemDto>> UpdateQuizAsync(long quizId, QuizDTO.UpdateQuizDto dto, long updatedByUserId)
            => _repo.UpdateQuizAsync(quizId, dto, updatedByUserId);

        public Task<ServiceResponse<bool>> DeleteQuizAsync(long quizId, long requestedByUserId)
            => _repo.DeleteQuizAsync(quizId, requestedByUserId);

        public Task<ServiceResponse<bool>> PublishQuizAsync(long quizId, long requestedByUserId)
            => _repo.PublishQuizAsync(quizId, requestedByUserId);
    }
}
