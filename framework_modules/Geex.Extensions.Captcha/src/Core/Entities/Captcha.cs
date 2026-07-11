using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using HotChocolate.Types;
using MongoDB.Bson;
using Volo.Abp;

namespace Geex.Extensions.Captcha.Core.Entities;

public enum CaptchaType
{
    Number,
    English,
    NumberAndLetter,
    Chinese,
}

public class SmsCaptcha : Captcha;

public abstract class Captcha
{
    public string Code { get; init; } = string.Empty;
    public CaptchaType CaptchaType { get; init; }

    protected Captcha(string code, string key)
    {
        Code = code;
        Key = key;
    }

    protected Captcha(CaptchaType captchaType = CaptchaType.Number, int captchaLength = 4)
    {
        Key = ObjectId.GenerateNewId().ToString();
        CaptchaType = captchaType;
        Code = captchaType switch
        {
            CaptchaType.English => GetRandomLetters(captchaLength),
            CaptchaType.NumberAndLetter => GetRandomNumsAndLetters(captchaLength),
            CaptchaType.Chinese => GetRandomHanzis(captchaLength),
            _ => GetRandomNums(captchaLength),
        };
    }

    public string Key { get; init; } = string.Empty;

    protected static string GetRandomNums(int length)
    {
        var numArray = new int[length];
        for (var index = 0; index < length; ++index)
        {
            numArray[index] = RandomHelper.GetRandom(0, 9);
        }

        return numArray.AsEnumerable().Select(x => x.ToString()).JoinAsString("");
    }

    protected string GetRandomLetters(int length)
    {
        var strArray = "a,b,c,d,e,f,g,h,i,j,k,l,m,n,p,q,r,s,t,u,v,w,x,y,z,A,B,C,D,E,F,G,H,I,J,K,L,M,N,P,P,Q,R,S,T,U,V,W,X,Y,Z".Split(',');
        var str = "";
        var num = -1;
        var random = new Random();
        for (var index1 = 1; index1 < length + 1; ++index1)
        {
            if (num != -1)
            {
                random = new Random(index1 * num * (int)DateTime.Now.Ticks);
            }

            var index2 = random.Next(strArray.Length);
            if (num != -1 && num == index2)
            {
                return GetRandomLetters(length);
            }

            num = index2;
            str += strArray[index2];
        }

        return str;
    }

    protected string GetRandomNumsAndLetters(int length)
    {
        var strArray = "0,1,2,3,4,5,6,7,8,9,a,b,c,d,e,f,g,h,i,j,k,l,m,n,p,q,r,s,t,u,v,w,x,y,z,A,B,C,D,E,F,G,H,I,J,K,L,M,N,P,P,Q,R,S,T,U,V,W,X,Y,Z".Split(',');
        var str = "";
        var num = -1;
        var random = new Random();
        for (var index1 = 1; index1 < length + 1; ++index1)
        {
            if (num != -1)
            {
                random = new Random(index1 * num * (int)DateTime.Now.Ticks);
            }

            var index2 = random.Next(61);
            if (num != -1 && num == index2)
            {
                return GetRandomNumsAndLetters(length);
            }

            num = index2;
            str += strArray[index2];
        }

        return str;
    }

    protected static string GetRandomHanzis(int length)
    {
        var strArray = "0,1,2,3,4,5,6,7,8,9,a,b,c,d,e,f".Split(',');
        var encoding = Encoding.GetEncoding("GB2312");
        string? str1 = null;
        for (var index1 = 0; index1 < length; ++index1)
        {
            var random1 = RandomHelper.GetRandom(11, 14);
            var str2 = strArray[random1];
            var index2 = random1 == 13 ? RandomHelper.GetRandom(0, 7) : RandomHelper.GetRandom(0, 16);
            var str3 = strArray[index2];
            var random2 = RandomHelper.GetRandom(10, 16);
            var str4 = strArray[random2];
            var random3 = random2 switch
            {
                10 => RandomHelper.GetRandom(1, 16),
                15 => RandomHelper.GetRandom(0, 15),
                _ => RandomHelper.GetRandom(0, 16),
            };
            var index3 = random3;
            var str5 = strArray[index3];
            var bytes = new byte[2]
            {
                Convert.ToByte(str2 + str3, 16),
                Convert.ToByte(str4 + str5, 16),
            };
            str1 += encoding.GetString(bytes);
        }

        return str1!;
    }

    public class CaptchaGqlType : GqlConfig.Object<Captcha>
    {
        protected override void Configure(IObjectTypeDescriptor<Captcha> descriptor)
        {
            descriptor.BindFieldsExplicitly();
            descriptor.Field(x => x.CaptchaType);
            descriptor.Field(x => x.Key);
            descriptor.Field((ImageCaptcha x) => x.Bitmap).Use(next => async context =>
            {
                await next(context);
                if (context.Result is MemoryStream stream)
                {
                    context.Result = Convert.ToBase64String(stream.ToArray());
                }
            }).Type<StringType>();
            base.Configure(descriptor);
        }
    }
}
