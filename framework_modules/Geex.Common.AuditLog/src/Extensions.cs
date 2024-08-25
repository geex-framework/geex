using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Geex.Common.Authorization;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Types;
using Microsoft.Extensions.Logging;

namespace Geex.Common.AuditLogs
{
    public static class Extensions
    {
        public static IObjectTypeDescriptor<T> AuditFieldsImplicitly<T>(this IObjectTypeDescriptor<T> descriptor) where T : class
        {
            var propertyList = descriptor.GetFields();
            foreach (var item in propertyList)
            {
                item.Audit();
            }
            return descriptor;
        }

        public static IObjectFieldDescriptor Audit(this IObjectFieldDescriptor fieldDescriptor)
        {
            fieldDescriptor = fieldDescriptor.Directive<AuditDirectiveType>();
            return fieldDescriptor;
        }
    }
}
