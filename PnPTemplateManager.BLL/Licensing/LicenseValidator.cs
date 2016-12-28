using Microsoft.SharePoint.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PnPTemplateManager.Licensing
{
    public static class LicenseValidator
    {
        internal static bool ValidateLicense(string company, string md5Hash)
        {
            try
            {
                ClientContext clientContext = SharePointContextFactory.GetClientContext(SiteType.Documentation);

                var list = clientContext.Web.Lists.GetByTitle("Wizdom Licenses");
                clientContext.Load(list);
                clientContext.ExecuteQuery();
                var query = new CamlQuery();
                query.ViewXml = String.Format("<View>"
                                            + "<Query>"
                                            + "<Where>"
                                            + "<And>"
                                            + "<Eq><FieldRef Name='Company_Name' /><Value Type='Text'>{0}</Value></Eq>"
                                            + "<Eq><FieldRef Name='Hash_key' /><Value Type='Text'>{1}</Value></Eq>"
                                             + "</And>"
                                            + "</Where>"
                                             + "</Query>"
                                            + "</View>", company, md5Hash);
                var listEnum = list.GetItems(query);
                clientContext.Load(listEnum);
                clientContext.ExecuteQuery();

                if (listEnum.Count > 0)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception e)
            {
                return false;
            }


        }
    }
}
