using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Core
{
    public enum BoxButton
    {
        OK = 0,
        OKCancel = 1,
        YesNoCancel = 3,
        YesNo = 4
    }
    public enum BoxImage
    {
        None = 0,
        //
        // Сводка:
        //     The message box contains a symbol consisting of white X in a circle with a red
        //     background.
        Error = 16,
        //
        // Сводка:
        //     The message box contains a symbol consisting of a question mark in a circle.
        //     The question mark message icon is no longer recommended because it does not clearly
        //     represent a specific type of message and because the phrasing of a message as
        //     a question could apply to any message type. In addition, users can confuse the
        //     question mark symbol with a help information symbol. Therefore, do not use this
        //     question mark symbol in your message boxes. The system continues to support its
        //     inclusion only for backward compatibility.
        Question = 32,
        //
        // Сводка:
        //     The message box contains a symbol consisting of an exclamation point in a triangle
        //     with a yellow background.
        Warning = 48,
        //
        // Сводка:
        //     The message box contains a symbol consisting of a lowercase letter i in a circle.
        Information = 64
    }
    public enum BoxResult
    {
        //
        // Сводка:
        //     The message box returns no result.
        None = 0,
        //
        // Сводка:
        //     The result value of the message box is OK.
        OK = 1,
        //
        // Сводка:
        //     The result value of the message box is Cancel.
        Cancel = 2,
        //
        // Сводка:
        //     The result value of the message box is Yes.
        Yes = 6,
        //
        // Сводка:
        //     The result value of the message box is No.
        No = 7
    }
    //
    // Сводка:
    //     Specifies special display options for a message box.
    [Flags]
    public enum BoxOptions
    {
        //
        // Сводка:
        //     No options are set.
        None = 0,
        //
        // Сводка:
        //     The message box is displayed on the default desktop of the interactive window
        //     station. Specifies that the message box is displayed from a .NET Windows Service
        //     application in order to notify the user of an event.
        DefaultDesktopOnly = 131072,
        //
        // Сводка:
        //     The message box text and title bar caption are right-aligned.
        RightAlign = 524288,
        //
        // Сводка:
        //     All text, buttons, icons, and title bars are displayed right-to-left.
        RtlReading = 1048576,
        //
        // Сводка:
        //     The message box is displayed on the currently active desktop even if a user is
        //     not logged on to the computer. Specifies that the message box is displayed from
        //     a .NET Windows Service application in order to notify the user of an event.
        ServiceNotification = 2097152
    }

    public interface IMessageBox
    {
        public BoxResult Show(string messageBoxText, string caption, BoxButton button, BoxImage icon, 
               BoxResult defaultResult = BoxResult.None, BoxOptions options = BoxOptions.None);
    }
    public class SaveFileItem
    {
        public string DrityFileName {  get; set; } = string.Empty;
        public IList<SaveFileItem>? Items { get; set; }
    }
    public static class VMBaseFileDocumentExt
    {
        public static bool DrityOrHasChildDrity(this VMBaseFileDocument d)
        {
            if (d.IsDirty) return true;
            
            if (d is ComplexFileDocumentVM c && c.ChildDocuments != null) 
                foreach (var sd in c.ChildDocuments) 
                    if (sd.DrityOrHasChildDrity()) return true; 
            return false;
        }
        public static SaveFileItem? GenerateDrityTree(this VMBaseFileDocument d, SaveFileItem? root = null) 
        {
            if (!d.DrityOrHasChildDrity()) return null;
            SaveFileItem Res = new SaveFileItem { DrityFileName = d.DrityFileName };
            if (root != null)
            {
                if (root.Items == null ) root.Items = new List<SaveFileItem>();
                root.Items.Add(Res);
            }
            if (d is ComplexFileDocumentVM c && c.ChildDocuments != null) foreach (var sd in c.ChildDocuments)  sd.GenerateDrityTree(Res); 
            
            return Res;
        }
    }

    public interface ISaveFilesDialog
    {
        public BoxResult Show(IList<SaveFileItem> RootItems);
    }
    public enum VisitAddToGroup
    {
        AddToCurrent,
        NewGroup,
        SingleVisit
    }
    //public record  CreateNewVisitDialogResult(        
    //    BoxResult BoxResult, 
    //    string VisitFile, 
    //    string GroupFile,
    //    VisitAddToGroup VisitAddToGroup
    //    );
    public interface ICreateNewVisitDialog
    {
        string VisitFile { get; }
        string GroupFile { get; }
        VisitAddToGroup VisitAddToGroup { get; }
        public BoxResult Show();
    }
    public interface IFileDialog
    {
        public string Title { get; set; }
        public string InitialDirectory { get; set; }
        public string Filter { get; set; }
        public bool ValidateNames { get; set; }
        public IList<object> CustomPlaces { get; set; }
        public bool CheckPathExists { get; set; }
        public bool CheckFileExists { get; set; }
        public bool AddExtension { get; set; }
        public string DefaultExt { get; set; }
        public bool ShowDialog();
        //
        // Сводка:
        //     Gets an array that contains one file name for each selected file.
        //
        // Возврат:
        //     An array of System.String that contains one file name for each selected file.
        //     The default is an array with a single item whose value is System.String.Empty.
        public string[] FileNames { get; }
        //
        // Сводка:
        //     Gets or sets a string containing the full path of the file selected in a file
        //     dialog.
        //
        // Возврат:
        //     A System.String that is the full path of the file selected in the file dialog.
        //     The default is System.String.Empty.
        public string FileName { get; set; }
    }

    public interface IFileOpenDialog: IFileDialog
    {
        public bool ReadOnlyChecked { get; set; }
        public bool ShowReadOnly { get; set; }
    }
    public interface IFileSaveDialog : IFileDialog
    {
        public bool CreatePrompt { get; set; }
        public bool OverwritePrompt { get; set; }
    }
    public interface IOpenFolderDialog
    {
        public string Title { get; set; }
        public string InitialDirectory { get; set; }
        public bool ValidateNames { get; set; }
        public IList<object> CustomPlaces { get; set; }
        public bool ShowDialog();
        string FolderName { get; set; }
    }
}
