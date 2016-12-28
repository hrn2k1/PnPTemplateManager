using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PnPTemplateManager.Managers;
using PnPTemplateManager.Managers.Contracts;
using PnPTemplateManager.Managers.Implementations;
using System.Reflection;
using System.Web.SessionState;
using System.Configuration;
using System.Web.Http.Tracing;
using System.Web;

namespace PnPTemplateManager.Web
{
    public class IocConfig
    {
        public static void Register()
        {
            Ioc.RegisterType<IAppSettingsManager,AppSettingsManager>();
            Ioc.RegisterType<IFileStorageManager, FilebasedStorageManager>();
            Ioc.RegisterType<ITemplateManager, TemplateManager>();

            // special registrations
            // Session provider. Default is inProc, which is an internal class inside System.Web >.<
            Type inProcSessionStateStoreType = Assembly.GetAssembly(typeof(SessionStateModule))
                .CreateInstance("System.Web.SessionState.InProcSessionStateStore")
                .GetType();
            Ioc.RegisterType(typeof(SessionStateStoreProviderBase), inProcSessionStateStoreType);

            // IOC Registrations from web.config
            ConfigurationManager.AppSettings.AllKeys
                .Where(
                    k =>
                        k.StartsWith("IOC:", StringComparison.InvariantCultureIgnoreCase) ||
                        k.StartsWith(Environment.MachineName + "-IOC:"))
                .ToList()
                .ForEach(key =>
                {
                    if (key.StartsWith("IOC:") &&
                        // if theres a machine specific registration for this key, skip this registration, to allow the machine specific registration to be used instead.
                        ConfigurationManager.AppSettings.AllKeys.Any(
                            k2 => k2.Equals(Environment.MachineName + "-" + key)))
                        return;

                    string interfaceName = key.Split(':')[1];
                    string implementationName = ConfigurationManager.AppSettings[key];
                    Type interfaceType = null;

                    if (interfaceName.Equals("System.Web.SessionState.SessionStateStoreProviderBase"))
                        // speciel case for SessionStateStoreProviderBase
                        interfaceType = typeof(SessionStateStoreProviderBase);
                    else if (interfaceName.Equals("System.Web.Http.Tracing.ITraceWriter"))
                        // speciel case for System.Web.Http.Tracing.ITraceWriter
                        interfaceType = typeof(ITraceWriter);
                    else
                        interfaceType = Type.GetType(interfaceName);

                    Type implementationType = Type.GetType(implementationName);
                    if (interfaceType == null)
                        throw new ConfigurationErrorsException("Wizdom IOC could not find interface " + interfaceName);
                    if (implementationType == null)
                        throw new ConfigurationErrorsException("Wizdom IOC could not find implementation " +
                                                               implementationName);
                    Ioc.RegisterType(interfaceType, implementationType);
                });

            

        }
    }
}
