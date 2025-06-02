using System.IO;
using System.Text;

// ReSharper disable once CheckNamespace
namespace System.Security.Cryptography
{
    internal static class SystemSecurityCryptographyExtensions
    {
        static readonly MD5 _md5 = MD5CryptoServiceProvider.Create();

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
            var bytes = _md5.ComputeHash(stream);
            stream.Position = 0;
            var sb = new StringBuilder();
            foreach (var b in bytes)
            {
                sb.Append(b.ToString("x2"));
            }
            string hashString = sb.ToString();
            return hashString;
        }
    }
}
