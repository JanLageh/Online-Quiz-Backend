using AutoMapper;
using OnlineQuiz.DTOs;
using OnlineQuiz.IRepository;
using OnlineQuiz.IServices;
using OnlineQuiz.Models.Response;
using static OnlineQuiz.DTOs.AttemptDtos;

namespace OnlineQuiz.Services
{
    public class QuizService : IQuizService
    {
        private readonly IQuizRepository _repo;
        private readonly IAttemptRepository _attemptRepo;
        private readonly IMapper _mapper;

        public QuizService(IQuizRepository repo, IAttemptRepository attemptRepo, IMapper mapper)
        {
            _repo = repo;
            _attemptRepo = attemptRepo;
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

        public Task<ServiceResponse<QuizDTO.QuestionDto>> AddQuestionAsync(QuizDTO.CreateQuestionDto dto)
            => _repo.AddQuestionAsync(dto);

        public Task<ServiceResponse<IEnumerable<QuizDTO.QuestionDto>>> GetQuestionsByQuizIdAsync(long quizId)
            => _repo.GetQuestionsByQuizIdAsync(quizId);

        public Task<ServiceResponse<bool>> AddChoicesAsync(long questionId, IEnumerable<QuizDTO.CreateChoiceDto> choices)
            => _repo.AddChoicesAsync(questionId, choices);

        public Task<ServiceResponse<IEnumerable<QuizDTO.ChoiceDto>>> GetChoicesByQuestionIdAsync(long questionId)
            => _repo.GetChoicesByQuestionIdAsync(questionId);

        // ATTEMPT MANAGEMENT - Delegate to AttemptRepository
        public Task<AttemptResultDto> SubmitAttemptAsync(SubmitAttemptDto dto, long userId)
            => _attemptRepo.SubmitAttemptAsync(dto, userId);

        public Task<List<AttemptListDto>> GetUserAttemptsAsync(long userId)
            => _attemptRepo.GetUserAttemptsAsync(userId);

        public Task<List<AttemptListDto>> GetQuizAttemptsAsync(long quizId)
            => _attemptRepo.GetQuizAttemptsAsync(quizId);

        public Task<AttemptDetailDto> GetAttemptDetailAsync(long attemptId)
            => _attemptRepo.GetAttemptDetailAsync(attemptId);
    }
}