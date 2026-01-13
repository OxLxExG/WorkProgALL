using CommunityToolkit.Mvvm.Input;
using Core;
using Global;
using Loggin;
using Serilog;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Xml.Serialization;
using System.Runtime.CompilerServices;

namespace HorizontDrilling.ViewModels
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
                toolServer.Add("ToolText", new ToolButton
                {
                    ToolTip = toolTip == null ? null : new ToolTip { Content = toolTip },
                    ContentID = nameof(LogVM),
                    Priority = -990 + prioInc,
                    Content = toolicon,
                    Command = new RelayCommand(ShowLogForm)
                });
            }
        }
        private void ShowLogForm() => DockManagerVM.AddOrGetandShow(typeof(LogVM).Name, FormAddedFrom.User/*, VMBase.ServiceProvider.GetRequiredService<LogVM>*/);
    }
    public abstract class TextLogVM : ToolVM
    {
        //public delegate void ClearAction();
        //public event ClearAction? OnClear;

        Action? OnClear;

        protected abstract void FreezeLogger();
        public void InitLogger(Action ClearBox)
        {
            OnClear = ClearBox;
        }
        public ICommand  Clear => new RelayCommand(() => OnClear?.Invoke());
        public ICommand DoFreeze => new RelayCommand(() => Freeze = !Freeze);

        CommandMenuItemVM frz;
        [XmlIgnore] public ObservableCollection<PriorityItem> CItems { get; set; } = new ObservableCollection<PriorityItem>();
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
            DynAdapter.OnActivateDynItems += ActivateDynItems;
            DynAdapter.OnDeActivateDynItems += DeActivateDynItems;
            CItems.Add(new CommandMenuItemVM
            {
                Header = Properties.Resources.m_Clear,
                Command = Clear ,
                IconSource = "pack://application:,,,/Images/Clear.png",
            });
            frz = new CommandMenuItemVM
            {
                Header = Properties.Resources.m_Freeze,
                IsChecked = Freeze,
                IsCheckable = true,
                Command = DoFreeze,
                IconSource = "pack://application:,,,/Images/Freeze.png",
            };
            CItems.Add(frz);
            CItems.Add(new Core.Separator());
            CItems.Add(new CommandMenuItemVM
            {
                Header = Properties.Resources.nfile_Close,
                Command = new RelayCommand(Close),
            });
            CItems.Add(new Core.Separator());
            CItems.Add(new CommandMenuItemVM
            {
                Header = "Generate Error",
                Command = new RelayCommand(() =>
                {
                    throw new FlagsException("Generate Test Error");
                }),
            });
            CItems.Add(new CommandMenuItemVM
            {
                Header = "Generate Info",
                Command = new RelayCommand(() =>
                {
                    Logger.Info?.Information("Info Generated");
                }),
            });
            CItems.Add(new CommandMenuItemVM
            {
                Header = "Generate Warning",
                Command = new RelayCommand(() =>
                {
                    Log.Warning("Warning Generated");
                }),
            });
        }
        public override void Close()
        {
            DynAdapter.OnActivateDynItems -= ActivateDynItems;
            DynAdapter.OnDeActivateDynItems -= DeActivateDynItems;
            base.Close();
        }

        private bool freeze = false;
        [XmlIgnore] public bool Freeze
        {
            get => freeze;
            set
            {
                if (SetProperty(ref freeze, value))
                {
                    if ( _freezeToolButton != null
                    && _freezeToolButton.TryGetTarget(out var tb)
                    && tb != null) tb.IsChecked = freeze;
                    frz.IsChecked = freeze;
                    FreezeLogger();
                }
            }
        }
        WeakReference<CheckToolButton>? _freezeToolButton;
        protected virtual void ActivateDynItems()
        {
            var ftb = new CheckToolButton
            {
                ToolTip = new ToolTip { Content = Properties.Resources.m_Freeze + " (" + Title + ")" },
                ContentID = "Freeze" + ContentID,
                Content = "pack://application:,,,/Images/Freeze.png",
                Priority = 1001,
                FontSize = 32,
                Size = 40,
                IsChecked = Freeze,
                // binding  buttonVM.Command=>formVM.Freeze=>buttonVM.IsChecked
                Command = DoFreeze,
            };
            _freezeToolButton = new WeakReference<CheckToolButton>(ftb);
            var clb = new ToolButton
            {
                ToolTip = new ToolTip { Content = Properties.Resources.m_Clear + " (" + Title + ")" },
                ContentID = "Clear" + ContentID,
                Content = "pack://application:,,,/Images/Clear.png",
                FontSize = 32,
                Size = 40,
                Priority = 1000,
                Command = Clear
            };
            var range = new[] { ftb, clb };

            //IToolServer toolServer = ServiceProvider.GetRequiredService<IToolServer>();
            ToolBarServer.Add("ToolGlyph", range);
            DynAdapter.DynamicItems.AddRange(range);

           // var l = ServiceProvider.GetRequiredService<ILogger<LogTextBlock>>();
           Logger.Trace?.Debug(" ActivateToolMenu {ContentID}", ContentID);
        }
        protected virtual void DeActivateDynItems()
        {
            // var l = ServiceProvider.GetRequiredService<ILogger<LogTextBlock>>();
            Logger.Trace?.Debug("~ActivateToolMenu {ContentID}", ContentID);
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
                 "\ue946",
                 $"{Properties.Resources.m_Show}: {Properties.Resources.m_Info}")
        { }
    }
    public class ExceptLogVM : TextLogVM
    {
        public ExceptLogVM()
        {
            Title = Properties.Resources.strTitleExceptions;
            ContentID = nameof(ExceptLogVM);
            IconSource = "pack://application:,,,/Images/ExceptLog.png";
            ToolTip = Title;
            CanClose = true;
        }
        protected override void FreezeLogger()
        {
           Logger.ErrorLevel.MinimumLevel = Freeze? Logger.LevelStop : Logger.LevelDefaultError;
        }
    }
    public class TraceLogVM : TextLogVM
    {
        protected override void FreezeLogger()
        {
            Logger.TraceLevel.MinimumLevel = Freeze ? Logger.LevelStop : Logger.LevelDefaultTrace;
        }
        public TraceLogVM()
        {
            Title = Properties.Resources.m_Trace;
            ContentID = nameof(TraceLogVM);
            IconSource = "pack://application:,,,/Images/TraceLog.png";
            ToolTip = Title;
            CanClose = true;
        }
    }
    public class InfoLogVM : TextLogVM
    {
        protected override void FreezeLogger()
        {
            Logger.InfoLevel.MinimumLevel = Freeze ? Logger.LevelStop : Logger.LevelDefaultInfo;
        }
        public InfoLogVM()
        {
            Title = Properties.Resources.m_Info;
            ContentID = nameof(InfoLogVM);
            IconSource = "\ue946";
            ToolTip = Title;
            CanClose = true;
        }
    }
}
