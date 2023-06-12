using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Geex.Bms.Demo.Core.Aggregates.books;
using MediatR;

namespace Geex.Bms.Demo.Core.GqlSchemas.BorrowRecords.Inputs
{
    public class CreateBorrowRecordInput: IRequest<BorrowRecord>
    {
        public string UserPhone { get; set; }
        public string BookISBN { get; set; }
    }
}
