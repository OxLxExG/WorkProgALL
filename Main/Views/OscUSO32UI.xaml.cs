using Connections;
using Main.ViewModels;
using ScottPlot;
using System;
using System.Collections.Generic;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Main.Views
{
    /// <summary>
    /// Логика взаимодействия для OscUSO32UI.xaml
    /// </summary>
    public partial class OscUSO32UI : UserControl
    {
        ScottPlot.Plottables.DataStreamer streamer;

        int cntRefr;
        void OnUSO32Data(int data)
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
        int Amp 
        { 
            get 
            { 
                if (DataContext is OscUSO32VM vm)
                {
                    return vm.Amp;
                }
                else return 1; 
            } 
        }
        string CustomFormatter(double position)
        {
            var p = Math.Abs(position) / Amp;// 1000000 * 2.5);
            return p switch
            {
                0               => "0",
                < 500_000       => $"{p/1000:F2}uV",
                < 500_000_000   => $"{p/1000_000:F2}mV",
                _               => $"{p/ 1000_000_000:F2}V",
            };
               

            //if (position == 0)
            //    return "0";
            //else if (position > 0)
            //    return $"{p}";
            //else
            //    return $"{p}";
        }

        public OscUSO32UI()
        {
            InitializeComponent();

            var plt = SctPlot.Plot;
            streamer = plt.Add.DataStreamer(1000);
            streamer.ManageAxisLimits = true;

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
            plt.Axes.Bottom.Min = 0;
            plt.Axes.Bottom.Max = 1000;
            plt.Axes.ContinuouslyAutoscale = true;
            plt.Axes.ContinuousAutoscaleAction = a =>
            {
                a.Plot.Axes.AutoScale();
                a.Plot.Axes.Bottom.Min = 0;
                a.Plot.Axes.Bottom.Max = 1000;
                //if (streamer.Axes.YAxis.Max < 0.001 && streamer.Axes.YAxis.Min > -0.001)
                //{
                //    streamer.Axes.YAxis.Max = 0.001;
                //    streamer.Axes.YAxis.Min = -0.001;
                //}
            };
            var yAxis2 = plt.Axes.AddLeftAxis();

            yAxis2.TickGenerator = new ScottPlot.TickGenerators.NumericAutomatic
            {
                LabelFormatter = CustomFormatter
            };


            //yAxis2.Max = 0.001;
            //yAxis2.Min = -0.001;
            streamer.Axes.YAxis = yAxis2;

            //sig.Axes.YAxis = yAxis2;
            //sig.Axes.XAxis = plt.Axes.Bottom;
            PixelPadding padding = new(60, 0, 0, 0);
            SctPlot.Plot.Layout.Fixed(padding);
            SctPlot.Refresh();
            DataContextChanged += OscUSO32UI_DataContextChanged;
        }
        private void OscUSO32UI_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (DataContext is OscUSO32VM vm)
            {
                vm.ViewLoaded(OnUSO32Data);
            };
        }
    }
}
