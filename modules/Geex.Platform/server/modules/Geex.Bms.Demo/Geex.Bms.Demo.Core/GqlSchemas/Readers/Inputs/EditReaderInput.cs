using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Geex.Bms.Demo.Core.Aggregates.books;
using MediatR;

namespace Geex.Bms.Demo.Core.GqlSchemas.Readers.Inputs
{
    public class EditReaderInput: IRequest<Unit>
    {

        public string Name { get; set; }
        public string Gender { get; set; }
        public string BirthDate { get; set; }
        public string Phone { get; set; }
    }
}
