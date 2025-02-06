using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;
using Microsoft.Extensions.DependencyInjection;
using Xceed.Wpf.AvalonDock;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using Xceed.Wpf.AvalonDock.Layout;

namespace Core
{
    public record StdLogg(bool Error, bool Info, bool Trace, bool Monitor)
    {
        public StdLogg() : this(true, false, false, false) { }
    }
    public record StdLoggs(StdLogg Box, StdLogg File)
    {
        public StdLoggs() : this(new(), new()) { }
    }
    public record GlobalSettings(StdLoggs Logging, string Culture, string GroupDir, string ProjectDir)
    {
        public GlobalSettings() : this(new(), "en-US", string.Empty, string.Empty) { }
    }
    public static class RootMenusID
    {
        public static string ROOT => "ROOT";
        public static string NFile => "NFile";

        public static string NFile_Create => "NFile_Create";
        public static string NFile_Create_Group => "NCreate_NewGroup";
        public static string NFile_Create_Project => "NCreate_NewProject";
        public static string NFile_Create_File => "NCreate_NewFile";

        public static string NFile_Open => "NFile_Open";
        public static string NFile_Open_Group => "NOpen_Group";
        public static string NFile_Open_ALL => "NOpen_ALL";
        public static string NFile_Open_Project => "NOpen_Project";
        public static string NFile_Open_File => "NOpen_File";

        public static string NFile_Add => "NFile_Add";
        public static string NFile_Add_Project => "NAdd_Project";
        public static string NFile_Add_NewProject => "NAdd_NewProject";
        public static string NFile_Add_File => "NAdd_File";

        public static string NFile_Close => "NFile_Close";
        public static string NFile_CloseAll => "NFile_CloseAll";

        public static string NFile_Save => "NFile_Save";
        public static string NFile_SaveAS => "NFile_SaveAS";
        public static string NFile_SaveProject => "NFile_saveProject";
        public static string NFile_SaveAll => "NFile_SaveAll";

        public static string NFile_Last_ProjectGroups => "NFile_Last_ProjectGroups";
        public static string NFile_Last_Projects => "NFile_Last_Projects";
        public static string NFile_Last_Files => "NFile_Last_Files";

        public static string NShow => "NShow";
        public static string NDebugs => "NDebugs";
        public static string NHidden => "NHidden";

    }
}
