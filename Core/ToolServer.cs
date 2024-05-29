using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace Core
{
    public interface IToolServer
    {
        public void AddBar(ToolBarVM toolBar);
        public void DelBar(string BarContentID);
        public void Add(string BarContentID, IEnumerable<ToolButton> Tools);
        public void Add(string BarContentID, ToolButton Tool);
        public void Remove(string ContentID);
        public void Remove(IEnumerable<ToolButton> Tools);
        public bool Contains(string ContentID);
        public bool Contains(ToolButton Tool);
        public void UpdateSeparatorGroup(string BarContentID);
    }

    internal class ToolServer: PriorityServer, IToolServer
    {
        private static List<ToolBarVM> barVMs = new List<ToolBarVM>();
        public void Add(string BarContentID, IEnumerable<ToolButton> Tools)
        {
            foreach (var br in barVMs)
                if (br.ContentID == BarContentID)
                {
                    base.Add(br.Items, Tools);
                    return;
                }
        }
        public void Add(string BarContentID, ToolButton Tool) => Add(BarContentID, new[] { Tool });
        

        public void AddBar(ToolBarVM toolBar)
        {
            barVMs.Add(toolBar);
        }
        public bool Contains(string ContentID)
        {
           foreach( var br in barVMs)
              foreach(var t in br.Items)
                    if (t.ContentID == ContentID) return true;
           return false;                    
        }
        public bool Contains(ToolButton Tool)
        {
            foreach (var br in barVMs)
                foreach (var t in br.Items)
                    if (t  == Tool) return true;
            return false;
        }

        public void DelBar(string BarContentID)
        {
            foreach (var br in barVMs)
                if (br.ContentID == BarContentID)
                {
                    barVMs.Remove(br);
                    return;
                }
        }

        public void Remove(string ContentID)
        {
            foreach (var br in barVMs)
                foreach (var t in br.Items)
                    if (t.ContentID == ContentID)
                    {
                        br.Items.Remove(t);
                        UpdateSeparatorGroup(br.Items);
                        return;
                    }
        }

        public void Remove(IEnumerable<ToolButton> Tools)
        {
            foreach (var br in barVMs) 
                foreach (var t in br.Items)
                    if (Tools.Contains(t))
                    {
                        foreach (var i in Tools) br.Items.Remove(i);
                        UpdateSeparatorGroup(br.Items);
                        return;
                    }                
        }

        public void UpdateSeparatorGroup(string BarContentID)
        {
            foreach (var br in barVMs)
                if (br.ContentID == BarContentID)
                {
                    base.UpdateSeparatorGroup(br.Items);
                    return;
                }
        }
    }
}
