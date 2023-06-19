using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// ReSharper disable once CheckNamespace
namespace System.Security.Cryptography
{
    public static class SystemSecurityCryptographyExtensions
    {
        static readonly MD5CryptoServiceProvider _md5 = new MD5CryptoServiceProvider();

        /// <summary>
        /// 使用MD5加密字符串
        /// </summary>
        /// <param name="str">待加密的字符</param>
        /// <returns></returns>
        public static string Md5(this string str)
        {
            if (string.IsNullOrEmpty(str))
            {
                return string.Empty;
            }
            byte[] arr = UTF8Encoding.Default.GetBytes(str);
            byte[] bytes = _md5.ComputeHash(arr);
            var md5 = BitConverter.ToString(bytes);
            return md5;
        }

        /// <summary>
        /// 使用MD5加密字符串
        /// </summary>
        /// <returns></returns>
        public static string Md5(this MemoryStream stream)
        {
            if (stream.Position != 0)
            {
                throw new Exception("stream must position at 0!");
            }
            byte[] bytes = _md5.ComputeHash(stream);
            stream.Position = 0;
            var md5 = BitConverter.ToString(bytes);
            return md5;
        }
    }
}
