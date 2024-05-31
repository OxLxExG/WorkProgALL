using Core;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Main.Views
{
    [ValueConversion(typeof(bool), typeof(Visibility))]
    public class BoolToVisibilityConverter : IValueConverter
    {

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is bool && targetType == typeof(Visibility))
            {
                bool val = (bool)value;
                return val ? Visibility.Visible : Visibility.Collapsed;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
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

    public class CreateVisitDialogVM: VMBase
    {
        //Validation
        void PropertyChangingEventHandler(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(VisitFile))
            {
                if (!VisitOnlySelected) GroupFile = VisitFile;
            }
            else if (e.PropertyName == nameof(NewGroupSelected))
            {
                if (NewGroupSelected) GroupFile = VisitFile;
            }
            else if (e.PropertyName == nameof(CurrentGroupSelected))
            {
                if (CurrentGroupSelected) GroupFile = VisitFile;
            }
        }
        public CreateVisitDialogVM() 
        {
            PropertyChanged += PropertyChangingEventHandler;
            VisitFile = "Visit1";
        }
        private string _VisitFile = string.Empty    ;
        public string VisitFile { get => _VisitFile; set => SetProperty(ref _VisitFile, value); }

        private string _GroupFile = string.Empty;
        public string GroupFile { get=> _GroupFile; set=> SetProperty(ref _GroupFile, value); }

        bool _VisitOnlySelected = true;
        public bool VisitOnlySelected { get => _VisitOnlySelected; set => SetProperty(ref _VisitOnlySelected, value); }

        bool _CurrentGroupSelected;
        public bool CurrentGroupSelected { get => _CurrentGroupSelected; set => SetProperty(ref _CurrentGroupSelected, value); }

        bool _NewGroupSelected;
        public bool NewGroupSelected { get => _NewGroupSelected; set => SetProperty(ref _NewGroupSelected, value); }

        bool _cbIsEnabled = true;
        public bool cbIsEnabled { get => _cbIsEnabled; set => SetProperty(ref _cbIsEnabled, value); }

        bool _cbIsChecked;
        public bool cbIsChecked { get => _cbIsChecked; set => SetProperty(ref _cbIsChecked, value); }

        bool _cbIsVisible = true;
        public bool cbIsVisible { get => _cbIsVisible; set => SetProperty(ref _cbIsVisible, value); }

        bool _cbiCurrGroupIsEnable;
        public bool cbiCurrGroupIsEnable { get => _cbiCurrGroupIsEnable; set => SetProperty(ref _cbiCurrGroupIsEnable, value); }

        public VisitAddToGroup VisitAddToGroup { get; set; }
    }
    /// <summary>
    /// Логика взаимодействия для CreateVisitDialog.xaml
    /// </summary>
    public partial class CreateVisitDialog : Window
    {
        public BoxResult Result { get; set; } = BoxResult.Cancel;
        public CreateVisitDialog()
        {
            InitializeComponent();            
            this.Owner = App.Current.MainWindow;
            SourceInitialized += (o, e) => this.HideMinimizeAndMaximizeButtons();
        }

        private void btSave_Click(object sender, RoutedEventArgs e)
        {
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
            }
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
