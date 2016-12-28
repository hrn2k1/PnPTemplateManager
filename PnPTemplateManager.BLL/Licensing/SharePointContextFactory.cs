using Microsoft.SharePoint.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using System.Web.Configuration;

namespace PnPTemplateManager.Licensing
{
    public static class SharePointContextFactory
    {
        public static ClientContext GetClientContext(SiteType siteType)
        {
            string site = "";
            switch (siteType)
            {
                case SiteType.ReleaseNote:
                    site = WebConfigurationManager.AppSettings["releasenoteSite"];
                    break;
                case SiteType.Documentation:
                    site = WebConfigurationManager.AppSettings["documentationSite"];
                    break;
                default:
                    break;
            }

            string password = WebConfigurationManager.AppSettings["password"];
            string username = WebConfigurationManager.AppSettings["siteusername"];

            var secure = new SecureString();
            foreach (char c in password)
            {
                secure.AppendChar(c);

            }
            var clientContext = new ClientContext(site);
            clientContext.Credentials = new SharePointOnlineCredentials(username, secure);
            clientContext.Load(clientContext.Web);
            clientContext.ExecuteQuery();

            return clientContext;
        }
    }

    public enum SiteType
    {
        ReleaseNote, Documentation
    }
}
