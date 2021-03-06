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
    
    public partial class BA_SiteType
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public BA_SiteType()
        {
            this.BA_Action = new HashSet<BA_Action>();
            this.BA_Phase = new HashSet<BA_Phase>();
            this.BA_Role = new HashSet<BA_Role>();
            this.BA_Site = new HashSet<BA_Site>();
            this.BA_SiteTypeMetadataDefinitions = new HashSet<BA_SiteTypeMetadataDefinitions>();
            this.BA_Principal = new HashSet<BA_Principal>();
        }
    
        public int Id { get; set; }
        public string Name { get; set; }
        public int InstanceId { get; set; }
        public string SiteTemplatePath { get; set; }
        public int StartPhaseId { get; set; }
        public string MetadataFormDesign { get; set; }
        public bool ApprovalEnabled { get; set; }
        public bool AddDefaultAssociatedGroups { get; set; }
        public bool CreateDefaultAssociatedGroups { get; set; }
    
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<BA_Action> BA_Action { get; set; }
        public virtual BA_Instance BA_Instance { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<BA_Phase> BA_Phase { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<BA_Role> BA_Role { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<BA_Site> BA_Site { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<BA_SiteTypeMetadataDefinitions> BA_SiteTypeMetadataDefinitions { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<BA_Principal> BA_Principal { get; set; }
    }
}
