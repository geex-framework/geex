using System;

namespace MediatX
{
    public class InvalidHandlerException : Exception
    {
        public InvalidHandlerException()
        {

        }
        public InvalidHandlerException(string message) : base(message)
        {
        }
    }
}
