using Communications.MetaData;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using static Communications.MetaData.BinaryParser;
using System.Xml.Serialization;

namespace Main.Models
{
    /// <summary>
    /// абстракции
    /// </summary>
    public abstract class ItemM : INotifyPropertyChanged
    {
        public Guid Id { get; set; } = Guid.NewGuid();

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
    }
    public abstract class ComplexM: ItemM
    {
       protected void ItemsAdd<CHILDS>(ItemM item) where CHILDS : ItemM
       {
            if (item is CHILDS t)
            {
                if (Items == null) Items = new ObservableCollection<ItemM> ();
                if (!Items.Contains(item)) Items.Add(item);
            }
            else throw new InvalidOperationException();
        }
        protected void ItemsRemove<CHILDS>(ItemM item) where CHILDS : ItemM
        {
            if (item is CHILDS)
            {
                if (Items != null)
                {
                    Items.Remove(item);
                    if (Items.Count == 0) Items = null;
                }
            }
            else throw new InvalidOperationException();
        }

        public ObservableCollection<ItemM>? Items { get; set; }
        public bool ShouldSerializeItems() => Items != null && Items.Count>0;

    }
    public class Device: ItemM
    {
        public static readonly Device Empty = new Device();
    }

    /// <summary>
    /// реальные классы
    /// </summary>
    public sealed class DeviceTelesystem : Device;
    public sealed class DeviceTelesystem2 : Device;

    public record class RamReadInfo(string RAMFileName, int from, int too)
    {
        public RamReadInfo() : this(string.Empty, 0, 0) { }
    }
    public sealed class DevicePB : Device
    {
        public RamReadInfo? ramReadInfo { get; set; }
        public bool ShouldSerializeramReadInfo() => ramReadInfo != null;

        #region timout
        private int _timout = 2097;
        public int timout { get => _timout; set => SetProperty(ref _timout, value); }
        public bool ShouldSerializetimout() => _timout != 2097;
        #endregion

        #region property Device PB Meta Data 
        public string Name => metaData.name;
        public int Address => GetD<int>(Atr.adr);
        public string info => GetD<string>(Atr.info);
        public int chip => GetD<int>(Atr.chip);
        public int serial => GetD<int>(Atr.serial);
        public int SupportUartSpeed => GetD<int>(Atr.SupportUartSpeed);
        public int NoPowerDataCount => GetD<int>(Atr.NoPowerDataCount);
        private T GetD<T>(Atr atr)
        {
            var v = metaData.attrs.FirstOrDefault(a => a.Atr == atr);
            return v.value != null ? (T)v.value : default!;
        }
        [XmlElement("struct_t", Namespace = StructDef.NAMESPACE)]
        public StructDef metaData { get; set; } = StructDef.Empty;
        public bool ShouldSerializeData() => metaData != StructDef.Empty;
        #endregion
    }
    public class Trip : ComplexM
    {
        public int TripStatus { get; set; } // TODO:
        public DateTime? DTimStart { get; set; }
        public bool ShouldSerializeDTimStart() => DTimStart.HasValue;
        public DateTime? DTimStop { get; set; }
        public bool ShouldSerializeDTimStop() => DTimStop.HasValue;
        public DateTime? DTimStartDrilling { get; set; }
        public bool ShouldSerializeDTimStartDrilling() => DTimStartDrilling.HasValue;
        public DateTime? DTimStopDrilling { get; set; }
        public bool ShouldSerializeDTimStopDrilling() => DTimStopDrilling.HasValue;
        public string? ReasonTrip { get; set; }
        public bool ShouldSerializeReasonTrip() => ReasonTrip != null;
        public DateTime? Delay { get; set; }
        public bool ShouldSerializeDelay() => Delay.HasValue;

        public void ItemsAdd(Device item) => ItemsAdd<Device>(item);
        public void ItemsRemove(Device item) => ItemsRemove<Device>(item);
    }
    public class Visit : ComplexM
    {
        public const string NS = "http://tempuri.org/horizont.pb";

        public DateTime? DTimStart { get; set; }
        public bool ShouldSerializeDTimStart() => DTimStart.HasValue;
        public DateTime? DTimStop { get; set; }
        public bool ShouldSerializeDTimStop() => DTimStop.HasValue;

        public void ItemsAdd(Trip item) => ItemsAdd<Trip>(item);
        public void ItemsRemove(Trip item) => ItemsRemove<Trip>(item);

        public static XmlSerializer Serializer => new XmlSerializer(typeof(Visit), null, new[]
        {
                            typeof(DevicePB),
                            typeof(DeviceTelesystem),
                            typeof(DeviceTelesystem2),
                            typeof(Trip),
        }, null, NS, null);

    }
}
