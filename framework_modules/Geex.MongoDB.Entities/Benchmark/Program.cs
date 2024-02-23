using MongoDB.Driver;
using MongoDB.Entities;

using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Benchmark
{
    public class Author : EntityBase<Author>
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public Date Birthday { get; set; }
        public IQueryable<Book> Books => DbContext.Query<Book>();
    }

    public class Book : EntityBase<Book>
    {
        public string Title { get; set; }
        public string AuthorId { get; set; }
        public Date PublishedOn { get; set; }
    }

    public static class Program
    {
        private static readonly ConcurrentBag<byte> booksCreated = new ConcurrentBag<byte>();
        private static readonly ConcurrentBag<byte> authorsCreated = new ConcurrentBag<byte>();
        private const int authorCount = 100;
        private const int booksPerAuthor = 1000;
        private const int concurrentTasks = 4;

        private static async Task Main()
        {
            await DB.InitAsync("benchmark-mongodb-entities");

            Console.WriteLine("creating books and authors...");
            Console.WriteLine();

            var sw = new Stopwatch();
            sw.Start();

            var range = Enumerable.Range(1, authorCount);
            var result = Parallel.ForEach(range, new ParallelOptions { MaxDegreeOfParallelism = concurrentTasks }, number =>
            {
                var author = new Author
                {
                    FirstName = "first name " + number.ToString(),
                    LastName = "last name " + number.ToString(),
                    Birthday = DateTime.UtcNow
                };
                author.SaveAsync().GetAwaiter().GetResult();
                authorsCreated.Add(0);
                var books = new ConcurrentBag<Book>();

                for (int i = 1; i <= booksPerAuthor; i++)
                {
                    var book = new Book();
                    book.Id = default;
                    book.Title = $"author {number} - book {i}";
                    book.PublishedOn = DateTime.UtcNow;
                    book.AuthorId = author.Id;
                    books.Add(book);
                    booksCreated.Add(0);

                    Console.Write($"\rauthors: {authorsCreated.Count} | books: {booksCreated.Count}                    ");

                }
                books.SaveAsync().GetAwaiter().GetResult();
            });

            while (authorsCreated.Count != authorCount)
            {
                Thread.Sleep(100);
            }
            Console.WriteLine();
            Console.WriteLine($"done in {sw.Elapsed:hh':'mm':'ss}");


            sw.Restart();
            var dbContext = new DbContext();
            var author = dbContext.Query<Author>()
                           .FirstOrDefault(a => a.FirstName == "first name 66" && a.LastName == "last name 66");

            Console.WriteLine();
            Console.WriteLine($"found author 66 by name in [{sw.Elapsed.TotalMilliseconds:0}ms] with an un-indexed query - his id: {author.Id}");
            Console.WriteLine();


            sw.Restart();
            author = await dbContext.Query<Author>()
                       .OneAsync(author.Id);

            Console.WriteLine();
            Console.WriteLine($"looking up author 66 by Id took [{sw.Elapsed.TotalMilliseconds:0}ms]");
            Console.WriteLine();


            sw.Restart();
            var book555 = author.Books
                            .Where(b => b.Title == "author 66 - book 55")
                            .ToList()
                            .FirstOrDefault();

            Console.WriteLine();
            Console.WriteLine($"found book 55 of author 66 by title in [{sw.Elapsed.TotalMilliseconds:0}ms] - title field is not indexed");
            Console.WriteLine();


            Console.WriteLine();
            Console.WriteLine("creating index for book title...");
            sw.Restart();
            var indexTask = DB.Index<Book>()
                              .Key(b => b.Title, KeyType.Ascending)
                              .Option(o => o.Background = false)
                              .CreateAsync();

            while (!indexTask.IsCompleted)
            {
                Console.Write($"\rindexing time: {sw.Elapsed.TotalSeconds:0} seconds");
                Task.Delay(1000).Wait();
            }
            Console.WriteLine();
            Console.WriteLine("indexing done!");
            Console.WriteLine();


            sw.Restart();
            book555 = author.Books
                            .Where(b => b.Title == "author 66 - book 55")
                            .ToList()
                            .FirstOrDefault();

            Console.WriteLine();
            Console.WriteLine($"found book 55 of author 66 by title in [{sw.Elapsed.TotalMilliseconds:0}ms] - title field is indexed");
            Console.WriteLine();


            sw.Restart();
            var authorIds = dbContext.Query<Book>()
                            .Where(b => b.Title == "author 99 - book 99" ||
                                        b.Title == "author 33 - book 33")
                            .Select(b => b.AuthorId)
                            .ToList();

            Console.WriteLine();
            Console.WriteLine($"fetched 2 book Ids by title in [{sw.Elapsed.TotalMilliseconds:0}ms] - title field is indexed");
            Console.WriteLine();


            sw.Restart();
            var parents = dbContext.Query<Author>().Where(x => authorIds.Contains(x.Id))
                            .ToArray();

            Console.WriteLine();
            Console.WriteLine($"reverse relationship access finished in [{sw.Elapsed.TotalMilliseconds:0}ms]");
            Console.WriteLine();
            Console.WriteLine("the following authors were returned:");
            Console.WriteLine();
            foreach (var a in parents)
            {
                Console.WriteLine($"name: {a.FirstName} {a.LastName}");
            }
            Console.WriteLine();
            Console.WriteLine("press a key to continnue...");
            Console.ReadLine();

            _ = DB.Collection<Book>().Indexes.DropAllAsync();
        }
    }
}
