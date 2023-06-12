using System.Linq;
using System.Threading.Tasks;
using Geex.Common.Abstraction.Auditing;
using Geex.Common.Abstraction.Gql.Types;
using MongoDB.Entities;
using Geex.Bms.Demo.Core.Aggregates.books;
using Geex.Bms.Demo.Core.GqlSchemas.Books.Inputs;
using Geex.Bms.Demo.Core.GqlSchemas.Readers.Inputs;
using Geex.Common.Abstractions;

namespace Geex.Bms.Demo.Books.Core.Operations.Abstractions
{
    /// <summary>
    /// Book相关操作
    /// </summary>
    public class ReaderMutation: MutationExtension<ReaderMutation>
    {
        private readonly DbContext _dbContext;
   

        public ReaderMutation(DbContext dbContext)
        {
            this._dbContext = dbContext;

        }

        /// <summary>
        /// 创建Book
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public async Task<Reader> CreateReader(CreateReaderInput input)
        {

            var entity = new Reader(input.Name,input.Gender,input.BirthDate,input.Phone);

            return _dbContext.Attach(entity);
        }

        /// <summary>
        /// 编辑Book
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public async Task<bool> EditReader(string id,EditReaderInput input)
        {
            var entity = _dbContext.Queryable<Reader>().GetById(id);
            if (entity == null)
            {
                throw new BusinessException($"Readers未找到:{id}");
            }
            entity.Name = input.Name;
            entity.Gender = input.Gender;
            entity.BirthDate = input.BirthDate;
            entity.Phone = input.Phone;
            return true;
        }
    }
}
