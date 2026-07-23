using System;
using System.IO;
using System.Linq;
using MongoDB.Bson;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Geex.Extensions.Captcha.Core;

public class ImageCaptcha : Captcha
{
    private const int DefaultLength = 5;
    /// <summary>Digits + uppercase letters, excluding ambiguous 0/O/1/I/L.</summary>
    private const string UnambiguousCharset = "23456789ABCDEFGHJKLMNPQRSTUVWXYZ";

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

    public ImageCaptcha()
        : base(GenerateImageCode(DefaultLength), ObjectId.GenerateNewId().ToString())
    {
        CaptchaType = CaptchaType.NumberAndLetter;
    }

    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public MemoryStream Bitmap => CreateCaptchaBitmap(Code);

    private static string GenerateImageCode(int length)
    {
        var chars = new char[length];
        for (var i = 0; i < length; i++)
        {
            chars[i] = UnambiguousCharset[Random.Shared.Next(UnambiguousCharset.Length)];
        }

        return new string(chars);
    }

    /// <summary>HSV -> RGB. h in [0,360), s/v in [0,1].</summary>
    private static Color ColorFromHsv(double h, double s, double v)
    {
        h = (h % 360 + 360) % 360;
        var c = v * s;
        var x = c * (1 - Math.Abs(h / 60 % 2 - 1));
        var m = v - c;
        double r1, g1, b1;
        if (h < 60) { r1 = c; g1 = x; b1 = 0; }
        else if (h < 120) { r1 = x; g1 = c; b1 = 0; }
        else if (h < 180) { r1 = 0; g1 = c; b1 = x; }
        else if (h < 240) { r1 = 0; g1 = x; b1 = c; }
        else if (h < 300) { r1 = x; g1 = 0; b1 = c; }
        else { r1 = c; g1 = 0; b1 = x; }

        return Color.FromRgb(
            (byte)Math.Clamp((int)((r1 + m) * 255), 0, 255),
            (byte)Math.Clamp((int)((g1 + m) * 255), 0, 255),
            (byte)Math.Clamp((int)((b1 + m) * 255), 0, 255));
    }

    private static Color RandomBackground(Random random)
        => ColorFromHsv(random.NextDouble() * 360, 0.08 + random.NextDouble() * 0.12, 0.92 + random.NextDouble() * 0.08);

    private static Color RandomTextColor(Random random)
        => ColorFromHsv(random.NextDouble() * 360, 0.55 + random.NextDouble() * 0.4, 0.35 + random.NextDouble() * 0.35);

    private static Color RandomNoiseColor(Random random)
        => ColorFromHsv(random.NextDouble() * 360, 0.25 + random.NextDouble() * 0.55, 0.45 + random.NextDouble() * 0.4);

    private static MemoryStream CreateCaptchaBitmap(string code)
    {
        const int width = 150;
        const int height = 44;
        using var image = new Image<Rgba32>(width, height);
        var random = new Random(HashCode.Combine(code));
        var background = RandomBackground(random);

        image.Mutate(ctx =>
        {
            ctx.BackgroundColor(background);

            // Soft background speckles
            for (var i = 0; i < 80; i++)
            {
                var x = random.Next(width);
                var y = random.Next(height);
                ctx.Fill(RandomNoiseColor(random), new RectangleF(x, y, 1.5f, 1.5f));
            }

            // Interference lines
            for (var i = 0; i < 10; i++)
            {
                var thickness = 0.8f + (float)random.NextDouble() * 1.4f;
                ctx.DrawLine(
                    RandomNoiseColor(random),
                    thickness,
                    new PointF(random.Next(width), random.Next(height)),
                    new PointF(random.Next(width), random.Next(height)));
            }

            // Bezier-like wavy polyline
            var wavePoints = Enumerable.Range(0, 6)
                .Select(i => new PointF(i * (width / 5f), random.Next(8, height - 8)))
                .ToArray();
            for (var i = 0; i < wavePoints.Length - 1; i++)
            {
                ctx.DrawLine(RandomNoiseColor(random), 1.2f, wavePoints[i], wavePoints[i + 1]);
            }

            var font = ResolveCaptchaFont(22);
            var charWidth = (width - 16f) / Math.Max(code.Length, 1);
            for (var i = 0; i < code.Length; i++)
            {
                var ch = code[i].ToString();
                var angle = (float)(random.NextDouble() * 36 - 18);
                var offsetX = 8 + i * charWidth + (float)(random.NextDouble() * 4 - 2);
                var offsetY = 8 + (float)(random.NextDouble() * 8 - 2);
                var color = RandomTextColor(random);

                ctx.SetDrawingTransform(
                    Matrix3x2Extensions.CreateRotationDegrees(angle, new PointF(offsetX + 8, offsetY + 12)));
                ctx.DrawText(ch, font, color, new PointF(offsetX, offsetY));
                ctx.SetDrawingTransform(System.Numerics.Matrix3x2.Identity);
            }

            // Foreground speckles over text
            for (var i = 0; i < 40; i++)
            {
                ctx.Fill(RandomNoiseColor(random), new RectangleF(random.Next(width), random.Next(height), 1.2f, 1.2f));
            }
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
