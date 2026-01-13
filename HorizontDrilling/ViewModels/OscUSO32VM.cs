using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Xml.Serialization;
using Connections;
using Core;

namespace HorizontDrilling.ViewModels
{
    using Global;
    using HorizontDrilling.Properties;

    [RegService(null, IsSingle: false, Advanced: AdvancedRegs.Form)]
    public class OscUSO32VM: ToolVM
    {
        float _width;
        public float width { get => _width; set => _width = value; }
        string _Name = string.Empty;
        public string Name { get=>_Name; 
            set 
            {
                if (_Name != value)
                {
                    _Name = value;
                    Title = $"{Name}: {Resources.strOsc}";
                    ToolTip = Title;
                    OnPropertyChanged();
                }
            } 
        } 
        public OscUSO32VM()
        {
            ShowStrategy = Core.ShowStrategy.Bottom;
            FloatingWidth = 400;
            FloatingHeight = 200;
            AutoHideHeight = 200;
            AutoHideWidth = 400;
            FloatingTop = (SystemParameters.PrimaryScreenHeight - 200) / 2;
            FloatingLeft = (SystemParameters.PrimaryScreenWidth - 400) / 2;
            CanClose = true;
            IconSource = "\ue9d9";
        }
    }
}
