using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExceptLog
{
    public record StdLogg(bool Error, bool Info, bool Trace, bool Monitor)
    {
        public StdLogg() : this(true, false, false, false) { }
    }
    public record StdLoggs(StdLogg Box, StdLogg File)
    {
        public StdLoggs() : this(new(), new()) { }
    }
    //public record GlobalSettings(StdLoggs Logging, string Culture, bool Group, string GroupDir, string ProjectDir)
    //{
    //    public GlobalSettings() : this(new(), "en-US", false, string.Empty, string.Empty) { }
    //}

}
