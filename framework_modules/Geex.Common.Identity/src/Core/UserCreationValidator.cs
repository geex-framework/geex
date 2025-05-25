using MongoDB.Entities;

namespace Geex.Common.Identity.Core
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
