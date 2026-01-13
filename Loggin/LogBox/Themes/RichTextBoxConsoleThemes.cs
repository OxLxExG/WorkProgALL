#region Copyright 2021-2023 C. Augusto Proiete & Contributors
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
#endregion

using System.Collections.Generic;

namespace Serilog.Sinks.RichTextBox.Themes
{
    internal static class RichTextBoxConsoleThemes
    {
        public static RichTextBoxConsoleTheme Monitor { get; } = new RichTextBoxConsoleTheme
                (
                    new Dictionary<RichTextBoxThemeStyle, BoxThemeAdonisStyle>
                    {
                        [RichTextBoxThemeStyle.Text] = new BoxThemeAdonisStyle(),// { Foreground = AdonisColors.White },
                        [RichTextBoxThemeStyle.SecondaryText] = new BoxThemeAdonisStyle { Foreground = AdonisColors.Magenta, FontWeight = "Bold" },
                        [RichTextBoxThemeStyle.TertiaryText] = new BoxThemeAdonisStyle { Foreground = AdonisColors.Magenta, FontWeight = "Bold" },
                        [RichTextBoxThemeStyle.Invalid] = new BoxThemeAdonisStyle { Foreground = AdonisColors.Yellow },
                        [RichTextBoxThemeStyle.Null] = new BoxThemeAdonisStyle { Foreground = AdonisColors.Blue },
                        [RichTextBoxThemeStyle.Name] = new BoxThemeAdonisStyle { Foreground = AdonisColors.Gray },
                        [RichTextBoxThemeStyle.String] = new BoxThemeAdonisStyle { Foreground = AdonisColors.Cyan },
                        [RichTextBoxThemeStyle.Number] = new BoxThemeAdonisStyle { Foreground = AdonisColors.Magenta },
                        [RichTextBoxThemeStyle.Boolean] = new BoxThemeAdonisStyle { Foreground = AdonisColors.Blue },
                        [RichTextBoxThemeStyle.Scalar] = new BoxThemeAdonisStyle { Foreground = AdonisColors.Green },
                    }
                );

        public static RichTextBoxConsoleTheme Literate { get; } = new RichTextBoxConsoleTheme
        (
            new Dictionary<RichTextBoxThemeStyle, BoxThemeAdonisStyle>
            {
                [RichTextBoxThemeStyle.Text] = new BoxThemeAdonisStyle(),// { Foreground = AdonisColors.White },
                [RichTextBoxThemeStyle.SecondaryText] = new BoxThemeAdonisStyle(),// { Foreground = AdonisColors.Gray },
                [RichTextBoxThemeStyle.TertiaryText] = new BoxThemeAdonisStyle (),// { Foreground = AdonisColors.DarkGray },
                [RichTextBoxThemeStyle.Invalid] = new BoxThemeAdonisStyle { Foreground = AdonisColors.Yellow },
                [RichTextBoxThemeStyle.Null] = new BoxThemeAdonisStyle { Foreground = AdonisColors.Blue },
                [RichTextBoxThemeStyle.Name] = new BoxThemeAdonisStyle { Foreground = AdonisColors.Gray },
                [RichTextBoxThemeStyle.String] = new BoxThemeAdonisStyle { Foreground = AdonisColors.Cyan },
                [RichTextBoxThemeStyle.Number] = new BoxThemeAdonisStyle { Foreground = AdonisColors.Magenta },
                [RichTextBoxThemeStyle.Boolean] = new BoxThemeAdonisStyle { Foreground = AdonisColors.Blue },
                [RichTextBoxThemeStyle.Scalar] = new BoxThemeAdonisStyle { Foreground = AdonisColors.Green },
                [RichTextBoxThemeStyle.LevelVerbose] = new BoxThemeAdonisStyle { Foreground = AdonisColors.Gray },
                [RichTextBoxThemeStyle.LevelDebug] = new BoxThemeAdonisStyle { Foreground = AdonisColors.Gray },
                [RichTextBoxThemeStyle.LevelInformation] = new BoxThemeAdonisStyle { Foreground = AdonisColors.White },
                [RichTextBoxThemeStyle.LevelWarning] = new BoxThemeAdonisStyle { Foreground = AdonisColors.Yellow },
                [RichTextBoxThemeStyle.LevelError] = new BoxThemeAdonisStyle { Foreground = AdonisColors.White, Background = AdonisColors.DarkRed },
                [RichTextBoxThemeStyle.LevelFatal] = new BoxThemeAdonisStyle { Foreground = AdonisColors.White, Background = AdonisColors.DarkRed },
            }
        );

        public static RichTextBoxConsoleTheme Grayscale { get; } = new RichTextBoxConsoleTheme
        (
            new Dictionary<RichTextBoxThemeStyle, BoxThemeAdonisStyle>
            {
                [RichTextBoxThemeStyle.Text] = new BoxThemeAdonisStyle { Foreground = AdonisColors.White },
                [RichTextBoxThemeStyle.SecondaryText] = new BoxThemeAdonisStyle { Foreground = AdonisColors.Gray },
                [RichTextBoxThemeStyle.TertiaryText] = new BoxThemeAdonisStyle { Foreground = AdonisColors.DarkGray },
                [RichTextBoxThemeStyle.Invalid] = new BoxThemeAdonisStyle { Foreground = AdonisColors.White, Background = AdonisColors.DarkGray },
                [RichTextBoxThemeStyle.Null] = new BoxThemeAdonisStyle { Foreground = AdonisColors.White },
                [RichTextBoxThemeStyle.Name] = new BoxThemeAdonisStyle { Foreground = AdonisColors.Gray },
                [RichTextBoxThemeStyle.String] = new BoxThemeAdonisStyle { Foreground = AdonisColors.White },
                [RichTextBoxThemeStyle.Number] = new BoxThemeAdonisStyle { Foreground = AdonisColors.White },
                [RichTextBoxThemeStyle.Boolean] = new BoxThemeAdonisStyle { Foreground = AdonisColors.White },
                [RichTextBoxThemeStyle.Scalar] = new BoxThemeAdonisStyle { Foreground = AdonisColors.White },
                [RichTextBoxThemeStyle.LevelVerbose] = new BoxThemeAdonisStyle { Foreground = AdonisColors.DarkGray },
                [RichTextBoxThemeStyle.LevelDebug] = new BoxThemeAdonisStyle { Foreground = AdonisColors.DarkGray },
                [RichTextBoxThemeStyle.LevelInformation] = new BoxThemeAdonisStyle { Foreground = AdonisColors.White },
                [RichTextBoxThemeStyle.LevelWarning] = new BoxThemeAdonisStyle { Foreground = AdonisColors.White, Background = AdonisColors.DarkGray },
                [RichTextBoxThemeStyle.LevelError] = new BoxThemeAdonisStyle { Foreground = AdonisColors.Black, Background = AdonisColors.White },
                [RichTextBoxThemeStyle.LevelFatal] = new BoxThemeAdonisStyle { Foreground = AdonisColors.Black, Background = AdonisColors.White },
            }
        );

        public static RichTextBoxConsoleTheme Colored { get; } = new RichTextBoxConsoleTheme
        (
            new Dictionary<RichTextBoxThemeStyle, BoxThemeAdonisStyle>
            {
                [RichTextBoxThemeStyle.Text] = new BoxThemeAdonisStyle { Foreground = AdonisColors.Gray },
                [RichTextBoxThemeStyle.SecondaryText] = new BoxThemeAdonisStyle { Foreground = AdonisColors.DarkGray },
                [RichTextBoxThemeStyle.TertiaryText] = new BoxThemeAdonisStyle { Foreground = AdonisColors.DarkGray },
                [RichTextBoxThemeStyle.Invalid] = new BoxThemeAdonisStyle { Foreground = AdonisColors.Yellow },
                [RichTextBoxThemeStyle.Null] = new BoxThemeAdonisStyle { Foreground = AdonisColors.White },
                [RichTextBoxThemeStyle.Name] = new BoxThemeAdonisStyle { Foreground = AdonisColors.White },
                [RichTextBoxThemeStyle.String] = new BoxThemeAdonisStyle { Foreground = AdonisColors.White },
                [RichTextBoxThemeStyle.Number] = new BoxThemeAdonisStyle { Foreground = AdonisColors.White },
                [RichTextBoxThemeStyle.Boolean] = new BoxThemeAdonisStyle { Foreground = AdonisColors.White },
                [RichTextBoxThemeStyle.Scalar] = new BoxThemeAdonisStyle { Foreground = AdonisColors.White },
                [RichTextBoxThemeStyle.LevelVerbose] = new BoxThemeAdonisStyle { Foreground = AdonisColors.Gray, Background = AdonisColors.DarkGray },
                [RichTextBoxThemeStyle.LevelDebug] = new BoxThemeAdonisStyle { Foreground = AdonisColors.White, Background = AdonisColors.DarkGray },
                [RichTextBoxThemeStyle.LevelInformation] = new BoxThemeAdonisStyle { Foreground = AdonisColors.White, Background = AdonisColors.Blue },
                [RichTextBoxThemeStyle.LevelWarning] = new BoxThemeAdonisStyle { Foreground = AdonisColors.DarkGray, Background = AdonisColors.Yellow },
                [RichTextBoxThemeStyle.LevelError] = new BoxThemeAdonisStyle { Foreground = AdonisColors.White, Background = AdonisColors.Red },
                [RichTextBoxThemeStyle.LevelFatal] = new BoxThemeAdonisStyle { Foreground = AdonisColors.White, Background = AdonisColors.Red },
            }
        );
    }
}
