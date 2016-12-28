using PnPTemplateManager.Managers.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Configuration;

namespace PnPTemplateManager.Managers.Implementations
{
    public class AppSettingsManager : IAppSettingsManager
    {
        public string GetAppSetting(string key)
        {
            // App settings can be targeted in 4 different ways (the first two is only for debug mode):
            // first it will search for a key like "BSC-PC-DEVELOPMENT-AZURE-AppUrl" (if the machinename is BSC-PC, the Branch is Dev and the configuration is sat to "debug azure", and the key is AppUrl)
            // Next it will search for a key like "BSC-PC-DEVELOPMENT-AppUrl" (if the machinename is BSC-PC, the branch is dev and the key is AppUrl)
            // third it will search for a key like "BSC-PC-AppUrl" (if the machinename is BSC-PC and the key is AppUrl)
            // Lastly it will just search for the key
#if DEBUG
            if (!string.IsNullOrEmpty(System.Web.Hosting.HostingEnvironment.ApplicationPhysicalPath))
            {
                // Branch is determined by looking the at the directory path. It must container either Development or Master
                var Branch =
                    System.Web.Hosting.HostingEnvironment.ApplicationPhysicalPath.IndexOf("Development", StringComparison.CurrentCultureIgnoreCase) > 0
                        ? "DEVELOPMENT"
                        : System.Web.Hosting.HostingEnvironment.ApplicationPhysicalPath.IndexOf("Master", StringComparison.CurrentCultureIgnoreCase) > 0
                            ? "MASTER"
                            : "";
                string Env = string.Empty;
#if AZURE
                Env = "AZURE";
#elif ONPREM
                Env = "ONPREM";
#endif
                if (WebConfigurationManager.AppSettings.AllKeys.Contains($"{Environment.MachineName}-{Env}-{Branch}-{key}"))
                    return WebConfigurationManager.AppSettings[$"{Environment.MachineName}-{Env}-{Branch}-{key}"];
                if (WebConfigurationManager.AppSettings.AllKeys.Contains($"{Environment.MachineName}-{Env}-{key}"))
                    return WebConfigurationManager.AppSettings[$"{Environment.MachineName}-{Env}-{key}"];
            }
#endif


            if (WebConfigurationManager.AppSettings.AllKeys.Contains(Environment.MachineName + "-" + key))
                return WebConfigurationManager.AppSettings[Environment.MachineName + "-" + key];
            return WebConfigurationManager.AppSettings[key];
        }

        public bool IsAppSetting(string key)
        {
            return WebConfigurationManager.AppSettings.AllKeys.Contains(key);
        }
        public string GetConnectionString(string key)
        {
            // see GetAppSetting, for info about how the key is resolved
#if DEBUG
            // Branch is determined by looking the at the directory path. It must container either Development or Master
            var Branch =
                System.Web.Hosting.HostingEnvironment.ApplicationPhysicalPath.IndexOf("Development", StringComparison.CurrentCultureIgnoreCase) > 0
                    ? "DEVELOPMENT"
                    : System.Web.Hosting.HostingEnvironment.ApplicationPhysicalPath.IndexOf("Master", StringComparison.CurrentCultureIgnoreCase) > 0
                        ? "MASTER"
                        : "";
            string Env = string.Empty;
#if AZURE
                Env = "AZURE";
#elif ONPREM
            Env = "ONPREM";
#endif
            if (WebConfigurationManager.ConnectionStrings[$"{Environment.MachineName}-{Env}-{Branch}-{key}"] != null)
                return WebConfigurationManager.ConnectionStrings[$"{Environment.MachineName}-{Env}-{Branch}-{key}"].ConnectionString;
            if (WebConfigurationManager.ConnectionStrings[$"{Environment.MachineName}-{Env}-{key}"] != null)
                return WebConfigurationManager.ConnectionStrings[$"{Environment.MachineName}-{Env}-{key}"].ConnectionString;
#endif
            return WebConfigurationManager.ConnectionStrings[Environment.MachineName + "-" + key]?.ConnectionString ??
                   WebConfigurationManager.ConnectionStrings[key].ConnectionString;
        }
        public bool IsConnectionString(string key)
        {
            return WebConfigurationManager.ConnectionStrings[key] != null;
        }

        public string AppUrl
        {
            get { return GetAppSetting("AppUrl"); }
        }

        public string BlobUrl
        {
            get { return GetAppSetting("BlobUrl"); }
        }

        public string ClientId
        {
            get { return GetAppSetting("ClientId"); }
        }
    }
}
