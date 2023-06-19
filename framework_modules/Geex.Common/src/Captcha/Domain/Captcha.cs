using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Volo.Abp;

namespace Geex.Common.Captcha.Domain
{
    public enum CaptchaType
    {
        Number,
        English,
        NumberAndLetter,
        Chinese,
    }

    public class SmsCaptcha : Captcha
    {
        
    }

    public class ImageCaptcha : Captcha
    {
        [JsonIgnore]
        public MemoryStream Bitmap => CreateCaptchaBitmap(Code);
        private static MemoryStream CreateCaptchaBitmap(string code)
        {
            Random random = new Random();
            Color[] colorArray = new Color[8]
            {
                Color.Black,
                Color.Red,
                Color.DarkBlue,
                Color.Green,
                Color.Orange,
                Color.Brown,
                Color.DarkCyan,
                Color.Purple
            };
            string[] strArray = new string[5]
            {
                "Verdana",
                "Microsoft Sans Serif",
                "Comic Sans MS",
                "Arial",
                "宋体"
            };
            Bitmap bitmap = new Bitmap(code.Length * 18, 32);
            Graphics graphics = Graphics.FromImage((Image)bitmap);
            graphics.Clear(Color.White);
            for (int index = 0; index < 100; ++index)
            {
                int x = random.Next(bitmap.Width);
                int y = random.Next(bitmap.Height);
                graphics.DrawRectangle(new Pen(Color.LightGray, 0.0f), x, y, 1, 1);
            }

            for (int startIndex = 0; startIndex < code.Length; ++startIndex)
            {
                int index1 = random.Next(7);
                int index2 = random.Next(5);
                Font font = new Font(strArray[index2], 15f, FontStyle.Bold);
                Brush brush = (Brush)new SolidBrush(colorArray[index1]);
                int num = 4;
                if ((startIndex + 1) % 2 == 0)
                    num = 2;
                graphics.DrawString(code.Substring(startIndex, 1), font, brush, (float)(3 + startIndex * 12), (float)num);
            }

            MemoryStream memoryStream = new MemoryStream();
            bitmap.Save((Stream)memoryStream, ImageFormat.Png);
            graphics.Dispose();
            bitmap.Dispose();
            return memoryStream;
        }
    }

    public abstract class Captcha
    {
        public string Code { get; init; }
        public CaptchaType CaptchaType { get; init; }

        protected Captcha(string code, string key)
        {
            Code = code;
            Key = key;
        }
        protected Captcha(CaptchaType captchaType = CaptchaType.Number, int captchaLength = 4)
        {
            Key = Guid.NewGuid().ToString();
            CaptchaType = captchaType;
            switch (captchaType)
            {
                case CaptchaType.English:
                    Code = this.GetRandomLetters(captchaLength);
                    break;
                case CaptchaType.NumberAndLetter:
                    Code = this.GetRandomNumsAndLetters(captchaLength);
                    break;
                case CaptchaType.Chinese:
                    Code = Captcha.GetRandomHanzis(captchaLength);
                    break;
                default:
                    Code = Captcha.GetRandomNums(captchaLength);
                    break;
            }
        }

        public string Key { get; init; }


        protected static string GetRandomNums(int length)
        {
            int[] numArray = new int[length];
            for (int index = 0; index < length; ++index)
                numArray[index] = RandomHelper.GetRandom(0, 9);
            return numArray.AsEnumerable().Select(x => x.ToString()).JoinAsString("");
        }

        protected string GetRandomLetters(int length)
        {
            string[] strArray = "a,b,c,d,e,f,g,h,i,j,k,l,m,n,p,q,r,s,t,u,v,w,x,y,z,A,B,C,D,E,F,G,H,I,J,K,L,M,N,P,P,Q,R,S,T,U,V,W,X,Y,Z".Split(',');
            string str = "";
            int num = -1;
            Random random = new Random();
            for (int index1 = 1; index1 < length + 1; ++index1)
            {
                if (num != -1)
                    random = new Random(index1 * num * (int)DateTime.Now.Ticks);
                int index2 = random.Next(strArray.Length);
                if (num != -1 && num == index2)
                    return this.GetRandomLetters(length);
                num = index2;
                str += strArray[index2];
            }
            return str;
        }

        protected string GetRandomNumsAndLetters(int length)
        {
            string[] strArray = "0,1,2,3,4,5,6,7,8,9,a,b,c,d,e,f,g,h,i,j,k,l,m,n,p,q,r,s,t,u,v,w,x,y,z,A,B,C,D,E,F,G,H,I,J,K,L,M,N,P,P,Q,R,S,T,U,V,W,X,Y,Z".Split(',');
            string str = "";
            int num = -1;
            Random random = new Random();
            for (int index1 = 1; index1 < length + 1; ++index1)
            {
                if (num != -1)
                    random = new Random(index1 * num * (int)DateTime.Now.Ticks);
                int index2 = random.Next(61);
                if (num != -1 && num == index2)
                    return this.GetRandomNumsAndLetters(length);
                num = index2;
                str += strArray[index2];
            }
            return str;
        }

        protected static string GetRandomHanzis(int length)
        {
            string[] strArray = "0,1,2,3,4,5,6,7,8,9,a,b,c,d,e,f".Split(',');
            Encoding encoding = Encoding.GetEncoding("GB2312");
            string str1 = (string)null;
            for (int index1 = 0; index1 < length; ++index1)
            {
                int random1 = RandomHelper.GetRandom(11, 14);
                string str2 = strArray[random1];
                int index2 = random1 == 13 ? RandomHelper.GetRandom(0, 7) : RandomHelper.GetRandom(0, 16);
                string str3 = strArray[index2];
                int random2 = RandomHelper.GetRandom(10, 16);
                string str4 = strArray[random2];
                int random3;
                switch (random2)
                {
                    case 10:
                        random3 = RandomHelper.GetRandom(1, 16);
                        break;
                    case 15:
                        random3 = RandomHelper.GetRandom(0, 15);
                        break;
                    default:
                        random3 = RandomHelper.GetRandom(0, 16);
                        break;
                }
                int index3 = random3;
                string str5 = strArray[index3];
                byte[] bytes = new byte[2]
                {
          Convert.ToByte(str2 + str3, 16),
          Convert.ToByte(str4 + str5, 16)
                };
                str1 += encoding.GetString(bytes);
            }
            return str1;
        }
    }

}
