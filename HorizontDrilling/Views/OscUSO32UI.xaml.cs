using Core;
using Global;
using HorizontDrilling.ViewModels;
using ScottPlot;
using ScottPlot.AxisPanels;
using System;
using System.Windows;
using System.Windows.Controls;

namespace HorizontDrilling.Views
{
    /// <summary>
    /// Логика взаимодействия для OscUSO32UI.xaml
    /// </summary>
    public partial class OscUSO32UI : UserControl
    {
        ScottPlot.Plottables.DataStreamer streamer;
        LeftAxis YAxis;

        int AxisMax = 1000;
        int Amp = 1;
        //{ 
        //    get 
        //    { 
        //        if (DataContext is OscUSO32VM vm)
        //        {
        //            return vm.Amp;
        //        }
        //        else return 1; 
        //    } 
        //}
        const double KADCV = 2.5 / int.MaxValue;
        const double KADCmV = 2.5 * 1_000 / int.MaxValue;
        const double KADCuV = 2.5 * 1_000_000 / int.MaxValue;
        string CustomFormatter(double position)
        {
            var p = Math.Abs(position) / Amp;// 1000000 * 2.5);
            return p switch
            {
                0               => "0",
                < 500           => $"{p * KADCuV:F2}uV",
                < 500_000       => $"{p * KADCuV:F1}uV",
                < 500_000_000   => $"{p * KADCmV:F1}mV",
                _               => $"{p * KADCV:F2}V",
            };
               

            //if (position == 0)
            //    return "0";
            //else if (position > 0)
            //    return $"{p}";
            //else
            //    return $"{p}";
        }
        int ConvertColorToUInt(System.Windows.Media.Color color)
        {
            int value = (color.A << 24) | (color.R << 16) | (color.G << 8) | color.B;
            return value;
        }
        void OnNewTheme(object? sender, bool IsDark)
        {
            var bc = (System.Windows.Media.Color)Application.Current.Resources[AdonisUI.Colors.Layer1BackgroundColor];
            SctPlot.Plot.FigureBackground.Color = ScottPlot.Color.FromARGB(ConvertColorToUInt(bc));
        }
        public OscUSO32UI()
        {
            InitializeComponent();

            ThemeChangeEvent.ThemeChanged += OnNewTheme;

            var plt = SctPlot.Plot;
            //streamer.Period = 10;
            //streamer.ManageAxisLimits = true;
            

            var bc = (System.Windows.Media.Color)Application.Current.Resources[AdonisUI.Colors.Layer1BackgroundColor];
            plt.FigureBackground.Color = ScottPlot.Color.FromARGB(ConvertColorToUInt(bc));
                 
            //plt.DataBackground.Color = Colors.Navy.Darken(0.1);
            //plt.Grid.MajorLineColor = Colors.Navy.Lighten(0.1);
            //streamer.LineWidth = 3;
            //streamer.Color = new("#2b9433");
            //streamer.AlwaysUseLowDensityMode = true;

            //for (int i = 0; i < 1000; i++)
            //{
            //    streamer.Add(0);
            //}
            plt.Grid.MajorLineColor = ScottPlot.Colors.Black.WithAlpha(.2);
            plt.Grid.IsBeneathPlottables = false;

            // disable mouse interaction by default
            SctPlot.UserInputProcessor.Disable();


            // give the plot a dark background with light text
            //plt.FigureBackground.Color = new("#1c1c1e");
            //plt.Axes.Color(new("#888888"));

            // shade regions between major grid lines
            //plt.Grid.XAxisStyle.FillColor1 = new ScottPlot.Color("#888888").WithAlpha(10);
            //plt.Grid.YAxisStyle.FillColor1 = new ScottPlot.Color("#888888").WithAlpha(10);

            ////plt grid line colors
            //plt.Grid.XAxisStyle.MajorLineStyle.Color = ScottPlot.Colors.White.WithAlpha(15);
            //plt.Grid.YAxisStyle.MajorLineStyle.Color = ScottPlot.Colors.White.WithAlpha(15);
            //plt.Grid.XAxisStyle.MinorLineStyle.Color = ScottPlot.Colors.White.WithAlpha(5);
            //plt.Grid.YAxisStyle.MinorLineStyle.Color = ScottPlot.Colors.White.WithAlpha(5);

            ////pltble minor grid lines by defining a positive width
            //plt.Grid.XAxisStyle.MinorLineStyle.Width = 1;
            //plt.Grid.YAxisStyle.MinorLineStyle.Width = 1;
            //pltle[] dataX = { 1, 2, 3, 4, 5 };
            //double[] dataY = { 1, 4, 9, 16, 25 };
            //var sig = plt.Add.Signal(Generate.Cos(1000,10,5,100));

            plt.Layout.Frameless();
            //plt.Axes.Bottom.Min = 0;
            //plt.Axes.Bottom.Max = AxisMax;
            plt.Axes.ContinuouslyAutoscale = true;
            //plt.Axes.Rules.Add(new LockedHorizontal(plt.Axes.Bottom,0,1000));
            YAxis = plt.Axes.AddLeftAxis();
            //var XAxis = plt.Axes.AddBottomAxis();
            //XAxis.Min = 0; XAxis.Max = AxisMax;

            YAxis.TickGenerator = new ScottPlot.TickGenerators.NumericAutomatic
            {
                LabelFormatter = CustomFormatter
            };

            streamer = plt.Add.DataStreamer(AxisMax);
            streamer.Axes.YAxis = YAxis;

            plt.Axes.ContinuousAutoscaleAction = a =>
            {
                a.Plot.Axes.AutoScaleY(YAxis);
            };
            PixelPadding padding = new(60, 0, 0, 0);
            SctPlot.Plot.Layout.Fixed(padding);
            SctPlot.Refresh();
            DataContextChanged += OscUSO32UI_DataContextChanged;
        }
        bool BindUsoData;
        private void OscUSO32UI_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if ((!BindUsoData) && DataContext is OscUSO32VM vm)
            {
                var src = RootFileDocumentVM.Find(vm.ContentIDs![1]);
                if (src is BaseBusUSOVM b)
                {
                    vm.Name = b.Name;
                    b.OnUSODataEvent += OnUSO32Data;
                    b.PropertyChanged += (s, e) =>
                    {
                        if (e.PropertyName == "Amp")
                        {
                            Int32.TryParse((s as BusUSO32VM)!.Amp, out Amp);
                        }
                    };
                    BindUsoData = true;
                }
            }
        }
        int cntRefr;
        void OnUSO32Data(BaseBusUSOVM sender, int data)
        {
            var d = data;// / 1000000 * 2.5;
            streamer.Add(d);
            streamer.ViewWipeRight();
            if (cntRefr++ > 31)
            {
                cntRefr = 0;
                SctPlot.Refresh();
            }
        }

        private void SctPlot_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (DataContext is OscUSO32VM u)
            {
                var W = Convert.ToInt32(e.NewSize.Width)*4;//TPrb
                if (W != AxisMax)
                {
                    AxisMax = W; 
                    SctPlot.Plot.PlottableList.Remove(streamer);
                    streamer = SctPlot.Plot.Add.DataStreamer(AxisMax);
                    streamer.Axes.YAxis = YAxis;
                }
             //   var l = VMBase.ServiceProvider.GetRequiredService<ILogger<OscUSO32UI>>();
             //   l.LogTrace("OscUSO32UI.AxisMax = {}", AxisMax);

            }
        }
    }
}
