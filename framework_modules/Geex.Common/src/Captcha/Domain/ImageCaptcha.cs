using System;
using System.IO;
using System.Linq;
using Geex.Extensions.Messaging.Requests;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Geex.Common.Captcha.Domain
{
    public class ImageCaptcha : Captcha
    {
        [Newtonsoft.Json.JsonIgnore]
        [System.Text.Json.Serialization.JsonIgnore]
        public MemoryStream Bitmap => CreateCaptchaBitmap(Code);

        private static MemoryStream CreateCaptchaBitmap(string code)
        {
            const int width = 120;
            const int height = 40;
            using var image = new Image<Rgba32>(width, height);
            image.Mutate(ctx =>
            {
                ctx.BackgroundColor(Color.White);
                var font = SystemFonts.CreateFont(SystemFonts.Families.First().Name, 24, FontStyle.Bold);
                var random = new Random();
                for (var i = 0; i < 6; i++)
                {
                    var x1 = random.Next(width);
                    var y1 = random.Next(height);
                    var x2 = random.Next(width);
                    var y2 = random.Next(height);
                    ctx.DrawLine(Color.LightGray, 1f, new PointF(x1, y1), new PointF(x2, y2));
                }

                ctx.DrawText(code, font, Color.Black, new PointF(10, 6));
            });

            var stream = new MemoryStream();
            image.SaveAsPng(stream);
            stream.Position = 0;
            return stream;
        }
    }
}
