using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PnPTemplateManager.Managers.Contracts
{
    public interface IAppSettingsManager
    {
        string AppUrl { get; }
        string BlobUrl { get; }
        string ClientId { get; }
        string GetAppSetting(string key);
        bool IsAppSetting(string key);
        string GetConnectionString(string key);
        bool IsConnectionString(string key);
    }
}
