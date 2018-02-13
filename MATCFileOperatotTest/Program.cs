using log4net;
using MATCFileOperate;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.Entity.Core.Common.EntitySql;
using System.Data.Entity.Core.EntityClient;
using System.Data.Entity.Core.Metadata.Edm;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Security;
using System.Security.AccessControl;
using System.Security.Permissions;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace MATCFileOperatotTest
{
    class Program
    {
        static void Main(string[] args)
            
        {
            string _SQLConnectionString = "Data Source=MAKSIMOV;Initial Catalog=GBUMATC;Integrated Security=False;User ID=Бушмакин;Password=453459;";
            log4net.Config.XmlConfigurator.Configure();
            DirectoryInfo di; 
            List<DirectoryInfo> dList = new List<DirectoryInfo>();
            List<FileInfo> h;
            foreach (string dir in args)
            {
                di = new DirectoryInfo(dir);
                dList.Add(di);
                h = new List<FileInfo>();
            }
            FileOperator FOp = new FileOperator(new SqlConnection(_SQLConnectionString), LogManager.GetLogger(typeof(FileOperator)),false);
            try
            {
                var ttt = (new FileIOPermission(FileIOPermissionAccess.AllAccess, "\\\\moscow.gbumac.ru\\МАЦ\\Мониторинг\\Паспорта_и_Макеты\\DfsrPrivate"));
                ttt.Demand();
                DirectoryInfo _directory = new DirectoryInfo("\\\\moscow.gbumac.ru\\МАЦ\\Мониторинг\\Паспорта_и_Макеты\\DfsrPrivate");
                //FOp.MD5CalculateParallel(20);
                PermissionSet ps1 = new PermissionSet(PermissionState.Unrestricted);
                var ds = new DirectorySecurity(_directory.FullName, AccessControlSections.Access);
                var ar =ds.GetAccessRules(true, true, typeof(SecurityIdentifier));
                
                var perm = (new FileIOPermission(FileIOPermissionAccess.Read, _directory.FullName));
                ps1.AddPermission(perm);
                ps1.Demand();

                var Files1 = _directory.EnumerateFiles("*", SearchOption.AllDirectories)
                       .Where(seq =>
                       {
                           //PermissionSet ps1 = new PermissionSet(PermissionState.None);
                           
                           //var perm = (new FileIOPermission(FileIOPermissionAccess.AllAccess, seq.FullName));
                           ps1.AddPermission(perm);
                           ps1.Demand();
                           if (perm.GetPathList(FileIOPermissionAccess.Read).Count() > 0 & (seq.Attributes & FileAttributes.Hidden) != FileAttributes.Hidden & seq.DirectoryName != "DfsrPrivate" & seq.Name != "DfsrPrivate")
                           { return true; }
                           else { return false; };
                       }).ToList();
            }
            finally { }

            FOp.Load(dList);
        }
    }
}
