using MongoDB.Entities;

namespace Geex.Extensions.Identity.Core
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
