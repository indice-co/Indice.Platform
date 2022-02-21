using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Indice.Extensions;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.Primitives;
using SixLabors.Shapes;

namespace Indice.Services
{
    /// <summary>
    /// Generates an avatar based on a given name (first and last name) plus parameters
    /// </summary>
    public class AvatarGenerator
    {
        private readonly FontCollection _openSansFont;
        private readonly AvatarColor[] _backgroundColours;
        private readonly HashSet<int> _allowedSizes = new HashSet<int>() { 16, 24, 32, 48, 64, 128, 192, 256, 512 };

        /// <summary>
        /// Allowed tile sizes. Only these sizes are available.
        /// </summary>
        public ICollection<int> AllowedSizes => _allowedSizes;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="options">Generator options to use</param>
        public AvatarGenerator(AvatarOptions options) {
            // https://www.materialpalette.com
            if (options?.Palette is null || options.Palette.Count == 0) {
                _backgroundColours = new[] {
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
                };
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
            _openSansFont.Install(GetFontResourceStream("open-sans", "OpenSans-Regular.ttf"));
        }

        /// <summary>
        /// Image process and writes to <paramref name="output"/> <see cref="Stream"/>.
        /// </summary>
        /// <param name="output">The output <see cref="Stream"/> to write to.</param>
        /// <param name="firstName">First name to use.</param>
        /// <param name="lastName">Last name to use.</param>
        /// <param name="size">Image size.</param>
        /// <param name="jpeg">Specifies whether the image has .jpg extension.</param>
        /// <param name="background">The background color to use.</param>
        /// <param name="foreground">The foreground color to use.</param>
        /// <param name="circular">Determines whether the tile will be circular or sqare. Defaults to false (sqare)</param>
        public void Generate(Stream output, string firstName, string lastName, int size = 192, bool jpeg = false, string background = null, string foreground = null, bool circular = false) {
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
                var center = new PointF(image.Width / 2, image.Height / 2);
                if (circular) {
                    image.Mutate(x => x.Fill(Rgba32.Transparent));
                    image.Mutate(x => x.Fill(accentColor.Background, new EllipsePolygon(center, image.Width / 2)));
                } else {
                    image.Mutate(x => x.Fill(accentColor.Background));
                }
                var fonts = new FontCollection();
                // For production application we would recomend you create a FontCollection singleton and manually install the ttf fonts yourself as using SystemFonts can be expensive and you risk font existing or not existing on a deployment by deployment basis.
                var font = _openSansFont.CreateFont("Open Sans", 70, FontStyle.Regular); // for scaling water mark size is largly ignored.
                // Measure the text size.
                var textSize = TextMeasurer.Measure(avatarText, new RendererOptions(font));
                // Find out how much we need to scale the text to fill the space (up or down).
                var scalingFactor = Math.Min(image.Width * 0.6f / textSize.Width, image.Height * 0.6f / textSize.Height);
                // Create a new font.
                var scaledFont = new Font(font, scalingFactor * font.Size);
                var textGraphicOptions = new TextGraphicsOptions(true) {
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
                image.Mutate(x => x.DrawText(textGraphicOptions, avatarText, scaledFont, accentColor.Color, center));
                image.Save(output, jpeg ? (IImageFormat)JpegFormat.Instance : PngFormat.Instance);
            }
            output.Seek(0, SeekOrigin.Begin);
        }

        private static Stream GetFontResourceStream(string familyName, string fileName) => 
            typeof(AvatarGenerator).Assembly.GetManifestResourceStream($"Indice.AspNetCore.Fonts.{familyName.Replace('-', '_')}.{fileName}");
    }

    /// <summary>
    /// Internal class that represents a background, foreground pair of colors.
    /// </summary>
    public class AvatarColor
    {
        /// <summary>
        /// Creates an <see cref="AvatarColor"/> based on hex color strings for background and foreground.
        /// </summary>
        /// <param name="background">The background color.</param>
        /// <param name="color">The foreground color.</param>
        public AvatarColor(string background, string color = null) {
            Background = Rgba32.FromHex(background);
            if (!string.IsNullOrWhiteSpace(color)) {
                Color = Rgba32.FromHex(color);
            } else {
                Color = PerceivedBrightness(Background) > 130 ? Rgba32.Black : Rgba32.White;
            }
        }

        /// <summary>
        /// The background color.
        /// </summary>
        public Rgba32 Background { get; }
        /// <summary>
        /// The foreground color.
        /// </summary>
        public Rgba32 Color { get; }

        private int PerceivedBrightness(Rgba32 color) => (int)Math.Sqrt((color.R * color.R * .299) + (color.G * color.G * .587) + (color.B * color.B * .114));
    }


    /// <summary>
    /// Avatar feature Options. Parameters including palette colours as available sizes.
    /// </summary>
    public class AvatarOptions
    {
        /// <summary>
        /// The color palette to use. List of randomly used colors
        /// </summary>
        public ICollection<AvatarColor> Palette { get; set; } = new List<AvatarColor>();

        /// <summary>
        /// Additional valid tile sizes. Only these sizes are available.
        /// </summary>
        public ICollection<int> TileSizes { get; set; } = new HashSet<int>();
    }
}
