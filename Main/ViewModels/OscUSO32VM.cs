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

namespace Main.ViewModels
{
    public class OscUSO32VM: ToolVM
    {

        public void OnUSO32Data(BaseBusUSOVM sender, int data)
        {
            BindToView?.Invoke(data);
            if (sender is BusUSO32VM b)
            {
                Int32.TryParse(b.Amp, out Amp); 
            }
        }

        [XmlIgnore] public int Amp = 1;

        Action<int>? BindToView;// {  get; set; }
                                // 
        bool BindUsoData;
        public void ViewLoaded(Action<int> action)
        {
            BindToView = action;
            if (!BindUsoData)
            {
                var src = RootFileDocumentVM.Find(ContentID!.Split('.', StringSplitOptions.RemoveEmptyEntries)[1]);
                if (src is BaseBusUSOVM b)
                {
                    b.OnUSODataEvent += OnUSO32Data;
                    BindUsoData = true;
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
            Title = "Osc USO32";            
            IconSource = new Uri("pack://application:,,,/Images/Osc.png");
            ToolTip = Title;
        }
    }
}
