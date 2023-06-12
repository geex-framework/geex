using System.Linq;
using System.Threading.Tasks;
using Geex.Bms.Demo.Core.Aggregates.books;
using Geex.Bms.Demo.Core.GqlSchemas.BookCategorys.Inputs;
using Geex.Common.Abstraction.Gql.Types;
using Geex.Common.Abstractions;
using MongoDB.Entities;

namespace Geex.Bms.Demo.Core.GqlSchemas.BookCategorys
{
    /// <summary>
    /// Book相关操作
    /// </summary>
    public class BookCategoryMutation  : MutationExtension<BookCategoryMutation>
    {
        private readonly DbContext _dbContext;

        public BookCategoryMutation(DbContext dbContext)
        {
            this._dbContext = dbContext;
        }


        /// <summary>
        /// 创建Book
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public async Task<BookCategory> CreateBookCategory(CreateBookCategoryInput input)
        {
            var entity = new BookCategory(input.Name);
            entity.Describe = input.Describe;
            return _dbContext.Attach(entity);
        }

        /// <summary>
        /// 编辑Book
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public async Task<bool> EditBookCategory(string id,EditBookCategoryInput input)
        {
            var entity = _dbContext.Queryable<BookCategory>().GetById(id);
            if (entity == null)
            {
                throw new BusinessException($"BookCategory未找到:{id}");
            }
            entity.Name = input.Name;
            entity.Describe = input.Describe;
            return true;
        }

        /// <summary>
        /// 删除Book
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public async Task<bool> DeleteBookCategory(string[] ids)
        {
            await _dbContext.DeleteAsync<BookCategory>(x => ids.Contains(x.Id));
            return true;
        }
    }
}
