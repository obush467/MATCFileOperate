using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MATCFileOperate
{
    public class kFileInfo
    {
        [Key]
        public int ID { get; set; }
        [StringLength(400)]
        public string FullName { get; set; }
        [StringLength(400)]
        public string Name { get; set; }
        [StringLength(400)]
        public string DirectoryName { get; set; }
        public bool IsReadOnly { get; set; }
        public DateTime CreationTime { get; set; }
        public DateTimeOffset CreationTimeUtc { get; set; }
        public DateTime LastAccessTime { get; set; }
        public DateTimeOffset LastAccessTimeUtc { get; set; }
        public DateTime LastWriteTime { get; set; }
        public DateTimeOffset LastWriteTimeUtc { get; set; }
        public int Attributes { get; set; }
        [MaxLength(16)]
        public byte[] MD5 { get; set; }
        public long Length { get; set; }

        public kFileInfo(FileInfo file)
        {
            FullName = file.FullName;
            Name = file.Name;
            IsReadOnly = file.IsReadOnly;
            CreationTime = file.CreationTime;
            CreationTimeUtc = file.CreationTimeUtc;
            LastAccessTime = file.LastAccessTime;
            LastAccessTimeUtc = file.LastAccessTimeUtc;
            LastWriteTime = file.LastWriteTime;
            LastWriteTimeUtc = file.LastWriteTimeUtc;
            DirectoryName = file.DirectoryName;
            Length = file.Length;
            Attributes = (int)file.Attributes;

        }
        public kFileInfo() : base() { }
    }
}
