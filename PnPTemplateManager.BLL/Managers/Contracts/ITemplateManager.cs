using PnPTemplateManager.Models;
using System.Collections.Generic;

namespace PnPTemplateManager.Managers.Contracts
{
    public interface ITemplateManager
    {
        List<PnPFileInfo> GetPnPTemplates();
        PnPFileInfo GetPnPTemplateFileFromSite(CreatePnPTemplateRequest request);
        string ApplyPnPTemplateOnSite(ApplyPnPTemplateRequest request);
        string DeletePnPPackageFile(string pnpPackageName);
        string SendPnPPackageFile(string fileName);
        //string CreateSiteAndApplyPnPTemplate(ApplyPnPTemplateRequest request);
        void SavePnPPackage();

    }
}
