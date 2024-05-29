using CommunityToolkit.Mvvm.Input;
using Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Xaml.Behaviors;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.ConstrainedExecution;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Xml.Serialization;
using WorkProgMain.Models;
using WorkProgMain.Properties;
using WpfDialogs;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;
using static System.Net.WebRequestMethods;

namespace WorkProgMain.ViewModels
{
    //    <!--<c:PriorityMenu Priority = "0" Name="NFile" Header="{x:Static res:Resources.m_File}">
    //    <c:PriorityMenu Priority = "11" Header="_Save" Command="{Binding DocLayoutSaveCommand}"/>
    //    <c:PriorityMenu Priority = "12" Header="_Restore" Command="{Binding DocLayoutRestoreCommand}"/>
    //    <c:PriorityMenu Priority = "13" Header="_Load" Command="{Binding DocLayoutLoadCommand}"/>
    //    <c:PriorityMenu Priority = "14" Header="_UnLoad" Command="{Binding DocLayoutUnloadCommand}"/>
    //    <c:PrioritySeparator Priority = "1000" />
    //    -->< !--< a:MenuItemEx Header = "_Avalon" /> -->< !--
    //    < c:PriorityMenu Priority = "1111" Header="E_xit" Command="{Binding CloseWindowCommand}" 
    //              CommandParameter="{Binding ElementName=WindowMain}"/>
    //</c:PriorityMenu>
    //<c:PriorityMenu Priority = "1" Name="NShow" Header="{x:Static res:Resources.m_Show}"/>
    //<c:PriorityMenu Priority = "2" Name="NHidden" Header="{x:Static res:Resources.m_Hidden}">
    //    <i:Interaction.Triggers>
    //        <i:EventTrigger EventName = "SubmenuOpened" >
    //            < i:InvokeCommandAction Command = "{Binding HiddenSubmenuOpenedCommand}"
    //                                   CommandParameter="{Binding ElementName=NHidden}"/>
    //        </i:EventTrigger>
    //    </i:Interaction.Triggers>
    //    <c:PriorityMenu Priority = "0" Name="NShowAll" Header="{x:Static res:Resources.m_ShowAll}"
    //              Command="{Binding ShowAllHiddenCommand}"
    //              CommandParameter="{Binding ElementName=NHidden}"/>
    //    <c:PrioritySeparator Priority = "100" Name="NShowAllSeparator"/>
    //</c:PriorityMenu>
    //<c:PriorityMenu Priority = "11" Name="NControll" Header="{x:Static res:Resources.m_Controll}" Visibility="Collapsed"/>
    //<c:PriorityMenu Priority = "12" Name="NMetrology" Header="{x:Static res:Resources.m_Metrology}" Visibility="Collapsed"/>-->
    //public class HiddenVM: OnSubMenuOpenMenuItemVM { }
    public class MenuVM : VMBase//, IMenuItemServer
    {
        public ObservableCollection<PriorityItem> RootItems => MenuServer.Items;
        public MenuVM(IMenuItemServer menuItemServer) 
        {
            MMenus.CreateMenusStructure();

            menuItemServer.Add(RootMenusID.NFile, new MenuItemVM[]
            {
                        #region TEST Menus
                        //new CommandMenuItemVM
                        //{
                        //    Priority=1190,
                        //    Header="HIde Dock Manager",
                        //    Command= new RelayCommand(() =>
                        //        MainVindowVM.ActionHideDockManager?.Invoke())
                        //},
                        //new CommandMenuItemVM
                        //{
                        //    Priority=1199,
                        //    Header="Show Dock Manager",
                        //    Command= new RelayCommand(() =>
                        //        MainVindowVM.ActionShowDockManager?.Invoke())
                        //},
                        //new CommandMenuItemVM
                        //{
                        //    Priority=3199,
                        //    Header="Save Dock Manager",
                        //    Command= new RelayCommand(() =>
                        //    {
                        //        var d = FSave();
                        //        d.Title = Properties.Resources.nfile_Create;
                        //        d.Filter = "Text documents (.xml)|*.xml|Any documents (.doc)|*.doc|Any (*)|*";
                        //        d.DefaultExt = ".xml";
                        //        d.OverwritePrompt = true;
                        //        d.CreatePrompt = true;
                        //        d.CustomPlaces = new object[]
                        //        {
                        //            @"C:\Users\Public\Documents\Горизонт\WorkProg\Projects\",
                        //            @"C:\XE\Projects\Device2\_exe\Debug\Метрология\",
                        //            @"G:\Мой диск\mtr\",
                        //            new Guid("FDD39AD0-238F-46AF-ADB4-6C85480369C7"),//документы
                        //            new Guid("1777F761-68AD-4D8A-87BD-30B759FA33DD"),//избрвнное
                        //        };
                        //        if (d.ShowDialog())
                        //        {
                        //            ServiceProvider.GetRequiredService<IDockManagerSerialization>().Serialize(d.FileName);
                        //        }
                        //    }),

                        //    //Command = new RelayCommand<string>(f =>MainVindowVM.ActionSaveDockManager?.Invoke(f !))
                        //    //OnSelectFileAction = f =>
                        //    //    MainVindowVM.ActionSaveDockManager?.Invoke(f)                            
                        //    //{
                        //    //    //var all = dockManager.Layout.Descendents().OfType<LayoutContent>().ToArray();
                        //    //    //foreach (var n in all)
                        //    //    //{
                        //    //    //    XmlSerializer writer = new XmlSerializer(typeof(FormBase));
                        //    //    //    //null,
                        //    //    //    //AbstractConnection.GetConnectionTypes(), null, null);
                        //    //    //    StringWriter s = new StringWriter();
                        //    //    //    writer.Serialize(s, n.Content);

                        //    //    //    n.ContentId = s.ToString();
                        //    //    //}
                        //    //    var serializer = new XmlLayoutSerializer(dockManager);
                        //    //    using (var stream = new StreamWriter(string.Format(@".\AvalonDock_{0}.xml", 1)))
                        //    //        serializer.Serialize(stream);
                        //    //})
                        //},
                        //new CommandMenuItemVM
                        //{
                        //    Priority=3199,
                        //    Header="Restore Dock Manager",
                        //    Command= new RelayCommand(() =>
                        //    {
                        //        var d = FOpen();
                        //        d.Title = Properties.Resources.nfile_Open;
                        //        d.Filter = "Text documents (.xml)|*.xml|Any documents (.doc)|*.doc|Any (*)|*";
                        //        d.DefaultExt = ".xml";
                        //        d.CustomPlaces = new object[]
                        //        {
                        //            @"C:\Users\Public\Documents\Горизонт\WorkProg\Projects\",
                        //            @"C:\XE\Projects\Device2\_exe\Debug\Метрология\",
                        //            @"G:\Мой диск\mtr\",
                        //            new Guid("FDD39AD0-238F-46AF-ADB4-6C85480369C7"),//документы
                        //            new Guid("1777F761-68AD-4D8A-87BD-30B759FA33DD"),//избрвнное
                        //        };
                        //        if (d.ShowDialog())
                        //        {
                        //            DockManagerVM.Clear();
                        //            ServiceProvider.GetRequiredService<IDockManagerSerialization>().Deserialize(d.FileName);
                        //            VMBaseForm.OnVisibleChange(null);
                        //        }
                        //    }),
                        //    //Command = new RelayCommand<string>(f =>
                        //    //{
                        //    //    //DockManagerVM.Clear();
                        //    //    //MainVindowVM.ActionLoadDockManager?.Invoke(f!);
                        //    //    VMBaseForms.OnVisibleChange(this);

                        //    //})
                        //    //OnSelectFileAction = f =>
                        //    //{
                        //    //    DockManagerVM.Clear();
                        //    //    MainVindowVM.ActionLoadDockManager?.Invoke(f);
                        //    //    VMBaseForms.OnVisibleChange(this);
                        //    //},
                        //},
                        #endregion

                        new CommandMenuItemVM 
                        {
                            Priority = MMenus.SaveAll.Priority,
                            Header = MMenus.SaveAll.Header,
                            ContentID = MMenus.SaveAll.ContentID,
                            InputGestureText = "Ctrl+S",
                            Command = new RelayCommand(() => RootFileDocumentVM.Instance?.Save())
                        },
                        new CommandMenuItemVM 
                        {
                            Priority = MMenus.CloseAll.Priority,
                            Header = MMenus.CloseAll.Header, 
                            ContentID = MMenus.CloseAll.ContentID,
                            Command = new RelayCommand(() => 
                            {
                                RootFileDocumentVM.Instance?.Remove();
                                RootFileDocumentVM.Instance = null;
                                DockManagerVM.Clear();
                            })
                        },
                        new CommandMenuItemVM
                        {
                            Priority =100000,
                            InputGestureText="Ctrl+X",
                            Header="E_xit",
                            Command = new CloseProgramCommand()                            
                        },

            }); 
            menuItemServer.Add(RootMenusID.ROOT,new MenuHiddens());

        }
    }
}
