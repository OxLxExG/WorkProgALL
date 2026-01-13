#region Copyright 2021-2023 C. Augusto Proiete & Contributors
//
// Licensed under the Apache License, Version 2.0 (the "License";
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

using System.Windows;

namespace Serilog.Sinks.RichTextBox.Themes
{
    internal static class AdonisColors
    {
        private static readonly object _syncLock = new object();

        static AdonisColors()
        {
            lock (_syncLock)
            {
                Black =  AdonisUI.Colors.Layer0BackgroundColor;
                DarkBlue =  AdonisUI.Colors.DarkBlueColor;
                DarkGreen =  AdonisUI.Colors.DarkGreenColor;
                DarkCyan =  AdonisUI.Colors.DarkCyanColor;
                DarkRed =  AdonisUI.Colors.DarkRedColor;
                DarkMagenta =  AdonisUI.Colors.DarkMagentaColor;
                DarkYellow =  AdonisUI.Colors.DarkYellowColor;

                Gray = AdonisUI.Colors.ForegroundColor;
                DarkGray = AdonisUI.Colors.ForegroundColor;

                Blue =  AdonisUI.Colors.BlueColor;
                Green =  AdonisUI.Colors.GreenColor;
                Cyan =  AdonisUI.Colors.CyanColor;
                Red =  AdonisUI.Colors.RedColor;
                Magenta =  AdonisUI.Colors.MagentaColor;
                Yellow =  AdonisUI.Colors.YellowColor;

                White =  AdonisUI.Colors.ForegroundColor;
            }
        }


        public static ComponentResourceKey Black { get;   }
        public static ComponentResourceKey DarkBlue { get;   }
        public static ComponentResourceKey DarkGreen { get;   }
        public static ComponentResourceKey DarkCyan { get;   }
        public static ComponentResourceKey DarkRed { get;   }
        public static ComponentResourceKey DarkMagenta { get;   }
        public static ComponentResourceKey DarkYellow { get;   }
        public static ComponentResourceKey Gray { get;   }
        public static ComponentResourceKey DarkGray { get;   }
        public static ComponentResourceKey Blue { get;   }
        public static ComponentResourceKey Green { get;   }
        public static ComponentResourceKey Cyan { get;   }
        public static ComponentResourceKey Red { get;   }
        public static ComponentResourceKey Magenta { get;   }
        public static ComponentResourceKey Yellow { get;   }
        public static ComponentResourceKey White { get;   }
    }


    //internal static class ConsoleHtmlColor
    //{
    //    private static readonly object _syncLock = new object();


    //    public static void UpdateThemeColors(object? sender, bool e)
    //    {
    //        lock (_syncLock)
    //        {
    //            Black = Application.Current.Resources[AdonisUI.Colors.Layer0BackgroundColor].ToString()!;
    //            DarkBlue = Application.Current.Resources[AdonisUI.Colors.DarkBlueColor].ToString()!;
    //            DarkGreen =     Application.Current.Resources[AdonisUI.Colors.DarkGreenColor].ToString()!;
    //            DarkCyan = Application.Current.Resources[AdonisUI.Colors.DarkCyanColor].ToString()!;
    //            DarkRed = Application.Current.Resources[AdonisUI.Colors.DarkRedColor].ToString()!;
    //            DarkMagenta = Application.Current.Resources[AdonisUI.Colors.DarkMagentaColor].ToString()!;
    //            DarkYellow = Application.Current.Resources[AdonisUI.Colors.DarkYellowColor].ToString()!;

    //            Gray = "#c0c0c0";
    //            DarkGray = "#808080";

    //            Blue =  Application.Current.Resources[AdonisUI.Colors.BlueColor].ToString()!;
    //            Green = Application.Current.Resources[AdonisUI.Colors.GreenColor].ToString()!;
    //            Cyan =  Application.Current.Resources[AdonisUI.Colors.CyanColor].ToString()!;
    //            Red =   Application.Current.Resources[AdonisUI.Colors.RedColor].ToString()!;
    //            Magenta = Application.Current.Resources[AdonisUI.Colors.MagentaColor].ToString()!;
    //            Yellow = Application.Current.Resources[AdonisUI.Colors.YellowColor].ToString()!;

    //            White = Application.Current.Resources[AdonisUI.Colors.ForegroundColor].ToString()!;
    //        }
    //    }

    //    static ConsoleHtmlColor()
    //    {
    //        lock (_syncLock)
    //        {
    //            ThemeChangeEvent.ThemeChanged += UpdateThemeColors;

    //            Black = "#000000";
    //            DarkBlue = "#000080";
    //            DarkGreen = "#008000";
    //            DarkCyan = "#008080";
    //            DarkRed = "#800000";
    //            DarkMagenta = "#800080";
    //            DarkYellow = "#808000";
    //            Gray = "#c0c0c0";
    //            DarkGray = "#808080";
    //            Blue = "#0000ff";
    //            Green = "#00ff00";
    //            Cyan = "#00ffff";
    //            Red = "#ff0000";
    //            Magenta = "#ff00ff";
    //            Yellow = "#ffff00";
    //            White = "#ffffff";

    //            //Black = "{DynamicResource {x:Static adonisUi:Brushes.Layer0BackgroundBrush}}";// "#000000";

    //            //DarkBlue =    "{DynamicResource {x:Static adonisUi:Brushes.DarkBlueBrush}}";   // "#000080";
    //            //DarkGreen =   "{DynamicResource {x:Static adonisUi:Brushes.DarkGreenBrush}}";  // "#008000";
    //            //DarkCyan =    "{DynamicResource {x:Static adonisUi:Brushes.DarkCyanBrush}}";   // "#008080";
    //            //DarkRed =     "{DynamicResource {x:Static adonisUi:Brushes.DarkRedBrush}}";    // "#800000";
    //            //DarkMagenta = "{DynamicResource {x:Static adonisUi:Brushes.DarkMagentaBrush}}";// "#800080";
    //            //DarkYellow =  "{DynamicResource {x:Static adonisUi:Brushes.DarkYellowBrush}}"; // "#808000";

    //            //Gray = "#c0c0c0";
    //            //DarkGray = "#808080";

    //            //Blue =     "{DynamicResource {x:Static adonisUi:Brushes.BlueBrush}}";   //"#0000ff";
    //            //Green =    "{DynamicResource {x:Static adonisUi:Brushes.GreenBrush}}";  //"#00ff00";
    //            //Cyan =     "{DynamicResource {x:Static adonisUi:Brushes.CyanBrush}}";   //"#00ffff";
    //            //Red =      "{DynamicResource {x:Static adonisUi:Brushes.RedBrush}}";    //"#ff0000";
    //            //Magenta =  "{DynamicResource {x:Static adonisUi:Brushes.MagentaBrush}}";//"#ff00ff";
    //            //Yellow =   "{DynamicResource {x:Static adonisUi:Brushes.YellowBrush}}"; //"#ffff00";

    //            //White =    "{DynamicResource {x:Static adonisUi:Brushes.ForegroundBrush}}";// "#ffffff";
    //        }
    //    }

    //    private static void ThemeChangeEvent_ThemeChanged(object? sender, bool e)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public static string Black { get; private set; }
    //    public static string DarkBlue { get; private set; } 
    //    public static string DarkGreen { get; private set; }
    //    public static string DarkCyan { get; private set; }
    //    public static string DarkRed { get; private set; }
    //    public static string DarkMagenta  { get; private set; }
    //    public static string DarkYellow { get; private set; }
    //    public static string Gray { get; private set; }
    //    public static string DarkGray { get; private set; }
    //    public static string Blue { get; private set; }
    //    public static string Green { get; private set; }
    //    public static string Cyan { get; private set; }
    //    public static string Red { get; private set; }
    //    public static string Magenta { get; private set; }
    //    public static string Yellow { get; private set; }
    //    public static string White { get; private set; }
    //}
}
