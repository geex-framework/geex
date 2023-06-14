﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq.Expressions;

namespace MongoDB.Entities.Tests
{
    [TestClass]
    public class Props
    {
        [TestMethod]
        public void prop_dotted()
        {
            Expression<Func<Book, object>> exp = x => x.ReviewList[0].Rating;
            var res = exp.FullPath();
            Assert.AreEqual("ReviewList.Rating", res);

            Expression<Func<Book, object>> exp1 = x => x.ReviewArray[0].Books[0].ReviewArray[0].Books[0].ModifiedOn;
            var res1 = exp1.FullPath();
            Assert.AreEqual("ReviewArray.Books.ReviewArray.Books.ModifiedOn", res1);

            Expression<Func<Book, object>> exp2 = x => x.ReviewArray[0].Books[0].Price;
            var res2 = exp2.FullPath();
            Assert.AreEqual("ReviewArray.Books.Price", res2);

            Expression<Func<Book, object>> exp3 = x => x.ReviewArray[0].Books[0].PriceInt;
            var res3 = exp3.FullPath();
            Assert.AreEqual("ReviewArray.Books.PriceInt", res3);

            Expression<Func<Book, object>> exp4 = x => x.ReviewArray[0].Books[0].PriceLong;
            var res4 = exp4.FullPath();
            Assert.AreEqual("ReviewArray.Books.PriceLong", res4);

            Expression<Func<Book, object>> exp5 = x => x.ReviewArray[0].Books[0].PriceDbl;
            var res5 = exp5.FullPath();
            Assert.AreEqual("ReviewArray.Books.PriceDbl", res5);

            Expression<Func<Book, object>> exp6 = x => x.ReviewArray[0].Books[0].PriceFloat;
            var res6 = exp6.FullPath();
            Assert.AreEqual("ReviewArray.Books.PriceFloat", res6);
        }

        [TestMethod]
        public void prop_name()
        {
            Expression<Func<Book, object>> exp = x => x.ReviewList[0].Rating;
            var res = Prop.Property(exp);
            Assert.AreEqual("Rating", res);

            Expression<Func<Book, object>> exp1 = x => x.ReviewArray[0].Books[0].ReviewArray[0].Books[0].ModifiedOn;
            var res1 = Prop.Property(exp1);
            Assert.AreEqual("ModifiedOn", res1);

            Expression<Func<Book, object>> exp2 = x => x.ReviewArray[0].Books[0].Price;
            var res2 = Prop.Property(exp2);
            Assert.AreEqual("Price", res2);

            Expression<Func<Book, object>> exp3 = x => x.ReviewArray[0].Books[0].PriceInt;
            var res3 = Prop.Property(exp3);
            Assert.AreEqual("PriceInt", res3);

            Expression<Func<Book, object>> exp4 = x => x.ReviewArray[0].Books[0].PriceLong;
            var res4 = Prop.Property(exp4);
            Assert.AreEqual("PriceLong", res4);

            Expression<Func<Book, object>> exp5 = x => x.ReviewArray[0].Books[0].PriceDbl;
            var res5 = Prop.Property(exp5);
            Assert.AreEqual("PriceDbl", res5);

            Expression<Func<Book, object>> exp6 = x => x.ReviewArray[0].Books[0].PriceFloat;
            var res6 = Prop.Property(exp6);
            Assert.AreEqual("PriceFloat", res6);
        }

        [TestMethod]
        public void prop_pos_filtered()
        {
            var res1 = Prop.PosFiltered<Book>(b => b.ReviewArray[0].Books[1].MainAuthor.Id);
            Assert.AreEqual("ReviewArray.$[a].Books.$[b].MainAuthor._id", res1);

            var res2 = Prop.PosFiltered<Book>(b => b.ReviewList[0].Rating);
            Assert.AreEqual("ReviewList.$[a].Rating", res2);
        }

        [TestMethod]
        public void prop_pos_all()
        {
            var res1 = Prop.PosAll<Book>(b => b.ReviewArray[0].Rating);
            Assert.AreEqual("ReviewArray.$[].Rating", res1);

            var res2 = Prop.PosAll<Book>(b => b.ReviewList[0].Rating);
            Assert.AreEqual("ReviewList.$[].Rating", res2);
        }

        [TestMethod]
        public void prop_pos()
        {
            var res1 = Prop.PosFirst<Book>(b => b.ReviewArray[0].Rating);
            Assert.AreEqual("ReviewArray.$.Rating", res1);

            var res2 = Prop.PosFirst<Book>(b => b.ReviewList[0].Rating);
            Assert.AreEqual("ReviewList.$.Rating", res2);
        }

        [TestMethod]
        public void prop_elements_root()
        {
            var res = Prop.Elements<Book>(b => b.Tags);
            Assert.AreEqual("Tags", res);
        }

        [TestMethod]
        public void prop_elements_nested()
        {
            var res = Prop.Elements<Book>(0, b => b.Tags);
            Assert.AreEqual("a.Tags", res);

            var res2 = Prop.Elements<Book>(1, b => b.ReviewList[0].Rating);
            Assert.AreEqual("b.ReviewList.Rating", res2);
        }
    }
}
