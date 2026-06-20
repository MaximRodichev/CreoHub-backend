using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using IoPath = System.IO.Path;

namespace CreoHub.Infrastructure.Persistence.Services;

public sealed record ProductOgData(
    string Name,
    string Price,                       // уже отформатировано, напр. "$12"
    IReadOnlyList<string> Features,
    string SellerName);

/// <summary>
/// Генерация og:image карточки товара на бренд-шаблоне (повторяет ImageMagick-прототип v7).
/// Чистый ImageSharp, без нативных зависимостей. Координаты — для холста 2560×1344.
/// </summary>
public static class ProductOgRenderer
{
    private const int   CanvasW = 2560, CanvasH = 1344;
    private const float RightEdge = 2465f;     // правый край колонки (right-align)
    private const float ColWidth  = 1180f;
    private const int   PreviewX = 130, PreviewY = 135, PreviewSize = 1040, PreviewRadius = 72;
    private const float PreviewRight = PreviewX + PreviewSize;   // 1170 — правый край превью
    private const int   MaxFeatures = 5;

    // Выход: рендерим в 2560 (чёткий текст), отдаём даунскейл до соц-размера + JPEG.
    // ~180–230 КБ вместо ~2 МБ PNG, в превью ленты/мессенджера неотличимо.
    private const int OutputW = 1600, OutputH = 840;
    private const int JpegQuality = 88;

    public static byte[] Render(string assetsDir, Stream? preview, Stream? sellerLogo, ProductOgData data)
    {
        var dark = Color.ParseHex("0d1b2e");
        var blue = Color.ParseHex("1a7dff");

        var fonts = new FontCollection();
        var f800  = fonts.Add(IoPath.Combine(assetsDir, "Unbounded-800.ttf"));
        var f700  = fonts.Add(IoPath.Combine(assetsDir, "Unbounded-700.ttf"));
        var f600  = fonts.Add(IoPath.Combine(assetsDir, "Unbounded-600.ttf"));

        var fTitle       = f800.CreateFont(76);
        var fPrice       = f800.CreateFont(86);
        var fFeat        = f700.CreateFont(60);
        var fSeller      = f800.CreateFont(50);
        var fSellerLabel = f600.CreateFont(32);

        using var canvas = Image.Load<Rgba32>(IoPath.Combine(assetsDir, "template.png"));
        if (canvas.Width != CanvasW || canvas.Height != CanvasH)
            canvas.Mutate(c => c.Resize(CanvasW, CanvasH));

        // ── Превью: квадрат cover-crop со скруглением ──
        if (preview is not null)
        {
            using var prev = Image.Load<Rgba32>(preview);
            prev.Mutate(c => c.Resize(new ResizeOptions
            {
                Size = new Size(PreviewSize, PreviewSize),
                Mode = ResizeMode.Crop,
                Position = AnchorPositionMode.Center,
            }));
            ApplyRoundedCorners(prev, PreviewRadius);
            canvas.Mutate(c => c.DrawImage(prev, new Point(PreviewX, PreviewY), 1f));
        }

        // ── Правый столбец (right-align): название → цена → фичи ──
        float y = 120f;

        var titleOpts = new RichTextOptions(fTitle)
        {
            Origin = new PointF(RightEdge, y),
            WrappingLength = ColWidth,
            HorizontalAlignment = HorizontalAlignment.Right,
            TextAlignment = TextAlignment.End,
            LineSpacing = 0.82f,
            VerticalAlignment = VerticalAlignment.Top,
        };
        canvas.Mutate(c => c.DrawText(titleOpts, data.Name, dark));
        y += TextMeasurer.MeasureSize(data.Name, titleOpts).Height + 36;

        var priceOpts = new RichTextOptions(fPrice)
        {
            Origin = new PointF(RightEdge, y),
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Top,
        };
        canvas.Mutate(c => c.DrawText(priceOpts, data.Price, blue));
        y += TextMeasurer.MeasureSize(data.Price, priceOpts).Height + 60;

        var feats = data.Features.Where(s => !string.IsNullOrWhiteSpace(s)).ToList();
        var featText = feats.Count > MaxFeatures
            ? string.Join("\n", feats.Take(MaxFeatures)) + "\nи так далее"
            : string.Join("\n", feats);
        if (featText.Length > 0)
        {
            var featOpts = new RichTextOptions(fFeat)
            {
                Origin = new PointF(RightEdge, y),
                WrappingLength = ColWidth,
                HorizontalAlignment = HorizontalAlignment.Right,
                TextAlignment = TextAlignment.End,
                LineSpacing = 1.0f,
                VerticalAlignment = VerticalAlignment.Top,
            };
            canvas.Mutate(c => c.DrawText(featOpts, featText, dark));
        }

        // ── Лого продавца (снизу-слева) ──
        if (sellerLogo is not null)
        {
            using var logo = Image.Load<Rgba32>(sellerLogo);
            logo.Mutate(c => c.Resize(new ResizeOptions { Size = new Size(120, 120), Mode = ResizeMode.Max }));
            canvas.Mutate(c => c.DrawImage(logo, new Point(130, 1200), 1f));
        }

        // ── Продавец: текст по правому краю превью ──
        float sy = 1212f;
        var sLabel = new RichTextOptions(fSellerLabel)
        {
            Origin = new PointF(PreviewRight, sy),
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Top,
        };
        canvas.Mutate(c => c.DrawText(sLabel, "Продавец", dark));
        sy += TextMeasurer.MeasureSize("Продавец", sLabel).Height + 4;
        var sName = new RichTextOptions(fSeller)
        {
            Origin = new PointF(PreviewRight, sy),
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Top,
        };
        canvas.Mutate(c => c.DrawText(sName, data.SellerName, dark));

        canvas.Mutate(c => c.Resize(OutputW, OutputH));
        using var ms = new MemoryStream();
        canvas.SaveAsJpeg(ms, new JpegEncoder { Quality = JpegQuality });
        return ms.ToArray();
    }

    // ── Скругление углов (каноничный рецепт ImageSharp.Drawing) ──
    private static void ApplyRoundedCorners(Image<Rgba32> img, float radius)
    {
        var corners = BuildCorners(img.Width, img.Height, radius);
        img.Mutate(c =>
        {
            c.SetGraphicsOptions(new GraphicsOptions
            {
                Antialias = true,
                AlphaCompositionMode = PixelAlphaCompositionMode.DestOut,
            });
            foreach (var p in corners)
                c.Fill(Color.Black, p);
        });
    }

    private static PathCollection BuildCorners(int w, int h, float radius)
    {
        var square = new RectangularPolygon(-0.5f, -0.5f, radius, radius);
        IPath cornerTL = square.Clip(new EllipsePolygon(radius - 0.5f, radius - 0.5f, radius));

        float rightPos  = w - cornerTL.Bounds.Width + 1;
        float bottomPos = h - cornerTL.Bounds.Height + 1;

        IPath cornerTR = cornerTL.RotateDegree(90).Translate(rightPos, 0);
        IPath cornerBR = cornerTL.RotateDegree(180).Translate(rightPos, bottomPos);
        IPath cornerBL = cornerTL.RotateDegree(270).Translate(0, bottomPos);

        return new PathCollection(cornerTL, cornerTR, cornerBR, cornerBL);
    }
}
