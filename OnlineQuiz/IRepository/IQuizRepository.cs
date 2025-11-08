using OnlineQuiz.DTOs;
using OnlineQuiz.Models.Response;

namespace OnlineQuiz.IRepository
{
    public interface IQuizRepository
    {
        // Quiz Management
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

        // Question Management
        Task<ServiceResponse<QuizDTO.QuestionDto>> AddQuestionAsync(QuizDTO.CreateQuestionDto dto);
        Task<ServiceResponse<IEnumerable<QuizDTO.QuestionDto>>> GetQuestionsByQuizIdAsync(long quizId);
        Task<ServiceResponse<QuizDTO.QuestionDto>> GetQuestionByIdAsync(long questionId);
        Task<ServiceResponse<QuizDTO.QuestionDto>> UpdateQuestionAsync(long questionId, QuizDTO.UpdateQuestionDto dto, long userId);
        Task<ServiceResponse<bool>> DeleteQuestionAsync(long questionId, long userId);

        // Choice Management
        Task<ServiceResponse<bool>> AddChoicesAsync(long questionId, IEnumerable<QuizDTO.CreateChoiceDto> choices);
        Task<ServiceResponse<IEnumerable<QuizDTO.ChoiceDto>>> GetChoicesByQuestionIdAsync(long questionId);
        Task<ServiceResponse<QuizDTO.ChoiceDto>> UpdateChoiceAsync(long choiceId, QuizDTO.UpdateChoiceDto dto, long userId);
        Task<ServiceResponse<bool>> DeleteChoiceAsync(long choiceId, long userId);
    }
}