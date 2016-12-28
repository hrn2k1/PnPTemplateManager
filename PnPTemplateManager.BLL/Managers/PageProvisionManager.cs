using Microsoft.SharePoint.Client;
using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.SharePoint.Client.WebParts;
using OfficeDevPnP.Core;
using OfficeDevPnP.Core.Framework.Provisioning.Connectors;
using OfficeDevPnP.Core.Framework.Provisioning.ObjectHandlers;
using OfficeDevPnP.Core.Framework.Provisioning.Providers.Xml;
using OfficeDevPnP.Core.Framework.Provisioning.Model;
using OfficeDevPnP.Core.Utilities;
using System.Web;
using System.Web.Configuration;
using FileLevel = Microsoft.SharePoint.Client.FileLevel;

namespace PnPTemplateManager.Managers
{
    public class PageProvisionManager
    {
        public ProvisioningTemplate Extract(ClientContext sourceSiteContext, ProvisioningTemplateCreationInformation ptci)
        {
            ProvisioningTemplate template = new ProvisioningTemplate();
            try
            {
                Web srcWeb = sourceSiteContext.Web;

                template = new ProvisioningTemplate();
                var srcPageLib = srcWeb.GetPagesLibrary();
                var srcItems = srcPageLib.GetItems(new CamlQuery());
                sourceSiteContext.Load(srcItems, itms => itms.Include(itm => itm.File, itm => itm.Folder, itm => itm.DisplayName, itm => itm.ContentType));
                sourceSiteContext.ExecuteQuery();
                var pages = new System.Collections.Generic.List<string>();

                foreach (var srcItem in srcItems)
                {
                    if (srcItem.ContentType.Name.ToLower().Contains("page"))
                    {
                        var srcFile = srcItem.File;
                        if (srcFile.Name.EndsWith(".aspx", StringComparison.InvariantCultureIgnoreCase))
                        {
                            pages.Add($"{srcPageLib.Title}/{srcFile.Name}");
                        }
                    }
                }
                int pageNo = 1;
                foreach (var page in pages)
                {

                    try
                    {
                        GetPage(srcWeb, template, ptci, page);
                        Console.WriteLine("{0}/{1}: Page '{2}' is extracted successfully", pageNo, pages.Count, page);
                        pageNo++;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("{0}/{1}: Page '{2}' is failed to extract. error: {3}", pageNo, pages.Count, page, ex.Message);
                    }
                }

            }
            catch (Exception ex)
            {
                throw ex;
            }
            return template;
        }
        private bool CheckOutIfNeeded(Web web, Microsoft.SharePoint.Client.File targetFile)
        {
            var checkedOut = false;
            try
            {
                if (targetFile.ListItemAllFields.ServerObjectIsNull.HasValue
                    && !targetFile.ListItemAllFields.ServerObjectIsNull.Value
                    && targetFile.ListItemAllFields.ParentList.ForceCheckout)
                {
                    if (targetFile.CheckOutType == CheckOutType.None)
                    {
                        targetFile.CheckOut();
                    }
                    checkedOut = true;
                }
            }
            catch (ServerException ex)
            {
                // Handling the exception stating the "The object specified does not belong to a list."
                if (ex.ServerErrorCode != -2146232832)
                {
                    throw;
                }
            }
            return checkedOut;
        }
        private void GetPage(Web web, ProvisioningTemplate template, ProvisioningTemplateCreationInformation creationInfo, string pageUrl)
        {
            try
            {
                var pageFullUrl = UrlUtility.Combine(web.ServerRelativeUrl, pageUrl);

                var file = web.GetFileByServerRelativeUrl(pageFullUrl);
                web.Context.Load(file,f=>f.Name, f => f.CheckOutType, f => f.ListItemAllFields.ParentList.ForceCheckout, f => f.Level);
                web.Context.ExecuteQueryRetry();
                FileLevel fileLevel= file.Level;
                var checkedOut = CheckOutIfNeeded(web, file);
                try
                {
                    var listItem = file.EnsureProperty(f => f.ListItemAllFields);
                    if (listItem != null)
                    {
                        if (listItem.FieldValues.ContainsKey("WikiField"))
                        {
                            #region Wiki page

                            var fullUri = new Uri(UrlUtility.Combine(web.Url, web.RootFolder.WelcomePage));

                            var folderPath =
                                fullUri.Segments.Take(fullUri.Segments.Count() - 1)
                                    .ToArray()
                                    .Aggregate((i, x) => i + x)
                                    .TrimEnd('/');
                            var fileName = fullUri.Segments[fullUri.Segments.Count() - 1];

                            var homeFile = web.GetFileByServerRelativeUrl(pageFullUrl);

                            LimitedWebPartManager limitedWPManager =
                                homeFile.GetLimitedWebPartManager(PersonalizationScope.Shared);

                            web.Context.Load(limitedWPManager);

                            var webParts = web.GetWebParts(pageFullUrl);

                            var page = new Page()
                            {
                                Layout = WikiPageLayout.Custom,
                                Overwrite = true,
                                Url = Tokenize(fullUri.PathAndQuery, web.Url),
                            };
                            var pageContents = listItem.FieldValues["WikiField"].ToString();

                            Regex regexClientIds = new Regex(@"id=\""div_(?<ControlId>(\w|\-)+)");
                            if (regexClientIds.IsMatch(pageContents))
                            {
                                foreach (Match webPartMatch in regexClientIds.Matches(pageContents))
                                {
                                    String serverSideControlId = webPartMatch.Groups["ControlId"].Value;

                                    try
                                    {
                                        String serverSideControlIdToSearchFor = String.Format("g_{0}",
                                            serverSideControlId.Replace("-", "_"));

                                        WebPartDefinition webPart =
                                            limitedWPManager.WebParts.GetByControlId(serverSideControlIdToSearchFor);
                                        web.Context.Load(webPart,
                                            wp => wp.Id,
                                            wp => wp.WebPart.Title,
                                            wp => wp.WebPart.ZoneIndex
                                            );
                                        web.Context.ExecuteQueryRetry();

                                        var webPartxml = TokenizeWebPartXml(web,
                                            web.GetWebPartXml(webPart.Id, pageFullUrl));

                                        page.WebParts.Add(new OfficeDevPnP.Core.Framework.Provisioning.Model.WebPart()
                                        {
                                            Title = webPart.WebPart.Title,
                                            Contents = webPartxml,
                                            Order = (uint)webPart.WebPart.ZoneIndex,
                                            Row = 1,
                                            // By default we will create a onecolumn layout, add the webpart to it, and later replace the wikifield on the page to position the webparts correctly.
                                            Column = 1
                                            // By default we will create a onecolumn layout, add the webpart to it, and later replace the wikifield on the page to position the webparts correctly.
                                        });

                                        pageContents = Regex.Replace(pageContents, serverSideControlId,
                                            string.Format("{{webpartid:{0}}}", webPart.WebPart.Title),
                                            RegexOptions.IgnoreCase);
                                    }
                                    catch (ServerException)
                                    {
                                        Console.WriteLine(
                                            "Found a WebPart ID which is not available on the server-side. ID: {0}",
                                            serverSideControlId);
                                    }
                                }
                            }

                            page.Fields.Add("WikiField", pageContents);
                            template.Pages.Add(page);

                            // Set the homepage
                            if (template.WebSettings == null)
                            {
                                template.WebSettings = new WebSettings();
                            }
                            //template.WebSettings.WelcomePage = homepageUrl;

                            #endregion
                        }
                        else
                        {
                            if (web.Context.HasMinimalServerLibraryVersion(Constants.MINIMUMZONEIDREQUIREDSERVERVERSION))
                            {
                                // Not a wikipage
                                template = GetFileContents(web, template, pageFullUrl, creationInfo, pageUrl);

                            }
                            else
                            {
                                Console.WriteLine(
                                    string.Format(
                                        "Page content export requires a server version that is newer than the current server. Server version is {0}, minimal required is {1}",
                                        web.Context.ServerLibraryVersion, Constants.MINIMUMZONEIDREQUIREDSERVERVERSION),
                                    ProvisioningMessageType.Warning);
                                Console.WriteLine(
                                    "Page content export requires a server version that is newer than the current server. Server version is {0}, minimal required is {1}",
                                    web.Context.ServerLibraryVersion, Constants.MINIMUMZONEIDREQUIREDSERVERVERSION);
                            }
                        }
                    }
                }
                catch (ServerException ex)
                {
                    if (ex.ServerErrorCode != -2146232832)
                    {
                        throw;
                    }
                    else
                    {
                        if (web.Context.HasMinimalServerLibraryVersion(Constants.MINIMUMZONEIDREQUIREDSERVERVERSION))
                        {
                            // Page does not belong to a list, extract the file as is
                            template = GetFileContents(web, template, pageFullUrl, creationInfo, pageUrl);

                        }
                        else
                        {
                            Console.WriteLine(
                                string.Format(
                                    "Page content export requires a server version that is newer than the current server. Server version is {0}, minimal required is {1}",
                                    web.Context.ServerLibraryVersion, Constants.MINIMUMZONEIDREQUIREDSERVERVERSION),
                                ProvisioningMessageType.Warning);
                            Console.WriteLine(
                                "Page content export requires a server version that is newer than the current server. Server version is {0}, minimal required is {1}",
                                web.Context.ServerLibraryVersion, Constants.MINIMUMZONEIDREQUIREDSERVERVERSION);
                        }
                    }
                }
                switch (fileLevel)
                {
                    case Microsoft.SharePoint.Client.FileLevel.Published:
                        {
                            file.PublishFileToLevel(Microsoft.SharePoint.Client.FileLevel.Published);
                            break;
                        }
                    case Microsoft.SharePoint.Client.FileLevel.Draft:
                        {
                            file.PublishFileToLevel(Microsoft.SharePoint.Client.FileLevel.Draft);
                            break;
                        }
                    default:
                        {
                            if (checkedOut)
                            {
                                file.CheckIn("", CheckinType.MajorCheckIn);
                                web.Context.ExecuteQueryRetry();
                            }
                            break;
                        }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private ProvisioningTemplate GetFileContents(Web web, ProvisioningTemplate template, string pageFullUrl, ProvisioningTemplateCreationInformation creationInfo, string pageUrl)
        {
            try
            {
                var fullUri = new Uri(UrlUtility.Combine(web.Url, pageUrl));

                var folderPath =
                    fullUri.Segments.Take(fullUri.Segments.Count() - 1)
                        .ToArray()
                        .Aggregate((i, x) => i + x)
                        .TrimEnd('/');
                var fileName = fullUri.Segments[fullUri.Segments.Count() - 1];

                var webParts = web.GetWebParts(pageFullUrl);

                var file = web.GetFileByServerRelativeUrl(pageFullUrl);

                var homeFile = new OfficeDevPnP.Core.Framework.Provisioning.Model.File()
                {
                    Folder = Tokenize(folderPath, web.Url),
                    Src = fileName,
                    Overwrite = true,
                };

                // Add field values to file

                RetrieveFieldValues(web, file, homeFile);

                // Add WebParts to file
                foreach (var webPart in webParts)
                {
                    var webPartxml = TokenizeWebPartXml(web, web.GetWebPartXml(webPart.Id, pageFullUrl));

                    OfficeDevPnP.Core.Framework.Provisioning.Model.WebPart newWp = new OfficeDevPnP.Core.Framework.
                        Provisioning.Model.WebPart()
                    {
                        Title = webPart.WebPart.Title,
                        Row = (uint)webPart.WebPart.ZoneIndex,
                        Order = (uint)webPart.WebPart.ZoneIndex,
                        Contents = webPartxml
                    };
#if !SP2016
                    // As long as we've no CSOM library that has the ZoneID we can't use the version check as things don't compile...
                    if (web.Context.HasMinimalServerLibraryVersion(Constants.MINIMUMZONEIDREQUIREDSERVERVERSION))
                    {
                        newWp.Zone = webPart.ZoneId;
                    }
#endif
                    homeFile.WebParts.Add(newWp);
                }
                template.Files.Add(homeFile);

                // Persist file using connector
                if (creationInfo.PersistBrandingFiles)
                {
                    PersistFile(web, creationInfo, folderPath, fileName);
                }
                return template;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private void PersistFile(Web web, ProvisioningTemplateCreationInformation creationInfo, string folderPath,
            string fileName, Boolean decodeFileName = false)
        {
            try
            {
                if (creationInfo.FileConnector != null)
                {
                    SharePointConnector connector = new SharePointConnector(web.Context, web.Url, "dummy");

                    Uri u = new Uri(web.Url);
                    if (folderPath.IndexOf(u.PathAndQuery, StringComparison.InvariantCultureIgnoreCase) > -1)
                    {
                        folderPath = folderPath.Replace(u.PathAndQuery, "");
                    }

                    String container = folderPath.Trim('/').Replace("%20", " ").Replace("/", "\\");
                    String persistenceFileName =
                        (decodeFileName ? HttpUtility.UrlDecode(fileName) : fileName).Replace("%20", " ");

                    using (Stream s = connector.GetFileStream(fileName, folderPath))
                    {
                        if (s != null)
                        {
                            creationInfo.FileConnector.SaveFileStream(
                                persistenceFileName, container, s);
                        }
                    }
                }
                else
                {
                    Console.WriteLine("No connector present to persist homepage.", ProvisioningMessageType.Error);
                    Console.WriteLine("No connector present to persist homepage");
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private OfficeDevPnP.Core.Framework.Provisioning.Model.File RetrieveFieldValues(Web web,
            Microsoft.SharePoint.Client.File file, OfficeDevPnP.Core.Framework.Provisioning.Model.File modelFile)
        {
            try
            {
                Microsoft.SharePoint.Client.ListItem listItem = null;
                try
                {
                    listItem = file.EnsureProperty(f => f.ListItemAllFields);
                }
                catch
                {
                }

                if (listItem != null)
                {
                    var list = listItem.ParentList;

                    var fields = list.Fields;
                    web.Context.Load(fields,
                        fs => fs.IncludeWithDefaultProperties(f => f.TypeAsString, f => f.InternalName, f => f.Title));
                    web.Context.ExecuteQueryRetry();

                    var fieldValues = listItem.FieldValues;

                    var fieldValuesAsText = listItem.EnsureProperty(li => li.FieldValuesAsText).FieldValues;

                    var fieldstoExclude = new[]
                    {
                    "ID",
                    "GUID",
                    "Author",
                    "Editor",
                    "FileLeafRef",
                    "FileRef",
                    "File_x0020_Type",
                    "Modified_x0020_By",
                    "Created_x0020_By",
                    "Created",
                    "Modified",
                    "FileDirRef",
                    "Last_x0020_Modified",
                    "Created_x0020_Date",
                    "File_x0020_Size",
                    "FSObjType",
                    "IsCheckedoutToLocal",
                    "ScopeId",
                    "UniqueId",
                    "VirusStatus",
                    "_Level",
                    "_IsCurrentVersion",
                    "ItemChildCount",
                    "FolderChildCount",
                    "SMLastModifiedDate",
                    "owshiddenversion",
                    "_UIVersion",
                    "_UIVersionString",
                    "Order",
                    "WorkflowVersion",
                    "DocConcurrencyNumber",
                    "ParentUniqueId",
                    "CheckedOutUserId",
                    "SyncClientId",
                    "CheckedOutTitle",
                    "SMTotalSize",
                    "SMTotalFileStreamSize",
                    "SMTotalFileCount",
                    "ParentVersionString",
                    "ParentLeafName",
                    "SortBehavior",
                    "StreamHash",
                    "TaxCatchAll",
                    "TaxCatchAllLabel",
                    "_ModerationStatus",
                    //"HtmlDesignAssociated",
                    //"HtmlDesignStatusAndPreview",
                    "MetaInfo",
                    "CheckoutUser",
                    "NoExecute",
                    "_HasCopyDestinations",
                    "ContentVersion",
                    "UIVersion",
                };

                    foreach (var fieldValue in fieldValues.Where(f => !fieldstoExclude.Contains(f.Key)))
                    {
                        if (fieldValue.Value != null && !string.IsNullOrEmpty(fieldValue.Value.ToString()))
                        {
                            var field = fields.FirstOrDefault(fs => fs.InternalName == fieldValue.Key);

                            string value = string.Empty;

                            switch (field.TypeAsString)
                            {
                                case "URL":
                                    value = Tokenize(fieldValuesAsText[fieldValue.Key], web.Url, web);
                                    break;
                                case "User":
                                    var userFieldValue = fieldValue.Value as Microsoft.SharePoint.Client.FieldUserValue;
                                    if (userFieldValue != null)
                                    {
#if !ONPREMISES
                                        value = userFieldValue.Email;
#else
                                value = userFieldValue.LookupValue;
#endif
                                    }
                                    break;
                                case "LookupMulti":
                                    var lookupFieldValue =
                                        fieldValue.Value as Microsoft.SharePoint.Client.FieldLookupValue[];
                                    if (lookupFieldValue != null)
                                    {
                                        value = Tokenize(JsonUtility.Serialize(lookupFieldValue), web.Url);
                                    }
                                    break;
                                case "TaxonomyFieldType":
                                    var taxonomyFieldValue =
                                        fieldValue.Value as Microsoft.SharePoint.Client.Taxonomy.TaxonomyFieldValue;
                                    if (taxonomyFieldValue != null)
                                    {
                                        value = Tokenize(JsonUtility.Serialize(taxonomyFieldValue), web.Url);
                                    }
                                    break;
                                case "TaxonomyFieldTypeMulti":
                                    var taxonomyMultiFieldValue =
                                        fieldValue.Value as
                                            Microsoft.SharePoint.Client.Taxonomy.TaxonomyFieldValueCollection;
                                    if (taxonomyMultiFieldValue != null)
                                    {
                                        value = Tokenize(JsonUtility.Serialize(taxonomyMultiFieldValue), web.Url);
                                    }
                                    break;
                                case "ContentTypeIdFieldType":
                                default:
                                    value = Tokenize(fieldValue.Value.ToString(), web.Url, web);
                                    break;
                            }

                            if (fieldValue.Key == "ContentTypeId")
                            {
                                // Replace the content typeid with a token
                                var ct = list.GetContentTypeById(value);
                                if (ct != null)
                                {
                                    value = string.Format("{{contenttypeid:{0}}}", ct.Name);
                                }
                            }

                            // We process real values only
                            if (value != null && !String.IsNullOrEmpty(value) && value != "[]")
                            {
                                modelFile.Properties.Add(fieldValue.Key, value);
                            }
                        }
                    }
                }

                return modelFile;
            }

            catch (Exception ex)
            {
                throw ex;
            }
        }

        private string Tokenize(string url, string webUrl, Web web = null)
        {
            try
            {
                String result = null;

                if (string.IsNullOrEmpty(url))
                {
                    // nothing to tokenize...
                    result = String.Empty;
                }
                else
                {
                    // Decode URL
                    url = Uri.UnescapeDataString(url);
                    // Try with theme catalog
                    if (url.IndexOf("/_catalogs/theme", StringComparison.InvariantCultureIgnoreCase) > -1)
                    {
                        var subsite = false;
                        if (web != null)
                        {
                            subsite = web.IsSubSite();
                        }
                        if (subsite)
                        {
                            result = url.Substring(url.IndexOf("/_catalogs/theme", StringComparison.InvariantCultureIgnoreCase)).Replace("/_catalogs/theme", "{sitecollection}/_catalogs/theme");
                        }
                        else
                        {
                            result = url.Substring(url.IndexOf("/_catalogs/theme", StringComparison.InvariantCultureIgnoreCase)).Replace("/_catalogs/theme", "{themecatalog}");
                        }
                    }

                    // Try with master page catalog
                    if (url.IndexOf("/_catalogs/masterpage", StringComparison.InvariantCultureIgnoreCase) > -1)
                    {
                        var subsite = false;
                        if (web != null)
                        {
                            subsite = web.IsSubSite();
                        }
                        if (subsite)
                        {
                            result = url.Substring(url.IndexOf("/_catalogs/masterpage", StringComparison.InvariantCultureIgnoreCase)).Replace("/_catalogs/masterpage", "{sitecollection}/_catalogs/masterpage");
                        }
                        else
                        {
                            result = url.Substring(url.IndexOf("/_catalogs/masterpage", StringComparison.InvariantCultureIgnoreCase)).Replace("/_catalogs/masterpage", "{masterpagecatalog}");
                        }
                    }

                    // Try with site URL
                    if (result != null)
                    {
                        url = result;
                    }
                    Uri uri;
                    if (Uri.TryCreate(webUrl, UriKind.Absolute, out uri))
                    {
                        string webUrlPathAndQuery = HttpUtility.UrlDecode(uri.PathAndQuery);
                        if (url.IndexOf(webUrlPathAndQuery, StringComparison.InvariantCultureIgnoreCase) > -1)
                        {
                            result = (uri.PathAndQuery.Equals("/") && url.StartsWith(uri.PathAndQuery))
                                ? "{site}" + url // we need this for DocumentTemplate attribute of pnp:ListInstance also on a root site ("/") without managed path
                                : url.Replace(webUrlPathAndQuery, "{site}");
                        }
                    }

                    // Default action
                    if (String.IsNullOrEmpty(result))
                    {
                        result = url;
                    }
                }

                return result;
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }
        private string TokenizeWebPartXml(Web web, string xml)
        {
            try
            {
                var lists = web.Lists;
                web.Context.Load(web, w => w.ServerRelativeUrl, w => w.Id);
                web.Context.Load(lists, ls => ls.Include(l => l.Id, l => l.Title));
                web.Context.ExecuteQueryRetry();

                foreach (var list in lists)
                {
                    xml = Regex.Replace(xml, list.Id.ToString(), string.Format("{{listid:{0}}}", list.Title), RegexOptions.IgnoreCase);
                }
                xml = Regex.Replace(xml, web.Id.ToString(), "{siteid}", RegexOptions.IgnoreCase);
                xml = Regex.Replace(xml, "(\"" + web.ServerRelativeUrl + ")(?!&)", "\"{site}", RegexOptions.IgnoreCase);
                xml = Regex.Replace(xml, "'" + web.ServerRelativeUrl, "'{site}", RegexOptions.IgnoreCase);
                xml = Regex.Replace(xml, ">" + web.ServerRelativeUrl, ">{site}", RegexOptions.IgnoreCase);
                return xml;
            }
            catch (Exception ex)
            {

                throw ex;
            }

        }
        public void AddJsLink(Microsoft.SharePoint.Client.ClientContext ctx)
        {

            Web web = ctx.Web;
            ctx.Load(web, w => w.UserCustomActions);
            ctx.ExecuteQuery();

            ctx.Load(web, w => w.UserCustomActions, w => w.Url, w => w.AppInstanceId);
            ctx.ExecuteQuery();

            UserCustomAction userCustomAction = web.UserCustomActions.Add();
            userCustomAction.Location = "Microsoft.SharePoint.StandardMenu";
            userCustomAction.Group = "SiteActions";
            BasePermissions perms = new BasePermissions();
            perms.Set(PermissionKind.ManageWeb);
            userCustomAction.Rights = perms;
            userCustomAction.Sequence = 100;
            userCustomAction.Title = "Say Hello";

            string url = "javascript:alert('Hello SharePoint Custom Action!!!');";


            userCustomAction.Url = url;
            userCustomAction.Update();
            ctx.ExecuteQuery();

            // Remove the entry from the 'Recents' node
            Microsoft.SharePoint.Client.NavigationNodeCollection nodes = web.Navigation.QuickLaunch;
            ctx.Load(nodes, n => n.IncludeWithDefaultProperties(c => c.Children));
            ctx.ExecuteQuery();
            var recent = nodes.Where(x => x.Title == "Recent").FirstOrDefault();
            if (recent != null)
            {
                var appLink = recent.Children.Where(x => x.Title == "Site Modifier").FirstOrDefault();
                if (appLink != null) appLink.DeleteObject();
                ctx.ExecuteQuery();
            }
        }
        
    }
}
