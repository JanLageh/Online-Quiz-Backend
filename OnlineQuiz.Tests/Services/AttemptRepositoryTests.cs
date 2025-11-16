using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using OnlineQuiz.Data;
using OnlineQuiz.Models;
using OnlineQuiz.Repository;
using Xunit;
using static OnlineQuiz.DTOs.AttemptDtos;

namespace OnlineQuiz.Tests.Repository
{
    public class AttemptRepositoryTests : IDisposable
    {
        private readonly OnlineQuizDbContext _context;
        private readonly AttemptRepository _repository;

        public AttemptRepositoryTests()
        {
            // Create in-memory database for testing
            var options = new DbContextOptionsBuilder<OnlineQuizDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new OnlineQuizDbContext(options);
            _repository = new AttemptRepository(_context);

            // Seed test data
            SeedTestData();
        }

        private void SeedTestData()
        {
            // Create test user
            var user = new UserModel
            {
                UserId = 1,
                Email = "student@test.com",
                FullName = "Test Student",
                PasswordHash = "hashedpassword",
                Status = "Active",
                CreatedAt = DateTime.UtcNow
            };
            _context.Users.Add(user);

            // Create test teacher
            var teacherUser = new UserModel
            {
                UserId = 2,
                Email = "teacher@test.com",
                FullName = "Test Teacher",
                PasswordHash = "hashedpassword",
                Status = "Active",
                CreatedAt = DateTime.UtcNow
            };
            _context.Users.Add(teacherUser);

            var teacher = new TeacherModel
            {
                UserId = 2,
                User = teacherUser
            };
            _context.Teachers.Add(teacher);

            // Create test course
            var course = new CourseModel
            {
                CourseId = 1,
                Code = "CS101",
                Name = "Intro to CS",
                InstructorUserId = 2,
                Department = "Computer Science",
                Status = "Active",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                CreatedBy = 2
            };
            _context.Courses.Add(course);

            // Create test quiz with questions and choices
            var quiz = new QuizModel
            {
                QuizId = 1,
                CourseId = 1,
                Title = "Test Quiz",
                IsPublished = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.Quizzes.Add(quiz);

            // Question 1: Single choice
            var question1 = new QuestionModel
            {
                QuestionId = 1,
                QuizId = 1,
                Type = "Single",
                Body = "What is 2+2?",
                Points = 10,
                SortOrder = 1
            };
            _context.Questions.Add(question1);

            var choice1_1 = new ChoiceModel { ChoiceId = 1, QuestionId = 1, Body = "3", IsCorrect = false };
            var choice1_2 = new ChoiceModel { ChoiceId = 2, QuestionId = 1, Body = "4", IsCorrect = true };
            var choice1_3 = new ChoiceModel { ChoiceId = 3, QuestionId = 1, Body = "5", IsCorrect = false };
            _context.Choices.AddRange(choice1_1, choice1_2, choice1_3);

            // Question 2: Multiple choice
            var question2 = new QuestionModel
            {
                QuestionId = 2,
                QuizId = 1,
                Type = "Multiple",
                Body = "Select even numbers:",
                Points = 15,
                SortOrder = 2
            };
            _context.Questions.Add(question2);

            var choice2_1 = new ChoiceModel { ChoiceId = 4, QuestionId = 2, Body = "1", IsCorrect = false };
            var choice2_2 = new ChoiceModel { ChoiceId = 5, QuestionId = 2, Body = "2", IsCorrect = true };
            var choice2_3 = new ChoiceModel { ChoiceId = 6, QuestionId = 2, Body = "3", IsCorrect = false };
            var choice2_4 = new ChoiceModel { ChoiceId = 7, QuestionId = 2, Body = "4", IsCorrect = true };
            _context.Choices.AddRange(choice2_1, choice2_2, choice2_3, choice2_4);

            _context.SaveChanges();
        }

        [Fact]
        public async Task SubmitAttemptAsync_WithCorrectSingleChoiceAnswer_ReturnsFullScore()
        {
            // Arrange
            var submitDto = new SubmitAttemptDto
            {
                QuizId = 1,
                Answers = new List<SubmitAnswerDto>
                {
                    new SubmitAnswerDto { QuestionId = 1, ChoiceId = 2 } // Correct answer
                }
            };

            // Act
            var result = await _repository.SubmitAttemptAsync(submitDto, 1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.QuizId);
            Assert.Equal(10, result.Score); // Full points for question 1
            Assert.Single(result.QuestionResults);
            Assert.True(result.QuestionResults[0].IsCorrect);
            Assert.Equal(10, result.QuestionResults[0].EarnedPoints);
        }

        [Fact]
        public async Task SubmitAttemptAsync_WithIncorrectSingleChoiceAnswer_ReturnsZeroScore()
        {
            // Arrange
            var submitDto = new SubmitAttemptDto
            {
                QuizId = 1,
                Answers = new List<SubmitAnswerDto>
                {
                    new SubmitAnswerDto { QuestionId = 1, ChoiceId = 1 } // Wrong answer
                }
            };

            // Act
            var result = await _repository.SubmitAttemptAsync(submitDto, 1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(0, result.Score); // No points
            Assert.Single(result.QuestionResults);
            Assert.False(result.QuestionResults[0].IsCorrect);
            Assert.Equal(0, result.QuestionResults[0].EarnedPoints);
        }

        [Fact]
        public async Task SubmitAttemptAsync_WithCorrectMultipleChoiceAnswer_ReturnsFullScore()
        {
            // Arrange
            var submitDto = new SubmitAttemptDto
            {
                QuizId = 1,
                Answers = new List<SubmitAnswerDto>
                {
                    new SubmitAnswerDto
                    {
                        QuestionId = 2,
                        ChoiceIds = new List<long> { 5, 7 } // Correct answers (2 and 4)
                    }
                }
            };

            // Act
            var result = await _repository.SubmitAttemptAsync(submitDto, 1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(15, result.Score); // Full points for question 2
            Assert.Single(result.QuestionResults);
            Assert.True(result.QuestionResults[0].IsCorrect);
            Assert.Equal(15, result.QuestionResults[0].EarnedPoints);
        }

        [Fact]
        public async Task SubmitAttemptAsync_WithIncorrectMultipleChoiceAnswer_ReturnsZeroScore()
        {
            // Arrange
            var submitDto = new SubmitAttemptDto
            {
                QuizId = 1,
                Answers = new List<SubmitAnswerDto>
                {
                    new SubmitAnswerDto
                    {
                        QuestionId = 2,
                        ChoiceIds = new List<long> { 4, 5 } // Wrong combination
                    }
                }
            };

            // Act
            var result = await _repository.SubmitAttemptAsync(submitDto, 1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(0, result.Score); // No points
            Assert.Single(result.QuestionResults);
            Assert.False(result.QuestionResults[0].IsCorrect);
            Assert.Equal(0, result.QuestionResults[0].EarnedPoints);
        }

        [Fact]
        public async Task SubmitAttemptAsync_WithAllCorrectAnswers_ReturnsFullScore()
        {
            // Arrange
            var submitDto = new SubmitAttemptDto
            {
                QuizId = 1,
                Answers = new List<SubmitAnswerDto>
                {
                    new SubmitAnswerDto { QuestionId = 1, ChoiceId = 2 }, // Correct
                    new SubmitAnswerDto { QuestionId = 2, ChoiceIds = new List<long> { 5, 7 } } // Correct
                }
            };

            // Act
            var result = await _repository.SubmitAttemptAsync(submitDto, 1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(25, result.Score); // 10 + 15 = 25
            Assert.Equal(25, result.MaxScore);
            Assert.Equal(100, result.Percentage);
            Assert.Equal(2, result.QuestionResults.Count);
            Assert.All(result.QuestionResults, qr => Assert.True(qr.IsCorrect));
        }

        [Fact]
        public async Task SubmitAttemptAsync_WithMixedAnswers_ReturnsPartialScore()
        {
            // Arrange
            var submitDto = new SubmitAttemptDto
            {
                QuizId = 1,
                Answers = new List<SubmitAnswerDto>
                {
                    new SubmitAnswerDto { QuestionId = 1, ChoiceId = 2 }, // Correct (10 points)
                    new SubmitAnswerDto { QuestionId = 2, ChoiceIds = new List<long> { 4 } } // Wrong (0 points)
                }
            };

            // Act
            var result = await _repository.SubmitAttemptAsync(submitDto, 1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(10, result.Score); // Only first question correct
            Assert.Equal(25, result.MaxScore);
            Assert.Equal(40, result.Percentage); // 10/25 * 100 = 40%
            Assert.Equal(2, result.QuestionResults.Count);
            Assert.True(result.QuestionResults[0].IsCorrect);
            Assert.False(result.QuestionResults[1].IsCorrect);
        }

        [Fact]
        public async Task SubmitAttemptAsync_WithInvalidQuizId_ThrowsException()
        {
            // Arrange
            var submitDto = new SubmitAttemptDto
            {
                QuizId = 999, // Non-existent quiz
                Answers = new List<SubmitAnswerDto>()
            };

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(
                async () => await _repository.SubmitAttemptAsync(submitDto, 1)
            );
        }

        [Fact]
        public async Task SubmitAttemptAsync_SavesAttemptToDatabase()
        {
            // Arrange
            var submitDto = new SubmitAttemptDto
            {
                QuizId = 1,
                Answers = new List<SubmitAnswerDto>
                {
                    new SubmitAnswerDto { QuestionId = 1, ChoiceId = 2 }
                }
            };

            // Act
            var result = await _repository.SubmitAttemptAsync(submitDto, 1);

            // Assert
            var savedAttempt = await _context.Attempts
                .Include(a => a.AttemptAnswers)
                .FirstOrDefaultAsync(a => a.AttemptId == result.AttemptId);

            Assert.NotNull(savedAttempt);
            Assert.Equal(1, savedAttempt.UserId);
            Assert.Equal(1, savedAttempt.QuizId);
            Assert.Equal(10, savedAttempt.Score);
            Assert.NotNull(savedAttempt.SubmittedAt);
            Assert.Single(savedAttempt.AttemptAnswers);
        }

        [Fact]
        public async Task GetUserAttemptsAsync_ReturnsAttemptsForSpecificUser()
        {
            // Arrange
            var submitDto = new SubmitAttemptDto
            {
                QuizId = 1,
                Answers = new List<SubmitAnswerDto>
                {
                    new SubmitAnswerDto { QuestionId = 1, ChoiceId = 2 }
                }
            };
            await _repository.SubmitAttemptAsync(submitDto, 1);

            // Act
            var attempts = await _repository.GetUserAttemptsAsync(1);

            // Assert
            Assert.NotNull(attempts);
            Assert.Single(attempts);
            Assert.Equal("Test Student", attempts[0].StudentName);
            Assert.Equal("student@test.com", attempts[0].StudentEmail);
            Assert.Equal("Test Quiz", attempts[0].QuizTitle);
        }

        [Fact]
        public async Task GetUserAttemptsAsync_WithNoAttempts_ReturnsEmptyList()
        {
            // Act
            var attempts = await _repository.GetUserAttemptsAsync(1);

            // Assert
            Assert.NotNull(attempts);
            Assert.Empty(attempts);
        }

        [Fact]
        public async Task GetQuizAttemptsAsync_ReturnsAttemptsForSpecificQuiz()
        {
            // Arrange
            var submitDto = new SubmitAttemptDto
            {
                QuizId = 1,
                Answers = new List<SubmitAnswerDto>
                {
                    new SubmitAnswerDto { QuestionId = 1, ChoiceId = 2 }
                }
            };
            await _repository.SubmitAttemptAsync(submitDto, 1);

            // Act
            var attempts = await _repository.GetQuizAttemptsAsync(1);

            // Assert
            Assert.NotNull(attempts);
            Assert.Single(attempts);
            Assert.Equal(1, attempts[0].QuizId);
            Assert.Equal("Test Quiz", attempts[0].QuizTitle);
        }

        [Fact]
        public async Task GetQuizAttemptsAsync_OrdersBySubmittedAtDescending()
        {
            // Arrange - Create multiple attempts
            for (int i = 0; i < 3; i++)
            {
                var submitDto = new SubmitAttemptDto
                {
                    QuizId = 1,
                    Answers = new List<SubmitAnswerDto>
                    {
                        new SubmitAnswerDto { QuestionId = 1, ChoiceId = 2 }
                    }
                };
                await _repository.SubmitAttemptAsync(submitDto, 1);
                await Task.Delay(10); // Small delay to ensure different timestamps
            }

            // Act
            var attempts = await _repository.GetQuizAttemptsAsync(1);

            // Assert
            Assert.Equal(3, attempts.Count);
            // Most recent should be first
            for (int i = 0; i < attempts.Count - 1; i++)
            {
                Assert.True(attempts[i].SubmittedAt >= attempts[i + 1].SubmittedAt);
            }
        }

        [Fact]
        public async Task GetAttemptDetailAsync_ReturnsDetailedAttemptInfo()
        {
            // Arrange
            var submitDto = new SubmitAttemptDto
            {
                QuizId = 1,
                Answers = new List<SubmitAnswerDto>
                {
                    new SubmitAnswerDto { QuestionId = 1, ChoiceId = 2 }
                }
            };
            var result = await _repository.SubmitAttemptAsync(submitDto, 1);

            // Act
            var detail = await _repository.GetAttemptDetailAsync(result.AttemptId);

            // Assert
            Assert.NotNull(detail);
            Assert.Equal(result.AttemptId, detail.AttemptId);
            Assert.Equal(1, detail.UserId);
            Assert.Equal("Test Student", detail.UserName);
            Assert.Equal(1, detail.QuizId);
            Assert.Equal("Test Quiz", detail.QuizTitle);
            Assert.Equal(10, detail.Score);
            Assert.Single(detail.Answers);
            Assert.Equal(1, detail.Answers[0].QuestionId);
            Assert.True(detail.Answers[0].IsCorrect);
        }

        [Fact]
        public async Task GetAttemptDetailAsync_WithInvalidAttemptId_ThrowsException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<Exception>(
                async () => await _repository.GetAttemptDetailAsync(999)
            );
        }

        [Fact]
        public async Task GetAttemptDetailAsync_CalculatesPointsEarnedCorrectly()
        {
            // Arrange
            var submitDto = new SubmitAttemptDto
            {
                QuizId = 1,
                Answers = new List<SubmitAnswerDto>
                {
                    new SubmitAnswerDto { QuestionId = 1, ChoiceId = 2 }, // Correct - 10 points
                    new SubmitAnswerDto { QuestionId = 2, ChoiceIds = new List<long> { 5, 7 } } // Correct - 15 points
                }
            };
            var result = await _repository.SubmitAttemptAsync(submitDto, 1);

            // Act
            var detail = await _repository.GetAttemptDetailAsync(result.AttemptId);

            // Assert
            Assert.NotNull(detail);
            Assert.Equal(3, detail.Answers.Count); // 1 for single, 2 for multiple

            // Single choice answer should get full 10 points
            var singleAnswer = detail.Answers.First(a => a.QuestionId == 1);
            Assert.Equal(10, singleAnswer.PointsEarned);

            // Multiple choice answers should split 15 points (7.5 each)
            var multipleAnswers = detail.Answers.Where(a => a.QuestionId == 2).ToList();
            Assert.Equal(2, multipleAnswers.Count);
            Assert.All(multipleAnswers, a => Assert.Equal(7.5m, a.PointsEarned));
        }

        [Fact]
        public async Task SubmitAttemptAsync_WithEmptyAnswers_ReturnsZeroScore()
        {
            // Arrange
            var submitDto = new SubmitAttemptDto
            {
                QuizId = 1,
                Answers = new List<SubmitAnswerDto>() // No answers
            };

            // Act
            var result = await _repository.SubmitAttemptAsync(submitDto, 1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(0, result.Score);
            Assert.Equal(25, result.MaxScore);
            Assert.Equal(0, result.Percentage);
            Assert.Empty(result.QuestionResults);
        }

        [Fact]
        public async Task SubmitAttemptAsync_IncludesCorrectChoiceIdsInResults()
        {
            // Arrange
            var submitDto = new SubmitAttemptDto
            {
                QuizId = 1,
                Answers = new List<SubmitAnswerDto>
                {
                    new SubmitAnswerDto { QuestionId = 1, ChoiceId = 1 } // Wrong answer
                }
            };

            // Act
            var result = await _repository.SubmitAttemptAsync(submitDto, 1);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.QuestionResults);
            var questionResult = result.QuestionResults[0];
            Assert.Contains(2, questionResult.CorrectChoiceIds); // Correct answer is choice 2
            Assert.Contains(1, questionResult.SelectedChoiceIds); // Student selected choice 1
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }
    }
}