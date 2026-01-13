using System.Collections.Specialized;
using System.Xml.Serialization;

namespace HorizontDrilling.Models
{
    //internal class VisitItem
    //{
    //    /// <summary>
    //    ///  vst file
    //    /// </summary>
    //    internal string LocalPathVisit { get; set; } = string.Empty;
    //    //internal bool IsUnloaded { get; set; }
    //    //internal bool ShouldSerializeIsUnloaded() => IsUnloaded == true;
    //    //[XmlIgnore] internal Visit? Visit { get; set; }
    //}

    public class GroupFile
    {
        /// <summary>
        /// vstgrp File
        /// </summary>
       // [XmlIgnore] public string GgoupFullFile { get; set; } = string.Empty;
        /// <summary>
        ///  vst files
        /// </summary>
        public StringCollection LocalPathVisit { get; set; } = new ();
        public bool ShouldSerializeLocalPathVisit() => LocalPathVisit.Count > 0;
        public StringCollection LocalPathOpenAverFiles { get; set; } = new();
        public bool ShouldSerializeLocalPathOpenAverFiles() => LocalPathOpenAverFiles.Count > 0;
        public static XmlSerializer Serializer => new XmlSerializer(typeof(GroupFile));
    }
}
