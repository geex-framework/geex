using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Entities;
using Geex.Bms.Demo.Core.Aggregates.books;
using Geex.Common.Settings.Core;
using Geex.Common.Settings.Abstraction;
using SharpCompress.Readers;

namespace Geex.Bms.Demo.Core.Migrations
{
    public class _20230517103815_init : DbMigration
    {
        public override async Task UpgradeAsync(DbContext dbContext)
        {
            
            var readers = new List<Reader>(){
                new Reader("张三","男","1993-12-14","111"),
                new Reader("李四","男","1994-12-14","222"),
                new Reader("王五","男","1995-12-14","333"),
                new Reader("赵六","男","1996-12-14","444"),
            };
            dbContext.Attach(readers);

            var bookCategory = new List<BookCategory>(){
              new BookCategory("文学类") { Describe = "包括小说、散文、诗歌、剧本等。"},
              new BookCategory("社科类") { Describe = "包括社会学等。"},
              new BookCategory("科技类"),
              new BookCategory("生活类"),
              new BookCategory("教育类"),
            };
            dbContext.Attach(bookCategory);

            var bookList = new List<Book>(){
              new Book("如何阅读一本书","","莫提默·J. 艾德勒，查尔斯·范多伦","商务印书馆",DateTimeOffset.Now.AddYears(-12), "BN111", bookCategory[4]),
              new Book("如何阅读一本书","","莫提默·J. 艾德勒，查尔斯·范多伦","商务印书馆",DateTimeOffset.Now.AddYears(-12), "BN111", bookCategory[4]),
              new Book("如何阅读一本书","","莫提默·J. 艾德勒，查尔斯·范多伦","商务印书馆",DateTimeOffset.Now.AddYears(-12), "BN111", bookCategory[4]),

              new Book("高效阅读","","梁实秋","中国青年出版社",DateTimeOffset.Now.AddYears(-10), "BN222", bookCategory[4]),
              new Book("高效阅读","","梁实秋","中国青年出版社",DateTimeOffset.Now.AddYears(-10), "BN222", bookCategory[4]),
              new Book("高效阅读","","梁实秋","中国青年出版社",DateTimeOffset.Now.AddYears(-10), "BN222", bookCategory[4]),

              new Book("如何高效学习","","芭芭拉·奥克利","中信出版社",DateTimeOffset.Now.AddYears(-8), "BN333", bookCategory[4]),
              new Book("如何高效学习","","芭芭拉·奥克利","中信出版社",DateTimeOffset.Now.AddYears(-8), "BN333", bookCategory[4]),

              new Book("沟通的艺术","","卡耐基","人民邮电出版社",DateTimeOffset.Now.AddYears(-6), "BN444", bookCategory[4]),

              new Book("硅谷钢铁侠","","埃隆·马斯克","中信出版社",DateTimeOffset.Now.AddYears(-10), "BN555", bookCategory[1]),
              new Book("硅谷钢铁侠","","埃隆·马斯克","中信出版社",DateTimeOffset.Now.AddYears(-10), "BN555", bookCategory[1]),
              new Book("硅谷钢铁侠","","埃隆·马斯克","中信出版社",DateTimeOffset.Now.AddYears(-10), "BN555", bookCategory[1]),
              new Book("硅谷钢铁侠","","埃隆·马斯克","中信出版社",DateTimeOffset.Now.AddYears(-10), "BN555", bookCategory[1]),

              new Book("黑客与画家","","保罗·格雷厄姆","中国电力出版社",DateTimeOffset.Now.AddYears(-12), "BN666", bookCategory[1]),
              new Book("黑客与画家","","保罗·格雷厄姆","中国电力出版社",DateTimeOffset.Now.AddYears(-12), "BN666", bookCategory[1]),
              new Book("黑客与画家","","保罗·格雷厄姆","中国电力出版社",DateTimeOffset.Now.AddYears(-12), "BN666", bookCategory[1]),

              new Book("浪潮之巅","","吴军","文化发展出版社",DateTimeOffset.Now.AddYears(-9), "BN777", bookCategory[1]),
            };
            dbContext.Attach(bookList);


             var borrowRecordList = new List<BorrowRecord>(){ 
             
                 new BorrowRecord(readers[0], bookList[0]),
                 new BorrowRecord(readers[0], bookList[4]),
                 new BorrowRecord(readers[0], bookList[6]),
                 new BorrowRecord(readers[1], bookList[2]),
                 new BorrowRecord(readers[1], bookList[8]),
                 new BorrowRecord(readers[2], bookList[10]),
             };

             dbContext.Attach(borrowRecordList);

            await dbContext.SaveChanges();
        }
    }
}
