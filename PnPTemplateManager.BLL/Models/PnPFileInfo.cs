using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PnPTemplateManager.Models
{
    public class PnPFileInfo
    {

        public string Name { get; set; }
        public string DirectoryUrl { get; set; }
        public string FullName { get; set; }
        public DateTime CreationTime { get; set; }
        public DateTime ModificationTime { get; set; }
        /// <summary>
        /// Size in KB
        /// </summary>
        public decimal Size { get; set; }
    }
}
