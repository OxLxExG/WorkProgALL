using Communications.Project;
using Core;
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace WorkProgMain.ViewModels
{
    public abstract class BaseVisitItemVM : VMBase
    {
        public bool IsExpanded { get; set; }
        public bool IsActive { get; set; }
        #region Factory
        protected static readonly Dictionary<Type,Func<AbstractVisitItem, BaseVisitItemVM>> _factory = new ();

        protected static void AddFactory(Type tp, Func<AbstractVisitItem, BaseVisitItemVM> FactoryFunc)
        {
           if (!_factory.ContainsKey(tp))
                _factory.Add(tp, FactoryFunc);
        }
        protected static void AddFactory<M,VM>()
            where VM : BaseVisitItemVM , new()
            where M : AbstractVisitItem
        {
            var tp = typeof(M);
            if (!_factory.ContainsKey(tp))
                _factory.Add(tp, m => new VM() 
                { 
                    model = (M)m 
                });
        }
        protected static BaseVisitItemVM GetBaseVisitItemVM(AbstractVisitItem m)
        {
            Func<AbstractVisitItem, BaseVisitItemVM> f;
            if (_factory.TryGetValue(m.GetType(), out f!)) ///TODO: Recur Base Type Check ????
               return f(m);
            throw new ArgumentException();
        }
        internal static T GetVM<T>(AbstractVisitItem m)
            where T : BaseVisitItemVM
        {
            var r = GetBaseVisitItemVM(m);
            if (r is T t) //ERROR then bus
                return t;
            throw new ArgumentException();
        }
        #endregion
        internal abstract void SetModel(AbstractVisitItem? m);// => _model = m;

        protected AbstractVisitItem? _model;
        [XmlIgnore] public AbstractVisitItem? model { get => _model; 
            set
            {
                if (_model == value) return;
                SetModel(value);
            } 
        }
    }
    public abstract class SimpleVisitItemVM: BaseVisitItemVM
    {
        internal override void SetModel(AbstractVisitItem? m)
        {
            if (m is Device)
            {
               if (string.IsNullOrEmpty(ContentID)) ContentID = m.Id.ToString("D");
                _model = m;
            }
            else throw new ArgumentException(); 
        }            
        [XmlIgnore] public new Device? model { get=> (Device?) base.model; set => base.model = value; }
    }
    public abstract class ComplexBaseVisitItemVM: BaseVisitItemVM
    {
        public abstract void ItemsRemove(BaseVisitItemVM item);
        public abstract void ItemsAdd(BaseVisitItemVM item);
        public abstract bool ContainsModel(string modelID);
    }
    public abstract class ComplexBaseVisitItemVM<CHILD, CHILDVM> : ComplexBaseVisitItemVM
        where CHILD : AbstractVisitItem
        where CHILDVM : BaseVisitItemVM, new()
    {
        internal override void SetModel(AbstractVisitItem? m)
        {
            if (m is ComplexAbstractVisitItem<CHILD> mm) SetModel(mm);
            else throw new ArgumentException();
        }
        internal void SetModel(ComplexAbstractVisitItem<CHILD>? m)
        {
            if (_model != null || m == null) throw new InvalidOperationException();            
            _model = m;
            var ID = m!.Id.ToString("D");
            if (string.IsNullOrEmpty(ContentID))
            {
                ContentID = ID;
                Items = new ObservableCollection<CHILDVM>(m.Items.Select(GetVM<CHILDVM>));
                
            }
            else if (ContentID != ID) throw new InvalidOperationException();
            else foreach (var item in m.Items)
            {
                var id = item.Id.ToString("D");
                var md = GetModel(id);
                if (md == null)
                {
                    ItemsAdd(GetVM<CHILDVM>(item));
                }
                else md.model = item;
            }
        }
        [XmlIgnore] public new ComplexAbstractVisitItem<CHILD>? model 
        { 
            get => (ComplexAbstractVisitItem<CHILD>?)base.model; 
            set => base.model = value; 
        }

        //[XmlIgnore]
        public ObservableCollection<CHILDVM> Items { get; set; } = new ObservableCollection<CHILDVM>();
        public bool ShouldSerializeItems() => Items.Count > 0;
        public CHILDVM? GetModel(string modelID) => Items.FirstOrDefault(vm => vm.ContentID == modelID);
        public override bool ContainsModel(string modelID) => GetModel(modelID) != null;
        public override void ItemsAdd(BaseVisitItemVM item)
        {
            if (item is CHILDVM t)
            {
                if (!Items.Contains(t)) Items.Add(t);
            }
            else throw new InvalidOperationException();
        }
        public override void ItemsRemove(BaseVisitItemVM item)
        {
            if (item is CHILDVM t) Items.Remove(t);
            else throw new InvalidOperationException();
        }
    }
    public class DeviceVM : SimpleVisitItemVM;
    public class DevicePBVM : DeviceVM;
    public class DeviceT1VM : DeviceVM;
    public class DeviceT2VM : DeviceVM;
    public class BusVM : ComplexBaseVisitItemVM< Device, DeviceVM>;
    public class BusPBVM : BusVM;// ComplexBaseVisitItemVM<DevicePB, DevicePBVM>;
    public class PipeVM : ComplexBaseVisitItemVM<Bus, BusVM>;
    public class TripVM : ComplexBaseVisitItemVM<Pipe, PipeVM>;
    public class VisitVM : ComplexBaseVisitItemVM<Trip, TripVM>
    {
        public static implicit operator VisitVM(Visit d)
        {
            var r = new VisitVM();
            r.SetModel(d);
            return r;
        }
        static VisitVM() 
        {
            AddFactory<Visit, VisitVM>();
            AddFactory<Trip, TripVM>();
            AddFactory<Pipe, PipeVM>();
            AddFactory<Bus, BusVM>();
            AddFactory<BusPB, BusPBVM>();
            AddFactory<DevicePB, DevicePBVM>();
            AddFactory<DeviceTelesystem, DeviceT1VM>();
            AddFactory<DeviceTelesystem2, DeviceT2VM>();
        }
    }
    public class VisitDocument : ComplexFileDocumentVM 
    {
        public VisitVM VisitVM
        {
            get 
            { 
                if (Model == null) Model = new VisitVM();
                    
                return (VisitVM)Model;
            } 
            set
            {                
                Model = value; 
            }
        }
    }
}
