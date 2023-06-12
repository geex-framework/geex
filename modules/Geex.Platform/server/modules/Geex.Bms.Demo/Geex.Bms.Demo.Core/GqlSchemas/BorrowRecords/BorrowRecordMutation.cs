using System;
using System.Linq;
using System.Threading.Tasks;
using Geex.Common.Abstraction.Auditing;
using Geex.Common.Abstraction.Gql.Types;
using MongoDB.Entities;
using Geex.Bms.Demo.Core.Aggregates.books;
using Geex.Bms.Demo.Core.GqlSchemas.Books.Inputs;
using Geex.Common.Abstractions;
using Geex.Bms.Demo.Core.GqlSchemas.Books;
using Geex.Bms.Demo.Core.GqlSchemas.BorrowRecords.Inputs;
using Geex.Common.Settings.Abstraction;
using Geex.Common.Settings.Api.Aggregates.Settings.Inputs;
using MediatR;
using Geex.Bms.Demo.Core;

namespace Geex.Bms.Demo.Books.Core.Operations.Abstractions
{
    /// <summary>
    /// Book相关操作
    /// </summary>
    public class BorrowRecordMutation : MutationExtension<BorrowRecordMutation>
    {
        private readonly DbContext _dbContext;
        private readonly IMediator _mediator;
        public BorrowRecordMutation(DbContext dbContext, IMediator mediator)
        {
            this._dbContext = dbContext;
            this._mediator = mediator;
        }


        /// <summary>
        /// 创建BorrowRecord
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public async Task<bool> CreateBorrowRecord(CreateBorrowRecordInput input)
        {

            var entityBook = _dbContext.Queryable<Book>().FirstOrDefault(x => x.ISBN == input.BookISBN);
            if (entityBook == null)
            {
                throw new BusinessException($"Book未找到:{input.BookISBN}");
            }
            var entityReaders = _dbContext.Queryable<Reader>().FirstOrDefault(x => x.Phone == input.UserPhone);
            if (entityReaders == null)
            {
                throw new BusinessException($"未找到借阅人:{input.UserPhone}");
            }

            var settings = (await _mediator.Send(new GetSettingsInput(SettingScopeEnumeration.Global, DemoSettings.MaxBorrowingQtySettings))).FirstOrDefault()!.Value.GetValue<int>();

            var currentNotReturnCount = _dbContext.Queryable<BorrowRecord>().Count(x => x.ReaderId == entityReaders.Id && x.ReturnDate == null);

            if (currentNotReturnCount >= settings)
            {
                throw new Exception($"超过最大借阅数量{settings}，请先归还图书后在借阅.");
            }

            var order = new BorrowRecord(entityReaders, entityBook);
            _dbContext.Attach(order);

            return true;
        }

        /// <summary>
        /// 归还BorrowRecord
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public async Task<bool> EditBorrowRecord(EditBorrowRecordInput input)
        {
            var entityBook = _dbContext.Queryable<Book>().FirstOrDefault(x => x.ISBN == input.BookISBN);
            if (entityBook == null)
            {
                throw new BusinessException($"Book未找到:{input.BookISBN}");
            }
            var entityReaders = _dbContext.Queryable<Reader>().FirstOrDefault(x => x.Phone == input.UserPhone);
            if (entityReaders == null)
            {
                throw new BusinessException($"未找到借阅人:{input.UserPhone}");
            }

            var entity = _dbContext.Queryable<BorrowRecord>().FirstOrDefault(x => x.BookId == entityBook.Id && x.ReaderId == entityReaders.Id);
            if (entity == null)
            {
                throw new BusinessException($"BorrowRecord未找到:{entityReaders.Id} - {entityBook.Id}");
            }
            entity.BookReturn();
            return true;
        }
    }
}
