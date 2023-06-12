using System;
using System.Collections.Generic;
using System.Linq;

using Geex.Bms.Demo.Core.Aggregates.Books.Enum;
using Geex.Common.Abstraction.Auditing;
using Geex.Common.Abstraction.Entities;
using Geex.Common.Abstraction.Gql.Inputs;
using Geex.Common.Abstraction.Storage;

using MediatR;

using Microsoft.Extensions.DependencyInjection;

namespace Geex.Bms.Demo.Core.Aggregates.books
{

    /// <summary>
    /// this is a aggregate root of this module, we name it the same as the module feel free to change it to its real name
    /// </summary>

    // * 继承Entity：包含主键ID、创建时间、修改时间
    public class Book : Entity<Book>, IAuditEntity
    {
        public Book(string name, string cover, string author, string press, DateTimeOffset publicationDate, string iSBN, BookCategory bookCategory)  : this()
        {
            Name = name;
            Cover = cover;
            Author = author;
            Press = press;
            PublicationDate = publicationDate;
            ISBN = iSBN;


            SetBookCategory(bookCategory);
            BookStatus = BookStatusEnum.Available;
        }

        private Book()
        {
         
            BookCategory = new ResettableLazy<BookCategory?>(() => DbContext.Queryable<BookCategory>().GetById(this.BookCategoryId));
            BorrowRecords = new ResettableLazy<List<BorrowRecord>>(() => DbContext.Queryable<BorrowRecord>().Where(x => x.BookId == this.Id).ToList());
        }



        public void SetBookCategory(BookCategory bookCategory)
        {
            
            if (bookCategory == null)
            {
                throw new Exception("图书分类不存在.");
            }

            BookCategoryId = bookCategory.Id;

        }


        public void Lending()
        {
            if (BookStatus == BookStatusEnum.OnLoan)
            {
                throw new Exception("图书已经被借出.");
            }
            BookStatus = BookStatusEnum.OnLoan;
        }

        public void Return()
        {
            BookStatus = BookStatusEnum.Available;
        }


        public string Name { get; set; }

        public string Cover { get; set; }
        // * 文件支持：DB、OSS存储使用方式只需要引入IBlobObject实体：原来是单体项目分模块，走MeditR，之前是为了实现一个模块化的，跨模块调用所以引入了MeditR，但是MeditR会增加项目复杂度，

        public virtual IBlobObject? Attachments => ServiceProvider.GetService<IMediator>().Send(new QueryInput<IBlobObject>()).Result.FirstOrDefault(x => x.Id == Cover);
        
        public string Author { get; set; }
        public string Press { get; set; }
        // * 时间类型使用DateTimeOffset：这是C# 12目前比较推荐使用，比DateTime功能更加完善
        public DateTimeOffset PublicationDate { get; set; }
        public string ISBN { get; set; }
        public virtual string BookCategoryId { get; set; }
        // * 使用 ResettableLazy 包裹实体：在业务中，有些字段是非常昂贵的消耗性能，我们可以显示的去标记成ResettableLazy，这样在使用的时候手动去value使用。ResettableLazy 允许您在需要时计算值，然后缓存该值以便将来重复使用，同时还可以通过调用 Reset() 方法来清除缓存并重新计算值

        public virtual ResettableLazy<BookCategory?> BookCategory { get; }

        public BookStatusEnum BookStatus { get; set; }
        public virtual ResettableLazy<List<BorrowRecord>> BorrowRecords { get; } 

        /// <inheritdoc />
        public AuditStatus AuditStatus { get; set; }

        /// <inheritdoc />
        public string? AuditRemark { get; set; }

        /// <inheritdoc />
        public bool Submittable { get; }
    }
}
