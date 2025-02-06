using Communications.MetaData;
using Connections;
using Core;
using Main.Properties;
using Main.ViewModels;
using System;
using System.Collections.Specialized;
using System.IO;
using System.Xml.Serialization;

namespace Main.Models
{

    internal static class ProjectFile
    {
        internal static string GetTmpFile(string File, string newExt)
        {
            var p = Path.GetDirectoryName(File)+ "\\.hb\\";
            var n = Path.GetFileNameWithoutExtension(File);
            return p + n + newExt;
        }
        internal static void CloseRoot(bool user)
        {
            RootFileDocumentVM.Instance?.Remove(user);
            RootFileDocumentVM.Instance = null;
            DockManagerVM.Clear();
            Settings.Default.CurrentRoot = string.Empty;
        }
        internal static void LoadRoot(string file , bool IsVisit)
        {
            if (IsVisit)
            {
                RootFileDocumentVM.Instance = VisitDocument.Load(file, true); 
            }
            else
                RootFileDocumentVM.Instance = GroupDocument.Load(file);
        }
        internal static void CreateRoot(CreateNewVisitDialogResult res, string rootFile)
        {
            var d = Path.GetDirectoryName(rootFile)!;
            Directory.CreateDirectory(d);
            Directory.CreateDirectory(d+"\\.hb\\");

            if (res.VisitType == NewVisitType.SingleVisit)
            {
                RootFileDocumentVM.Instance = VisitDocument.CreateAndSave(rootFile, true);
            }
            else
            {
                RootFileDocumentVM.Instance = GroupDocument.CreateAndSave(rootFile);
            }
        }
        internal static void Add(string visitFile)
        {
            if (RootFileDocumentVM.Instance is GroupDocument gd)
            {
                VisitDocument v;
                if (File.Exists(visitFile))
                    v = VisitDocument.Load(visitFile, false);
                else
                {
                    var d = Path.GetDirectoryName(visitFile)!;
                    Directory.CreateDirectory(d);
                    Directory.CreateDirectory(d + "\\.hb\\");

                    v = VisitDocument.CreateAndSave(visitFile, false);
                    //v.VisitVM.ItemsAdd(new TripVM());
                }

                // TripVM tripVM = (TripVM) new Trip { Delay = DateTime.Now };

                var trip = v.VisitVM.Add((TripVM)new Trip
                {
                    DTimStart = DateTime.Now,
                    Delay = DateTime.Now
                }
                );
                var bus = trip.Add(new BusPBVM { VMConn = new SerialVM {BaudRate = 125000, PortName = "COM5", } });
                bus.ItemsAdd(new DevicePB { metaData = BinaryParser.Parse(BinaryParser.Meta_Ind) });
                bus.ItemsAdd(new DevicePB { metaData = BinaryParser.Parse(BinaryParser.Meta_NNK) });
                bus.Interval = 4200;

                bus = trip.Add(new BusPBVM { VMConn = new NetVM() });
                bus.ItemsAdd(new DevicePB { metaData = BinaryParser.Parse(BinaryParser.Meta_CAL) });

                var bus32 = trip.Add(new BusUSO32VM { VMConn = new NopConVM() });
                bus32.ItemsAdd(new DeviceTelesystem());
                

                //v.VisitVM
                //    .Add(new Trip { DTimStart = System.DateTime.Now })
                //    .Add(new SerialPipe { SerialConn = new SerialConn() })
                //    .Add(new BusPB())
                //    .Add(new DevicePB { metaData = BinaryParser.Parse(BinaryParser.Meta_Ind) });
                //v.VisitVM
                //    .Add(new Trip { DTimStart = System.DateTime.Now })
                //    .Add(new NetPipe { NetConn = new NetConn() })
                //    .Add(new BusPB())
                //    .Add(new DevicePB { metaData= BinaryParser.Parse(BinaryParser.Meta_NNK) });

                v.IsDirty = true;

                gd.AddVisit(v);
                gd.Save();
            }

        }    
        static CreateNewVisitDialogResult dlgRes = null!;
        static bool IsCreateProject => dlgRes.VisitType == NewVisitType.SingleVisit || dlgRes.VisitType == NewVisitType.NewGroup;
        static bool IsAddVisit => dlgRes.VisitType != NewVisitType.SingleVisit;// dlgRes.VisitType == NewVisitType.AddVisit || dlgRes.VisitType == NewVisitType.NewGroup;

        internal static void LoadRoot(string file)
        {
            var ext = Path.GetExtension(file);
            if ((ext == ".vst") || (ext == ".vstgrp"))
            {
                LoadRoot(file, ext == ".vst");
            }
            else throw new InvalidDataException(ext);
        }
        internal static void CreateOldProject(string file)
        {

            var ext = Path.GetExtension(file);
            if ((ext == ".vst") || (ext == ".vstgrp"))
            {
                LoadRoot(file, ext == ".vst");
            }
            else throw new InvalidDataException(ext);
            Settings.Default.CurrentRoot = file;
            var d = Directory.GetParent(file);
            Settings.Default.CurrentWorkDir = d!.Parent!.FullName;
            var n = Path.GetFileNameWithoutExtension(file);
            Settings.Default.CurrentGroupName = ext == ".vstgrp" ? n : string.Empty;
            Settings.Default.CurrentVisitName = ext == ".vst" ? n : string.Empty;
            Settings.Default.IsSingleVisit = ext == ".vst";
            Settings.Default.Save();
        }
        internal static void AddNewProject(ICreateNewVisitDialog d)
        {
            if (d != null)
            {
                d.AddToCurrent = true;
                if (d.Show(result => dlgRes = result) == BoxResult.OK)
                {
                    Add(dlgRes.VisitFullFile);
                    Settings.Default.CurrentVisitName = dlgRes.VisitFile;
                    Settings.Default.Save();
                }
            }
        }

        internal static void CreateNewProject(ICreateNewVisitDialog d)
        {
            if (d != null && d.Show(result => dlgRes = result) == BoxResult.OK)
            {
                ////
                // CreateNewVisitDialog closed
                // All checked
                ////
                

                string rootFile = dlgRes.VisitType == NewVisitType.SingleVisit ? dlgRes.VisitFullFile : dlgRes.GroupFullFile;
                /// создаем новый проект NewVisitType.SingleVisit || r.VisitType == NewVisitType.NewGroup
                /// сначала удаляем старый
                if (IsCreateProject)
                {
                    CloseRoot(true);
                    //DoCloseProject?.Invoke(dlgRes, new(true));
                    /// создаем новый проект заезд или группу
                    CreateRoot(dlgRes, rootFile);
                    //DoCreateProject?.Invoke(dlgRes, new(rootFile));
                }
               if (IsAddVisit)
                {
                    Add(dlgRes.VisitFullFile);
                    /// создаем и добавляем заезд в группу
                    //DoAddNewToGroup?.Invoke(dlgRes, new(dlgRes.VisitFullFile));
                }
               if (IsCreateProject) Settings.Default.CurrentRoot = rootFile;
               Settings.Default.CurrentWorkDir = dlgRes.RootDir;
               Settings.Default.CurrentGroupName = dlgRes.GroupFile;
               Settings.Default.CurrentVisitName = dlgRes.VisitFile;
               Settings.Default.IsSingleVisit = dlgRes.VisitType == NewVisitType.SingleVisit;
               Settings.Default.Save();
            }
        }
        internal static string RootDir => Settings.Default.CurrentWorkDir;
        internal static string WorkDirPath
        {
            get
            {
                if (SingleVisit) return Path.Combine(RootDir, VisitName + "\\");
                else return Path.Combine(RootDir, GroupName + "\\");
            }
        }
        internal static string GroupName => Settings.Default.CurrentGroupName;
        internal static string VisitName => Settings.Default.CurrentVisitName;
        internal static StringCollection WorkDirs => Settings.Default.WorkDirs;
        internal static bool SingleVisit => Settings.Default.IsSingleVisit;
    }
}
