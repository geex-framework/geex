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
    public class IdentityUserServiceTests : TestsBase
    {
        public IdentityUserServiceTests(TestApplicationFactory factory) : base(factory)
        {
        }

        [Fact]
        public async Task CreateUserServiceShouldWork()
        {
            // Arrange
            var testUsername = $"testuser_{ObjectId.GenerateNewId()}";
            var password = "Password123!".ToMd5();

            // Act
            using (var scope = ScopedService.CreateScope())
            {
                var uow = scope.ServiceProvider.GetService<IUnitOfWork>();
                await uow.DbContext.Query<User>().Where(x => x.PhoneNumber == "15555555556").DeleteAsync();

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
        }

        [Fact]
        public async Task PasswordHashingShouldWork()
        {
            // Arrange
            var plainPassword = "Password123!";
            var testUsername = $"hashtest_{ObjectId.GenerateNewId()}";

            // Act
            using (var scope = ScopedService.CreateScope())
            {
                var uow = scope.ServiceProvider.GetService<IUnitOfWork>();
                var passwordHasher = scope.ServiceProvider.GetService<IPasswordHasher<IUser>>();

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

                var user = await uow.Request(request);
                await uow.SaveChanges();

                // Assert
                (user as User).Password.ShouldNotBe(plainPassword);
                var verificationResult = passwordHasher.VerifyHashedPassword(user, (user as User).Password, plainPassword);
                verificationResult.ShouldNotBe(PasswordVerificationResult.Failed);
            }
        }

        [Fact]
        public async Task CheckPasswordShouldWork()
        {
            // Arrange
            var password = "Password123!".ToMd5();
            var testUsername = $"checkpwd_{ObjectId.GenerateNewId()}";

            // Act & Assert
            using (var scope = ScopedService.CreateScope())
            {
                var uow = scope.ServiceProvider.GetService<IUnitOfWork>();

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

                user.CheckPassword(password).ShouldBe(true);
                user.CheckPassword("WrongPassword").ShouldBe(false);
            }
        }

        [Fact]
        public async Task ChangePasswordServiceShouldWork()
        {
            // Arrange
            var originalPassword = "Password123!".ToMd5();
            var newPassword = "NewPassword123!".ToMd5();
            var testUsername = $"changepwd_{ObjectId.GenerateNewId()}";

            // Act
            using (var scope = ScopedService.CreateScope())
            {
                var uow = scope.ServiceProvider.GetService<IUnitOfWork>();

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

                user.ChangePassword(originalPassword, newPassword);
                await uow.SaveChanges();

                // Assert
                user.CheckPassword(newPassword).ShouldBe(true);
                user.CheckPassword(originalPassword).ShouldBe(false);
            }
        }

        [Fact]
        public async Task ChangePasswordWithInvalidOriginShouldThrow()
        {
            // Arrange
            var testUsername = $"invalidpwd_{ObjectId.GenerateNewId()}";

            // Act & Assert
            using (var scope = ScopedService.CreateScope())
            {
                var uow = scope.ServiceProvider.GetService<IUnitOfWork>();

                var user = await uow.Request(new CreateUserRequest
                {
                    Username = testUsername,
                    Email = $"{testUsername}@test.com",
                    Password = "Password123!".ToMd5(),
                    Nickname = "Test User",
                    IsEnable = true,
                    RoleIds = new List<string>(),
                    OrgCodes = new List<string>()
                });
                await uow.SaveChanges();

                Should.Throw<BusinessException>(() =>
                    user.ChangePassword("WrongPassword".ToMd5(), "NewPassword123!".ToMd5()));
            }
        }

        [Fact]
        public async Task EditUserServiceShouldWork()
        {
            // Arrange
            var testUsername = $"edituser_{ObjectId.GenerateNewId()}";
            var uniquePhoneNumber = $"155{ObjectId.GenerateNewId().ToString().Substring(0, 8)}";
            string userId;

            // Create user
            using (var setupScope = ScopedService.CreateScope())
            {
                var setupUow = setupScope.ServiceProvider.GetService<IUnitOfWork>();
                await setupUow.DbContext.Query<User>().Where(x => x.PhoneNumber == uniquePhoneNumber).DeleteAsync();

                var user = await setupUow.Request(new CreateUserRequest
                {
                    Username = testUsername,
                    Email = $"{testUsername}@test.com",
                    Password = "Password123!".ToMd5(),
                    Nickname = "Original Nickname",
                    IsEnable = true,
                    RoleIds = new List<string>(),
                    OrgCodes = new List<string>()
                });
                await setupUow.SaveChanges();
                userId = user.Id;
            }

            // Act - Edit user
            using (var editScope = ScopedService.CreateScope())
            {
                var editUow = editScope.ServiceProvider.GetService<IUnitOfWork>();

                var editRequest = new EditUserRequest
                {
                    Id = userId,
                    Nickname = "Updated Nickname",
                    Email = "updated@test.com",
                    PhoneNumber = uniquePhoneNumber,
                    IsEnable = false
                };

                var editedUser = await editUow.Request(editRequest);
                await editUow.SaveChanges();

                // Assert
                editedUser.Nickname.ShouldBe("Updated Nickname");
                editedUser.Email.ShouldBe("updated@test.com");
                editedUser.PhoneNumber.ShouldBe(uniquePhoneNumber);
                editedUser.IsEnable.ShouldBe(false);
            }
        }

        [Fact]
        public async Task DeleteUserServiceShouldWork()
        {
            // Arrange
            var testUsername = $"deleteuser_{ObjectId.GenerateNewId()}";
            string userId;

            // Create user
            using (var setupScope = ScopedService.CreateScope())
            {
                var setupUow = setupScope.ServiceProvider.GetService<IUnitOfWork>();

                var user = await setupUow.Request(new CreateUserRequest
                {
                    Username = testUsername,
                    Email = $"{testUsername}@test.com",
                    Password = "Password123!",
                    Nickname = "Test User",
                    IsEnable = true,
                    RoleIds = new List<string>(),
                    OrgCodes = new List<string>()
                });
                await setupUow.SaveChanges();
                userId = user.Id;
            }

            // Act - Delete user
            bool result;
            using (var deleteScope = ScopedService.CreateScope())
            {
                var deleteUow = deleteScope.ServiceProvider.GetService<IUnitOfWork>();

                var deleteRequest = new DeleteUserRequest { Id = userId };
                result = await deleteUow.Request(deleteRequest);
                await deleteUow.SaveChanges();
            }

            // Assert
            result.ShouldBe(true);
            using (var verifyScope = ScopedService.CreateScope())
            {
                var verifyUow = verifyScope.ServiceProvider.GetService<IUnitOfWork>();
                var deletedUser = verifyUow.Query<IUser>().FirstOrDefault(x => x.Id == userId);
                deletedUser.ShouldBeNull();
            }
        }

        [Fact]
        public async Task ResetUserPasswordServiceShouldWork()
        {
            // Arrange
            var testUsername = $"resetpwd_{ObjectId.GenerateNewId()}";
            var newPassword = "ResetPassword123!";
            string userId;

            // Create user
            using (var setupScope = ScopedService.CreateScope())
            {
                var setupUow = setupScope.ServiceProvider.GetService<IUnitOfWork>();

                var user = await setupUow.Request(new CreateUserRequest
                {
                    Username = testUsername,
                    Email = $"{testUsername}@test.com",
                    Password = "Password123!",
                    Nickname = "Test User",
                    IsEnable = true,
                    RoleIds = new List<string>(),
                    OrgCodes = new List<string>()
                });
                await setupUow.SaveChanges();
                userId = user.Id;
            }

            // Act
            using (var resetScope = ScopedService.CreateScope())
            {
                var resetUow = resetScope.ServiceProvider.GetService<IUnitOfWork>();

                var resetRequest = new ResetUserPasswordRequest
                {
                    UserId = userId,
                    Password = newPassword
                };

                var resetUser = await resetUow.Request(resetRequest);
                await resetUow.SaveChanges();

                // Assert
                resetUser.CheckPassword(newPassword).ShouldBe(true);
            }
        }

        [Fact]
        public async Task SetPasswordShouldWork()
        {
            // Arrange
            var testUsername = $"setpwd_{ObjectId.GenerateNewId()}";
            var newPassword = "NewTestPassword123!";

            // Act
            using (var scope = ScopedService.CreateScope())
            {
                var uow = scope.ServiceProvider.GetService<IUnitOfWork>();

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

                user.SetPassword(newPassword);
                await uow.SaveChanges();

                // Assert
                (user as User).Password.ShouldNotBe(newPassword);
                user.CheckPassword(newPassword).ShouldBe(true);
            }
        }
    }
}
