using OnlineQuiz.DTOs;
using OnlineQuiz.Models.Response;

namespace OnlineQuiz.IRepository
{
    public interface IQuizRepository
    {
        Task<ServiceResponse<QuizDTO.PagedResult<QuizDTO.QuizListItemDto>>> GetPagedQuizzesAsync(
            int page = 1,
            int pageSize = 10,
            long? courseId = null,
            long? teacherId = null);

        Task<ServiceResponse<QuizDTO.QuizDetailDto>> GetQuizByIdAsync(long quizId);
        Task<ServiceResponse<QuizDTO.QuizListItemDto>> CreateQuizAsync(QuizDTO.CreateQuizDto dto, long createdByUserId);
        Task<ServiceResponse<QuizDTO.QuizListItemDto>> UpdateQuizAsync(long quizId, QuizDTO.UpdateQuizDto dto, long updatedByUserId);
        Task<ServiceResponse<bool>> DeleteQuizAsync(long quizId, long requestedByUserId);
        Task<ServiceResponse<bool>> PublishQuizAsync(long quizId, long requestedByUserId);

        // Question methods
        Task<ServiceResponse<QuizDTO.QuestionDto>> AddQuestionAsync(QuizDTO.CreateQuestionDto dto);
        Task<ServiceResponse<IEnumerable<QuizDTO.QuestionDto>>> GetQuestionsByQuizIdAsync(long quizId);

        // Choice methods
        Task<ServiceResponse<bool>> AddChoicesAsync(long questionId, IEnumerable<QuizDTO.CreateChoiceDto> choices);
        Task<ServiceResponse<IEnumerable<QuizDTO.ChoiceDto>>> GetChoicesByQuestionIdAsync(long questionId);
    }
}
