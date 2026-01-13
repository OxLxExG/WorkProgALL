using CommunityToolkit.Mvvm.Collections;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace Core
{
    public class ToolItem : PriorityItemBase;
    public class ToolButton : ToolItem
    {
        object? _ContentTemplate;
        public object? ContentTemplate { get=>_ContentTemplate; set => SetProperty (ref _ContentTemplate,value); }

        object ? _Content;
        //string? _StringContent;
        public object? Content { get => _Content; set => SetProperty(ref _Content, value); }
        //    { 
        //        if (_Content == value) return;
        //        if (value is string s && s != _StringContent)
        //        {
        //            _StringContent = s;
        //            if (Uri.TryCreate(s, UriKind.Absolute, out var uriResult))
        //            {
        //                var im = new Image
        //                {
        //                    Source = new BitmapImage(uriResult),
        //                };
        //                SetProperty(ref _Content, im);
        //            }
        //            else
        //            {
        //                SetProperty(ref _Content, value);
        //            }
        //        }
        //        else SetProperty(ref _Content, value);
        //    } 
        //}
        public int FontSize { get; set; } = 16;
        public int Size { get; set; } = 24;
        public ICommand? Command { get; set; }
    }
    public class CheckToolButton : ToolButton
    {
        bool _checked;
        public bool IsChecked { get { return _checked; } set { SetProperty(ref _checked, value); } }
        //    {
        //        if (_checked != value) 
        //        {
        //            _checked = value;
        //            OnPropertyChanged(nameof(IsChecked));
        //        }                
        //    } 
        //}
    }
    public class ToolComboBox : ToolItem
    {
        public string ItemStringFormat { get; set; } = string.Empty;
        public IEnumerable? ItemsSource { get; set; }

        string _Text = string.Empty;
        public string Text { get => _Text; set => SetProperty(ref _Text, value); }
        public bool IsEditable { get; set; }
        public bool IsReadOnly { get; set; }
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
