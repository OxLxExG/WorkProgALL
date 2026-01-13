using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace HorizontDrilling.Views
{
    public static class Icons
    {
        public static ComponentResourceKey HPOn => new ComponentResourceKey(typeof(Icons), "HPOn");
        public static ComponentResourceKey HPOff => new ComponentResourceKey(typeof(Icons), "HPOff");
        public static ComponentResourceKey VZerro => new ComponentResourceKey(typeof(Icons), "VZerro");
        public static ComponentResourceKey VMinus => new ComponentResourceKey(typeof(Icons), "VMinus");
        public static ComponentResourceKey VPlus => new ComponentResourceKey(typeof(Icons), "VPlus");
    }
}
