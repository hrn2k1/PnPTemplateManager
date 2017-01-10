using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PnPTemplateManager.BLL;

namespace PnPTemplateManager.Managers
{
    
    public class MetadataManager
    {
        public IEnumerable<MetadataDto> GetMetadataForSharePointWebId(Guid sharePointWebId/*, out string siteName, out int siteTypeId, out SiteStatus status*/)
        {


            Wizdom365X2Entities db = new Wizdom365X2Entities();
            //siteName = "";
            //siteTypeId = 0;
            //status = SiteStatus.Error;
            List<MetadataDefinitionDto> metadataSchema = new List<MetadataDefinitionDto>();
            List<MetadataDto> result = new List<MetadataDto>();

            var site = db.BA_Site.FirstOrDefault(x => x.SharePointWebId == sharePointWebId);//unitOfWork.GetRepository<BA_Site>().Query().FirstOrDefault(x => x.SharePointWebId == sharePointWebId);
            if (site != null)
            {
                metadataSchema = GetMetadataDesignForm(site.SiteType_Id).ToList();
                //siteName = site.Name;
               // siteTypeId = site.SiteType_Id;
               // status = (SiteStatus)site.Status;
            }
            List<BA_Metadata> metadataList = db.BA_Metadata.Where(x => x.BA_Site.SharePointWebId == sharePointWebId).ToList(); //unitOfWork.GetRepository<BA_Metadata>().Query(x => x.MetadataDefinition).Where(x => x.Site.SharePointWebId == sharePointWebId).ToList();

            foreach (var metdef in metadataSchema)
            {
                var metadata = metadataList.FirstOrDefault(x => x.MetadataDefinition_Id == metdef.Id);
                if (metadata != null)
                {
                    result.Add(new MetadataDto
                    {
                        Id = metadata.Id,
                        MetadataDefinition = new MetadataDefinitionDto
                        {
                            HideOnNewForm = metadata.BA_MetadataDefinition.HideOnNewForm,
                            Name = metadata.BA_MetadataDefinition.Name,
                            Required = metadata.BA_MetadataDefinition.Required,
                            Type = metadata.BA_MetadataDefinition.Type,
                            Definition = metadata.BA_MetadataDefinition.Definition,
                            HideOnDisplayForm = metadata.BA_MetadataDefinition.HideOnDisplayForm,
                            HideOnEditForm = metadata.BA_MetadataDefinition.HideOnEditForm,
                            Id = metadata.BA_MetadataDefinition.Id
                        }, //Mapper.Map<MetadataDefinitionDto>(metadata.BA_MetadataDefinition),
                        Value = metadata.ValueDate.HasValue ? metadata.ValueDate.Value.UtcDateTime.ToString("s") :
                                (metadata.ValueDecimal.HasValue ? metadata.ValueDecimal.Value :
                                (object)metadata.Value)
                    });
                }
            }

            return result;
        }
        private IEnumerable<MetadataDefinitionDto> GetMetadataDesignForm(int sitetypeId)
        {
            Wizdom365X2Entities db = new Wizdom365X2Entities();
            var sitetype = db.BA_SiteType.Find(sitetypeId); //this.unitOfWork.GetRepository<BA_SiteType>().GetByKey(sitetypeId);
            if (sitetype == null)
                return new List<MetadataDefinitionDto>();

            string metadataDesign = sitetype.MetadataFormDesign;
            List<MetadataDefinitionDto> sitetypeMetadataList = GetMetadata(sitetypeId).ToList();
            if (!String.IsNullOrEmpty(metadataDesign))
            {
                List<MetadataDefinitionDto> metadataDesignFormList = JsonConvert.DeserializeObject<List<MetadataDefinitionDto>>(metadataDesign);

                for (int i = 0; i < metadataDesignFormList.Count; i++)
                {
                    if (!sitetypeMetadataList.Any(p => p.Id == metadataDesignFormList[i].Id))
                        metadataDesignFormList.Remove(metadataDesignFormList[i]);
                    else
                        metadataDesignFormList[i] = sitetypeMetadataList.Find(p => p.Id == metadataDesignFormList[i].Id);
                }
                for (int i = 0; i < sitetypeMetadataList.Count; i++)
                {
                    if (!metadataDesignFormList.Any(p => p.Id == sitetypeMetadataList[i].Id))
                        metadataDesignFormList.Add(sitetypeMetadataList[i]);
                }


                metadataDesign = JsonConvert.SerializeObject(metadataDesignFormList);
                sitetype.MetadataFormDesign = metadataDesign;
                //unitOfWork.SaveChanges();

                return metadataDesignFormList;
            }
            else
            {
                return sitetypeMetadataList;
            }


        }
        private IEnumerable<MetadataDefinitionDto> GetMetadata(int sitetypeId)
        {
            Wizdom365X2Entities db = new Wizdom365X2Entities();
            var result = new List<MetadataDefinitionDto>();
            var sitetype = db.BA_SiteType.Find(sitetypeId);//this.unitOfWork.GetRepository<BA_SiteType>().GetByKey(sitetypeId);
            if (sitetype == null)
                return result;

            var associations = sitetype.BA_SiteTypeMetadataDefinitions.OrderBy(x => x.SortOrder).ToList();
            foreach (var association in associations)
            {
                var sitetypeMetadataDefinition = association.BA_MetadataDefinition;
                var sitetypeMetadataDefinitionDto = new MetadataDefinitionDto
                {
                    HideOnDisplayForm = sitetypeMetadataDefinition.HideOnDisplayForm,
                    Definition = sitetypeMetadataDefinition.Definition,
                    HideOnEditForm = sitetypeMetadataDefinition.HideOnEditForm,
                    HideOnNewForm = sitetypeMetadataDefinition.HideOnNewForm,
                    Id = sitetypeMetadataDefinition.Id,
                    Name = sitetypeMetadataDefinition.Name,
                    Required = sitetypeMetadataDefinition.Required,
                    Type = sitetypeMetadataDefinition.Type

                };
                sitetypeMetadataDefinitionDto.NumberOfUsages = sitetypeMetadataDefinition.BA_Metadata.Any() ? sitetypeMetadataDefinition.BA_Metadata.Count : 0;
                sitetypeMetadataDefinitionDto.IsSitetypeMetadata = sitetypeMetadataDefinition.Instance_Id == null;
                sitetypeMetadataDefinitionDto.SortOrder = association.SortOrder;
                result.Add(sitetypeMetadataDefinitionDto);
            }

            return result;
        }
    }
    public enum SiteStatus : int
    {
        Queued = 0,
        Approved = 1,
        Rejected = 2,
        Creating = 3,
        Ready = 4,
        Error = 5,
        Disabled = 6
    }
    //[DataContract]
    public class MetadataDto
    {
        //[DataMember]
        public int Id { get; set; }
        //[DataMember]
        public object Value { get; set; }
        //[DataMember]
        public MetadataDefinitionDto MetadataDefinition { get; set; }
    }
    //[DataContract]
    public class MetadataDefinitionDto
    {
        // [DataMember]
        public int Id { get; set; }
        // [DataMember]
        public string Name { get; set; }
        //[DataMember]
        [JsonConverter(typeof(StringToJsonConverter))]
        public string Definition { get; set; }
        //[DataMember]
        public string Type { get; set; }
        //[DataMember]
        public bool Required { get; set; }
        // [DataMember]
        public int NumberOfUsages { get; set; }
        // [DataMember]
        public bool IsSitetypeMetadata { get; set; }
        // [DataMember]
        public int SortOrder { get; set; }

        //[DataMember]
        public bool HideOnDisplayForm { get; set; }
        //[DataMember]
        public bool HideOnNewForm { get; set; }
        //[DataMember]
        public bool HideOnEditForm { get; set; }

    }
    public class StringToJsonConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            throw new NotImplementedException();
        }


        public override object ReadJson(Newtonsoft.Json.JsonReader reader, System.Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
                return null;

            var obj = JObject.Load(reader);
            return obj.ToString(Formatting.None);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            writer.WriteRawValue((string)value);
        }
    }
    
}
