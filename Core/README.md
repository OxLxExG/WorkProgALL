# Core
Описывает глобальные переменные, классы, интерфейсы, стили, привязки
## VM
VMBase.cs - определяет класс от которого происходят все VM
```
    public class VMBase: ObservableObject
    {        
        public static IServiceProvider ServiceProvider => (IServiceProvider) Application.Current;
        public virtual string? ContentID { get; set; }
    }
```
## Priority 
### PriorityItems.cs
 Главное окно приложения имеет меню и ToolBarы, 
 VM их элементов потомки PriorityItem,
```
    public abstract class PriorityItem: VMBase
    {
        public int Priority { get; set; } = 100;
        public int Group => Priority / 100;
    }
    public class Separator: PriorityItem { }

    public abstract class PriorityItemBase : PriorityItem 
    {
        #region Visibility
        #region IsEnable

        public ToolTip? ToolTip { get; set; }
        public bool IconSourceEnable => IconSource != null;
        public string? IconSource { get; set; }
    }
```
 Priority - сортировка положения в меню
 Group - разные группы выделяются сепараторами
 ### VMBaseMenus, VMBaseToolBar
 VMBaseMenus.cs, VMBaseToolBar.cs - реализации VM основных меню и кнопок 
 ViewResourceTools.xaml, ViewResourceMenus.xaml - ResourceDictionary сстветствующие стили
 и DataTemplate

 ### MenuServer ToolServer
 добавлениe удалениe MenuItem, ToolItem должно происходить через 
 серверы (для сортировки и создания сепараторов)
 ```
     public interface IMenuItemServer
    {
        public void Add(string ParentContentID, IEnumerable<MenuItemVM> Menus);
        public void Remove(string ContentID);
        public void Remove(IEnumerable<MenuItemVM> Menus);
        public bool Contains(string ContentID);
        public void UpdateSeparatorGroup(string ParentContentID);
        public void UpdateSeparatorGroup(MenuItemVM? ParentContentID);
    }

    public interface IToolServer
    {
        public void AddBar(ToolBarVM toolBar);
        public void DelBar(string BarContentID);
        public void Add(string BarContentID, IEnumerable<ToolButton> Tools);
        public void Remove(string ContentID);
        public void Remove(IEnumerable<ToolButton> Tools);
        public bool Contains(string ContentID);
        public bool Contains(ToolButton Tool);
        public void UpdateSeparatorGroup(string BarContentID);
    }
 ```
 доступ к ним через сервисы
 ```
     public static class ServicesRoot
    {
        public static void Register(IServiceCollection services)
        {
            services.AddSingleton<IMenuItemServer, MenuServer>();
            services.AddSingleton<IToolServer, ToolServer>();
        }
    }
 ```
 ### View ViewResource.cs
 В основном окне меню и тулы обявлены так
 ```
    <Menu x:Name="menu" Style="{StaticResource ResourceKey={x:Static ToolBar.MenuStyleKey}}"
             ..........
             DataContext="{Binding MenuVM}"
             ItemsSource="{Binding RootItems}"   
             ...................
             ------------------------------------
             ItemContainerTemplateSelector="{StaticResource MenuTemplateSelectorVMKey}">
             --------------------------------------
       <Menu.Resources>
           <HierarchicalDataTemplate DataType="{x:Type c:MenuItemVM}"                                                      
                                     ItemsSource="{Binding Items}"/>
       </Menu.Resources>
   </Menu>

  <ToolBar Band="2" 
           .....
          ------------------------------------------------------------------
          ItemTemplateSelector="{StaticResource ToolTemplateSelectorVMKey}"
          ------------------------------------------------------------------
          DataContext="{Binding ToolGlyphVM}"
          ItemsSource="{Binding Items}">
  </ToolBar>
 ```
 ##### логическое древо привязка к модели
 меню:  
    DataContext = Binding MenuVM   
    ItemsSource = Binding RootItems      
 тулбар:  
    DataContext=Binding ToolGlyphVM  
    ItemsSource=Binding Items  

 ##### визуальное древо
 меню:  
    ItemContainerTemplateSelector = MenuTemplateSelector   
 тулбар:  
    ItemTemplateSelector = ToolTemplateSelector   
MenuTemplateSelector, ToolTemplateSelector объявлены в ***ViewResource.cs***
```
    internal static class AnyResuorceSelector
    {
        public static object? Get(ResourceDictionary _dictionary, object item, string suffix = "")
        {
            if (item != null)
            {
                Type? type = item.GetType();

                while (type != null)
                {
                    if (_dictionary.Contains(type.Name + suffix))
                    {
                        return _dictionary[type.Name + suffix];
                    }
                    type = type.BaseType;
                }
            }
            return null;
        }
    }

    public class ToolTemplateSelector : DataTemplateSelector
    {
        private static ResourceDictionary _dictionary;
        public static ResourceDictionary Dictionary => _dictionary;
        static ToolTemplateSelector()
        {
            _dictionary = new ResourceDictionary();

            _dictionary.Source = new Uri("Core;component/ViewResourceTools.xaml", UriKind.Relative);
        }
        public override DataTemplate SelectTemplate(object item, DependencyObject parentItemsControl)
        {
            var res = AnyResuorceSelector.Get(_dictionary, item);
            return (res != null) ? (DataTemplate)res : base.SelectTemplate(item, parentItemsControl);
        }
    }
    public class MenuTemplateSelector : ItemContainerTemplateSelector
    {
        private static ResourceDictionary _dictionary;
        public static ResourceDictionary Dictionary => _dictionary;
        static MenuTemplateSelector()
        {
            _dictionary = new ResourceDictionary();

            _dictionary.Source = new Uri("Core;component/ViewResourceMenus.xaml", UriKind.Relative);
        }
        public override DataTemplate SelectTemplate(object item, ItemsControl parentItemsControl)
        {
            var res = AnyResuorceSelector.Get(_dictionary, item);
            return (res != null) ? (DataTemplate)res : base.SelectTemplate(item, parentItemsControl);
        }
    }

```
имя класса VM == ключ DataTemplate в словаре ресурсов (ViewResourceMenus,ViewResourceTools)   
***var name = item == null ? null : item.GetType().Name;***   
словари ресурсов могут объеденяться
```
  MenuTemplateSelector.Dictionary.MergedDictionaries.Add(new ResourceDictionary()
  {
      Source = new Uri("pack://application:,,,/Views/MenusResource.xaml")
  });

```
при создании уникальных VM меню, toolBar  и словарей стилей к ним 
### регистрация статических менюшек
необходимо создать Factory класс реализующий IMenuItemClient
```
    public interface IMenuItemClient
    {
        public void AddStaticMenus(IMenuItemServer s);
    }
```
пример Factory класс
```
    public class ProjectsExplorerMenuFactory : IMenuItemClient
    {
        void IMenuItemClient.AddStaticMenus(IMenuItemServer _menuItemServer)
        {
            _menuItemServer.Add(RootMenusID.NShow, new[] {
                new CommandMenuItemVM
                {
                    ContentID = "CidShowProjectsExplorer",
                    Header = Properties.Resources.tProjectExplorer,
                    IconSource = "pack://application:,,,/Images/Project.PNG",
                    Priority = 0,
                    Command = new RelayCommand(ShowPE)
                },
            });
            _menuItemServer.Add(RootMenusID.NFile_Create, new MenuItemVM[] {
                new MenuOpenFile
                {
                    ContentID = "FOP",
                    Header = Properties.Resources.nfile_Open,
                    IconSource = "pack://application:,,,/Images/Project.PNG",
                    Title = Properties.Resources.nfile_Open,
                    Filter = "Text documents (.txt)|*.txt|Any documents (.doc)|*.doc|Any (*)|*",
                    DefaultExt = ".txt",
                    CustomPlaces = new object[] 
                    {
                        @"C:\Users\Public\Documents\Горизонт\WorkProg\Projects\",
                        @"C:\XE\Projects\Device2\_exe\Debug\Метрология\",
                        @"G:\Мой диск\mtr\",
                        new Guid("FDD39AD0-238F-46AF-ADB4-6C85480369C7"),//документы
                        new Guid("1777F761-68AD-4D8A-87BD-30B759FA33DD"),//избрвнное
                    }
                },
                new CommandMenuItemVM
                {
                    ContentID = "CidShowProjectsExplorer",
                    Header = Properties.Resources.nProject_New,
                    IconSource = "pack://application:,,,/Images/NewProject.PNG",
                    Priority = -1000,
                    Command = new RelayCommand(ShowPE)
                },
            });
      }
........................................

```
и добавить как сервис
```
  services.AddTransient<IMenuItemClient, ProjectsExplorerMenuFactory>();
```
при запуске программы меню добавятся
```
    var ms = _host.Services.GetRequiredService<IMenuItemServer>();
    var madds = _host.Services.GetRequiredService< IEnumerable<IMenuItemClient>>();
    foreach ( var madd in madds ) madd.AddStaticMenus(ms);
```



## Окна (Forms)
 управление AvalonDock 2.0
### VM
 VMBaseForms.cs определяет основные типы VM окон
 ```
 public abstract class VMBaseForms : VMBase
 public class DocumentVM : VMBaseForms
 public class ToolVM: VMBaseForms
 ```
 ViewResourceForms.xaml определяет стили (привязку VM к AvalonDock LayoutItem,LayoutAnchorableItem,LayoutDocumentItem  
 и заглушки DataTemplate (UserControl) содержимого окон 
 ```
     <Style x:Key="LayoutItemStyle" TargetType="{x:Type adctrl:LayoutItem}">
        <Setter Property="Title" Value="{Binding Model.Title}"/>
        <Setter Property="CloseCommand" Value="{Binding Model.CloseCommand}"/>
        <Setter Property="ContentId" Value="{Binding Model.ContentID}"/>
        ...........................................
    </Style>

    <Style x:Key="ToolVMStyle" 
           TargetType="{x:Type adctrl:LayoutAnchorableItem}" 
           BasedOn="{StaticResource LayoutItemStyle}">
        ..............................................
    </Style>

    <Style x:Key="DocumentVMStyle" 
           TargetType="{x:Type adctrl:LayoutDocumentItem}" 
           BasedOn="{StaticResource LayoutItemStyle}">
           ...........................................................
    </Style>
    
    <DataTemplate x:Key="ToolVMTemplate">
    ................................................
    </DataTemplate>


    <DataTemplate x:Key="DocumentVMTemplate">
    ...........................................
    </DataTemplate>
 ```

 ### определение  AvalonDock DockingManager в основном окне 
 ```
         <a:DockingManager Name="dockManager" 
                          DataContext="{StaticResource DockManagerVMKey}"
                          AnchorablesSource="{Binding Tools}"
                          DocumentsSource="{Binding Docs}"
                          DocumentHeaderTemplate="{StaticResource LayoutDocumentHeaderKey}"
                          DocumentTitleTemplate="{StaticResource LayoutDocumentHeaderKey}"
                          LayoutUpdateStrategy="{StaticResource LayoutInitializerKey}"
                          LayoutItemTemplateSelector="{StaticResource PanesTemplateSelectorKey}"
                          LayoutItemContainerStyleSelector="{StaticResource PanesStyleSelectorKey}"
                          .................................
 ```
 VM окон DockManagerVM  
 Tools - присоединяемые системные окна   
 Docs - присоединяемые окна документов   
 DockManagerVM  определен в основном модуле реализует IFormsServer (объявлен VMBaseForms.cs) для доступа к VM  
 <span style="color:red">**IFormsServer - на стадии разработки**</span>.
 ```
     public interface IFormsServer
    {
        /// <summary>
        /// регистрируем генератор модели представления
        /// </summary>
        /// <param name="RootContentID"> RootContentID.AnyData.AnyData...) </param>
        /// <param name="RegFunc">генератор модели представления</param>
        void RegisterModelView(string RootContentID, Func<VMBaseForms> RegFunc);
        /// <param name="ContentID"> ContentID= RootContentID.AnyData.AnyData...</param>
        /// <returns></returns>
        VMBaseForms AddOrGet(string ContentID);

        VMBaseForms? Contains(string ContentID);
        VMBaseForms Add(VMBaseForms vmbase);
        void Remove(VMBaseForms RemForm);
    }
 ```

 LayoutItemTemplateSelector = PanesTemplateSelector  
 LayoutItemContainerStyleSelector = PanesStyleSelector  

 PanesTemplateSelector, PanesStyleSelector объавлены в ViewResource.cs идея аналогичная для меню и тулбар
 ```
    public class FormResource
    {
        private static ResourceDictionary _dictionary;
        public static ResourceDictionary Dictionary => _dictionary;
        static FormResource()
        {
            _dictionary = new ResourceDictionary();

            _dictionary.Source = new Uri("Core;component/ViewResourceForms.xaml", UriKind.Relative);
        }
        public static object? Get(object item, string suffix) 
        ...............................
    }

    public class PanesStyleSelector : StyleSelector
    {
        public override System.Windows.Style SelectStyle(object item, System.Windows.DependencyObject container)
        {
            var res = FormResource.Get(item, "Style");
            return (res != null)?(Style) res : base.SelectStyle(item, container);
        }
    }
    public class PanesTemplateSelector : DataTemplateSelector
    {
        public override System.Windows.DataTemplate SelectTemplate(object item, System.Windows.DependencyObject container)
        {
            var res = FormResource.Get(item, "Template");
            return (res != null) ? (DataTemplate)res : base.SelectTemplate(item, container);
        }
    }
 ```
только к имени VM в словаре добавляются суффиксы "Style" или "Template", также поиск ведется среди родителей VM

реальные FormsResource.xaml
```
    <Style x:Key="ProjectsExplorerVMStyle" 
           TargetType="{x:Type adctrl:LayoutAnchorableItem}" 
           BasedOn="{StaticResource LayoutAnchorableItemStyle}"/>

    <DataTemplate x:Key="ProjectsExplorerVMTemplate">
        <v:ProjectsExplorerUC/>
    </DataTemplate>

    <DataTemplate x:Key="TextLogVMTemplate">
        <v:TextLogUC/>
    </DataTemplate>
```

словарь ресурсов может объедениться
```
   FormResource.Dictionary.MergedDictionaries.Add(new ResourceDictionary()
   {
       Source = new Uri("pack://application:,,,/Views/FormsResource.xaml")
   });
```
### Динамическое изменение меню и тулбаров при изменении фокуса окна
идея минимизировать элементы управления в окнах
```
  protected List<PriorityItemBase> DynamicItems = new List<PriorityItemBase>();

  protected delegate void ActivateHandler();
  protected event ActivateHandler? OnMenuActivate;
  protected event ActivateHandler? OnMenuDeActivate;
```
по событию *OnMenuActivate* создать tools, menus, вручную добавить в DynamicItems, использовать IToolServer, IMenuItemServer

событие *OnMenuDeActivate* предназначено только для обнуления внутренних ссылок, если есть, на tools, menus.
Очищать DynamicItems и удалять tools, menus из IToolServer, IMenuItemServer не надо, будет удалено автоматически
### регистрирование окна
VM окна регистрируется следующим образом  
имеется следующая вспомогательная конструкция
```
    public interface IFormsRegistrator
    {
        void Register(IFormsServer fs);
    }
    public class FormsRegistrator<T> : IFormsRegistrator
        where T : VMBaseForms
    {
        public void Register(IFormsServer fs)
        {
            fs.RegisterModelView(typeof(T).Name, VMBase.ServiceProvider.GetRequiredService<T>);
        }
    }
```
модели представления добавляются как сервисы
и как IFormsRegistrator через обертку FormsRegistrator
```
                services.AddTransient<ExceptLogVM>();
                services.AddTransient<IFormsRegistrator, FormsRegistrator<ExceptLogVM>>();
```
затем когда создано основное окно, menu, toolbars, dockmanager VM окна регистрируются в DockManagerVM реализующий IFormsServer
```
  var fs = _host.Services.GetRequiredService<IFormsServer>();
  var frs = _host.Services.GetRequiredService<IEnumerable<IFormsRegistrator>>();
  foreach (var fr in frs) fr.Register(fs);
```
