using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Core
{
    public abstract class PriorityItem: VMBase
    {
        public int Priority { get; set; } = 100;
        public int Group => Priority / 100;
    }
    public class Separator: PriorityItem { }

    public abstract class PriorityItemBase : PriorityItem 
    {
        #region Visibility
        private Visibility _visibility;
        public Visibility Visibility { get => _visibility; set => SetProperty(ref _visibility, value); }
        #endregion

        #region IsEnable
        private bool _IsEnable = true;
        public bool IsEnable { get => _IsEnable; set => SetProperty(ref _IsEnable, value); }
        #endregion

        ToolTip? _toolTip;
        public ToolTip? ToolTip { get=>_toolTip; set=>SetProperty(ref _toolTip, value); }
        public bool IconSourceEnable => IconSource != null;

        string? _IconSource;
        public string? IconSource { get => _IconSource; set => SetProperty(ref _IconSource, value); }
    }

    public abstract class PriorityServer
    {
        public void Add(ObservableCollection<PriorityItem> Items, IEnumerable<PriorityItemBase> add )
        {
            var sorted = from mi in add
                         orderby mi.Priority
                         select mi;
            int index = Items.Count();
            if (sorted.Count() > 0 && index > 0)
            {
                var mFirstPriority = Items.FirstOrDefault((mi) => mi.Priority > sorted.First().Priority);
                if (mFirstPriority != null) index = Items.IndexOf(mFirstPriority);
            }
            foreach (var m2 in sorted) Items.Insert(index++, m2);
            UpdateSeparatorGroup(Items);
        }
        public void UpdateSeparatorGroup(ObservableCollection<PriorityItem> Items)
        {

            if (Items.Count > 0)
            {
                // remove begin end separators
                while (Items.Count > 0 && IsSeparator(Items[0])) Items.RemoveAt(0);
                while (Items.Count > 0 && IsSeparator(Items[^1])) Items.RemoveAt(Items.Count - 1);
                int i = 1;
                while (i < Items.Count)
                {
                    // remove double separators
                    while (IsSeparator(Items[i - 1]) && IsSeparator(Items[i])) Items.RemoveAt(i);
                    // remove error separator
                    if (i+1 < Items.Count && 
                        Items[i - 1].Group == Items[i+1].Group && 
                        !IsSeparator(Items[i - 1]) && !IsSeparator(Items[i+1]) && IsSeparator(Items[i])) Items.RemoveAt(i);
                    // add separator
                    if (Items[i - 1].Group != Items[i].Group && 
                        !IsSeparator(Items[i - 1]) && !IsSeparator(Items[i]))
                    {
                        Items.Insert(i, new Separator { Priority = Items[i - 1].Priority });
                    }
                    i++;
                }
            }
            static bool IsSeparator(VMBase m)
            {
                return (m is Separator);
            }
        }

    }
}
