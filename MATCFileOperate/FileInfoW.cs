using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace MATCFileOperate
{
    public class FileInfoW
    {
        public FileInfo fileInfo { get; set; }
        public byte[] MD5 { get; set; }
        public matcFileName Name  { get; set; }
        public bool NeedRename { get; set; } = false;
        public bool NeedRemove { get; set; } = false;
        public bool NeedDelete { get; set; } = false;
        public bool Renamed { get; set; } = false;
        public bool Removed { get; set; } = false;
        public bool Deleted { get; set; } = false;
        public DirectoryInfo RemoveTo { get; set; }
        public FileInfoW(FileInfo vfileInfo):this(vfileInfo,new matcFileName())
        { 
        }

        public FileInfoW(FileInfo vfileInfo, matcFileName vmatcFileName)
        {
            fileInfo = vfileInfo;
            Name = vmatcFileName;
        }
    }
}
