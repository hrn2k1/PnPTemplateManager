using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.XPath;
using Microsoft.SharePoint.Client;
using Microsoft.SharePoint.Client.Taxonomy;
using OfficeDevPnP.Core.Enums;
using OfficeDevPnP.Core.Framework.Provisioning.Model;
using OfficeDevPnP.Core.Framework.Provisioning.ObjectHandlers;
using OfficeDevPnP.Core.Framework.Provisioning.ObjectHandlers.TokenDefinitions;
using Field = Microsoft.SharePoint.Client.Field;
using PnPSiteField= OfficeDevPnP.Core.Framework.Provisioning.Model.Field;

namespace PnPTemplateManager.Managers
{
    public class SiteColumnProvisionManager
    {
        public TokenParser Provision(Web web, ProvisioningTemplate template, TokenParser parser,
            ProvisioningTemplateApplyingInformation applyingInformation)
        {
            var existingFields = web.Fields;

            web.Context.Load(existingFields, fs => fs.Include(f => f.Id));
            web.Context.ExecuteQueryRetry();
            var existingFieldIds = existingFields.AsEnumerable().Select(l => l.Id).ToList();
            var fields = template.SiteFields;

            foreach (var field in fields)
            {
                var templateFieldElement =
                    XElement.Parse(parser.ParseString(field.SchemaXml, "~sitecollection", "~site"));
                var fieldId = templateFieldElement.Attribute("ID").Value;

                if (!existingFieldIds.Contains(Guid.Parse(fieldId)))
                {
                    try
                    {
                        Debug.WriteLine("Adding field {0} to site columns", fieldId);
                        CreateField(web, templateFieldElement, parser, field.SchemaXml);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("Adding field {0} failed. Error: {1} {2}", fieldId, ex.Message, ex.StackTrace);
                        throw;
                    }
                }
                else
                    try
                    {
                        Debug.WriteLine("Updating field {0} to site columns", fieldId);
                        UpdateField(web, fieldId, templateFieldElement, parser, field.SchemaXml);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("Updating field {0} failed. Error: {1} {2}", fieldId, ex.Message, ex.StackTrace);
                        throw;
                    }
            }

            return parser;
        }

        private bool IsFieldXmlValid(string fieldXml, TokenParser parser, ClientRuntimeContext context)
        {
            var isValid = true;
            var leftOverTokens = parser.GetLeftOverTokens(fieldXml);
            if (!leftOverTokens.Any())
            {
                var fieldElement = XElement.Parse(fieldXml);
                if (fieldElement.Attribute("Type").Value == "TaxonomyFieldType")
                {
                    var termStoreIdElement =
                        fieldElement.XPathSelectElement("//ArrayOfProperty/Property[Name='SspId']/Value");
                    if (termStoreIdElement != null)
                    {
                        var termStoreId = Guid.Parse(termStoreIdElement.Value);
                        var taxSession = TaxonomySession.GetTaxonomySession(context);
                        try
                        {
                            taxSession.EnsureProperty(t => t.TermStores);
                            var store = taxSession.TermStores.GetById(termStoreId);
                            context.Load(store);
                            context.ExecuteQueryRetry();
                            if (store.ServerObjectIsNull.HasValue && !store.ServerObjectIsNull.Value)
                            {
                                var termSetIdElement =
                                    fieldElement.XPathSelectElement("//ArrayOfProperty/Property[Name='TermSetId']/Value");
                                if (termSetIdElement != null)
                                {
                                    var termSetId = Guid.Parse(termSetIdElement.Value);
                                    try
                                    {
                                        var termSet = store.GetTermSet(termSetId);
                                        context.Load(termSet);
                                        context.ExecuteQueryRetry();
                                        isValid = termSet.ServerObjectIsNull.HasValue &&
                                                  !termSet.ServerObjectIsNull.Value;
                                    }
                                    catch (Exception)
                                    {
                                        isValid = false;
                                    }
                                }
                            }
                        }
                        catch (Exception)
                        {
                            isValid = false;
                        }
                    }
                    else
                    {
                        isValid = false;
                    }
                }
            }
            else
            {
                //Some tokens where not replaced
                isValid = false;
            }
            return isValid;
        }

        private void UpdateField(Web web, string fieldId, XElement templateFieldElement, TokenParser parser,
            string originalFieldXml)
        {
            var existingField = web.Fields.GetById(Guid.Parse(fieldId));
            web.Context.Load(existingField, f => f.SchemaXml);
            web.Context.ExecuteQueryRetry();

            var existingFieldElement = XElement.Parse(existingField.SchemaXml);

            var equalityComparer = new XNodeEqualityComparer();

            if (equalityComparer.GetHashCode(existingFieldElement) != equalityComparer.GetHashCode(templateFieldElement))
            // Is field different in template?
            {
                if (existingFieldElement.Attribute("Type").Value == templateFieldElement.Attribute("Type").Value)
                // Is existing field of the same type?
                {
                    if (IsFieldXmlValid(parser.ParseString(originalFieldXml), parser, web.Context))
                    {
                        var listIdentifier = templateFieldElement.Attribute("List") != null
                            ? templateFieldElement.Attribute("List").Value
                            : null;

                        if (listIdentifier != null)
                        {
                            // Temporary remove list attribute from list
                            templateFieldElement.Attribute("List").Remove();
                        }

                        foreach (var attribute in templateFieldElement.Attributes())
                        {
                            if (existingFieldElement.Attribute(attribute.Name) != null)
                            {
                                existingFieldElement.Attribute(attribute.Name).Value = attribute.Value;
                            }
                            else
                            {
                                existingFieldElement.Add(attribute);
                            }
                        }
                        foreach (var element in templateFieldElement.Elements())
                        {
                            if (existingFieldElement.Element(element.Name) != null)
                            {
                                existingFieldElement.Element(element.Name).Remove();
                            }
                            existingFieldElement.Add(element);
                        }

                        if (existingFieldElement.Attribute("Version") != null)
                        {
                            existingFieldElement.Attributes("Version").Remove();
                        }
                        existingField.SchemaXml = parser.ParseString(existingFieldElement.ToString(), "~sitecollection",
                            "~site");
                        existingField.UpdateAndPushChanges(true);
                        web.Context.Load(existingField, f => f.TypeAsString, f => f.DefaultValue);
                        web.Context.ExecuteQueryRetry();

                        var isDirty = false;
#if !SP2013
                        if (originalFieldXml.ContainsResourceToken())
                        {
                            var originalFieldElement = XElement.Parse(originalFieldXml);
                            var nameAttributeValue = originalFieldElement.Attribute("DisplayName") != null
                                ? originalFieldElement.Attribute("DisplayName").Value
                                : "";
                            if (nameAttributeValue.ContainsResourceToken())
                            {
                                existingField.TitleResource.SetUserResourceValue(nameAttributeValue, parser);
                                isDirty = true;
                            }
                            var descriptionAttributeValue = originalFieldElement.Attribute("Description") != null
                                ? originalFieldElement.Attribute("Description").Value
                                : "";
                            if (descriptionAttributeValue.ContainsResourceToken())
                            {
                                existingField.DescriptionResource.SetUserResourceValue(descriptionAttributeValue, parser);
                                isDirty = true;
                            }
                        }
#endif
                        if (isDirty)
                        {
                            existingField.Update();
                            web.Context.ExecuteQueryRetry();
                        }
                        if ((existingField.TypeAsString == "TaxonomyFieldType" ||
                             existingField.TypeAsString == "TaxonomyFieldTypeMulti") &&
                            !string.IsNullOrEmpty(existingField.DefaultValue))
                        {
                            var taxField = web.Context.CastTo<TaxonomyField>(existingField);
                            ValidateTaxonomyFieldDefaultValue(taxField);
                        }
                    }
                    else
                    {
                        // The field Xml was found invalid
                        var tokenString = parser.GetLeftOverTokens(originalFieldXml)
                            .Aggregate(string.Empty, (acc, i) => acc + " " + i);
                        //scope.LogError("The field was found invalid: {0}", tokenString);
                        Debug.Fail("The field was found invalid: {0}", tokenString);
                        throw new Exception(string.Format("The field was found invalid: {0}", tokenString));
                    }
                }
                else
                {
                    var fieldName = existingFieldElement.Attribute("Name") != null
                        ? existingFieldElement.Attribute("Name").Value
                        : existingFieldElement.Attribute("StaticName").Value;
                    //WriteWarning(string.Format(CoreResources.Provisioning_ObjectHandlers_Fields_Field__0____1___exists_but_is_of_different_type__Skipping_field_, fieldName, fieldId), ProvisioningMessageType.Warning);

                    //scope.LogWarning(CoreResources.Provisioning_ObjectHandlers_Fields_Field__0____1___exists_but_is_of_different_type__Skipping_field_, fieldName, fieldId);
                    Debug.WriteLine("Adding field {0} {1} exists but is of different type and skipping the field",
                        fieldName, fieldId);
                }
            }
        }

        private string ParseFieldSchema(string schemaXml, ListCollection lists)
        {
            foreach (var list in lists)
            {
                schemaXml = Regex.Replace(schemaXml, list.Id.ToString(), string.Format("{{listid:{0}}}", list.Title),
                    RegexOptions.IgnoreCase);
            }

            return schemaXml;
        }

        private void CreateField(Web web, XElement templateFieldElement, TokenParser parser, string originalFieldXml)
        {
            var listIdentifier = templateFieldElement.Attribute("List") != null
                ? templateFieldElement.Attribute("List").Value
                : null;

            if (listIdentifier != null)
            {
                // Temporary remove list attribute from list
                templateFieldElement.Attribute("List").Remove();
            }

            var fieldXml = parser.ParseString(templateFieldElement.ToString(), "~sitecollection", "~site");

            if (IsFieldXmlValid(fieldXml, parser, web.Context))
            {
                var field = web.Fields.AddFieldAsXml(fieldXml, false, AddFieldOptions.AddFieldInternalNameHint);
                web.Context.Load(field, f => f.TypeAsString, f => f.DefaultValue, f => f.InternalName, f => f.Title);
                web.Context.ExecuteQueryRetry();

                // Add newly created field to token set, this allows to create a field + use it in a formula in the same provisioning template
                parser.AddToken(new FieldTitleToken(web, field.InternalName, field.Title));

                var isDirty = false;
#if !SP2013
                if (originalFieldXml.ContainsResourceToken())
                {
                    var originalFieldElement = XElement.Parse(originalFieldXml);
                    var nameAttributeValue = originalFieldElement.Attribute("DisplayName") != null
                        ? originalFieldElement.Attribute("DisplayName").Value
                        : "";
                    if (nameAttributeValue.ContainsResourceToken())
                    {
                        field.TitleResource.SetUserResourceValue(nameAttributeValue, parser);
                        isDirty = true;
                    }
                    var descriptionAttributeValue = originalFieldElement.Attribute("Description") != null
                        ? originalFieldElement.Attribute("Description").Value
                        : "";
                    if (descriptionAttributeValue.ContainsResourceToken())
                    {
                        field.DescriptionResource.SetUserResourceValue(descriptionAttributeValue, parser);
                        isDirty = true;
                    }
                }
#endif
                if (isDirty)
                {
                    field.Update();
                    web.Context.ExecuteQueryRetry();
                }

                if ((field.TypeAsString == "TaxonomyFieldType" || field.TypeAsString == "TaxonomyFieldTypeMulti") &&
                    !string.IsNullOrEmpty(field.DefaultValue))
                {
                    var taxField = web.Context.CastTo<TaxonomyField>(field);
                    ValidateTaxonomyFieldDefaultValue(taxField);
                }
            }
            else
            {
                // The field Xml was found invalid
                var tokenString = parser.GetLeftOverTokens(fieldXml).Aggregate(string.Empty, (acc, i) => acc + " " + i);
                // scope.LogError("The field was found invalid: {0}", tokenString);
                Debug.WriteLine("The field was found invalid: {0}", tokenString);
                throw new Exception(string.Format("The field was found invalid: {0}", tokenString));
            }
        }


        private void ValidateTaxonomyFieldDefaultValue(TaxonomyField field)
        {
            //get validated value with correct WssIds
            var validatedValue = GetTaxonomyFieldValidatedValue(field, field.DefaultValue);
            if (!string.IsNullOrEmpty(validatedValue) && field.DefaultValue != validatedValue)
            {
                field.DefaultValue = validatedValue;
                field.UpdateAndPushChanges(true);
                field.Context.ExecuteQueryRetry();
            }
        }

        private string GetTaxonomyFieldValidatedValue(TaxonomyField field, string defaultValue)
        {
            string res = null;
            object parsedValue = null;
            field.EnsureProperty(f => f.AllowMultipleValues);
            if (field.AllowMultipleValues)
            {
                parsedValue = new TaxonomyFieldValueCollection(field.Context, defaultValue, field);
            }
            else
            {
                TaxonomyFieldValue taxValue = null;
                if (TryParseTaxonomyFieldValue(defaultValue, out taxValue))
                {
                    parsedValue = taxValue;
                }
            }
            if (parsedValue != null)
            {
                var validateValue = field.GetValidatedString(parsedValue);
                field.Context.ExecuteQueryRetry();
                res = validateValue.Value;
            }
            return res;
        }

        private bool TryParseTaxonomyFieldValue(string value, out TaxonomyFieldValue taxValue)
        {
            var res = false;
            taxValue = new TaxonomyFieldValue();
            if (!string.IsNullOrEmpty(value))
            {
                var split = value.Split(new[] { ";#" }, StringSplitOptions.None);
                var wssId = 0;

                if (split.Length > 0 && int.TryParse(split[0], out wssId))
                {
                    taxValue.WssId = wssId;
                    res = true;
                }

                if (res && split.Length == 2)
                {
                    var term = split[1];
                    var splitTerm = term.Split(new[] { "|" }, StringSplitOptions.None);
                    var termId = Guid.Empty;
                    if (splitTerm.Length > 0)
                    {
                        res = Guid.TryParse(splitTerm[splitTerm.Length - 1], out termId);
                        taxValue.TermGuid = termId.ToString();
                        if (res && splitTerm.Length > 1)
                        {
                            taxValue.Label = splitTerm[0];
                        }
                    }
                    else
                    {
                        res = false;
                    }
                    res = true;
                }
                else if (split.Length == 1 && int.TryParse(value, out wssId))
                {
                    taxValue.WssId = wssId;
                    res = true;
                }
            }
            return res;
        }

        private string TokenizeTaxonomyField(Web web, XElement element)
        {
            // Replace Taxonomy field references to SspId, TermSetId with tokens
            var session = TaxonomySession.GetTaxonomySession(web.Context);
            var store = session.GetDefaultSiteCollectionTermStore();

            var sspIdElement =
                element.XPathSelectElement("./Customization/ArrayOfProperty/Property[Name = 'SspId']/Value");
            if (sspIdElement != null)
            {
                sspIdElement.Value = "{sitecollectiontermstoreid}";
            }
            var termSetIdElement =
                element.XPathSelectElement("./Customization/ArrayOfProperty/Property[Name = 'TermSetId']/Value");
            if (termSetIdElement != null)
            {
                var termSetId = Guid.Parse(termSetIdElement.Value);
                if (termSetId != Guid.Empty)
                {
                    var termSet = store.GetTermSet(termSetId);
                    store.Context.ExecuteQueryRetry();

                    if (!termSet.ServerObjectIsNull())
                    {
                        termSet.EnsureProperties(ts => ts.Name, ts => ts.Group);

                        termSetIdElement.Value = string.Format("{{termsetid:{0}:{1}}}",
                            termSet.Group.IsSiteCollectionGroup ? "{sitecollectiontermgroupname}" : termSet.Group.Name,
                            termSet.Name);
                    }
                }
            }

            return element.ToString();
        }

        private string TokenizeFieldFormula(string fieldXml)
        {
            var schemaElement = XElement.Parse(fieldXml);
            var formula = schemaElement.Descendants("Formula").FirstOrDefault();
            var processedFields = new List<string>();
            if (formula != null)
            {
                var formulaString = formula.Value;
                if (formulaString != null)
                {
                    var fieldRefs = schemaElement.Descendants("FieldRef");
                    foreach (var fieldRef in fieldRefs)
                    {
                        var fieldInternalName = fieldRef.Attribute("Name").Value;
                        if (!processedFields.Contains(fieldInternalName))
                        {
                            formulaString = formulaString.Replace(fieldInternalName,
                                $"[{{fieldtitle:{fieldInternalName}}}]");
                            processedFields.Add(fieldInternalName);
                        }
                    }
                    var fieldRefParent = schemaElement.Descendants("FieldRefs");
                    fieldRefParent.Remove();
                }
                formula.Value = formulaString;
            }

            return schemaElement.ToString();
        }

        public  bool PersistResourceValue(UserResource userResource, string token, ProvisioningTemplate template,
            ProvisioningTemplateCreationInformation creationInfo)
        {
            var returnValue = false;
            foreach (var language in template.SupportedUILanguages)
            {
                var culture = new CultureInfo(language.LCID);

                var value = userResource.GetValueForUICulture(culture.Name);
                userResource.Context.ExecuteQueryRetry();
                if (!string.IsNullOrEmpty(value.Value))
                {
                    returnValue = true;
                    //ResourceTokens.Add(new Tuple<string, int, string>(token, language.LCID, value.Value));
                }
            }

            return returnValue;
        }

        public ProvisioningTemplate Extract(Web web,
            ProvisioningTemplateCreationInformation creationInfo)
        {
            ProvisioningTemplate template = new ProvisioningTemplate();
            var existingFields = web.Fields;
            web.Context.Load(web, w => w.ServerRelativeUrl);
            web.Context.Load(existingFields,
                fs => fs.Include(f => f.Id, f => f.SchemaXml, f => f.TypeAsString, f => f.InternalName, f => f.Title));
            web.Context.Load(web.Lists, ls => ls.Include(l => l.Id, l => l.Title));
            web.Context.ExecuteQueryRetry();

            var taxTextFieldsToMoveUp = new List<Guid>();

            foreach (var field in existingFields)
            {
                if (!BuiltInFieldId.Contains(field.Id))
                {
                    var fieldXml = field.SchemaXml;
                    var element = XElement.Parse(fieldXml);

                    // Check if the field contains a reference to a list. If by Guid, rewrite the value of the attribute to use web relative paths
                    var listIdentifier = element.Attribute("List") != null ? element.Attribute("List").Value : null;
                    if (!string.IsNullOrEmpty(listIdentifier))
                    {
                        var listGuid = Guid.Empty;
                        fieldXml = ParseFieldSchema(fieldXml, web.Lists);
                        element = XElement.Parse(fieldXml);
                        
                    }

                    // Check if the field is of type TaxonomyField
                    if (field.TypeAsString.StartsWith("TaxonomyField"))
                    {
                        var taxField = (TaxonomyField)field;
                        web.Context.Load(taxField, tf => tf.TextField, tf => tf.Id);
                        web.Context.ExecuteQueryRetry();
                        taxTextFieldsToMoveUp.Add(taxField.TextField);

                        fieldXml = TokenizeTaxonomyField(web, element);
                    }

                    // Check if we have version attribute. Remove if exists 
                    if (element.Attribute("Version") != null)
                    {
                        element.Attributes("Version").Remove();
                        fieldXml = element.ToString();
                    }
                    if (element.Attribute("Type").Value == "Calculated")
                    {
                        fieldXml = TokenizeFieldFormula(fieldXml);
                    }
                    if (creationInfo.PersistMultiLanguageResources)
                    {
#if !SP2013
                        // only persist language values for fields we actually will keep...no point in spending time on this is we clean the field afterwards
                        var persistLanguages = true;
                        if (creationInfo.BaseTemplate != null)
                        {
                            var index =
                                creationInfo.BaseTemplate.SiteFields.FindIndex(
                                    f => Guid.Parse(XElement.Parse(f.SchemaXml).Attribute("ID").Value).Equals(field.Id));

                            if (index > -1)
                            {
                                persistLanguages = false;
                            }
                        }

                        if (persistLanguages)
                        {
                            var fieldElement = XElement.Parse(fieldXml);
                            if (PersistResourceValue(field.TitleResource,
                                string.Format("Field_{0}_DisplayName", field.Title.Replace(" ", "_")), template,
                                creationInfo))
                            {
                                var fieldTitle = string.Format("{{res:Field_{0}_DisplayName}}",
                                    field.Title.Replace(" ", "_"));
                                fieldElement.SetAttributeValue("DisplayName", fieldTitle);
                            }
                            if (PersistResourceValue(field.DescriptionResource,
                                string.Format("Field_{0}_Description", field.Title.Replace(" ", "_")), template,
                                creationInfo))
                            {
                                var fieldDescription = string.Format("{{res:Field_{0}_Description}}",
                                    field.Title.Replace(" ", "_"));
                                fieldElement.SetAttributeValue("Description", fieldDescription);
                            }

                            fieldXml = fieldElement.ToString();
                        }
#endif
                    }

                    template.SiteFields.Add(new PnPSiteField { SchemaXml = fieldXml });
                }
            }
            // move hidden taxonomy text fields to the top of the list
            foreach (var textFieldId in taxTextFieldsToMoveUp)
            {
                var field =
                    template.SiteFields.First(
                        f => Guid.Parse(f.SchemaXml.ElementAttributeValue("ID")).Equals(textFieldId));
                template.SiteFields.RemoveAll(
                    f => Guid.Parse(f.SchemaXml.ElementAttributeValue("ID")).Equals(textFieldId));
                template.SiteFields.Insert(0, field);
            }
            // If a base template is specified then use that one to "cleanup" the generated template model
            if (creationInfo.BaseTemplate != null)
            {
                template = CleanupEntities(template, creationInfo.BaseTemplate);
            }

            return template;
        }

        private ProvisioningTemplate CleanupEntities(ProvisioningTemplate template, ProvisioningTemplate baseTemplate)
        {
            foreach (var field in baseTemplate.SiteFields)
            {
                var xDoc = XDocument.Parse(field.SchemaXml);
                var id = xDoc.Root.Attribute("ID") != null ? xDoc.Root.Attribute("ID").Value : null;
                if (id != null)
                {
                    var index =
                        template.SiteFields.FindIndex(
                            f => Guid.Parse(XElement.Parse(f.SchemaXml).Attribute("ID").Value).Equals(Guid.Parse(id)));

                    if (index > -1)
                    {
                        template.SiteFields.RemoveAt(index);
                    }
                }
            }

            return template;
        }
    }

    public static class XElementStringExtensions
    {
        public static string ElementAttributeValue(this string input, string attribute)
        {
            var element = XElement.Parse(input);
            return element.Attribute(attribute) != null ? element.Attribute(attribute).Value : null;
        }

        public static bool ContainsResourceToken(this string value)
        {
            if (value != null)
            {
                return Regex.IsMatch(value, "\\{(res|loc|resource|localize|localization):(.*?)(\\})",
                    RegexOptions.IgnoreCase);
            }
            return false;
        }

        public static bool SetUserResourceValue(this UserResource userResource, string tokenValue, TokenParser parser)
        {
            var isDirty = false;

            if (userResource != null && !string.IsNullOrEmpty(tokenValue))
            {
                var resourceValues = parser.GetResourceTokenResourceValues(tokenValue);
                foreach (var resourceValue in resourceValues)
                {
                    userResource.SetValueForUICulture(resourceValue.Item1, resourceValue.Item2);
                    isDirty = true;
                }
            }

            return isDirty;
        }
    }

    public class FieldTitleToken : TokenDefinition
    {
        private readonly string _value;

        public FieldTitleToken(Web web, string InternalName, string Title)
            : base(web, string.Format("{{fieldtitle:{0}}}", InternalName))
        {
            _value = Title;
        }

        public override string GetReplaceValue()
        {
            if (string.IsNullOrEmpty(CacheValue))
            {
                CacheValue = _value;
            }
            return CacheValue;
        }
    }
}
