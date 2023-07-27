using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HotChocolate.Types;

namespace Geex.Common.Abstraction
{
    public static class GqlConfig
    {
        public abstract class Interface<T> : InterfaceType<T>
        {
        }
        public abstract class Object<T> : ObjectType<T>
        {
        }
        public abstract class Input<T> : InputObjectType<T>
        {

        }
    }
}
