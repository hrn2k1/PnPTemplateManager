using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PnPTemplateManager.Models
{
    public class ApplyPnPTemplateRequest
    {
        public string SiteUrl { get; set; }
        public string AccessToken { get; set; }
        public string PnPFileUrl { get; set; }
        public string PnPFileName { get; set; }
        public string SiteName { get; set; }
        public string SiteTitle { get; set; }
        public string SiteDescription { get; set; }
        public string PnPXML { get; set; }

    }

    public class ApplyRambollTemplateRequest
    {
        public string SiteUrl { get; set; }
        public string AccessToken { get; set; }
        public string PnPXML { get; set; }

        public string ProjectNumber { get; set; }
        public string ApplyComponent { get; set; } 

    }
}
