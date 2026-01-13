using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;

namespace Core
{
    public class VMGlobal : ObservableObject//, IServiceProvider
    {
        public static IServiceProvider ServiceProvider => (IServiceProvider)Application.Current;


        //TODO: set -должен быть с PropertyChanged или виртуальный анализировать AnyData.AnyData
        /// <param name="RootContentID"> RootContentID@AnyData@AnyData...) </param>
        // при загрузки DockManager  ContentID присваивается генерирующейся VM viewмодели AddOrGet(string ContentID);
        // например для монитора СОМ если нет модели MM (COM) то закрыть (удалить) окно

        string? _ContentID;
        public string[]? ContentIDs=> _ContentID?.Split("@",StringSplitOptions.RemoveEmptyEntries);

        public static string[] SplitID(string contentID)
        {
            return contentID.Split("@", StringSplitOptions.RemoveEmptyEntries);
        }
        public virtual string? ContentID { get => _ContentID; set => SetProperty(ref _ContentID, value); }
    }

    public class VMBase: VMGlobal//, IServiceProvider
    {        
        public IMessageBox MsgBox => ServiceProvider.GetRequiredService<IMessageBox>();
        public static IFileOpenDialog FOpen() => ServiceProvider.GetRequiredService<IFileOpenDialog>();
        public static IFileSaveDialog FSave() => ServiceProvider.GetRequiredService<IFileSaveDialog>();
       // public object? GetService(Type serviceType) => ((IServiceProvider) Application.Current).GetService(serviceType);
        public static IMenuItemServer MenuItemServer => ServiceProvider.GetRequiredService<IMenuItemServer>();
        public static IToolServer ToolBarServer => ServiceProvider.GetRequiredService<IToolServer>();
    }
}
