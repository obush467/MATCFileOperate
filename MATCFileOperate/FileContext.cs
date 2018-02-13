using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.Entity;
using System.IO;
using System.ComponentModel.DataAnnotations;
using System.Data.Common;
using System.Data.Entity.Core.EntityClient;

namespace MATCFileOperate
{

    public class FileContext : DbContext
    {
        public FileContext(string nameOrConnectionString) : base(nameOrConnectionString)
        {}

        public FileContext(DbConnection dbConnection,bool contextOwnsConnection) : base(dbConnection, contextOwnsConnection)
        {}
        public FileContext() : base()
        { }

        public DbSet<kFileInfo> Files { get; set; }

    }
    
}
