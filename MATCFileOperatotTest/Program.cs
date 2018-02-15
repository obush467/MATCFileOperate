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
            DirectoryInfo _directory = new DirectoryInfo("e:\\111");
            DirectoryInfo[] directories;//= _directory.GetDirectories();
            List<FileInfo> OldFiles = new List<FileInfo>();

            foreach (string dir in args)
            {
                di = new DirectoryInfo(dir);
                dList.Add(di);
                h = new List<FileInfo>();
            }
            FileOperator FOp = new FileOperator(new SqlConnection(_SQLConnectionString), LogManager.GetLogger(typeof(FileOperator)),false);
            OldFiles = new List<FileInfo>();
            directories=new DirectoryInfo[] { _directory};

            matcFileName mmm = new matcFileName("01111ГУ987654_20180202", "^(\\d{5}ГУ\\d{6})(?:_(.*))*_(\\d{8})(?:_(\\d{1,2}))*$");

            var yyy = FOp.lsToFileInfoW(_directory, "^(\\d{5}ГУ\\d{6})(?:_(.*))*_(\\d{8})(?:_(\\d{1,2}))*$").ToList();
            var yyy1=FOp.CalcNumber(yyy.Where(sel=>sel.Name.Number==null ).ToList());
            foreach (var y in yyy1) { y.NeedRename = true; }
            FOp.DestinationDirectory = new DirectoryInfo("Z:\\");
            FOp.TransferableFilesList = yyy;
            FOp.calculateTransferableFilesList();
            FOp.LoadDestinationByNumbers();
            FOp.Load(dList);
        }

        
    }
}
