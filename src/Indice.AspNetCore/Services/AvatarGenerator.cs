using Indice.Extensions;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Indice.Services;

/// <summary>Generates an avatar based on a given name (first and last name) plus parameters</summary>
public class AvatarGenerator
{
    private readonly FontCollection _openSansFont;
    private readonly AvatarColor[] _backgroundColours;
    private readonly HashSet<int> _allowedSizes = new HashSet<int>() { 16, 24, 32, 48, 64, 128, 192, 256, 512 };

    /// <summary>Allowed tile sizes. Only these sizes are available.</summary>
    public ICollection<int> AllowedSizes => _allowedSizes;

    /// <summary>Constructor.</summary>
    /// <param name="options">Generator options to use</param>
    public AvatarGenerator(AvatarOptions options) {
        if (options?.Palette is null || options.Palette.Count == 0) {
            _backgroundColours = Create2026Palette();
        } else {
            _backgroundColours = options.Palette.ToArray();
        }
        if (options?.TileSizes?.Count > 0) {
            foreach (var size in options.TileSizes) {
                if (!_allowedSizes.Contains(size))
                    _allowedSizes.Add(size);
            }
        } 
        
        _openSansFont = new FontCollection();
        _openSansFont.Add(GetFontResourceStream("open-sans", "OpenSans-Regular.ttf"));
    }

    /// <summary>Image process and writes to <paramref name="output"/> <see cref="Stream"/>.</summary>
    /// <param name="output">The output <see cref="Stream"/> to write to.</param>
    /// <param name="firstName">First name to use.</param>
    /// <param name="lastName">Last name to use.</param>
    /// <param name="size">Image size.</param>
    /// <param name="contentType">Specifies the image content type. Defaults to image/webp</param>
    /// <param name="background">The background color to use.</param>
    /// <param name="foreground">The foreground color to use.</param>
    /// <param name="circular">Determines whether the tile will be circular or sqare. Defaults to false (sqare)</param>
    public void Generate(Stream output, string firstName, string? lastName, int size = 192, string contentType = "image/webp", string? background = null, string? foreground = null, bool circular = false) {
        var avatarText = string.Format("{0}{1}", firstName?.Length > 0 ? firstName[0] : ' ', lastName?.Length > 0 ? lastName[0] : ' ').ToUpper().RemoveDiacritics().Trim();
        if (int.TryParse(firstName, out var number) && string.IsNullOrWhiteSpace(lastName)) {
            avatarText = firstName;
        }
        var randomIndex = $"{firstName}{lastName}".ToCharArray().Sum(x => x) % _backgroundColours.Length;
        var accentColor = _backgroundColours[randomIndex];
        if (background != null) {
            accentColor = new AvatarColor(background, foreground);
        }
        using (var image = new Image<Rgba32>(size, size)) {
            // image center.
            var center = new PointF((float)image.Width / 2, (float)image.Height / 2);
            var backgroundBrush = new SolidBrush(accentColor.Background);
            if (circular) {
                image.Mutate(x => x.Paint(canvas => {
                    canvas.Clear(new SolidBrush(Color.Transparent));
                    canvas.FillEllipse(backgroundBrush, center, new SizeF(image.Width, image.Height));
                }));
            } else {
                image.Mutate(x => x.Paint(canvas => canvas.Fill(backgroundBrush)));
            }
            // For production application we would recomend you create a FontCollection singleton and manually install the ttf fonts yourself as using SystemFonts can be expensive and you risk font existing or not existing on a deployment by deployment basis.
            var font = _openSansFont.Get("Open Sans").CreateFont(70, FontStyle.Regular); // for scaling water mark size is largly ignored.
            // Measure the text size.
            var textSize = TextMeasurer.MeasureAdvance(avatarText, new TextOptions(font));
            // Find out how much we need to scale the text to fill the space (up or down).
            var scalingFactor = Math.Min(image.Width * 0.5f / textSize.Width, image.Height * 0.5f / textSize.Height);
            // Create a new font.
            var scaledFont = font.Family.CreateFont(scalingFactor * font.Size, FontStyle.Regular);
            var textOptions = new RichTextOptions(scaledFont) {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Origin = center
            };
            var textBrush = new SolidBrush(accentColor.Color);
            image.Mutate(x => x.Paint(canvas => canvas.DrawText(textOptions, avatarText, textBrush, null)));
            image.Save(output, contentType switch { 
                "image/jpeg" => JpegFormat.Instance, 
                "image/png" => PngFormat.Instance, 
                "image/webp" => WebpFormat.Instance, 
                _ => throw new Exception($"Unsupported ContentType {contentType}") });
        }
        output.Seek(0, SeekOrigin.Begin);
    }

    /// <summary>
    /// The classic Material Design palette (saturated 500 tones). This is the default palette used when <see cref="AvatarOptions.Palette"/> is empty.
    /// </summary>
    /// <remarks>https://www.materialpalette.com</remarks>
    public static AvatarColor[] CreateMaterialPalette() => [
        new AvatarColor("f44336", "ffffff"), // red
        new AvatarColor("e91e63", "ffffff"), // pink
        new AvatarColor("9c27b0", "ffffff"), // purple
        new AvatarColor("673ab7", "ffffff"), // deep-purple
        new AvatarColor("3f51b5", "ffffff"), // indigo
        new AvatarColor("2196f3", "ffffff"), // blue
        new AvatarColor("03a9f4", "ffffff"), // light-blue
        new AvatarColor("00bcd4", "ffffff"), // cyan
        new AvatarColor("009688", "ffffff"), // teal
        new AvatarColor("4caf50", "ffffff"), // green
        new AvatarColor("8bc34a", "ffffff"), // light-green
        new AvatarColor("cddc39", "000000"), // lime
        new AvatarColor("ffeb3b", "000000"), // yellow
        new AvatarColor("ffc107", "000000"), // amber
        new AvatarColor("ff9800", "000000"), // orange
        new AvatarColor("ff5722", "ffffff"), // deep-orange
        new AvatarColor("795548", "ffffff"), // brown
        new AvatarColor("bdbdbd", "000000"), // grey
        new AvatarColor("607d8b", "ffffff"), // blue-grey
    ];

    /// <summary>
    /// A modern tonal palette. Each entry pairs a soft tinted background with a deep foreground of the same hue,
    /// which reads calmer than saturated fills, sits well on both light and dark surfaces and keeps the initials above WCAG AA contrast.
    /// </summary>
    /// <example>
    /// <code>services.AddAvatars(options => options.Palette = AvatarGenerator.Create2026Palette());</code>
    /// </example>
    public static AvatarColor[] Create2026Palette() => [
        new AvatarColor("fecaca", "991b1b"), // red
        new AvatarColor("fed7aa", "9a3412"), // orange
        new AvatarColor("fde68a", "92400e"), // amber
        new AvatarColor("d9f99d", "3f6212"), // lime
        new AvatarColor("a7f3d0", "065f46"), // emerald
        new AvatarColor("99f6e4", "115e59"), // teal
        new AvatarColor("a5f3fc", "155e75"), // cyan
        new AvatarColor("bae6fd", "075985"), // sky
        new AvatarColor("bfdbfe", "1e40af"), // blue
        new AvatarColor("c7d2fe", "3730a3"), // indigo
        new AvatarColor("ddd6fe", "5b21b6"), // violet
        new AvatarColor("e9d5ff", "6b21a8"), // purple
        new AvatarColor("f5d0fe", "86198f"), // fuchsia
        new AvatarColor("fbcfe8", "9d174d"), // pink
        new AvatarColor("fecdd3", "9f1239"), // rose
        new AvatarColor("e2e8f0", "1e293b"), // slate
        new AvatarColor("e7e5e4", "292524"), // stone
    ];

    private static Stream GetFontResourceStream(string familyName, string fileName) => 
        typeof(AvatarGenerator).Assembly.GetManifestResourceStream($"Indice.AspNetCore.Fonts.{familyName.Replace('-', '_')}.{fileName}")!;
}

/// <summary>Internal class that represents a background, foreground pair of colors.</summary>
public class AvatarColor
{
    /// <summary>Creates an <see cref="AvatarColor"/> based on hex color strings for background and foreground.</summary>
    /// <param name="background">The background color.</param>
    /// <param name="color">The foreground color.</param>
    public AvatarColor(string background, string? color = null) {
        Background = SixLabors.ImageSharp.Color.ParseHex(background);
        if (!string.IsNullOrWhiteSpace(color)) {
            Color = SixLabors.ImageSharp.Color.ParseHex(color);
        } else {
            Color = PerceivedBrightness(Background) > 130 ? SixLabors.ImageSharp.Color.Black : SixLabors.ImageSharp.Color.White;
        }
    }

    /// <summary>The background color.</summary>
    public SixLabors.ImageSharp.Color Background { get; }
    /// <summary>The foreground color.</summary>
    public SixLabors.ImageSharp.Color Color { get; }

    private static int PerceivedBrightness(SixLabors.ImageSharp.Color color) {
        var pixel = color.ToPixel<Rgba32>();
        return (int)Math.Sqrt(((double)pixel.R * pixel.R * .299) + ((double)pixel.G * pixel.G * .587) + ((double)pixel.B * pixel.B * .114));
    }
}


/// <summary>Avatar feature Options. Parameters including palette colours as available sizes.</summary>
public class AvatarOptions
{
    /// <summary>The color palette to use. List of randomly used colors</summary>
    public ICollection<AvatarColor> Palette { get; set; } = new List<AvatarColor>();

    /// <summary>Additional valid tile sizes. Only these sizes are available.</summary>
    public ICollection<int> TileSizes { get; set; } = new HashSet<int>();
}
