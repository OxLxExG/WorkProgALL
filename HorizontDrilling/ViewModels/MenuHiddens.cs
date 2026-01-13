using CommunityToolkit.Mvvm.Input;
using Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace HorizontDrilling.ViewModels
{
    public class MenuHidden : CommandMenuItemVM { }

    public class MenuHiddens: OnSubMenuOpenMenuItemVM
    {
        public static MenuHiddens? Instance;

        public MenuHiddens() 
        {
            Instance = this;
          //  IconSource = "pack://application:,,,/Images/DockPane.PNG";
            Priority = 20;
            Header = Properties.Resources.m_Hidden;
            ContentID = RootMenusID.NHidden;

            DockManagerVM.FormVisibleChanged += s => IsEnable = DockManagerVM.Hiddens.Count() > 0;
            DockManagerVM.FormsCleared += (d,e) => IsEnable = false; 

            Items.Add(
                        new MenuHidden
                        {
                            Header = Properties.Resources.m_ShowAll,
                            Priority = 0,
                            Command = new RelayCommand(() =>
                            {
                                for (int i = 2; i < Instance!.Items.Count; i++)
                                    (Instance.Items[i] as CommandMenuItemVM)?.Command?.Execute(null);
                            })
                        });
            Items.Add(  new Separator { Priority = 0 });

            OnSubMenuAction = () =>
            {
                while (Instance!.Items.Count > 2) Instance.Items.RemoveAt(Instance.Items.Count - 1);

                foreach (var a in DockManagerVM.Hiddens)
                {
                    Instance.Items.Add(new MenuHidden
                    {
                        Header = a.Title,
                        Priority = 100,
                        Command = new RelayCommand(() => a.IsVisible = true),
                    });
                }
            };
        }
    }
}
