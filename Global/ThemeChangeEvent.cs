using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Global
{
    public static class ThemeChangeEvent
    {
       public static event EventHandler<bool>? ThemeChanged;
       public static void DoThemeChanged(object? sender, bool e)
        {
            ThemeChanged?.Invoke(sender, e);
        }

    }
}
