//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace PnPTemplateManager.BLL
{
    using System;
    using System.Collections.Generic;
    
    public partial class BA_App
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public BA_App()
        {
            this.BA_Instance = new HashSet<BA_Instance>();
        }
    
        public int Id { get; set; }
        public string Name { get; set; }
        public string SiteTemplatePath { get; set; }
    
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<BA_Instance> BA_Instance { get; set; }
    }
}
