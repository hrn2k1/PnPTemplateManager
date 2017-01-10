using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PnPTemplateManager.Managers
{
   public class ReplaceVariablesStrategyProjectMetadata 
    {
        private static object GetMetadataValue(Guid webId, string metadataName)
        {
            try
            {
                MetadataManager metadataManager = new MetadataManager();
                var metadataList = metadataManager.GetMetadataForSharePointWebId(webId);
                var metadata = metadataList.FirstOrDefault(x => x.MetadataDefinition.Name == metadataName);
                return metadata.Value;
            }
            catch (Exception)
            {

                return null;
            }
        }

        public static string ReplaceVariables(Guid webId, string value)
        {
            string partVariable = "{ProjectMetadata.";
            string key = "";
            string webPropertyValue = "";

            string rtnVal = value;
            if (value.Contains(partVariable))
            {
                string betwVal = value.Substring(value.IndexOf(partVariable) + partVariable.Length);
                key = betwVal.Remove(betwVal.IndexOf("}"));
                webPropertyValue = GetMetadataValue(webId,key)?.ToString();
            }

            return rtnVal.Replace((partVariable + key + "}"), webPropertyValue);
        }
    }
}
