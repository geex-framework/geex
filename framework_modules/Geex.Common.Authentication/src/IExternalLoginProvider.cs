using System.Threading.Tasks;
using Geex.Common.Abstraction;
using Geex.Common.Abstraction.Entities;

namespace Geex.Common.Authentication
{
    public interface IExternalLoginProvider
    {
        /// <summary>
        /// 登陆provider枚举
        /// </summary>
        public LoginProviderEnum Provider { get; }
        /// <summary>
        /// 外部登陆逻辑code=>user
        /// </summary>
        /// <param name="code"></param>
        /// <returns></returns>
        public Task<IUser> ExternalLogin(string code);
    }

}
