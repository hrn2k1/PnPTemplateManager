using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PnPTemplateManager.Models
{
    [Serializable]
    public class StorageFile
    {
        public string Path { get; set; }
        public byte[] Content { get; set; }
        public string Url { get; set; }

        public override string ToString()
        {
            return System.Text.UTF8Encoding.UTF8.GetString(Content);
        }
    }
}
