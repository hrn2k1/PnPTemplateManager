using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Hosting;
using Microsoft.SharePoint.Client;
using Form = System.Windows.Forms.Form;
using System.Windows.Forms;
using OfficeDevPnP.Core.Framework.Provisioning.Connectors;
using OfficeDevPnP.Core.Framework.Provisioning.Connectors.OpenXML.Model;
using OfficeDevPnP.Core.Framework.Provisioning.Model;
using OfficeDevPnP.Core.Framework.Provisioning.ObjectHandlers;
using OfficeDevPnP.Core.Framework.Provisioning.Providers.Xml;
using PnPTemplateManager.Managers;

namespace ShareProv
{
    public partial class frmMain : Form
    {
        public frmMain()
        {
            InitializeComponent();
        }


        public ClientContext Context { get; set; }

        public string SiteUrl { get; set; }
        public string UserName { get; set; }

        private void GetLoggedIn()
        {
            try
            {
                var frmLogin = new frmLogin();
                frmLogin.OnLoggedIn += frmLogin_OnLoggedIn;
                frmLogin.ShowDialog();
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        private void frmLogin_OnLoggedIn(object sender, LoginEventArgs e)
        {
            try
            {
                if (e?.Context != null)
                {
                    this.SiteUrl = e.SiteUrl;
                    this.UserName = e.UserName;
                    this.Context = e.Context;
                    this.Text = string.Format("SharePoint Provisioning ({0})", e.SiteUrl);
                    lblUserName.Text = e.UserName;
                    mitmConnect.Text = @"Change &Connection";
                }
                else
                {
                    mitmConnect.Text = @"&Connect";
                    this.Text = @"SharePoint Provisioning";
                }
            }
            catch (Exception ex)
            {

                MessageBox.Show(ex.Message);
            }
        }

        private void frmMain_Load(object sender, EventArgs e)
        {
            try
            {
                if (Context == null)
                    GetLoggedIn();
                webBrowser1.DocumentText = "<b>h<i>A</i>r<i>U</i>n</b>";
            }
            catch (Exception ex)
            {

                MessageBox.Show(ex.Message);
            }
        }

        private void mitmConnect_Click(object sender, EventArgs e)
        {
            try
            {
                GetLoggedIn();
            }
            catch (Exception ex)
            {

                MessageBox.Show(ex.Message);
            }
        }
        private string BuildPnPPackageName(Uri siteUrl)
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
        private void btnExtract_Click(object sender, EventArgs e)
        {
            try
            {
                DebugMonitor.Start();
                DebugMonitor.OnOutputDebugString += DebugMonitor_OnOutputDebugString;
                
               


                var web = Context.Web;
                Context.Load(web, w => w.Title, w => w.ServerRelativeUrl, w => w.Url);
                Context.ExecuteQuery();

                var siteUrl = new Uri(SiteUrl);

                var pnpFileName = "";

                pnpFileName = BuildPnPPackageName(siteUrl);

                var pnpTemplatePath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\PnPTemplates";
                var ptci = new ProvisioningTemplateCreationInformation(web);
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
                    var pagesTemplate = pageProvisionManager.Extract(Context, ptci);
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
                pgTemplate.SelectedObject = template;
                DebugMonitor.Stop();
            }
            catch (Exception ex)
            {

                MessageBox.Show(ex.Message);
            }
        }

        private void DebugMonitor_OnOutputDebugString(int pid, string text)
        {
           // lblMsg.Text = $"{pid}: {text}";
           System.IO.File.AppendAllText("Log.txt", $"{pid}: {text}{Environment.NewLine}");
        }

        private void btnSaveTemplate_Click(object sender, EventArgs e)
        {
            try
            {
                var template = (ProvisioningTemplate)pgTemplate.SelectedObject;
                if (template != null)
                {
                    if (saveFileDialog1.ShowDialog() == DialogResult.OK)
                    {
                        var fileFullPath = saveFileDialog1.FileName;
                        var fileInfo = new System.IO.FileInfo(fileFullPath);
                        var pnpTemplatePath = fileInfo.DirectoryName;

                        var pnpFileName = fileInfo.Name;
                        if (pnpFileName.EndsWith(".pnp", StringComparison.InvariantCultureIgnoreCase))
                            pnpFileName = pnpFileName.Substring(0, pnpFileName.Length - 4);
                        var web = Context.Web;
                        var ptci = new ProvisioningTemplateCreationInformation(web);
                        var fileSystemConnector = new FileSystemConnector(pnpTemplatePath, "");
                        ptci.FileConnector = new OpenXMLConnector($"{pnpFileName}.pnp", fileSystemConnector);
                        XMLTemplateProvider provider =
                            new XMLOpenXMLTemplateProvider((OpenXMLConnector)ptci.FileConnector);
                        provider.SaveAs(template, $"{pnpFileName}.xml");
                    }
                }
            }
            catch (Exception ex)
            {

                MessageBox.Show(ex.Message);
            }
        }

        private void btnUploadTemplate_Click(object sender, EventArgs e)
        {
            try
            {
                if (openFileDialog1.ShowDialog() == DialogResult.OK)
                {
                    var fileFullPath = openFileDialog1.FileName;
                    var fileInfo = new System.IO.FileInfo(fileFullPath);
                    var pnpTemplatePath = fileInfo.DirectoryName;

                    var pnpFileName = fileInfo.Name;
                    var template = new ProvisioningTemplate(); 
                    var fileSystemConnector = new FileSystemConnector(pnpTemplatePath, "");
                    XMLTemplateProvider provider =
                        new XMLOpenXMLTemplateProvider(new OpenXMLConnector(pnpFileName, fileSystemConnector));

                    template = provider.GetTemplate(pnpFileName.Replace(".pnp", ".xml"));
                    template.Connector = provider.Connector;
                    //pgTemplate.SelectedObjects = new object[] {template};
                    pgTemplate.SelectedObject = template;
                }
            }
            catch (Exception ex)
            {

                MessageBox.Show(ex.Message);
            }
        }
    }
}
