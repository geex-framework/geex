using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Geex.Common.Abstraction.Storage;

namespace Geex.Bms.Demo.Core.Aggregates.books
{
    public class Reader: Entity<Reader>
    {

        #region Methods 方法
        public Reader(string name,string gender,string birthDate,string phone):this()
        {
            Name = name;
            Gender = gender;
            BirthDate = birthDate;
            Phone = phone;
        }

        private Reader()
        {
            BorrowRecords = new ResettableLazy<List<BorrowRecord>>(() =>
                DbContext.Queryable<BorrowRecord>().Where(x => x.ReaderId == this.Id).ToList());
        }

        #endregion

        #region Properties 属性
        public string Name { get; set; }
        public string Gender { get; set; }
        public string BirthDate { get; set; }
        public string Phone { get; set; }

        public virtual ResettableLazy<List<BorrowRecord>> BorrowRecords { get; }
    }
        #endregion


}
