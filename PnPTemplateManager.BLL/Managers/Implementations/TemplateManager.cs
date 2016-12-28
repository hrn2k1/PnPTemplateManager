using Microsoft.SharePoint.Client;
using OfficeDevPnP.Core.Framework.Provisioning.Connectors;
using OfficeDevPnP.Core.Framework.Provisioning.Model;
using OfficeDevPnP.Core.Framework.Provisioning.ObjectHandlers;
using OfficeDevPnP.Core.Framework.Provisioning.Providers.Xml;
using PnPTemplateManager.BLL;
using PnPTemplateManager.Managers.Contracts;
using PnPTemplateManager.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web.Hosting;
using PnPTemplateManager.Managers.Contracts;

namespace PnPTemplateManager.Managers.Implementations
{

    public class TemplateManager : ITemplateManager
    {
        private const string TemplateFolder = "PnPTemplates";
        private IFileStorageManager fileStorageManager;

        public TemplateManager(IFileStorageManager fileStorageManager)
        {
            this.fileStorageManager = fileStorageManager;
        }

        public List<PnPFileInfo> GetPnPTemplates()
        {
            var pnpFiles = fileStorageManager.GetFiles(TemplateFolder, "*.pnp", true);
            var files = pnpFiles.Select(file =>
            {
                int lastSlashIndex = file.Path.LastIndexOf("/", StringComparison.Ordinal);
                string fileName = file.Path.Substring(lastSlashIndex + 1);
                return new PnPFileInfo()
                {
                    Name = fileName,
                    Size = file.Content.LongLength / 1024.0m
                };
            }).ToList();

            return files;

        }

        public PnPFileInfo GetPnPTemplateFileFromSite(CreatePnPTemplateRequest request)
        {
            var pnpPackageInfo = new PnPFileInfo();
            try
            {
                using (var context = TokenHelper.GetClientContextWithAccessToken(request.SiteUrl, request.AccessToken))
                {
                    var web = context.Web;
                    context.Load(web, w => w.Title, w => w.ServerRelativeUrl, w => w.Url);
                    context.ExecuteQuery();

                    var siteUrl = new Uri(request.SiteUrl);

                    var pnpFileName = "";

                    if (string.IsNullOrEmpty(request.PnpPackageName))
                    {
                        pnpFileName = BuildPnPPackageName(siteUrl);
                    }
                    else
                    {
                        pnpFileName = request.PnpPackageName;
                        if (pnpFileName.ToLower().EndsWith(".pnp"))
                            pnpFileName = pnpFileName.Substring(0, pnpFileName.Length - 4);
                    }
                    var pnpTemplatePath = HostingEnvironment.MapPath($"~/{TemplateFolder}");

                    var ptci = new ProvisioningTemplateCreationInformation(context.Web);
                    var fileSystemConnector = new FileSystemConnector(pnpTemplatePath, "");

                    ptci.PersistBrandingFiles = true;
                    ptci.PersistPublishingFiles = true;
                    ptci.PersistMultiLanguageResources = true;
                    ptci.FileConnector = new OpenXMLConnector($"{pnpFileName}.pnp", fileSystemConnector);

                    ptci.ProgressDelegate = delegate (String message, Int32 progress, Int32 total)
                    {
                        Console.WriteLine(@"{0:00}/{1:00} - {2}", progress, total, message);
                    };
                    ProvisioningTemplate template = new ProvisioningTemplate();
                    try
                    {
                        template = web.GetProvisioningTemplate(ptci);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("PnP engine failed to extract template. Error: {0}", ex.Message);
                    }

                    try
                    {
                        PageProvisionManager pageProvisionManager = new PageProvisionManager();
                        var pagesTemplate = pageProvisionManager.Extract(context, ptci);
                        foreach (var theFile in pagesTemplate.Files)
                        {
                            var existingFile =
                                template.Files.FirstOrDefault(
                                    f => f.Src.Equals(theFile.Src, StringComparison.InvariantCultureIgnoreCase));
                            if (existingFile == null)
                                template.Files.Add(theFile);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Failed to extract Pages. Error: {0}", ex.Message);
                    }
                    if (web.IsSubSite())
                    {
                        try
                        {
                            var siteColumnsTemplate = new SiteColumnProvisionManager().Extract(web, ptci);
                            foreach (var col in siteColumnsTemplate.SiteFields)
                            {
                                if (!template.SiteFields.Contains(col))
                                    template.SiteFields.Add(col);
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("Failed to extract Site Columns. Error: {0}", ex.Message);
                        }
                        try
                        {
                            var siteCTTemplate = new ContentTypeProvisionManager().Extract(web, ptci);
                            foreach (var ct in siteCTTemplate.ContentTypes)
                            {
                                if (!template.ContentTypes.Contains(ct))
                                    template.ContentTypes.Add(ct);
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("Failed to extract Site Content Types. Error: {0}", ex.Message);
                        }
                    }

                    XMLTemplateProvider provider = new XMLOpenXMLTemplateProvider((OpenXMLConnector)ptci.FileConnector);
                    provider.SaveAs(template, $"{pnpFileName}.xml");
                    string fileLocation = $"{pnpTemplatePath}\\{pnpFileName}.pnp";
                    var file = new FileInfo(fileLocation);

                    fileStorageManager.SaveFile($"{TemplateFolder}\\{pnpFileName}.pnp", System.IO.File.ReadAllBytes(fileLocation));

                    pnpPackageInfo = new PnPFileInfo()
                    {
                        Name = $"{pnpFileName}.pnp",
                        Size = file.Length / 1024.0m
                    };
                    if (file.Exists)
                        file.Delete();
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return pnpPackageInfo;
        }

        private static string BuildPnPPackageName(Uri siteUrl)
        {
            var pnpFileName = "";
            for (var i = 0; i < siteUrl.Segments.Length; i++)
            {
                var part = siteUrl.Segments[i].Replace("/", "");
                if (part == "" || part.ToLower() == "sites") continue;
                pnpFileName += (pnpFileName == "" ? "" : "_") + part;
            }
            return pnpFileName;
        }

        public string ApplyPnPTemplateOnSite(ApplyPnPTemplateRequest request)
        {
            try
            {
                ProvisioningTemplate template = null;
                var ptai = new ProvisioningTemplateApplyingInformation();
                ptai.ProgressDelegate = delegate (String message, Int32 progress, Int32 total)
                {
                    Console.WriteLine(@"{0:00}/{1:00} - {2}", progress, total, message);
                };
                if (string.IsNullOrEmpty(request.PnPXML)) //normal pnp file flow
                {
                    var storageFile = fileStorageManager.GetFile($"{TemplateFolder}\\{request.PnPFileName}");
                    var pnpTemplatePath = HostingEnvironment.MapPath($"~/{TemplateFolder}");
                    var tempFile = $"{pnpTemplatePath}\\{request.PnPFileName}";
                    System.IO.File.WriteAllBytes(tempFile, storageFile.Content);
                    using (
                        var context = TokenHelper.GetClientContextWithAccessToken(request.SiteUrl, request.AccessToken))
                    {
                        var fileSystemConnector = new FileSystemConnector(pnpTemplatePath, "");
                        XMLTemplateProvider provider =
                            new XMLOpenXMLTemplateProvider(new OpenXMLConnector(request.PnPFileName, fileSystemConnector));

                        template = provider.GetTemplate(request.PnPFileName.Replace(".pnp", ".xml"));
                        template.Connector = provider.Connector;

                        RefineTemplate(context, template, ptai);

                        context.Web.ApplyProvisioningTemplate(template, ptai);
                        if (System.IO.File.Exists(tempFile))
                        {
                            System.IO.File.Delete(tempFile);
                        }
                        return "{ \"IsSuccess\": true, \"Message\": \"PnP template has been applied successfully\" }";

                    }
                }
                else
                {
                    //PNP xml flow
                    using (Stream s = GenerateStreamFromString(request.PnPXML))
                    {
                        var t = new XMLPnPSchemaFormatter();
                        template = t.ToProvisioningTemplate(s);
                    }

                    using (var context = TokenHelper.GetClientContextWithAccessToken(request.SiteUrl, request.AccessToken))
                    {
                        RefineTemplate(context, template, ptai);
                        context.Web.ApplyProvisioningTemplate(template, ptai);

                        return "{ \"IsSuccess\": true, \"Message\": \"PnP template has been applied successfully\" }";

                    }
                }
            }
            catch (Exception ex)
            {

                return "{ \"IsSuccess\": false, \"Message\": \"PnP template apply failed, Error: " + ex.Message + "\" }";
            }
        }

        private void RefineTemplate(ClientContext context, ProvisioningTemplate template, ProvisioningTemplateApplyingInformation ptai)
        {
            var web = context.Web;
            var isSubSite = web.IsSubSite();
            if (isSubSite)
            {

                var tokenParser = new TokenParser(web, template);
                if (template.SiteFields.Any())
                {
                    var columnProvisionManager = new SiteColumnProvisionManager();
                    columnProvisionManager.Provision(web, template, tokenParser, ptai);
                }
                if (template.ContentTypes.Any())
                {
                    var contentTypeProvisionManager = new ContentTypeProvisionManager();
                    contentTypeProvisionManager.Provision(web, template, tokenParser, ptai);
                }
                template.Files.RemoveAll(
                    f => f.Src.EndsWith(".master", StringComparison.InvariantCultureIgnoreCase));
            }
            var pageLib = context.Web.GetPagesLibrary();

            if (template.Files.Any())
            {
                foreach (var file in template.Files)
                {
                    if (file.Src.EndsWith(".aspx"))
                    {
                        file.Src = $"{pageLib.Title}\\{file.Src}";
                    }
                }
            }
        }

        private Stream GenerateStreamFromString(string value)
        {
            MemoryStream stream = new MemoryStream();
            StreamWriter writer = new StreamWriter(stream);
            writer.Write(value);
            writer.Flush();
            stream.Position = 0;
            return stream;
        }

        public string DeletePnPPackageFile(string pnpPackageName)
        {
            try
            {
                fileStorageManager.DeleteFile($"{TemplateFolder}/{pnpPackageName}");
                return "{ \"IsSuccess\": true }";
            }
            catch (Exception ex)
            {

                return "{ \"IsSuccess\": false, \"Message\": \"" + ex.Message + "\" }";
            }
        }

        public string SendPnPPackageFile(string fileName)
        {
            try
            {
                var file = fileStorageManager.GetFile($"{TemplateFolder}/{fileName}");

                System.Web.HttpContext.Current.Response.Clear();
                System.Web.HttpContext.Current.Response.ContentType = "application/octet-stream";
                System.Web.HttpContext.Current.Response.Charset = "utf-8";
                System.Web.HttpContext.Current.Response.AppendHeader("Content-Disposition",
                    $"attachment; filename={fileName}");
                System.Web.HttpContext.Current.Response.BinaryWrite(file.Content);
                System.Web.HttpContext.Current.Response.Flush();
                System.Web.HttpContext.Current.Response.End();

                return "{ \"IsSuccess\": true }";
            }
            catch (Exception ex)
            {

                return "{ \"IsSuccess\": false, \"Message\": \"" + ex.Message + "\" }";
            }
        }



        public void SavePnPPackage()
        {
            var files = System.Web.HttpContext.Current.Request.Files;
            for (var iCnt = 0; iCnt < files.Count; iCnt++)
            {
                var hpf = files[iCnt];

                if (hpf.ContentLength > 0 && hpf.FileName.EndsWith(".pnp", StringComparison.InvariantCultureIgnoreCase))
                {
                    var fileSize = hpf.ContentLength;
                    var fileContent = new byte[fileSize];
                    hpf.InputStream.Read(fileContent, 0, fileSize);
                    fileStorageManager.SaveFile($"{TemplateFolder}\\{hpf.FileName}", fileContent);

                }
            }
        }
    }
}
