using Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Main.Views;

namespace Main.Services
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
    internal class CreateNewVisitDialog : ICreateNewVisitDialog
    {
        CreateVisitDialog createVisitDialog = new CreateVisitDialog();
        public string VisitFile => ((CreateNewVisitDialog) createVisitDialog.DataContext).VisitFile;

        public string GroupFile => ((CreateNewVisitDialog)createVisitDialog.DataContext).GroupFile;

        public VisitAddToGroup VisitAddToGroup => ((CreateNewVisitDialog)createVisitDialog.DataContext).VisitAddToGroup;

        public BoxResult Show()
        {
            createVisitDialog.ShowDialog();
            return createVisitDialog.Result;
        }
    }
}
