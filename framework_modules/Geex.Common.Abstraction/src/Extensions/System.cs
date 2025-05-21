using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Geex.Common.Abstraction;

namespace System
{
    public static class SystemExtensions
    {

        /// <summary>
        /// 用于强制触发属性成员调用
        /// </summary>
        public static object? Call(this object value, MethodInfo method, Type[] typeArguments,
            object[] arguments = null)
        {
            return method.MakeGenericMethod(typeArguments).Invoke(value, arguments);
        }

        /// <summary>
        /// 反射获取lazy值(带缓存)
        /// </summary>
        public static object? Call(this object value, MethodInfo method, object[] arguments = null)
        {
            return method.Invoke(value, arguments);
        }

        public static string ToShortDateString(this DateTimeOffset value)
        {
            return value.ToLocalTime().ToString("yyyy-MM-dd");
        }

        public static async Task<string> Compress(this string s)
        {
            byte[] data = Encoding.UTF8.GetBytes(s);
            using (var input = new MemoryStream(data))
            using (var output = new MemoryStream())
            {
                using (var gzip = new GZipStream(output, CompressionMode.Compress, true))
                {
                    await input.CopyToAsync(gzip);
                }

                return Convert.ToBase64String(output.ToArray());
            }
        }

        public static async Task<string> Decompress(this string s)
        {
            byte[] data = Convert.FromBase64String(s);
            using (var input = new MemoryStream(data))
            using (var gzip = new GZipStream(input, CompressionMode.Decompress))
            using (var output = new MemoryStream())
            {
                await gzip.CopyToAsync(output);
                return Encoding.UTF8.GetString(output.ToArray());
            }
        }

        /// <summary>
        /// generic exception model
        /// </summary>
        public static ExceptionModel? ToExceptionModel(this Exception value)
        {
            return new ExceptionModel()
            {
                ExceptionType = value.GetType().ToString(),
                Message = value.Message,
                Source = value.Source,
                Data = value.Data
            };
        }
    }
}
