using Geex.Common.Identity.Api.Aggregates.Users;
using MongoDB.Entities;

namespace Geex.Common.Identity.Core.Aggregates.Users
{
    public class UserCreationValidator : IUserCreationValidator
    {
        public DbContext DbContext { get; set; }

        public UserCreationValidator(DbContext dbContext)
        {
            this.DbContext = dbContext;
        }
    }
}
