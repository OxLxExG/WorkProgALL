using Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Main.Models
{
    public static class MMenus
    {
        public static readonly rootMenu LastVisitGroups = new rootMenu(RootMenusID.NFile, RootMenusID.NFile_Last_ProjectGroups,
                                                                     Properties.Resources.nfile_last_ProjectGroups, 10700);

        public static readonly rootMenu LastVisits = new rootMenu(RootMenusID.NFile, RootMenusID.NFile_Last_Projects,
                                                                     Properties.Resources.nfile_last_Projects, 10701);

        public static readonly rootMenu LastFiles = new rootMenu(RootMenusID.NFile, RootMenusID.NFile_Last_Files,
                                                                     Properties.Resources.nfile_last_Files, 10702);

        public static readonly rootMenu NewProjectGroup = new rootMenu(RootMenusID.NFile_Create, RootMenusID.NFile_Create_Group,
                                                                     Properties.Resources.nProjectGroup_New, 700);
        public static readonly rootMenu NewProject = new rootMenu(RootMenusID.NFile_Create, RootMenusID.NFile_Create_Project,
                                                                     Properties.Resources.nProject_New, 701);
        public static readonly rootMenu NewFile = new rootMenu(RootMenusID.NFile_Create, RootMenusID.NFile_Create_File,
                                                                     Properties.Resources.nFile_New, 702);

        public static readonly rootMenu OpenProjectGroup = new rootMenu(RootMenusID.NFile_Open, RootMenusID.NFile_Open_Group,
                                                                     Properties.Resources.nfile_Open_Group, 700);
        public static readonly rootMenu OpenProject = new rootMenu(RootMenusID.NFile_Open, RootMenusID.NFile_Open_Project,
                                                                     Properties.Resources.nfile_Open_Project, 701);
        public static readonly rootMenu OpenFile = new rootMenu(RootMenusID.NFile_Open, RootMenusID.NFile_Open_File,
                                                                     Properties.Resources.nfile_Open_File, 702);

        public static readonly rootMenu AddProject = new rootMenu(RootMenusID.NFile_Add, RootMenusID.NFile_Add_Project,
                                                                     Properties.Resources.nfile_Add_Project, 701);
        public static readonly rootMenu AddFile = new rootMenu(RootMenusID.NFile_Add, RootMenusID.NFile_Add_File,
                                                                     Properties.Resources.nfile_Add_File, 702);

        public static readonly rootMenu CloseFile = new rootMenu(RootMenusID.NFile, RootMenusID.NFile_Close,
                                                                     Properties.Resources.nfile_Close, 501);
        public static readonly rootMenu CloseAll = new rootMenu(RootMenusID.NFile, RootMenusID.NFile_CloseAll,
                                                                     Properties.Resources.nfile_CloseAll, 502);

        public static readonly rootMenu SaveFile = new rootMenu(RootMenusID.NFile, RootMenusID.NFile_Save,
                                                                     Properties.Resources.nfile_Save, 701);
        public static readonly rootMenu SaveFileAS = new rootMenu(RootMenusID.NFile, RootMenusID.NFile_SaveAS,
                                                                     Properties.Resources.nfile_SaveAS, 702);
        public static readonly rootMenu SaveAll = new rootMenu(RootMenusID.NFile, RootMenusID.NFile_SaveAll,
                                                                     Properties.Resources.nfile_SaveAll, 703);

        public static void CreateMenusStructure()
        {
            RootMenus.Items.AddRange(new[]
            {
                new rootMenu(RootMenusID.ROOT, RootMenusID.NFile, Properties.Resources.m_File, 0),
                    new rootMenu(RootMenusID.NFile, RootMenusID.NFile_Create, Properties.Resources.nfile_Create, 100),
                        //NewProjectGroup,
                        //NewProject,
                        //NewFile,
                    new rootMenu(RootMenusID.NFile, RootMenusID.NFile_Open, Properties.Resources.nfile_Open, 101),
                        //OpenProjectGroup,
                        //OpenProject,
                        //OpenFile,
                    new rootMenu(RootMenusID.NFile, RootMenusID.NFile_Add, Properties.Resources.nfile_Add, 300),
                        //AddProject, 
                        //AddFile,
                    //CloseFile,
                    //CloseAll,
                    //SaveFile,
                    //SaveFileAS,
                    //SaveAll,
                    LastVisitGroups,
                    LastVisits,
                    LastFiles,
                //    new rootMenu(RootMenusID.NFile, RootMenusID.NFile_Last_Projects, Properties.Resources.nfile_last_Projects, 701),
                //    new rootMenu(RootMenusID.NFile, RootMenusID.NFile_Last_File, Properties.Resources.nfile_last_Files, 702),

                new rootMenu(RootMenusID.ROOT, RootMenusID.NShow, Properties.Resources.m_Show, 10),
                    new rootMenu(RootMenusID.NShow, RootMenusID.NDebugs, Properties.Resources.m_Debugs, 100),
            });

        }
    }
}
