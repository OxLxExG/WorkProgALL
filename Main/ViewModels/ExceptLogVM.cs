using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows;
using System.Windows.Controls;
using TextBlockLogging;
using Microsoft.Extensions.DependencyInjection;
using WpfDialogs;
using Xceed.Wpf.AvalonDock.Layout;
using Core;
using System.Windows.Media.Media3D;
using Microsoft.Extensions.Logging;
using System.Diagnostics.Eventing.Reader;
using System.Threading;
using System.Windows.Threading;

namespace Main.ViewModels
{
    public class LogMenuFactory<LogVM> : IMenuItemClient
        where LogVM: TextLogVM
    {
        string header;
        int prioInc; 
        string? toolicon; 
        string? toolTip;
        public LogMenuFactory(string header, int prioInc, string? toolicon = null, string? toolTip = null) 
        { 
            this.header = header;
            this.toolicon = toolicon;
            this.prioInc = prioInc;
            this.toolTip = toolTip;
        }
        void IMenuItemClient.AddStaticMenus(IMenuItemServer _menuItemServer)
        {
            _menuItemServer.Add(RootMenusID.NDebugs, new[]
            {
                new CommandMenuItemVM
                {
                    ContentID = nameof(LogVM),
                    Header = header,
                    IconSource = toolicon == null ? null: toolicon,
                    Priority = 100+prioInc,
                    Command = new RelayCommand(ShowLogForm),
                },
            });

            if (toolicon != null)
            {
                IToolServer toolServer = VMBase.ServiceProvider.GetRequiredService<IToolServer>();
                toolServer.Add("ToolText", new[]
                {
                    new ToolButton
                    {
                        ToolTip = toolTip == null ? null : new ToolTip { Content = toolTip },
                        ContentID = nameof(LogVM),
                        IconSource = toolicon,
                        Priority = -990+prioInc,
                        Command = new RelayCommand(ShowLogForm)
                    },
                });
            }
        }
        private void ShowLogForm() => DockManagerVM.AddOrGetandShow(typeof(LogVM).Name, FormAddedFrom.User/*, VMBase.ServiceProvider.GetRequiredService<LogVM>*/);
    }
    public class TextLogVM : ToolVM
    {
        public delegate void ClearAction();
        public event ClearAction? OnClear;
        public ICommand Clear => new RelayCommand(() => OnClear?.Invoke());
        public TextLogVM()
        {
            ShowStrategy = Core.ShowStrategy.Bottom;
            FloatingWidth = 400;
            FloatingHeight = 200;
            AutoHideHeight = 200;
            AutoHideWidth = 400;
            FloatingTop = (SystemParameters.PrimaryScreenHeight - 200) / 2;
            FloatingLeft = (SystemParameters.PrimaryScreenWidth - 400) / 2;
            CanDockAsTabbedDocument = true;
            OnMenuActivate += ActivateDynItems;
            OnMenuDeActivate += DeActivateDynItems;
        }

        private bool freeze = false;
        public bool Freeze
        {
            get => freeze;
            set
            {
                if (freeze != value)
                {
                    freeze = value;
                    // binding formVM.Freeze => buttonVM.IsChecked
                    if (_freezeToolButton != null) _freezeToolButton.IsChecked = freeze;
                    OnPropertyChanged(nameof(Freeze));
                }
            }
        }
        CheckToolButton? _freezeToolButton;
        private void ActivateDynItems()
        {
            _freezeToolButton = new CheckToolButton
            {
                ToolTip = new ToolTip { Content = Properties.Resources.m_Freeze + " (" + Title + ")" },
                ContentID = "Freeze" + ContentID,
                IconSource = "pack://application:,,,/Images/Freeze.png",
                Priority = 101,
                IsChecked = Freeze,
                // binding  buttonVM.Command=>formVM.Freeze=>buttonVM.IsChecked
                Command = new RelayCommand(() => Freeze = !Freeze),
            };
            var clb = new ToolButton
            {
                ToolTip = new ToolTip { Content = Properties.Resources.m_Clear + " (" + Title + ")" },
                ContentID = "Clear" + ContentID,
                IconSource = "pack://application:,,,/Images/Clear.png",
                Priority = 100,
                Command = new RelayCommand(() => OnClear?.Invoke())
            };
            var range = new[] { _freezeToolButton, clb };

            IToolServer toolServer = ServiceProvider.GetRequiredService<IToolServer>();
            toolServer.Add("ToolGlyph", range);
            DynamicItems.AddRange(range);

            var l = ServiceProvider.GetRequiredService<ILogger<LogTextBlock>>();
            l.LogTrace(" ActivateToolMenu {} ", ContentID);
        }
        private void DeActivateDynItems()
        {
            _freezeToolButton = null;
            var l = ServiceProvider.GetRequiredService<ILogger<LogTextBlock>>();
            l.LogTrace("~ActivateToolMenu {} ", ContentID);
        }
    }
    public class ExceptLogMenuFactory : LogMenuFactory<ExceptLogVM>
    {
        public ExceptLogMenuFactory()
            : base(
                 Properties.Resources.strTitleExceptions,
                 0,
                 "pack://application:,,,/Images/ExceptLog.png",
                 $"{Properties.Resources.m_Show}: {Properties.Resources.strTitleExceptions}")
        { }
    }
    public class TraceLogMenuFactory : LogMenuFactory<TraceLogVM>
    {
        public TraceLogMenuFactory()
            : base(
                 Properties.Resources.m_Trace,
                 2,
                 "pack://application:,,,/Images/TraceLog.png",
                 $"{Properties.Resources.m_Show}: {Properties.Resources.m_Trace}")
        { }
    }
    public class InfoLogMenuFactory : LogMenuFactory<InfoLogVM>
    {
        public InfoLogMenuFactory()
            : base(
                 Properties.Resources.m_Info,
                 1,
                 "pack://application:,,,/Images/InfoLog.png",
                 $"{Properties.Resources.m_Show}: {Properties.Resources.m_Info}")
        { }
    }
    public class ExceptLogVM : TextLogVM
    {
        public ExceptLogVM()
        {
            Title = Properties.Resources.strTitleExceptions;
            ContentID = nameof(ExceptLogVM);
            IconSource = new Uri("pack://application:,,,/Images/ExceptLog.png");
            ToolTip = Title;
            CanClose = true;
        }
    }
    public class TraceLogVM : TextLogVM
    {
        public TraceLogVM()
        {
            Title = Properties.Resources.m_Trace;
            ContentID = nameof(TraceLogVM);
            IconSource = new Uri("pack://application:,,,/Images/TraceLog.png");
            ToolTip = Title;
            CanClose = true;
        }
    }
    public class InfoLogVM : TextLogVM
    {
        public InfoLogVM()
        {
            Title = Properties.Resources.m_Info;
            ContentID = nameof(InfoLogVM);
            IconSource = new Uri("pack://application:,,,/Images/InfoLog.png");
            ToolTip = Title;
            CanClose = true;
        }
    }
}
