using Geex.Extensions.Identity;
using Geex.Extensions.Identity.Requests;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Geex.Abstractions;
using Geex.Extensions.Identity.Core.Entities;
using MongoDB.Bson;

namespace Geex.Tests.FeatureTests
{
    [Collection(nameof(TestsCollection))]
    public class IdentityUserServiceTests
    {
        private readonly TestApplicationFactory _factory;

        public IdentityUserServiceTests(TestApplicationFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task CreateUserServiceShouldWork()
        {
            // Arrange
            using var scope = _factory.StartTestScope(out var service);
            var uow = service.GetService<IUnitOfWork>();
            await uow.DbContext.Query<User>().Where(x=>x.PhoneNumber == "15555555556").DeleteAsync();
            var testUsername = $"testuser_{ObjectId.GenerateNewId()}";

            var password = "Password123!";
            var request = new CreateUserRequest
            {
                Username = testUsername,
                Email = $"{testUsername}@test.com",
                Password = password,
                Nickname = "Test User",
                PhoneNumber = "15555555556",
                IsEnable = true,
                RoleIds = new List<string>(),
                OrgCodes = new List<string>()
            };

            // Act
            var user = await uow.Request(request);
            await uow.SaveChanges();

            // Assert
            user.ShouldNotBeNull();
            user.Username.ShouldBe(testUsername);
            user.Email.ShouldBe($"{testUsername}@test.com");
            user.Nickname.ShouldBe("Test User");
            user.IsEnable.ShouldBe(true);
            user.CheckPassword(password).ShouldBeTrue();
        }

        [Fact]
        public async Task PasswordHashingShouldWork()
        {
            // Arrange
            using var scope = _factory.StartTestScope(out var service);
            var uow = service.GetService<IUnitOfWork>();
            var passwordHasher = service.GetService<IPasswordHasher<IUser>>();
            var plainPassword = "Password123!";
            var testUsername = $"hashtest_{ObjectId.GenerateNewId()}";

            var request = new CreateUserRequest
            {
                Username = testUsername,
                Email = $"{testUsername}@test.com",
                Password = plainPassword,
                Nickname = "Test User",
                IsEnable = true,
                RoleIds = new List<string>(),
                OrgCodes = new List<string>()
            };

            // Act
            var user = await uow.Request(request);
            await uow.SaveChanges();

            // Assert
            (user as User).Password.ShouldNotBe(plainPassword);
            var verificationResult = passwordHasher.VerifyHashedPassword(user, (user as User).Password, plainPassword);
            verificationResult.ShouldNotBe(PasswordVerificationResult.Failed);
        }

        [Fact]
        public async Task CheckPasswordShouldWork()
        {
            // Arrange
            using var scope = _factory.StartTestScope(out var service);
            var uow = service.GetService<IUnitOfWork>();
            var password = "Password123!";
            var testUsername = $"checkpwd_{ObjectId.GenerateNewId()}";

            var user = await uow.Request(new CreateUserRequest
            {
                Username = testUsername,
                Email = $"{testUsername}@test.com",
                Password = password,
                Nickname = "Test User",
                IsEnable = true,
                RoleIds = new List<string>(),
                OrgCodes = new List<string>()
            });
            await uow.SaveChanges();

            // Act & Assert
            user.CheckPassword(password).ShouldBe(true);
            user.CheckPassword("WrongPassword").ShouldBe(false);
        }

        [Fact]
        public async Task ChangePasswordServiceShouldWork()
        {
            // Arrange
            using var scope = _factory.StartTestScope(out var service);
            var uow = service.GetService<IUnitOfWork>();
            var originalPassword = "Password123!";
            var newPassword = "NewPassword123!";
            var testUsername = $"changepwd_{ObjectId.GenerateNewId()}";

            var user = await uow.Request(new CreateUserRequest
            {
                Username = testUsername,
                Email = $"{testUsername}@test.com",
                Password = originalPassword,
                Nickname = "Test User",
                IsEnable = true,
                RoleIds = new List<string>(),
                OrgCodes = new List<string>()
            });
            await uow.SaveChanges();

            // Act
            user.ChangePassword(originalPassword, newPassword);
            await uow.SaveChanges();

            // Assert
            user.CheckPassword(newPassword).ShouldBe(true);
            user.CheckPassword(originalPassword).ShouldBe(false);
        }

        [Fact]
        public async Task ChangePasswordWithInvalidOriginShouldThrow()
        {
            // Arrange
            using var scope = _factory.StartTestScope(out var service);
            var uow = service.GetService<IUnitOfWork>();
            var testUsername = $"invalidpwd_{ObjectId.GenerateNewId()}";

            var user = await uow.Request(new CreateUserRequest
            {
                Username = testUsername,
                Email = $"{testUsername}@test.com",
                Password = "Password123!",
                Nickname = "Test User",
                IsEnable = true,
                RoleIds = new List<string>(),
                OrgCodes = new List<string>()
            });
            await uow.SaveChanges();

            // Act & Assert
            Should.Throw<BusinessException>(() =>
                user.ChangePassword("WrongPassword", "NewPassword123!"));
        }

        [Fact]
        public async Task EditUserServiceShouldWork()
        {
            // Arrange
            using var scope = _factory.StartTestScope(out var service);
            var uow = service.GetService<IUnitOfWork>();
            var testUsername = $"edituser_{ObjectId.GenerateNewId()}";
            var uniquePhoneNumber = $"155{ObjectId.GenerateNewId().ToString().Substring(0, 8)}";

            // Clean up any existing users with this phone number
            await uow.DbContext.Query<User>().Where(x => x.PhoneNumber == uniquePhoneNumber).DeleteAsync();

            var user = await uow.Request(new CreateUserRequest
            {
                Username = testUsername,
                Email = $"{testUsername}@test.com",
                Password = "Password123!",
                Nickname = "Original Nickname",
                IsEnable = true,
                RoleIds = new List<string>(),
                OrgCodes = new List<string>()
            });
            await uow.SaveChanges();

            var editRequest = new EditUserRequest
            {
                Id = user.Id,
                Nickname = "Updated Nickname",
                Email = "updated@test.com",
                PhoneNumber = uniquePhoneNumber,
                IsEnable = false
            };

            // Act
            var editedUser = await uow.Request(editRequest);
            await uow.SaveChanges();

            // Assert
            editedUser.Nickname.ShouldBe("Updated Nickname");
            editedUser.Email.ShouldBe("updated@test.com");
            editedUser.PhoneNumber.ShouldBe(uniquePhoneNumber);
            editedUser.IsEnable.ShouldBe(false);
        }

        [Fact]
        public async Task DeleteUserServiceShouldWork()
        {
            // Arrange
            using var scope = _factory.StartTestScope(out var service);
            var uow = service.GetService<IUnitOfWork>();
            var testUsername = $"deleteuser_{ObjectId.GenerateNewId()}";

            var user = await uow.Request(new CreateUserRequest
            {
                Username = testUsername,
                Email = $"{testUsername}@test.com",
                Password = "Password123!",
                Nickname = "Test User",
                IsEnable = true,
                RoleIds = new List<string>(),
                OrgCodes = new List<string>()
            });
            await uow.SaveChanges();
            var userId = user.Id;

            var deleteRequest = new DeleteUserRequest { Id = userId };

            // Act
            var result = await uow.Request(deleteRequest);
            await uow.SaveChanges();

            // Assert
            result.ShouldBe(true);
            using var verifyService = service.CreateScope();
            var verifyUow = verifyService.ServiceProvider.GetService<IUnitOfWork>();
            var deletedUser = verifyUow.Query<IUser>().FirstOrDefault(x => x.Id == userId);
            deletedUser.ShouldBeNull();
        }

        [Fact]
        public async Task ResetUserPasswordServiceShouldWork()
        {
            // Arrange
            using var scope = _factory.StartTestScope(out var service);
            var uow = service.GetService<IUnitOfWork>();
            var testUsername = $"resetpwd_{ObjectId.GenerateNewId()}";

            var user = await uow.Request(new CreateUserRequest
            {
                Username = testUsername,
                Email = $"{testUsername}@test.com",
                Password = "Password123!",
                Nickname = "Test User",
                IsEnable = true,
                RoleIds = new List<string>(),
                OrgCodes = new List<string>()
            });
            await uow.SaveChanges();

            var newPassword = "ResetPassword123!";
            var resetRequest = new ResetUserPasswordRequest
            {
                UserId = user.Id,
                Password = newPassword
            };

            // Act
            var resetUser = await uow.Request(resetRequest);
            await uow.SaveChanges();

            // Assert
            resetUser.CheckPassword(newPassword).ShouldBe(true);
        }

        [Fact]
        public async Task SetPasswordShouldWork()
        {
            // Arrange
            using var scope = _factory.StartTestScope(out var service);
            var uow = service.GetService<IUnitOfWork>();
            var testUsername = $"setpwd_{ObjectId.GenerateNewId()}";

            var user = await uow.Request(new CreateUserRequest
            {
                Username = testUsername,
                Email = $"{testUsername}@test.com",
                Password = "Password123!",
                Nickname = "Test User",
                IsEnable = true,
                RoleIds = new List<string>(),
                OrgCodes = new List<string>()
            });
            await uow.SaveChanges();

            var newPassword = "NewTestPassword123!";

            // Act
            user.SetPassword(newPassword);
            await uow.SaveChanges();

            // Assert
            (user as User).Password.ShouldNotBe(newPassword);
            user.CheckPassword(newPassword).ShouldBe(true);
        }
    }
}
