using CommunityToolkit.Mvvm.Collections;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace Core
{
    public class ToolButton : PriorityItemBase
    {
        public ICommand? Command { get; set; }
    }
    public class CheckToolButton : ToolButton
    {
        bool _checked;
        public bool IsChecked { get { return _checked; } set 
            {
                if (_checked != value) 
                {
                    _checked = value;
                    OnPropertyChanged(nameof(IsChecked));
                }                
            } 
        }
    }
    public class ToolBarVM: VMBase
    {
        public ObservableCollection<PriorityItem> Items { get; private set; }
        public ToolBarVM() 
        {
            Items = new ObservableCollection<PriorityItem>();
        }
    }
}
