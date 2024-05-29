using Connections;
using Connections.Interface;
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;
using System.Xml.Serialization;
using static Communications.MetaData.BinaryParser;

namespace Communications.Project
{
    public abstract class AbstractVisitItem: INotifyPropertyChanged
    {
        private Guid _id = Guid.Empty;
        public Guid Id 
        { 
            get
            {
                if (_id ==  Guid.Empty)
                    _id = Guid.NewGuid();
                return _id;
            }
            set => _id = value; 
        }
        
        public event PropertyChangedEventHandler? PropertyChanged;
        protected bool SetProperty<T>([NotNullIfNotNull(nameof(newValue))] ref T field, T newValue, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, newValue))
            {
                return false;
            }           
            field = newValue;

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

            return true;
        }
        #region property Parent        
        public virtual void UpdateParent(ComplexAbstractVisitItem? parent)
        {
            _Parent = parent;
        }
        private ComplexAbstractVisitItem? _Parent;
        [XmlIgnore] public ComplexAbstractVisitItem? Parent
        {
            get  => _Parent;
            set
            {
                if (_Parent != value)
                {
                    _Parent?.ItemsRemove(this); 
                    _Parent = value;
                    _Parent?.ItemsAdd(this);    
                }
            }
        }
        #endregion
    }
    public abstract class ComplexAbstractVisitItem : AbstractVisitItem
    {
        public abstract void ItemsRemove(AbstractVisitItem item);
        public abstract void ItemsAdd(AbstractVisitItem item);
    }
    /// <summary>
    /// сделал класс только для человекочитаемости XML файла сериализации
    /// </summary>
    /// <typeparam name="CHILDS"> Device,Bus,Pipe,Trip</typeparam>
    public abstract class ComplexAbstractVisitItem<CHILDS> : ComplexAbstractVisitItem 
        where CHILDS : AbstractVisitItem
    {
        [XmlIgnore]
        public ObservableCollection<CHILDS> Items { get; set; } = new ObservableCollection<CHILDS>();

        public override void UpdateParent(ComplexAbstractVisitItem? parent)
        {
            base.UpdateParent(parent);
            foreach (var item in Items) item.UpdateParent(this);
        }
        public override  void ItemsAdd(AbstractVisitItem item)
        {
            if (item is CHILDS t)
            {
                if (!Items.Contains(t)) Items.Add(t);
            }
            else throw new InvalidOperationException();
        }
        public override void ItemsRemove(AbstractVisitItem item)
        {
            if (item is CHILDS t) Items.Remove(t);
            else throw new InvalidOperationException();
        }
    }
    public class Device : AbstractVisitItem
    {
        public static readonly Device Empty = new Device();
    }
    public class DeviceTelesystem : Device  {    }
    public class DeviceTelesystem2 : Device {    }
    public record class RamReadInfo(string RAMFileName, int from, int too)
    {
        public RamReadInfo() : this(string.Empty, 0, 0) { }
    }
    /// <summary>
    /// устройства (сенсоры, черн.ящ., батареи, и т.д.) с обязательными метаданными
    /// </summary>    
    public sealed class DevicePB: Device
    {
        public RamReadInfo? ramReadInfo {  get; set; }
        public bool ShouldSerializeramReadInfo() => ramReadInfo != null;

        #region timout
        private int _timout = 2097;
        public int timout { get => _timout; set => SetProperty(ref _timout, value); }
        public bool ShouldSerializetimout() => _timout != 2097;
        #endregion

        #region property Device PB Meta Data 
        public string Name => metaData.name;
        public int Address => GetD<int>(MetaData.Atr.adr);
        public string info => GetD<string>(MetaData.Atr.info);
        public int chip => GetD<int>(MetaData.Atr.chip);
        public int serial => GetD<int>(MetaData.Atr.serial);
        public int SupportUartSpeed => GetD<int>(MetaData.Atr.SupportUartSpeed);
        public int NoPowerDataCount => GetD<int>(MetaData.Atr.NoPowerDataCount);
        private T GetD<T>(MetaData.Atr atr)
        {
            var v = metaData.attrs.FirstOrDefault(a => a.Atr == atr);
            return v.value != null ? (T)v.value : default!;
        }
        [XmlElement("struct_t", Namespace = StructDef.NAMESPACE)]
        public StructDef metaData { get; set; } = StructDef.Empty;
        public bool ShouldSerializeData() => metaData != StructDef.Empty;
        #endregion
    }
    /// <summary>
    /// для циклоопроса сенсоров на шине bus
    /// если нет хотябы одного Device то убить Bus
    /// </summary>
    public class Bus: ComplexAbstractVisitItem<Device> //Icomparable? // сериализуем
        //where DEVICE : Device
    {
        public static readonly Bus Empty = new Bus();

        public const string NOTBUS = "Bus not connected";

        #region Name
        private string _Name = NOTBUS;
        public string Name { get => _Name; set => SetProperty(ref _Name, value); }
        public bool ShouldSerializeName() => _Name != NOTBUS;
        #endregion
        public ObservableCollection<Device> Devices { get => Items; set { Items = value; } }
        public bool ShouldSerializeDevices() => Items.Count > 0;
    }
    public class BusPB: Bus//<DevicePB>
    {
        #region Interval
        private int _Interval = 2100;
        public int Interval { get => _Interval; set => SetProperty(ref _Interval, value); }
        public bool ShouldSerializeInterval() => _Interval != 2100;
        #endregion
    }
    /// <summary>
    /// wrapper for IConnection
    /// & Bus
    /// если нет хотябы одного Bus то убить Pipe
    /// </summary>
    public class Pipe: ComplexAbstractVisitItem<Bus> // IComparable сравнивать по IConnection
    {
        public static readonly Pipe Empty = new Pipe();

        [XmlIgnore]
        public IConnection? Connection { get; private set; }

        private AbstractConnection? con;

        [XmlElement("connection", IsNullable = false)]
        public AbstractConnection? connectionObj
        {
            get { return con; }
            set { con = value; Connection = con; }
        }
        public ObservableCollection<Bus> Buses { get => Items; set { Items = value; } }
        public bool ShouldSerializeBuses() => Items.Count > 0;
    }
    /// <summary>
    /// Рейс
    /// based on witsml:BhaRun
    /// </summary>
    public class Trip: ComplexAbstractVisitItem<Pipe>
    {
        public static readonly Trip Empty = new Trip();
        public int TripStatus { get; set; } // TODO:
        public DateTime? DTimStart { get; set; }
        public bool ShouldSerializeDTimStart()=> DTimStart.HasValue;
        public DateTime? DTimStop { get; set; }
        public bool ShouldSerializeDTimStop()=> DTimStop.HasValue;
        public DateTime? DTimStartDrilling { get; set; }
        public bool ShouldSerializeDTimStartDrilling() => DTimStartDrilling.HasValue;
        public DateTime? DTimStopDrilling { get; set; }
        public bool ShouldSerializeDTimStopDrilling() => DTimStopDrilling.HasValue;
        public string? ReasonTrip { get; set; }
        public bool ShouldSerializeReasonTrip() => ReasonTrip != null;
        public DateTime? Delay { get; set; }
        public bool ShouldSerializeDelay() => Delay.HasValue;
        public ObservableCollection<Pipe> Pipes { get => Items; set { Items = value; } }
        public bool ShouldSerializePipes() => Items.Count > 0;
    }
    /// <summary>
    /// Заезд 
    /// </summary>
    public class Visit: ComplexAbstractVisitItem<Trip>
    {
        public const string SCH = @"D:\Projects\C#\Communications\Project\XMLSchemaVisit.xsd";
        public const string NS = "http://tempuri.org/horizont.pb";
        public const string NS_PX = "vs";

        public static readonly Visit Empty = new Visit();
        /// <summary>
        /// локальные имена файлов witsMl
        /// select form by checkin xsd
        /// </summary>
      //   public StringCollection Documents { get; set; } = new StringCollection();
      //  public bool ShouldSerializeDocuments => Documents.Count > 0;
        public ObservableCollection<Trip> Trips { get => Items; set { Items = value; } }
        public bool ShouldSerializeTrips() => Items.Count > 0;
    }
}
