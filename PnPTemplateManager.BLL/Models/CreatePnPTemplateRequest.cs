using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PnPTemplateManager.Models
{
     public class CreatePnPTemplateRequest
    {
        public string SiteUrl { get; set; }
        public string AccessToken { get; set; }
        public string PnpPackageName { get; set; }
    }
}
