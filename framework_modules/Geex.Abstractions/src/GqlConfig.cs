﻿using HotChocolate.Types;

namespace Geex
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
        public abstract class Directive<T> : DirectiveType<T> where T : class
        {

        }
    }
}
