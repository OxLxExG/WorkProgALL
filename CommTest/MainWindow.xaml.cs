using Commands;
using Connections.Interface;
using Communications.MetaData;
using Connections;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Threading;
using System.Xml;
using System.Xml.Serialization;
using TextBlockLogging;
using static Communications.MetaData.BinaryParser;
using System.Windows.Shapes;
using Communications.Project;
using Xceed.Wpf.Toolkit;
using System.Windows.Input;
using System.Xml.Linq;
using System.Xml.Schema;
using System.Linq;

namespace SerialPortTest
{

    //public class ConnectionConverter : IValueConverter
    //{
    //    public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    //    {
    //        if (value == null) return "";
    //        else if (targetType == typeof(string))
    //        {
    //            XmlElement element = value as XmlElement;
    //            if (element.Name == "WRK") return element.ParentNode.Name;
    //            return element.Name;
    //        }
    //        else return "";
    //    }
    //    public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    //    {
    //        throw new NotSupportedException("This method is not supported.");
    //    }
    //}
    public class LogTextBlock : TextBlock, IFreeze
    {
        public bool Freeze { get; set; }
    }

    public class MonitorTextBlock : TextBlock
    {
        public static readonly DependencyProperty ConnectionProperty = DependencyProperty.Register(
                    "Connection",
                    typeof(string),
                    typeof(TextBlock),
                    new FrameworkPropertyMetadata(
                        null,
                        FrameworkPropertyMetadataOptions.AffectsMeasure |
                        FrameworkPropertyMetadataOptions.AffectsRender,
                        new PropertyChangedCallback(OnConnectionChanged),
                        new CoerceValueCallback(CoerceConnection)));

        private static object CoerceConnection(DependencyObject d, object baseValue)
        {
            return baseValue;
        }

        private static void OnConnectionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            //e.NewValue;
            //e.OldValue;
        }

        public string Connection
        {
            get { return (string)GetValue(ConnectionProperty); }
            set { SetValue(ConnectionProperty, value); }
        }
    }

    [Serializable()]
    public class Test
    {
        private AbstractConnection? con;
        [XmlIgnore]
        public IConnection? connection { get; private set; }
        [XmlElement("connection")]
        public AbstractConnection? connectionObj
        {
            get { return con; }
            set { con = value; connection = con; }
        }
    }
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private AbstractConnection sp = new SerialConn();
        private ISSDConnection ssd = new SSDConn();
        // private AbstractConnection sp = new NetConn();
        //private ConnectionMonitor? monitor;
        private TextBlockMonitor monitor = new TextBlockMonitor("monitor1",
            App.serviceProvider!.GetRequiredService<ILogTextBlockService>(),
            ConsoleLoggerQueueFullMode.DropWrite, 150, 150, 16);
        Test test = new Test();

        //Button b = new Button();

        public void DisConnect()
        {
            if (sp == null) { return; }
            sp.OnRowDataHandler -= monitor.onRxData;
            sp.OnRowSendHandler -= monitor.onTxData;
        }

        public void Connect()
        {
            if (this.sp != null) { DisConnect(); }
            sp.OnRowDataHandler += monitor.onRxData;
            sp.OnRowSendHandler += monitor.onTxData;
        }

        public MainWindow()
        {
            InitializeComponent();
            if (App.opt.Logging.Box.Error) App.serviceProvider?.GetRequiredService<ILogTextBlockService>().SetLogTextBlock("ExceptLogVM", log);
            if (App.opt.Logging.Box.Trace) App.serviceProvider?.GetRequiredService<ILogTextBlockService>().SetLogTextBlock("TraceLogVM", trace);
            if (App.opt.Logging.Box.Info) App.serviceProvider?.GetRequiredService<ILogTextBlockService>().SetLogTextBlock("InfoLogVM", info);
            App.serviceProvider?.GetRequiredService<ILogTextBlockService>().SetLogTextBlock("monitor1", memo);

            //b.Name = "B1";
            if (sp is SerialConn) ((SerialConn)sp).PortName = comPortName.Text;
            else if (sp is NetConn) ((NetConn)sp).Host = "192.168.4.1";
            test.connectionObj = sp;
            //monitor = new ConnectionMonitor(memo);
            Connect();
            UpdateSSDname();

            var nnk = BinaryParser.Parse(BinaryParser.Meta_NNK);
            var cal = BinaryParser.Parse(BinaryParser.Meta_CAL);
            var ind = BinaryParser.Parse(BinaryParser.Meta_Ind);
            App.logger?.LogInformation($"DataSize: {nnk.DataSize()}");

            //string fileName = "D:\\Projects\\C#\\SerialPortTest\\bin\\metadata.xml";
            //XmlSerializer xser = new XmlSerializer(typeof(StructDef));
            //using (XmlWriter w = XmlWriter.Create(fileName))
            //{
            //    w.WriteProcessingInstruction("xml-stylesheet", $"type=\"text/xsl\" href =\"{StructDef.SLTSTR}\"");
            //    xser.Serialize(w, nnk);
            //}

            //StructDef? bin = null;
            //using (var fs = new FileStream(fileName, FileMode.Open))
            //{
            //    bin = (StructDef?) xser.Deserialize(fs);                                
            //}
            string ProjName = "D:\\Projects\\C#\\SerialPortTest\\bin\\Project.xml";
            var visOut = new Visit();
            Visit? visIn = null;

            using (MemoryStream ms = new MemoryStream())
            {
                XmlWriterSettings sttngs = new XmlWriterSettings();
                sttngs.Indent = true;

                using (var w = XmlWriter.Create(ms, sttngs))
                {
                    w.WriteProcessingInstruction("xml-stylesheet", $"type=\"text/xsl\" href =\"{StructDef.SLTSTR}\"");
                    var t = new Trip(visOut);
                    var p = new Pipe(t, sp);
                    var bs = new Bus(p, "Test Bus");
                    var d12 = new DevicePB(bs, cal);
                    var d111 = new DevicePB(bs, nnk);
                    var d1 = new DeviceTelesystem(bs);
                    var d2 = new DeviceTelesystem(bs);
                    var d11 = new DevicePB(bs, ind);
                    var bs2 = new Bus(p, "Test Bus2");
                    var d4 = new DeviceTelesystem(bs2);
                    var d5 = new DeviceTelesystem2(bs2);
                    var d3 = new DevicePB(bs2, ind);
                    //visOut.UpdateParent(null);

                    XmlSerializerNamespaces ns = new XmlSerializerNamespaces();
                  //  ns.Add(StructDef.NS_PX, StructDef.NAMESPACE);
                   // ns.Add(Visit.NS_PX, Visit.NS);
                    ns.Add("xsi", System.Xml.Schema.XmlSchema.InstanceNamespace);
                    var obj = (new Type[] { typeof(DevicePB), typeof(DeviceTelesystem), typeof(DeviceTelesystem2) }).Concat(AbstractConnection.ConnectionTypes);
                    XmlSerializer xs = new XmlSerializer(typeof(Visit), null, obj.ToArray() ,null, Visit.NS, null);
                    xs.Serialize(w, visOut, ns);
                }

                ms.Position = 0;
                XDocument doc = XDocument.Load(new XmlTextReader(ms));
                doc.Root!.Add(new XAttribute(XName.Get("schemaLocation",XmlSchema.InstanceNamespace), 
                    $"{StructDef.NAMESPACE} {StructDef.SCHLOC} {Visit.NS} {Visit.SCH}"));
                doc.Save(ProjName);
            }
            using (var file = File.OpenRead(ProjName))
            {
                var obj = (new Type[] { typeof(DevicePB), typeof(DeviceTelesystem), typeof(DeviceTelesystem2) })
                    .Concat(AbstractConnection.ConnectionTypes);
                XmlSerializer xs = new XmlSerializer(typeof(Visit), null, obj.ToArray(), null, Visit.NS, null);
                visIn = (Visit?)xs.Deserialize(file);
                visIn?.UpdateParent(null);
                visIn?.UpdateParent(null);
            }

        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {

            // App.logger?.LogWarning("--BEGIN--");
            //   var s = App.logger?.BeginScope("Read Meta Data");
            //  App.logger?.LogInformation("--BEGIN--");
            ((AbstractConnection)sp).dbg = "info";
            sp.CheckLock();
            List<byte>? metadata = null;
            try
            {
                btSend.IsEnabled = false;
                await Task.Run(async () =>
                {
                    var t = DateTime.Now;
                    try
                    {
                        if (!sp.IsOpen) await sp.Open(20_000);
                        metadata = await Protocol.ReadMetaData(sp, 6, null, 2000);
                    }
                    finally
                    {
                        await sp.Close();
                        var dt = (DateTime.Now - t).TotalMilliseconds;
                        App.logger?.LogInformation($"---END--- dt: {dt}");
                    }
                });
            }
            finally
            {
                btSend.IsEnabled = true;
                sp.UnLock();
                if (monitor != null && memo.Freeze && metadata != null)
                {
                    memo.Inlines.Add(new LineBreak());
                    memo.Inlines.InsertBefore(memo.Inlines.FirstInline,
                          new Run(BitConverter.ToString(metadata.ToArray(), 0, 16) + "...\r\n") { Foreground = Brushes.Red });
                }
                // s?.Dispose();
                // App.logger?.LogDebug("--Read Meta Data--");

            }
        }

        private async void btSend2_Click(object sender, RoutedEventArgs e)
        {
            btSend2.IsEnabled = false;

            try
            {
                await Task.Run(async () =>
                {
                    byte adr = 6;
                    ulong from = 0;// 130976;
                    uint total = 0x1000_0000;// 130976 + 0x1000;
                    uint bufferSize = 0x20000;// 0x4350;
                    string file = "D:\\IDEs\\Data.bin";

                    using Stream dst = ReadRam.CreateFileStream(new(file), ref from);

                    var n = await ReadRam.Read(sp, dst, new(adr, from, total, ComRamReadOptions.Turbos.ts2M, ToEmpty: false, bufferSize),
                        UpdateStat);
                    App.logger?.LogWarning($"Readed {n:X}");
                });
            }
            finally
            {
                btSend2.IsEnabled = true;
            };
        }
        private async void btSend3_Click(object sender, RoutedEventArgs e)
        {
            var l = UpdateSSDname();
            if (l == null) return;
            ssd.Letter = (char)l;

            btSend3.IsEnabled = false;
            try
            {
                await Task.Run(async () =>
                {
                    ulong from = 0;
                    await ssd.Open();
                    ulong total = (ulong)ssd.SSDSize;
                    await ssd.Close();
                    uint bufferSize = 0x100_0000;
                    string file = "E:\\IDEs\\SSDData.bin";

                    using Stream dst = ReadRam.CreateFileStream(new(file), ref from);

                    var n = await ReadRam.Read(ssd, dst, new(from, total, ToEmpty: false, bufferSize),
                        UpdateStat);
                    App.logger?.LogWarning($"Readed {n:X}");
                });
            }
            finally { btSend3.IsEnabled = true; }
        }

        private void btWrite_Click(object sender, RoutedEventArgs e)
        {
            int a = 10;
            int b = 0;
            var c = a / b;
            XmlSerializer writer = new XmlSerializer(typeof(Test),
                null,
                AbstractConnection.ConnectionTypes, null, null);

            string fileName = "C:\\Projects\\C#\\SerialPortTest\\bin\\port.xml";
            FileStream file = File.Create(fileName);

            writer.Serialize(file, test);
            file.Close();
        }

        private void btNet_Checked(object sender, RoutedEventArgs e)
        {
            if (sp.IsLocked) btNet.IsChecked = !btNet.IsChecked;
            sp.CheckLocked();
            DisConnect();
            if ((bool)btNet.IsChecked! && sp is not SerialConn)
            {
                sp.Dispose();
                sp = new SerialConn();
                ((SerialConn)sp).PortName = comPortName.Text;
            }
            else
            {
                sp.Dispose();
                sp = new NetConn();
                ((NetConn)sp).Host = "192.168.4.1";
                ((NetConn)sp).Port = 5000;
            }
            test.connectionObj = sp;
            Connect();

        }
        #region Small Functions
        private void UpdateStat(ProgressData pd)
        {
            sb.Dispatcher.Invoke(DispatcherPriority.Background, () =>
            {
                sbGSpeed.Text = pd.globalVelosity;
                sbLSpeed.Text = pd.localVelosity;
                sbProc.Text = pd.progress;
                sbEla.Text = pd.elapsed;
                sbRem.Text = pd.remaining;
            });
        }
        private void btClear_Click(object sender, RoutedEventArgs e)
        {
            memo.Inlines.Clear();
        }
        private void btCancel_Click(object sender, RoutedEventArgs e)
        {
            sp.Cancel();
            ssd.Cancel();
        }
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            sp.Dispose();
            sp = null!;
            ssd.Dispose();
            ssd = null!;
        }
        private void btCleare_Click(object sender, RoutedEventArgs e)
        {
            log.Inlines.Clear();
        }

        private void btFreezee_Click(object sender, RoutedEventArgs e)
        {
            log.Freeze = (bool)btFreezee.IsChecked!;
            log.UpdateLayout();
        }

        private void btFreeze_Checked(object sender, RoutedEventArgs e)
        {
            if (monitor != null)
                memo.Freeze = (bool)btFreeze.IsChecked!;
        }

        private void btFreezet_Click(object sender, RoutedEventArgs e)
        {
            trace.Freeze = (bool)btFreezet.IsChecked!;
            trace.UpdateLayout();
        }

        private void btCleart_Click(object sender, RoutedEventArgs e)
        {
            trace.Inlines.Clear();
        }

        private void btCleari_Click(object sender, RoutedEventArgs e)
        {
            info.Inlines.Clear();
        }

        private void btFreezei_Click(object sender, RoutedEventArgs e)
        {
            info.Freeze = (bool)btFreezei.IsChecked!;
            info.UpdateLayout();
        }
        private char? UpdateSSDname()
        {
            DriveInfo[] allDrives = DriveInfo.GetDrives();

            foreach (DriveInfo d in allDrives) if (d.DriveType == DriveType.Removable)
                {
                    btSend3.Content = new TextBlock() { Text = $"SSDRam {d.Name[0]}" };
                    return d.Name[0];
                }
            return null;
        }
        #endregion
    }
}
