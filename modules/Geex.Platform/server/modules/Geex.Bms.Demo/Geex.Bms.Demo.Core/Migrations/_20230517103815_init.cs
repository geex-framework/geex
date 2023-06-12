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
                new Reader("����","��","1993-12-14","111"),
                new Reader("����","��","1994-12-14","222"),
                new Reader("����","��","1995-12-14","333"),
                new Reader("����","��","1996-12-14","444"),
            };
            dbContext.Attach(readers);

            var bookCategory = new List<BookCategory>(){
              new BookCategory("��ѧ��") { Describe = "����С˵��ɢ�ġ�ʫ�衢�籾�ȡ�"},
              new BookCategory("�����") { Describe = "�������ѧ�ȡ�"},
              new BookCategory("�Ƽ���"),
              new BookCategory("������"),
              new BookCategory("������"),
            };
            dbContext.Attach(bookCategory);

            var bookList = new List<Book>(){
              new Book("����Ķ�һ����","","Ī��Ĭ��J. �����գ����˹��������","����ӡ���",DateTimeOffset.Now.AddYears(-12), "BN111", bookCategory[4]),
              new Book("����Ķ�һ����","","Ī��Ĭ��J. �����գ����˹��������","����ӡ���",DateTimeOffset.Now.AddYears(-12), "BN111", bookCategory[4]),
              new Book("����Ķ�һ����","","Ī��Ĭ��J. �����գ����˹��������","����ӡ���",DateTimeOffset.Now.AddYears(-12), "BN111", bookCategory[4]),

              new Book("��Ч�Ķ�","","��ʵ��","�й����������",DateTimeOffset.Now.AddYears(-10), "BN222", bookCategory[4]),
              new Book("��Ч�Ķ�","","��ʵ��","�й����������",DateTimeOffset.Now.AddYears(-10), "BN222", bookCategory[4]),
              new Book("��Ч�Ķ�","","��ʵ��","�й����������",DateTimeOffset.Now.AddYears(-10), "BN222", bookCategory[4]),

              new Book("��θ�Чѧϰ","","�Ű������¿���","���ų�����",DateTimeOffset.Now.AddYears(-8), "BN333", bookCategory[4]),
              new Book("��θ�Чѧϰ","","�Ű������¿���","���ų�����",DateTimeOffset.Now.AddYears(-8), "BN333", bookCategory[4]),

              new Book("��ͨ������","","���ͻ�","�����ʵ������",DateTimeOffset.Now.AddYears(-6), "BN444", bookCategory[4]),

              new Book("��ȸ�����","","��¡����˹��","���ų�����",DateTimeOffset.Now.AddYears(-10), "BN555", bookCategory[1]),
              new Book("��ȸ�����","","��¡����˹��","���ų�����",DateTimeOffset.Now.AddYears(-10), "BN555", bookCategory[1]),
              new Book("��ȸ�����","","��¡����˹��","���ų�����",DateTimeOffset.Now.AddYears(-10), "BN555", bookCategory[1]),
              new Book("��ȸ�����","","��¡����˹��","���ų�����",DateTimeOffset.Now.AddYears(-10), "BN555", bookCategory[1]),

              new Book("�ڿ��뻭��","","���ޡ����׶�ķ","�й�����������",DateTimeOffset.Now.AddYears(-12), "BN666", bookCategory[1]),
              new Book("�ڿ��뻭��","","���ޡ����׶�ķ","�й�����������",DateTimeOffset.Now.AddYears(-12), "BN666", bookCategory[1]),
              new Book("�ڿ��뻭��","","���ޡ����׶�ķ","�й�����������",DateTimeOffset.Now.AddYears(-12), "BN666", bookCategory[1]),

              new Book("�˳�֮��","","���","�Ļ���չ������",DateTimeOffset.Now.AddYears(-9), "BN777", bookCategory[1]),
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
