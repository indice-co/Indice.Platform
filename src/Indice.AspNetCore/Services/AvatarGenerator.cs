﻿using System;
using System.IO;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.Primitives;
using System.Linq;
using Indice.Extensions;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats;

namespace Indice.Services
{
    /// <summary>
    /// Generates an avatar based on a given name (first and last name) plus parameters
    /// </summary>
    public class AvatarGenerator
    {
        /// <summary>
        /// Internal class that represents a background, foreground pair of colors.
        /// </summary>
        public class AvatarColor
        {
            /// <summary>
            /// create an <see cref="AvatarColor"/> based on hex color strings for background and foreground.
            /// </summary>
            /// <param name="bakcground"></param>
            /// <param name="color">The foreground</param>
            public AvatarColor(string bakcground, string color = null) {
                Background = Rgba32.FromHex(bakcground);
                if (!string.IsNullOrWhiteSpace(color)) {
                    Color = Rgba32.FromHex(color);
                } else {
                    Color = (PerceivedBrightness(Background) > 130 ? Rgba32.Black : Rgba32.White);
                }
            }
            /// <summary>
            /// The background
            /// </summary>
            public Rgba32 Background { get; }

            /// <summary>
            /// The foreground
            /// </summary>
            public Rgba32 Color { get; }

            private int PerceivedBrightness(Rgba32 color) {
                return (int)Math.Sqrt(
                color.R * color.R * .299 +
                color.G * color.G * .587 +
                color.B * color.B * .114);
            }
        }

        private AvatarColor[] _BackgroundColours;

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="palette"></param>
        public AvatarGenerator(params AvatarColor[] palette) {
            // https://www.materialpalette.com
            if (palette == null || palette.Length == 0) {
                _BackgroundColours = new[] {
                    new AvatarColor("f44336", "ffffff"), // red
                    new AvatarColor("e91e63", "ffffff"), // pink
                    new AvatarColor("9c27b0", "ffffff"), // purple
                    new AvatarColor("673ab7", "ffffff"), // deep-purple"
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
            }
        }

        /// <summary>
        /// Image process and whites to <paramref name="output"/> <see cref="Stream"/>
        /// </summary>
        /// <param name="output"></param>
        /// <param name="firstName"></param>
        /// <param name="lastName"></param>
        /// <param name="size"></param>
        /// <param name="jpeg"></param>
        /// <param name="background"></param>
        public void Generate(Stream output, string firstName, string lastName, int size = 192, bool jpeg = false, string background = null) {
            var avatarText = string.Format("{0}{1}", firstName?.Length > 0 ? firstName[0] : ' ', lastName?.Length > 0 ? lastName[0] : ' ').ToUpper().RemoveDiacritics();

            //var randomIndex = new Random().Next(0, _BackgroundColours.Length - 1);
            var randomIndex = $"{firstName}{lastName}".ToCharArray().Sum(x => x) % _BackgroundColours.Length;
            var accentColor = _BackgroundColours[randomIndex];
            if (background != null) {
                accentColor = new AvatarColor(background);
            }

            using (var img = new Image<Rgba32>(size, size)) {
                img.Mutate(x => x.Fill(accentColor.Background));
                // For production application we would recomend you create a FontCollection
                // singleton and manually install the ttf fonts yourself as using SystemFonts
                // can be expensive and you risk font existing or not existing on a deployment
                // by deployment basis.
                var font = SystemFonts.CreateFont("Arial", 72); // for scaling water mark size is largly ignored.
                // measure the text size
                var textSize = TextMeasurer.Measure(avatarText, new RendererOptions(font));
                //find out how much we need to scale the text to fill the space (up or down)
                var scalingFactor = Math.Min(img.Width * 0.6f / textSize.Width, img.Height * 0.6f / textSize.Height);
                //create a new font 
                var scaledFont = new Font(font, scalingFactor * font.Size);
                var center = new PointF(img.Width / 2, img.Height / 2);
                var textGraphicOptions = new TextGraphicsOptions(true) {
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
                img.Mutate(i => i.DrawText(textGraphicOptions, avatarText, scaledFont, accentColor.Color, center));
                img.Save(output, jpeg ? (IImageFormat)JpegFormat.Instance : PngFormat.Instance);
            }

            output.Seek(0, SeekOrigin.Begin);
        }
    }

    #region palette

    /*
     {
  "colors": [{
    "name": "red",
    "key": "red",
    "android": "red",
    "shades": {
      "50": {
        "hex": "#ffebee",
        "contrast": "black"
      },
      "100": {
        "hex": "#ffcdd2",
        "contrast": "black"
      },
      "200": {
        "hex": "#ef9a9a",
        "contrast": "black"
      },
      "300": {
        "hex": "#e57373",
        "contrast": "black"
      },
      "400": {
        "hex": "#ef5350",
        "contrast": "black"
      },
      "500": {
        "hex": "#f44336",
        "contrast": "white"
      },
      "600": {
        "hex": "#e53935",
        "contrast": "white"
      },
      "700": {
        "hex": "#d32f2f",
        "contrast": "white"
      },
      "800": {
        "hex": "#c62828",
        "contrast": "white"
      },
      "900": {
        "hex": "#b71c1c",
        "contrast": "white"
      },
      "A100": {
        "hex": "#ff8a80",
        "contrast": "black"
      },
      "A200": {
        "hex": "#ff5252",
        "contrast": "white"
      },
      "A400": {
        "hex": "#ff1744",
        "contrast": "white"
      },
      "A700": {
        "hex": "#d50000",
        "contrast": "white"
      }
    }
  }, {
    "name": "pink",
    "key": "pink",
    "android": "pink",
    "shades": {
      "50": {
        "hex": "#fce4ec",
        "contrast": "black"
      },
      "100": {
        "hex": "#f8bbd0",
        "contrast": "black"
      },
      "200": {
        "hex": "#f48fb1",
        "contrast": "black"
      },
      "300": {
        "hex": "#f06292",
        "contrast": "black"
      },
      "400": {
        "hex": "#ec407a",
        "contrast": "black"
      },
      "500": {
        "hex": "#e91e63",
        "contrast": "white"
      },
      "600": {
        "hex": "#d81b60",
        "contrast": "white"
      },
      "700": {
        "hex": "#c2185b",
        "contrast": "white"
      },
      "800": {
        "hex": "#ad1457",
        "contrast": "white"
      },
      "900": {
        "hex": "#880e4f",
        "contrast": "white"
      },
      "A100": {
        "hex": "#ff80ab",
        "contrast": "black"
      },
      "A200": {
        "hex": "#ff4081",
        "contrast": "white"
      },
      "A400": {
        "hex": "#f50057",
        "contrast": "white"
      },
      "A700": {
        "hex": "#c51162",
        "contrast": "white"
      }
    }
  }, {
    "name": "purple",
    "key": "purple",
    "android": "purple",
    "shades": {
      "50": {
        "hex": "#f3e5f5",
        "contrast": "black"
      },
      "100": {
        "hex": "#e1bee7",
        "contrast": "black"
      },
      "200": {
        "hex": "#ce93d8",
        "contrast": "black"
      },
      "300": {
        "hex": "#ba68c8",
        "contrast": "white"
      },
      "400": {
        "hex": "#ab47bc",
        "contrast": "white"
      },
      "500": {
        "hex": "#9c27b0",
        "contrast": "white"
      },
      "600": {
        "hex": "#8e24aa",
        "contrast": "white"
      },
      "700": {
        "hex": "#7b1fa2",
        "contrast": "white"
      },
      "800": {
        "hex": "#6a1b9a",
        "contrast": "white"
      },
      "900": {
        "hex": "#4a148c",
        "contrast": "white"
      },
      "A100": {
        "hex": "#ea80fc",
        "contrast": "black"
      },
      "A200": {
        "hex": "#e040fb",
        "contrast": "white"
      },
      "A400": {
        "hex": "#d500f9",
        "contrast": "white"
      },
      "A700": {
        "hex": "#aa00ff",
        "contrast": "white"
      }
    }
  }, {
    "name": "deep purple",
    "key": "deep-purple",
    "android": "deep_purple",
    "shades": {
      "50": {
        "hex": "#ede7f6",
        "contrast": "black"
      },
      "100": {
        "hex": "#d1c4e9",
        "contrast": "black"
      },
      "200": {
        "hex": "#b39ddb",
        "contrast": "black"
      },
      "300": {
        "hex": "#9575cd",
        "contrast": "white"
      },
      "400": {
        "hex": "#7e57c2",
        "contrast": "white"
      },
      "500": {
        "hex": "#673ab7",
        "contrast": "white"
      },
      "600": {
        "hex": "#5e35b1",
        "contrast": "white"
      },
      "700": {
        "hex": "#512da8",
        "contrast": "white"
      },
      "800": {
        "hex": "#4527a0",
        "contrast": "white"
      },
      "900": {
        "hex": "#311b92",
        "contrast": "white"
      },
      "A100": {
        "hex": "#b388ff",
        "contrast": "black"
      },
      "A200": {
        "hex": "#7c4dff",
        "contrast": "white"
      },
      "A400": {
        "hex": "#651fff",
        "contrast": "white"
      },
      "A700": {
        "hex": "#6200ea",
        "contrast": "white"
      }
    }
  }, {
    "name": "indigo",
    "key": "indigo",
    "android": "indigo",
    "shades": {
      "50": {
        "hex": "#e8eaf6",
        "contrast": "black"
      },
      "100": {
        "hex": "#c5cae9",
        "contrast": "black"
      },
      "200": {
        "hex": "#9fa8da",
        "contrast": "black"
      },
      "300": {
        "hex": "#7986cb",
        "contrast": "white"
      },
      "400": {
        "hex": "#5c6bc0",
        "contrast": "white"
      },
      "500": {
        "hex": "#3f51b5",
        "contrast": "white"
      },
      "600": {
        "hex": "#3949ab",
        "contrast": "white"
      },
      "700": {
        "hex": "#303f9f",
        "contrast": "white"
      },
      "800": {
        "hex": "#283593",
        "contrast": "white"
      },
      "900": {
        "hex": "#1a237e",
        "contrast": "white"
      },
      "A100": {
        "hex": "#8c9eff",
        "contrast": "black"
      },
      "A200": {
        "hex": "#536dfe",
        "contrast": "white"
      },
      "A400": {
        "hex": "#3d5afe",
        "contrast": "white"
      },
      "A700": {
        "hex": "#304ffe",
        "contrast": "white"
      }
    }
  }, {
    "name": "blue",
    "key": "blue",
    "android": "blue",
    "shades": {
      "50": {
        "hex": "#e3f2fd",
        "contrast": "black"
      },
      "100": {
        "hex": "#bbdefb",
        "contrast": "black"
      },
      "200": {
        "hex": "#90caf9",
        "contrast": "black"
      },
      "300": {
        "hex": "#64b5f6",
        "contrast": "black"
      },
      "400": {
        "hex": "#42a5f5",
        "contrast": "black"
      },
      "500": {
        "hex": "#2196f3",
        "contrast": "white"
      },
      "600": {
        "hex": "#1e88e5",
        "contrast": "white"
      },
      "700": {
        "hex": "#1976d2",
        "contrast": "white"
      },
      "800": {
        "hex": "#1565c0",
        "contrast": "white"
      },
      "900": {
        "hex": "#0d47a1",
        "contrast": "white"
      },
      "A100": {
        "hex": "#82b1ff",
        "contrast": "black"
      },
      "A200": {
        "hex": "#448aff",
        "contrast": "white"
      },
      "A400": {
        "hex": "#2979ff",
        "contrast": "white"
      },
      "A700": {
        "hex": "#2962ff",
        "contrast": "white"
      }
    }
  }, {
    "name": "light blue",
    "key": "light-blue",
    "android": "light_blue",
    "shades": {
      "50": {
        "hex": "#e1f5fe",
        "contrast": "black"
      },
      "100": {
        "hex": "#b3e5fc",
        "contrast": "black"
      },
      "200": {
        "hex": "#81d4fa",
        "contrast": "black"
      },
      "300": {
        "hex": "#4fc3f7",
        "contrast": "black"
      },
      "400": {
        "hex": "#29b6f6",
        "contrast": "black"
      },
      "500": {
        "hex": "#03a9f4",
        "contrast": "white"
      },
      "600": {
        "hex": "#039be5",
        "contrast": "white"
      },
      "700": {
        "hex": "#0288d1",
        "contrast": "white"
      },
      "800": {
        "hex": "#0277bd",
        "contrast": "white"
      },
      "900": {
        "hex": "#01579b",
        "contrast": "white"
      },
      "A100": {
        "hex": "#80d8ff",
        "contrast": "black"
      },
      "A200": {
        "hex": "#40c4ff",
        "contrast": "black"
      },
      "A400": {
        "hex": "#00b0ff",
        "contrast": "black"
      },
      "A700": {
        "hex": "#0091ea",
        "contrast": "white"
      }
    }
  }, {
    "name": "cyan",
    "key": "cyan",
    "android": "cyan",
    "shades": {
      "50": {
        "hex": "#e0f7fa",
        "contrast": "black"
      },
      "100": {
        "hex": "#b2ebf2",
        "contrast": "black"
      },
      "200": {
        "hex": "#80deea",
        "contrast": "black"
      },
      "300": {
        "hex": "#4dd0e1",
        "contrast": "black"
      },
      "400": {
        "hex": "#26c6da",
        "contrast": "black"
      },
      "500": {
        "hex": "#00bcd4",
        "contrast": "white"
      },
      "600": {
        "hex": "#00acc1",
        "contrast": "white"
      },
      "700": {
        "hex": "#0097a7",
        "contrast": "white"
      },
      "800": {
        "hex": "#00838f",
        "contrast": "white"
      },
      "900": {
        "hex": "#006064",
        "contrast": "white"
      },
      "A100": {
        "hex": "#84ffff",
        "contrast": "black"
      },
      "A200": {
        "hex": "#18ffff",
        "contrast": "black"
      },
      "A400": {
        "hex": "#00e5ff",
        "contrast": "black"
      },
      "A700": {
        "hex": "#00b8d4",
        "contrast": "black"
      }
    }
  }, {
    "name": "teal",
    "key": "teal",
    "android": "teal",
    "shades": {
      "50": {
        "hex": "#e0f2f1",
        "contrast": "black"
      },
      "100": {
        "hex": "#b2dfdb",
        "contrast": "black"
      },
      "200": {
        "hex": "#80cbc4",
        "contrast": "black"
      },
      "300": {
        "hex": "#4db6ac",
        "contrast": "black"
      },
      "400": {
        "hex": "#26a69a",
        "contrast": "black"
      },
      "500": {
        "hex": "#009688",
        "contrast": "white"
      },
      "600": {
        "hex": "#00897b",
        "contrast": "white"
      },
      "700": {
        "hex": "#00796b",
        "contrast": "white"
      },
      "800": {
        "hex": "#00695c",
        "contrast": "white"
      },
      "900": {
        "hex": "#004d40",
        "contrast": "white"
      },
      "A100": {
        "hex": "#a7ffeb",
        "contrast": "black"
      },
      "A200": {
        "hex": "#64ffda",
        "contrast": "black"
      },
      "A400": {
        "hex": "#1de9b6",
        "contrast": "black"
      },
      "A700": {
        "hex": "#00bfa5",
        "contrast": "black"
      }
    }
  }, {
    "name": "green",
    "key": "green",
    "android": "green",
    "shades": {
      "50": {
        "hex": "#e8f5e9",
        "contrast": "black"
      },
      "100": {
        "hex": "#c8e6c9",
        "contrast": "black"
      },
      "200": {
        "hex": "#a5d6a7",
        "contrast": "black"
      },
      "300": {
        "hex": "#81c784",
        "contrast": "black"
      },
      "400": {
        "hex": "#66bb6a",
        "contrast": "black"
      },
      "500": {
        "hex": "#4caf50",
        "contrast": "white"
      },
      "600": {
        "hex": "#43a047",
        "contrast": "white"
      },
      "700": {
        "hex": "#388e3c",
        "contrast": "white"
      },
      "800": {
        "hex": "#2e7d32",
        "contrast": "white"
      },
      "900": {
        "hex": "#1b5e20",
        "contrast": "white"
      },
      "A100": {
        "hex": "#b9f6ca",
        "contrast": "black"
      },
      "A200": {
        "hex": "#69f0ae",
        "contrast": "black"
      },
      "A400": {
        "hex": "#00e676",
        "contrast": "black"
      },
      "A700": {
        "hex": "#00c853",
        "contrast": "black"
      }
    }
  }, {
    "name": "light green",
    "key": "light-green",
    "android": "light_green",
    "shades": {
      "50": {
        "hex": "#f1f8e9",
        "contrast": "black"
      },
      "100": {
        "hex": "#dcedc8",
        "contrast": "black"
      },
      "200": {
        "hex": "#c5e1a5",
        "contrast": "black"
      },
      "300": {
        "hex": "#aed581",
        "contrast": "black"
      },
      "400": {
        "hex": "#9ccc65",
        "contrast": "black"
      },
      "500": {
        "hex": "#8bc34a",
        "contrast": "black"
      },
      "600": {
        "hex": "#7cb342",
        "contrast": "black"
      },
      "700": {
        "hex": "#689f38",
        "contrast": "black"
      },
      "800": {
        "hex": "#558b2f",
        "contrast": "white"
      },
      "900": {
        "hex": "#33691e",
        "contrast": "white"
      },
      "A100": {
        "hex": "#ccff90",
        "contrast": "black"
      },
      "A200": {
        "hex": "#b2ff59",
        "contrast": "black"
      },
      "A400": {
        "hex": "#76ff03",
        "contrast": "black"
      },
      "A700": {
        "hex": "#64dd17",
        "contrast": "black"
      }
    }
  }, {
    "name": "lime",
    "key": "lime",
    "android": "lime",
    "shades": {
      "50": {
        "hex": "#f9fbe7",
        "contrast": "black"
      },
      "100": {
        "hex": "#f0f4c3",
        "contrast": "black"
      },
      "200": {
        "hex": "#e6ee9c",
        "contrast": "black"
      },
      "300": {
        "hex": "#dce775",
        "contrast": "black"
      },
      "400": {
        "hex": "#d4e157",
        "contrast": "black"
      },
      "500": {
        "hex": "#cddc39",
        "contrast": "black"
      },
      "600": {
        "hex": "#c0ca33",
        "contrast": "black"
      },
      "700": {
        "hex": "#afb42b",
        "contrast": "black"
      },
      "800": {
        "hex": "#9e9d24",
        "contrast": "black"
      },
      "900": {
        "hex": "#827717",
        "contrast": "white"
      },
      "A100": {
        "hex": "#f4ff81",
        "contrast": "black"
      },
      "A200": {
        "hex": "#eeff41",
        "contrast": "black"
      },
      "A400": {
        "hex": "#c6ff00",
        "contrast": "black"
      },
      "A700": {
        "hex": "#aeea00",
        "contrast": "black"
      }
    }
  }, {
    "name": "yellow",
    "key": "yellow",
    "android": "yellow",
    "shades": {
      "50": {
        "hex": "#fffde7",
        "contrast": "black"
      },
      "100": {
        "hex": "#fff9c4",
        "contrast": "black"
      },
      "200": {
        "hex": "#fff59d",
        "contrast": "black"
      },
      "300": {
        "hex": "#fff176",
        "contrast": "black"
      },
      "400": {
        "hex": "#ffee58",
        "contrast": "black"
      },
      "500": {
        "hex": "#ffeb3b",
        "contrast": "black"
      },
      "600": {
        "hex": "#fdd835",
        "contrast": "black"
      },
      "700": {
        "hex": "#fbc02d",
        "contrast": "black"
      },
      "800": {
        "hex": "#f9a825",
        "contrast": "black"
      },
      "900": {
        "hex": "#f57f17",
        "contrast": "black"
      },
      "A100": {
        "hex": "#ffff8d",
        "contrast": "black"
      },
      "A200": {
        "hex": "#ffff00",
        "contrast": "black"
      },
      "A400": {
        "hex": "#ffea00",
        "contrast": "black"
      },
      "A700": {
        "hex": "#ffd600",
        "contrast": "black"
      }
    }
  }, {
    "name": "amber",
    "key": "amber",
    "android": "amber",
    "shades": {
      "50": {
        "hex": "#fff8e1",
        "contrast": "black"
      },
      "100": {
        "hex": "#ffecb3",
        "contrast": "black"
      },
      "200": {
        "hex": "#ffe082",
        "contrast": "black"
      },
      "300": {
        "hex": "#ffd54f",
        "contrast": "black"
      },
      "400": {
        "hex": "#ffca28",
        "contrast": "black"
      },
      "500": {
        "hex": "#ffc107",
        "contrast": "black"
      },
      "600": {
        "hex": "#ffb300",
        "contrast": "black"
      },
      "700": {
        "hex": "#ffa000",
        "contrast": "black"
      },
      "800": {
        "hex": "#ff8f00",
        "contrast": "black"
      },
      "900": {
        "hex": "#ff6f00",
        "contrast": "black"
      },
      "A100": {
        "hex": "#ffe57f",
        "contrast": "black"
      },
      "A200": {
        "hex": "#ffd740",
        "contrast": "black"
      },
      "A400": {
        "hex": "#ffc400",
        "contrast": "black"
      },
      "A700": {
        "hex": "#ffab00",
        "contrast": "black"
      }
    }
  }, {
    "name": "orange",
    "key": "orange",
    "android": "orange",
    "shades": {
      "50": {
        "hex": "#fff3e0",
        "contrast": "black"
      },
      "100": {
        "hex": "#ffe0b2",
        "contrast": "black"
      },
      "200": {
        "hex": "#ffcc80",
        "contrast": "black"
      },
      "300": {
        "hex": "#ffb74d",
        "contrast": "black"
      },
      "400": {
        "hex": "#ffa726",
        "contrast": "black"
      },
      "500": {
        "hex": "#ff9800",
        "contrast": "black"
      },
      "600": {
        "hex": "#fb8c00",
        "contrast": "black"
      },
      "700": {
        "hex": "#f57c00",
        "contrast": "black"
      },
      "800": {
        "hex": "#ef6c00",
        "contrast": "white"
      },
      "900": {
        "hex": "#e65100",
        "contrast": "white"
      },
      "A100": {
        "hex": "#ffd180",
        "contrast": "black"
      },
      "A200": {
        "hex": "#ffab40",
        "contrast": "black"
      },
      "A400": {
        "hex": "#ff9100",
        "contrast": "black"
      },
      "A700": {
        "hex": "#ff6d00",
        "contrast": "black"
      }
    }
  }, {
    "name": "deep orange",
    "key": "deep-orange",
    "android": "deep_orange",
    "shades": {
      "50": {
        "hex": "#fbe9e7",
        "contrast": "black"
      },
      "100": {
        "hex": "#ffccbc",
        "contrast": "black"
      },
      "200": {
        "hex": "#ffab91",
        "contrast": "black"
      },
      "300": {
        "hex": "#ff8a65",
        "contrast": "black"
      },
      "400": {
        "hex": "#ff7043",
        "contrast": "black"
      },
      "500": {
        "hex": "#ff5722",
        "contrast": "white"
      },
      "600": {
        "hex": "#f4511e",
        "contrast": "white"
      },
      "700": {
        "hex": "#e64a19",
        "contrast": "white"
      },
      "800": {
        "hex": "#d84315",
        "contrast": "white"
      },
      "900": {
        "hex": "#bf360c",
        "contrast": "white"
      },
      "A100": {
        "hex": "#ff9e80",
        "contrast": "black"
      },
      "A200": {
        "hex": "#ff6e40",
        "contrast": "black"
      },
      "A400": {
        "hex": "#ff3d00",
        "contrast": "white"
      },
      "A700": {
        "hex": "#dd2c00",
        "contrast": "white"
      }
    }
  }, {
    "name": "brown",
    "key": "brown",
    "android": "brown",
    "shades": {
      "50": {
        "hex": "#efebe9",
        "contrast": "black"
      },
      "100": {
        "hex": "#d7ccc8",
        "contrast": "black"
      },
      "200": {
        "hex": "#bcaaa4",
        "contrast": "black"
      },
      "300": {
        "hex": "#a1887f",
        "contrast": "white"
      },
      "400": {
        "hex": "#8d6e63",
        "contrast": "white"
      },
      "500": {
        "hex": "#795548",
        "contrast": "white"
      },
      "600": {
        "hex": "#6d4c41",
        "contrast": "white"
      },
      "700": {
        "hex": "#5d4037",
        "contrast": "white"
      },
      "800": {
        "hex": "#4e342e",
        "contrast": "white"
      },
      "900": {
        "hex": "#3e2723",
        "contrast": "white"
      }
    }
  }, {
    "name": "grey",
    "key": "grey",
    "android": "grey",
    "shades": {
      "50": {
        "hex": "#fafafa",
        "contrast": "black"
      },
      "100": {
        "hex": "#f5f5f5",
        "contrast": "black"
      },
      "200": {
        "hex": "#eeeeee",
        "contrast": "black"
      },
      "300": {
        "hex": "#e0e0e0",
        "contrast": "black"
      },
      "400": {
        "hex": "#bdbdbd",
        "contrast": "black"
      },
      "500": {
        "hex": "#9e9e9e",
        "contrast": "black"
      },
      "600": {
        "hex": "#757575",
        "contrast": "white"
      },
      "700": {
        "hex": "#616161",
        "contrast": "white"
      },
      "800": {
        "hex": "#424242",
        "contrast": "white"
      },
      "900": {
        "hex": "#212121",
        "contrast": "white"
      }
    }
  }, {
    "name": "blue grey",
    "key": "blue-grey",
    "android": "blue_grey",
    "shades": {
      "50": {
        "hex": "#eceff1",
        "contrast": "black"
      },
      "100": {
        "hex": "#cfd8dc",
        "contrast": "black"
      },
      "200": {
        "hex": "#b0bec5",
        "contrast": "black"
      },
      "300": {
        "hex": "#90a4ae",
        "contrast": "black"
      },
      "400": {
        "hex": "#78909c",
        "contrast": "white"
      },
      "500": {
        "hex": "#607d8b",
        "contrast": "white"
      },
      "600": {
        "hex": "#546e7a",
        "contrast": "white"
      },
      "700": {
        "hex": "#455a64",
        "contrast": "white"
      },
      "800": {
        "hex": "#37474f",
        "contrast": "white"
      },
      "900": {
        "hex": "#263238",
        "contrast": "white"
      }
    }
  }],
  "primaryColor": null,
  "accentColor": null,
  "rootUrl": "https://www.materialpalette.com/"
}
     */
    #endregion

}
