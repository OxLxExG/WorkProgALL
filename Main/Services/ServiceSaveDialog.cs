using Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WorkProgMain.Views;

namespace WorkProgMain.Services
{
    internal class ServiceSaveDialog : ISaveFilesDialog
    {
        public BoxResult Show(IList<SaveFileItem> RootItems)
        {
            var f = new FileSaveDialog();
            f.treeView.ItemsSource = RootItems;
            f.ShowDialog();
            return f.Result;
        }
    }
}
