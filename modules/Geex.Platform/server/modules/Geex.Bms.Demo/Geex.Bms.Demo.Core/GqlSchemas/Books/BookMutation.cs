using System.Linq;
using System.Threading.Tasks;

using Geex.Common.Abstraction.Auditing;
using Geex.Common.Abstraction.Gql.Types;

using MongoDB.Entities;

using Geex.Bms.Demo.Core.Aggregates.books;
using Geex.Bms.Demo.Core.GqlSchemas.Books.Inputs;
using Geex.Common.Abstraction.Gql.Inputs;
using Geex.Common.Abstractions;
using MediatR;

namespace Geex.Bms.Demo.Core.GqlSchemas.Books
{
    public class BookMutation : MutationExtension<BookMutation>, IHasAuditMutation<Book>
    {
        private readonly DbContext _dbContext;

        public BookMutation(DbContext dbContext)
        {

            _dbContext = dbContext;
        }
        /// <summary>
        /// 创建Book
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public async Task<Book> CreateBook(
            CreateBookInput input)
        {
            var entityBookCategory = _dbContext.Queryable<BookCategory>()
                .FirstOrDefault(x => x.Id == input.BookCategoryId);

            var entity = new Book(input.Name, input.Cover, input.Author, input.Press, input.PublicationDate, input.ISBN, entityBookCategory);

            return _dbContext.Attach(entity);
        }

        /// <summary>
        /// 编辑Book
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public async Task<bool> EditBook(string id, EditBookInput input)
        {
            var entity = _dbContext.Queryable<Book>().FirstOrDefault(x => x.Id == id);
            if (entity == null)
            {
                throw new BusinessException($"Book未找到:{id}");
            }
            if (input.Name != default)
            {
                entity.Name = input.Name;
            }

            if (input.Cover != default)
            {
                entity.Cover = input.Cover;
            }

            if (input.Author != default)
            {
                entity.Author = input.Author;
            }

            if (input.Press != default)
            {
                entity.Press = input.Press;
            }

            if (input.PublicationDate != default)
            {
                entity.PublicationDate = input.PublicationDate;
            }

            if (input.ISBN != default)
            {
                entity.ISBN = input.ISBN;
            }

            if (input.BookCategoryId != default)
            {
                var entityBookCategory = _dbContext.Queryable<Aggregates.books.BookCategory>()
                    .FirstOrDefault(x => x.Id == input.BookCategoryId);

                entity.SetBookCategory(entityBookCategory);
            }

            return true;
        }

        /// <summary>
        /// 删除Book
        /// </summary>
        /// <param name="ids"></param>
        /// <returns></returns>
        public async Task<bool> DeleteBook(string[] ids)
        {
            await _dbContext.DeleteAsync<Book>(x => ids.Contains(x.Id));
            return true;
        }
    }
}
