﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Driver;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MongoDB.Entities.Tests
{
    [TestClass]
    public class Templates
    {
        [TestMethod]
        public void missing_tags_throws()
        {
            var template = new Template(@"[
            {
              $lookup: {
                from: 'users',
                let: { user_id: '$<user_id>' },
                pipeline: [
                  { $match: {
                      $expr: {
                        $and: [ { $eq: [ '$_id', '$$<user_id>' ] },
                                { $eq: [ '$city', '<cityname>' ] }]}}}],
                as: 'user'
              }
            },
            {
              $match: {
                $expr: { $gt: [ { <size>: '<user>' }, 0 ] }
              }
            }]").Tag("size", "$size")
                .Tag("user", "$user")
                .Tag("missing", "blah");

            Assert.ThrowsException<InvalidOperationException>(() => template.ToString());
        }

        [TestMethod]
        public void extra_tags_throws()
        {
            var template = new Template(@"[
            {
              $lookup: {
                from: 'users',
                let: { user_id: '$<user_id>' },
                pipeline: [
                  { $match: {
                      $expr: {
                        $and: [ { $eq: [ '$_id', '$$<user_id>' ] },
                                { $eq: [ '$city', '<cityname>' ] }]}}}],
                as: 'user'
              }
            },
            {
              $match: {
                $expr: { $gt: [ { <size>: '<user>' }, 0 ] }
              }
            }]").Tag("size", "$size")
                .Tag("user", "$user");

            Assert.ThrowsException<InvalidOperationException>(() => template.ToString());
        }

        [TestMethod]
        public void tag_replacement_works()
        {
            var template = new Template(@"
            {
               $match: { '<OtherAuthors.Name>': /<search_term>/is }
            }")

            .Path<Book>(b => b.OtherAuthors[0].Name)
            .Tag("search_term", "Eckhart Tolle");

            const string expectation = @"
            {
               $match: { 'OtherAuthors.Name': /Eckhart Tolle/is }
            }";

            Assert.AreEqual(expectation, template.ToString());
        }

        [TestMethod]
        public void tag_replacement_works_for_collection()
        {
            var template = new Template<Author>(@"
            {
               $match: { '<Book>': /search_term/is }
            }")
            .Collection<Book>();

            const string expectation = @"
            {
               $match: { 'Book': /search_term/is }
            }";

            Assert.AreEqual(expectation, template.ToString());
        }

        [TestMethod]
        public void tag_replacement_works_for_property()
        {
            var template = new Template<Book, Author>(@"
            {
               $match: { '<Name>': /search_term/is }
            }")
            .Property(b => b.OtherAuthors[0].Name);

            const string expectation = @"
            {
               $match: { 'Name': /search_term/is }
            }";

            Assert.AreEqual(expectation, template.ToString());
        }

        [TestMethod]
        public void tag_replacement_with_new_expression()
        {
            var template = new Template(@"
            {
               $match: { 
                    '<OtherAuthors.Name>': /search_term/is,
                    '<OtherAuthors.Age2>: 55',
                    '<ReviewList.Books.Review>: null'
                }
            }")
            .Paths<Book>(b => new
            {
                b.OtherAuthors[0].Name,
                b.OtherAuthors[1].Age2,
                b.ReviewList[1].Books[1].Review
            });

            const string expectation = @"
            {
               $match: { 
                    'OtherAuthors.Name': /search_term/is,
                    'OtherAuthors.Age2: 55',
                    'ReviewList.Books.Review: null'
                }
            }";

            Assert.AreEqual(expectation, template.ToString());
        }

        [TestMethod]
        public async Task tag_replacement_with_db_aggregate()
        {
            var guid = Guid.NewGuid().ToString();
            var author1 = new Author { Name = guid, Age = 54 };
            var author2 = new Author { Name = guid, Age = 53 };
            await DB.SaveAsync(new[] { author1, author2 });

            var pipeline = new Template<Author>(@"
            [
                {
                  $match: { <Name>: '<author_name>' }
                },
                {
                  $sort: { <Age>: 1 }
                }
            ]")
                .Path(a => a.Name)
                .Tag("author_name", guid)
                .Path(a => a.Age);

            var results = await (await DB.PipelineCursorAsync(pipeline)).ToListAsync();

            Assert.AreEqual(2, results.Count);
            Assert.IsTrue(results[0].Name == guid);
            Assert.IsTrue(results.Last().Age == 54);
        }
    }
}
