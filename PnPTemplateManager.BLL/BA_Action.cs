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
    
    public partial class BA_Action
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public BA_Action()
        {
            this.BA_Phase = new HashSet<BA_Phase>();
        }
    
        public int Id { get; set; }
        public int SiteType_Id { get; set; }
    
        public virtual BA_SiteType BA_SiteType { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<BA_Phase> BA_Phase { get; set; }
    }
}
