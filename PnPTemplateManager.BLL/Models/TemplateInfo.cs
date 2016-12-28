using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PnPTemplateManager.Models
{
    [Serializable]
    public class TemplateInfo
    {
        public string FeatureToggle { get; set; }
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public bool Hidden { get; set; }
        public string WebTemplate { get; set; }
        public string ModuleName { get; set; }
        public string TemplateFilePath { get; set; }
    }
}
