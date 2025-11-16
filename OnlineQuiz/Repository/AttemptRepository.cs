using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.EntityFrameworkCore;
using OnlineQuiz.Data;
using OnlineQuiz.DTOs;
using OnlineQuiz.IRepository;
using OnlineQuiz.Models;
using static OnlineQuiz.DTOs.AttemptDtos;

namespace OnlineQuiz.Repository
{
    public class AttemptRepository : IAttemptRepository
    {
        private readonly OnlineQuizDbContext _context;

        public AttemptRepository(OnlineQuizDbContext context)
        {
            _context = context;
        }

        public async Task<AttemptResultDto> SubmitAttemptAsync(SubmitAttemptDto dto, long userId)
        {
            // Validate quiz exists
            var quiz = await _context.Quizzes
                .Include(q => q.Questions)
                    .ThenInclude(q => q.Choices)
                .FirstOrDefaultAsync(q => q.QuizId == dto.QuizId);

            if (quiz == null)
                throw new Exception("Quiz not found.");

            // Calculate total score
            decimal totalScore = 0;
            var questionResults = new List<QuestionResultDto>();

            // Create attempt
            var attempt = new AttemptModel
            {
                UserId = userId,
                QuizId = dto.QuizId,
                StartedAt = DateTime.UtcNow,
                SubmittedAt = DateTime.UtcNow,
                Score = 0 // Will be updated after grading
            };

            _context.Attempts.Add(attempt);
            await _context.SaveChangesAsync(); // Save to get AttemptId

            // Process each answer
            foreach (var answer in dto.Answers)
            {
                var question = quiz.Questions.FirstOrDefault(q => q.QuestionId == answer.QuestionId);
                if (question == null) continue;

                decimal earnedPoints = 0;
                bool isCorrect = false;

                if (question.Type == "Single")
                {
                    // Single choice question
                    var selectedChoice = question.Choices.FirstOrDefault(c => c.ChoiceId == answer.ChoiceId);
                    if (selectedChoice != null)
                    {
                        isCorrect = selectedChoice.IsCorrect;
                        earnedPoints = isCorrect ? question.Points : 0;

                        // Save answer
                        var attemptAnswer = new AttemptAnswerModel
                        {
                            AttemptId = attempt.AttemptId,
                            QuestionId = question.QuestionId,
                            ChoiceId = answer.ChoiceId,
                            IsCorrect = isCorrect,
                            FreeText = answer.TextAnswer
                        };
                        _context.AttemptAnswers.Add(attemptAnswer);
                    }
                }
                else if (question.Type == "Multiple")
                {
                    // Multiple choice question
                    var correctChoiceIds = question.Choices
                        .Where(c => c.IsCorrect)
                        .Select(c => c.ChoiceId)
                        .OrderBy(id => id)
                        .ToList();

                    var selectedChoiceIds = (answer.ChoiceIds ?? new List<long>())
                        .OrderBy(id => id)
                        .ToList();

                    isCorrect = correctChoiceIds.SequenceEqual(selectedChoiceIds);
                    earnedPoints = isCorrect ? question.Points : 0;

                    // Save each selected choice as an answer
                    foreach (var choiceId in selectedChoiceIds)
                    {
                        var attemptAnswer = new AttemptAnswerModel
                        {
                            AttemptId = attempt.AttemptId,
                            QuestionId = question.QuestionId,
                            ChoiceId = choiceId,
                            IsCorrect = isCorrect,
                            FreeText = null
                        };
                        _context.AttemptAnswers.Add(attemptAnswer);
                    }
                }

                totalScore += earnedPoints;

                // Add to results
                questionResults.Add(new QuestionResultDto
                {
                    QuestionId = question.QuestionId,
                    QuestionBody = question.Body,
                    Points = question.Points,
                    EarnedPoints = earnedPoints,
                    IsCorrect = isCorrect,
                    CorrectChoiceIds = question.Choices.Where(c => c.IsCorrect).Select(c => c.ChoiceId).ToList(),
                    SelectedChoiceIds = question.Type == "Multiple"
                        ? (answer.ChoiceIds ?? new List<long>())
                        : (answer.ChoiceId.HasValue ? new List<long> { answer.ChoiceId.Value } : new List<long>())
                });
            }

            // Update attempt score
            attempt.Score = totalScore;
            await _context.SaveChangesAsync();

            // Calculate max possible score
            var maxScore = quiz.Questions.Sum(q => q.Points);

            return new AttemptResultDto
            {
                AttemptId = attempt.AttemptId,
                QuizId = quiz.QuizId,
                QuizTitle = quiz.Title,
                Score = totalScore,
                MaxScore = maxScore,
                Percentage = maxScore > 0 ? (double)(totalScore / maxScore * 100) : 0,
                SubmittedAt = attempt.SubmittedAt ?? DateTime.UtcNow,
                QuestionResults = questionResults
            };
        }

        public async Task<List<AttemptListDto>> GetUserAttemptsAsync(long userId)
        {
            var attempts = await _context.Attempts
                .Include(a => a.Quiz)
                .Include(a => a.User)
                .Where(a => a.UserId == userId)
                .OrderByDescending(a => a.SubmittedAt)
                .Select(a => new AttemptListDto
                {
                    AttemptId = a.AttemptId,
                    QuizId = a.QuizId,
                    QuizTitle = a.Quiz.Title,
                    StudentName = a.User.FullName,
                    StudentEmail = a.User.Email,
                    Score = a.Score,
                    SubmittedAt = a.SubmittedAt ?? DateTime.UtcNow
                })
                .ToListAsync();

            return attempts;
        }

        public async Task<List<AttemptListDto>> GetQuizAttemptsAsync(long quizId)
        {
            var attempts = await _context.Attempts
                .Include(a => a.Quiz)
                .Include(a => a.User)
                .Where(a => a.QuizId == quizId)
                .OrderByDescending(a => a.SubmittedAt)
                .Select(a => new AttemptListDto
                {
                    AttemptId = a.AttemptId,
                    QuizId = a.QuizId,
                    QuizTitle = a.Quiz.Title,
                    StudentName = a.User.FullName,
                    StudentEmail = a.User.Email,
                    Score = a.Score,
                    SubmittedAt = a.SubmittedAt ?? DateTime.UtcNow
                })
                .ToListAsync();

            return attempts;
        }

        public async Task<AttemptDetailDto> GetAttemptDetailAsync(long attemptId)
        {
            var attempt = await _context.Attempts
                .Include(a => a.User)
                .Include(a => a.Quiz)
                    .ThenInclude(q => q.Questions)
                .Include(a => a.AttemptAnswers)
                    .ThenInclude(aa => aa.Question)
                .Include(a => a.AttemptAnswers)
                    .ThenInclude(aa => aa.Choice)
                .FirstOrDefaultAsync(a => a.AttemptId == attemptId);

            if (attempt == null)
                throw new Exception("Attempt not found.");

            // Calculate points earned per answer
            var answers = new List<AttemptAnswerDetailDto>();

            foreach (var aa in attempt.AttemptAnswers)
            {
                var question = aa.Question;
                decimal pointsEarned = 0;

                if (aa.IsCorrect == true)
                {
                    // For multiple choice, divide points among all correct selections
                    var totalCorrectSelections = attempt.AttemptAnswers
                        .Count(a => a.QuestionId == aa.QuestionId && a.IsCorrect == true);

                    //if (totalCorrectSelections > 0)
                    //    pointsEarned = question.Points / totalCorrectSelections;
                    //else
                    //    pointsEarned = question.Points;
                    pointsEarned = totalCorrectSelections > 0
                        ? question.Points / totalCorrectSelections
                        : question.Points;
                }

                answers.Add(new AttemptAnswerDetailDto
                {
                    QuestionId = aa.QuestionId,
                    QuestionBody = aa.Question.Body,
                    ChoiceId = aa.ChoiceId,
                    ChoiceText = aa.Choice?.Body,
                    IsCorrect = aa.IsCorrect ?? false,
                    PointsEarned = pointsEarned
                });
            }

            return new AttemptDetailDto
            {
                AttemptId = attempt.AttemptId,
                UserId = attempt.UserId,
                UserName = attempt.User.FullName,
                QuizId = attempt.QuizId,
                QuizTitle = attempt.Quiz.Title,
                Score = attempt.Score,
                SubmittedAt = attempt.SubmittedAt ?? DateTime.UtcNow,
                Answers = answers
            };
        }
    }
}