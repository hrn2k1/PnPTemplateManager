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
    
    public partial class BA_SiteTypeMetadataDefinitions
    {
        public int BA_SiteTypeId { get; set; }
        public int BA_MetadataDefinitionId { get; set; }
        public int SortOrder { get; set; }
    
        public virtual BA_MetadataDefinition BA_MetadataDefinition { get; set; }
        public virtual BA_SiteType BA_SiteType { get; set; }
    }
}