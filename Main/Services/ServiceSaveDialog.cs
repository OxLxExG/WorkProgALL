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
    internal class NewVisitDialog : ICreateNewVisitDialog
    {
        public bool AddToCurrent { get ; set; }

        public BoxResult Show(Action<CreateNewVisitDialogResult> result)
        {
            CreateVisitDialog createVisitDialog = new CreateVisitDialog();
            var vm = (CreateVisitDialogVM)createVisitDialog.DataContext;
            if (AddToCurrent)
            {
                vm.CurrentGroupSelected = true;
                createVisitDialog.SetOnlyAdd();
            }
            vm.result = result;            
            createVisitDialog.ShowDialog();
            return createVisitDialog.Result;
        }
    }
}
