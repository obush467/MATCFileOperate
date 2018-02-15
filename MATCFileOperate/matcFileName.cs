using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MATCFileOperate
{
    public class matcFileName
    {
        public string Number { get; set; }
        public string Content { get; set; }
        public string Date { get; set; }
        public string Counter { get; set; }
        public matcFileName() { }
        public matcFileName(string name,string pattern)
        {
            Regex regex = new Regex(pattern);
            Match m = regex.Match(name);
            int[] groupNumbers = regex.GetGroupNumbers();
            if (m.Success)
            {
                Number=m.Groups[1].Value;
                Content= m.Groups[2].Value;
                Date =m.Groups[3].Value;
                Counter=m.Groups[4].Value;
            }
        }
    public matcFileName(FileInfo fileI, string pattern):this(fileI.Name.Replace(fileI.Extension,""),pattern)
    {}
}
    
}
