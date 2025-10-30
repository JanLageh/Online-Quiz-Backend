using OnlineQuiz.DTOs;
using static OnlineQuiz.DTOs.AttemptDtos;

namespace OnlineQuiz.IRepository
{
    public interface IAttemptRepository
    {
        Task<AttemptResultDto> SubmitAttemptAsync(SubmitAttemptDto dto, long userId);
        Task<List<AttemptListDto>> GetUserAttemptsAsync(long userId);
        Task<List<AttemptListDto>> GetQuizAttemptsAsync(long quizId);
        Task<AttemptDetailDto> GetAttemptDetailAsync(long attemptId);
    }
}