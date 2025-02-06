using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Collections.ObjectModel;
using System.Windows.Media.Imaging;
using System.Windows.Controls.Primitives;

namespace Core
{
    public class VMBase: ObservableObject//, IServiceProvider
    {        
        public static IServiceProvider ServiceProvider => (IServiceProvider) Application.Current;
        public IMessageBox MsgBox => ServiceProvider.GetRequiredService<IMessageBox>();
        public static IFileOpenDialog FOpen() => ServiceProvider.GetRequiredService<IFileOpenDialog>();
        public static IFileSaveDialog FSave() => ServiceProvider.GetRequiredService<IFileSaveDialog>();
       // public object? GetService(Type serviceType) => ((IServiceProvider) Application.Current).GetService(serviceType);
        public static IMenuItemServer MenuItemServer => ServiceProvider.GetRequiredService<IMenuItemServer>();
        public static IToolServer ToolBarServer => ServiceProvider.GetRequiredService<IToolServer>();
        public static GlobalSettings globalSettings => ServiceProvider.GetRequiredService<GlobalSettings>();
        //TODO: set -должен быть с PropertyChanged или виртуальный анализировать AnyData.AnyData
        /// <param name="RootContentID"> RootContentID.AnyData.AnyData...) </param>
        // при загрузки DockManager  ContentID присваивается генерирующейся VM viewмодели AddOrGet(string ContentID);
        // например для монитора СОМ если нет модели MM (COM) то закрыть (удалить) окно
        string? _ContentID;
        public virtual string? ContentID { get=>_ContentID; set=>SetProperty(ref _ContentID,value); }
    }
}
