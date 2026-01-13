using Core;
using HorizontDrilling.Models;
using Microsoft.Win32;
using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using HorizontDrilling.Properties;
using AdonisUI.Controls;

namespace HorizontDrilling.Views
{
    [ValueConversion(typeof(bool), typeof(Visibility))]
    public class BoolToVisibilityConverter : IValueConverter
    {

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool && targetType == typeof(Visibility))
            {
                bool val = (bool)value;
                if (parameter is string s)
                {
                    val = s == "invers" ? !val : val;
                }
                return val ? Visibility.Visible : Visibility.Collapsed;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {

            if (value is Visibility && targetType == typeof(bool))
            {
                Visibility val = (Visibility)value;
                if (val == Visibility.Visible)
                    return true;
                else
                    return false;
            }
            throw new ArgumentException("Invalid argument/return type. Expected argument: Visibility and return type: bool");
        }
    }

    //static class IconExt
    //{
    //    public static ImageSource ToImageSource(this Icon icon)
    //    {
    //        ImageSource imageSource = Imaging.CreateBitmapSourceFromHIcon(
    //            icon.Handle,
    //            Int32Rect.Empty,
    //            BitmapSizeOptions.FromEmptyOptions());

    //        return imageSource;
    //    }
    //}

    //[ValueConversion(typeof(Icon), typeof(ImageSource))]
    //public class IconToImageConverter : IValueConverter
    //{
    //    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    //    {
    //        return (value is Icon i && targetType == typeof(ImageSource))? i.ToImageSource(): Binding.DoNothing;
    //    }
    //    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    //    {
    //        throw new NotImplementedException();
    //    }
    //}

    public class CreateVisitDialogVM: VMBase
    {
        //public bool Group => globalSettings.Group;

        public Action<CreateNewVisitDialogResult>? result;
        private void UpdateFullPath()
        {
            if (cbIsChecked && NewGroupSelected || VisitOnlySelected ) FullPath = Path.Combine(RootDir, VisitFile);

            else FullPath = Path.Combine(RootDir, GroupFile, VisitFile);
        }
        private bool BlockChangeGroupFile;
        //Validation
        void DoPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(GroupFile))
            {
                UpdateFullPath();
            }
            else if (e.PropertyName == nameof(RootDir))
            {
                UpdateFullPath();
            }
            else if (e.PropertyName == nameof(VisitOnlySelected))
            {
                if (VisitOnlySelected) VisitType = NewVisitType.SingleVisit;
                UpdateFullPath();
            }
            else if (e.PropertyName == nameof(VisitFile))
            {
                if (!VisitOnlySelected) GroupFile = VisitFile;
                UpdateFullPath();
            }
            else if (e.PropertyName == nameof(NewGroupSelected) || e.PropertyName == nameof(cbIsChecked))
            {
                if (NewGroupSelected)
                {
                    VisitType = NewVisitType.NewGroup;
                    GroupFile = VisitFile;
                }
                UpdateFullPath();
            }
            else if (e.PropertyName == nameof(CurrentGroupSelected))
            {
                if (CurrentGroupSelected)
                {
                    VisitType = NewVisitType.AddVisit;
                    GroupFile = ProjectFile.GroupName;
                    BlockChangeGroupFile = true;
                    RootDir = ProjectFile.RootDir;
                }
                else BlockChangeGroupFile = false;
                UpdateFullPath();
            }
        }
        public CreateVisitDialogVM() 
        {
            PropertyChanged += DoPropertyChanged;
            VisitFile = "Visit" + DateTime.Now.ToString(" dd-MM-yy");
            RootDir = ProjectFile.WorkDirs.Count > 0? ProjectFile.WorkDirs[0]!: ProjectFile.RootDir;
            CurrGroupIsEnable = !ProjectFile.SingleVisit;
        }

        public StringCollection WorkDirs => ProjectFile.WorkDirs;

        private string _VisitFile = string.Empty    ;
        public string VisitFile { get => _VisitFile; set => SetProperty(ref _VisitFile, value); }

        private string _RootDir = string.Empty;
        public string RootDir { get => _RootDir; set => SetProperty(ref _RootDir, value); }

        private string _GroupFile = string.Empty;
        public string GroupFile
        {
            get => _GroupFile; set
            {
                if (!BlockChangeGroupFile) SetProperty(ref _GroupFile, value);
            }
        }

        private string _FullPath = string.Empty;
        public string FullPath 
        { 
            get => _FullPath; 
            set 
            { 
                if (SetProperty(ref _FullPath, value))
                {
                    WarnCatVisible = Directory.Exists(value) && (Directory.GetFiles(value).Length > 0 || Directory.GetDirectories(value).Length > 0);
                }
            } 
        }

        bool _VisitOnlySelected = true;
        public bool VisitOnlySelected { get => _VisitOnlySelected; set => SetProperty(ref _VisitOnlySelected, value); }

        bool _CurrentGroupSelected;
        public bool CurrentGroupSelected { get => _CurrentGroupSelected; set => SetProperty(ref _CurrentGroupSelected, value); }
        
        bool _CurrGroupIsEnable;
        public bool CurrGroupIsEnable { get => _CurrGroupIsEnable; set => SetProperty(ref _CurrGroupIsEnable, value); }

        bool _NewGroupSelected;
        public bool NewGroupSelected { get => _NewGroupSelected; set => SetProperty(ref _NewGroupSelected, value); }


        bool _cbIsChecked = true;
        public bool cbIsChecked { get => _cbIsChecked; set => SetProperty(ref _cbIsChecked, value); }

        bool _WarnCatVisible = false;
        public bool WarnCatVisible { get => _WarnCatVisible; set => SetProperty(ref _WarnCatVisible, value); }
        public string VisitFullFile { get; set; } = string.Empty;
        public string GroupFullFile { get; set; } = string.Empty;
        public bool VisitExists { get; set; }
        public bool GroupExists { get; set; }
        /// 
        /// chekbox IsEnabled IsVisible должны управляться триггерами 
        /// нас интересует только IsChecked !!!!
        ///  
        //bool _cbIsEnabled = true;
        //public bool cbIsEnabled { get => _cbIsEnabled; set => SetProperty(ref _cbIsEnabled, value); }
        //bool _cbIsVisible = true; false if CurrentGroupSelected
        //public bool cbIsVisible { get => _cbIsVisible; set => SetProperty(ref _cbIsVisible, value); }

        private void VisitExistsDialog(string visit)
        {
            if (MsgBox.Show(string.Format(Resources.dlgVisitExisis.Replace("\\n", "\n"), visit),
                Resources.wFileSaveDialogTitle,
                BoxButton.OKCancel,
                BoxImage.Question) == BoxResult.Cancel) throw new CancelDialogException();
        }
        private void GroupExistsDialog(string grop)
        {
            if (MsgBox.Show(string.Format(Resources.dlgGroupExists.Replace("\\n", "\n"), grop),
                Resources.wFileSaveDialogTitle,
                BoxButton.OKCancel,
                BoxImage.Question) == BoxResult.Cancel) throw new CancelDialogException();
        }
        private void AllExistsDialog(string grop, string visit)
        {
            if (MsgBox.Show(string.Format(Resources.dlgVisGrpExisis.Replace("\\n", "\n"), grop, visit),
                Resources.wFileSaveDialogTitle,
                BoxButton.OKCancel,
                BoxImage.Question) == BoxResult.Cancel) throw new CancelDialogException();
        }

        public void CheckNewCreatedFiles()
        {
            switch (VisitType)
            {
                case NewVisitType.AddVisit:

                    VisitFullFile = Path.Combine(RootDir, GroupFile, VisitFile, VisitFile + ".vst");
                    GroupFullFile = Path.Combine(RootDir, GroupFile + ".vstgrp");
                    VisitExists = File.Exists(VisitFullFile);
                    if (VisitExists) VisitExistsDialog(VisitFullFile);
                    break;
                case NewVisitType.SingleVisit:
                    VisitFullFile = Path.Combine(RootDir, VisitFile, VisitFile + ".vst");
                    VisitExists = File.Exists(VisitFullFile);
                    if (VisitExists) VisitExistsDialog(VisitFullFile);
                    break;
                case NewVisitType.NewGroup:
                    if (cbIsChecked)
                    {
                        VisitFullFile = Path.Combine(RootDir, VisitFile, VisitFile + ".vst");
                        GroupFullFile = Path.Combine(RootDir, VisitFile, VisitFile + ".vstgrp");
                    }
                    else
                    {
                        VisitFullFile = Path.Combine(RootDir, GroupFile, VisitFile, VisitFile + ".vst");
                        GroupFullFile = Path.Combine(RootDir, GroupFile, GroupFile + ".vstgrp");
                    }
                    VisitExists = File.Exists(VisitFullFile);
                    GroupExists = File.Exists(GroupFullFile);
                    if (VisitExists && GroupExists) AllExistsDialog(GroupFullFile, VisitFullFile);
                    else if (GroupExists) GroupExistsDialog(GroupFullFile);
                    else if (VisitExists) VisitExistsDialog(VisitFullFile);
                    break;
                default:
                    break;
            }

            result?.Invoke(new(VisitFile, RootDir, GroupFile, VisitType, cbIsChecked, VisitFullFile,GroupFullFile, VisitExists, GroupExists));
            //ProjectFile.OnNewProject( GroupFile, RootDir, VisitFile, VisitType, cbIsChecked);
        }

        public NewVisitType VisitType { get; set; } = NewVisitType.SingleVisit;
    }
    /// <summary>
    /// Логика взаимодействия для CreateVisitDialog.xaml
    /// </summary>
    public partial class CreateVisitDialog : AdonisWindow
    {
        public BoxResult Result { get; set; } = BoxResult.Cancel;
        public CreateVisitDialog()
        {
            InitializeComponent();            
            this.Owner = App.Current.MainWindow;
            SourceInitialized += (o, e) =>
            {
                MinimizeButton.Visibility = Visibility.Collapsed;
                MaximizeRestoreButton.Visibility = Visibility.Collapsed;
                //this.HideMinimizeAndMaximizeButtons();
            };            
        }

        private void btSave_Click(object sender, RoutedEventArgs e)
        {
            ((CreateVisitDialogVM)DataContext).CheckNewCreatedFiles();
            Result = BoxResult.OK;
            Close();
        }

        private void btCancel_Click(object sender, RoutedEventArgs e)
        {
            Result = BoxResult.Cancel;
            Close();
        }

        private void btNewPath_Click(object sender, RoutedEventArgs e)
        {
            var f = new OpenFolderDialog();
            if (f.ShowDialog() == true)
            {
                bxPath.Text = f.FolderName;

                foreach (var fl in ProjectFile.WorkDirs) if (fl!.IsSameFiles(f.FolderName)) return;
                
                ProjectFile.WorkDirs.Add(f.FolderName);
            }
        }
        public void SetOnlyAdd()
        {
            lblPlasement.Visibility = Visibility.Collapsed;
            Plasement.Visibility = Visibility.Collapsed;
            lblGroupVisit.Visibility = Visibility.Collapsed;
            GroupVisit.Visibility = Visibility.Collapsed;
        }
    }

    public class ValidateFileNameRule : ValidationRule
    {
        public string ErrorText { get; set; } = string.Empty ;

        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            var fn = (string) value;

            var isValid = !string.IsNullOrEmpty(fn) &&
              fn.IndexOfAny(System.IO.Path.GetInvalidFileNameChars()) < 0; 
              //&& !File.Exists(System.IO.Path.Combine(sourceFolder, fn));

              if (isValid) return new ValidationResult(true, null);

              else return new ValidationResult(false, ErrorText);
        }
    }

}
