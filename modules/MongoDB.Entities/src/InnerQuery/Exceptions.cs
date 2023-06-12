// The MIT License (MIT)
// 
// Copyright (c) 2015 Pathmatics, Inc
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

namespace MongoDB.Entities.InnerQuery
{
    /// <summary>Exception thrown to indicate a failure to translate or execute the query</summary>
    public class InvalidQueryException : System.Exception
    {
        public InvalidQueryException(string message)
        {
            Message = message;
        }

        public override string Message { get; }
    }


    /// <summary>Exception thrown to indicate an internal failure - likely a bug :(</summary>
    public class MongoLinqPlusPlusInternalException: System.Exception
    {
        public MongoLinqPlusPlusInternalException(string message)
        {
            Message = message;
        }

        public override string Message { get; }
    }
}