using System;
using System.IO;
using System.Linq;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Geex.Extensions.Captcha.Core;

public class ImageCaptcha : Captcha
{
    private static readonly string[] PreferredFontFamilies =
    [
        "Arial",
        "DejaVu Sans",
        "Liberation Sans",
        "FreeSans",
        "Noto Sans",
        "Segoe UI",
        "Helvetica"
    ];

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
            var font = ResolveCaptchaFont(24);
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

    private static Font ResolveCaptchaFont(float size)
    {
        foreach (var name in PreferredFontFamilies)
        {
            if (SystemFonts.TryGet(name, out var family))
            {
                return family.CreateFont(size, FontStyle.Bold);
            }
        }

        var fallback = SystemFonts.Families.FirstOrDefault();
        if (!Equals(fallback, default(FontFamily)))
        {
            return fallback.CreateFont(size, FontStyle.Bold);
        }

        throw new BusinessException(
            GeexExceptionType.OnPurpose,
            message: "No system fonts available for image captcha generation.");
    }
}
