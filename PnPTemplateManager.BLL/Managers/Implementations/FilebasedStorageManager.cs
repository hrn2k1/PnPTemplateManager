using PnPTemplateManager.Managers.Contracts;
using PnPTemplateManager.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Hosting;

namespace PnPTemplateManager.Managers.Implementations
{
    public class FilebasedStorageManager : IFileStorageManager
    {
        private readonly IAppSettingsManager appSettingsManager;

        private bool secure;
        private string rootForTestPurpose;

        public FilebasedStorageManager(IAppSettingsManager appSettingsManager)
        {
            this.appSettingsManager = appSettingsManager;
        }

        internal string Root
        {
            get
            {
                if (string.IsNullOrEmpty(rootForTestPurpose))
                {

                    if (secure)
                    {
                        return HostingEnvironment.IsHosted
                            ? HostingEnvironment.MapPath("~/App_data/FileStorage")
                            : Path.Combine(Environment.CurrentDirectory, "SecureFileStorage");
                    }
                    else
                    {
                        return HostingEnvironment.IsHosted
                            ? HostingEnvironment.MapPath("~/FileStorage")
                            : Path.Combine(Environment.CurrentDirectory, "FileStorage");
                    }
                }
                return rootForTestPurpose;
            }
            set
            {
                rootForTestPurpose = value;
            }
        }

        public bool Secure
        {
            set { secure = value; }
            get { return secure; }
        }

        public StorageFile SaveFile(string path, string content, string cacheControl = null)
        {
            return SaveFile(path, Encoding.UTF8.GetBytes(content));
        }

        public StorageFile SaveFile(string path, byte[] content, string cacheControl = null)
        {
            // ensure folders
            string[] dirs = path.Replace("/", "\\").Trim('\\').Split('\\');
            string currentPath = Root;
            dirs.Reverse().Skip(1).Reverse().ToList().ForEach(d =>
            {
                currentPath += "\\" + d;
                if (!Directory.Exists(currentPath))
                    Directory.CreateDirectory(currentPath);
            });

            string fullPath = GetFullPath(path);
            File.WriteAllBytes(fullPath, content);
            var file = new StorageFile
            {
                Path = GetRelativePath(fullPath),
                Content = content,
                Url = secure ? "" : appSettingsManager.AppUrl + "FileStorage/" + path
            };
            return file;
        }

        public void DeleteFile(string path)
        {
            string fullPath = GetFullPath(path);
            var file = new FileInfo(fullPath);
            if (file.Exists)
                file.Delete();
        }


        public IEnumerable<StorageFile> GetFileWithoutContent(string path, string searchPattern, bool includeFilesFromSubDirs)
        {
            return GetFileInfos(path, searchPattern, includeFilesFromSubDirs).Select(file =>
                new StorageFile
                {
                    Path = GetRelativePath(file.FullName),
                    Url = secure
                          ? ""
                          : appSettingsManager.AppUrl + "FileStorage/" + path.Replace("\\", "/") + "/" + file.Name
                });
        }

        public IEnumerable<StorageFile> GetFiles(string path, string searchPattern, bool includeFilesFromSubDirs)
        {
            return GetFileInfos(path, searchPattern, includeFilesFromSubDirs).Select(file =>
                new StorageFile
                {
                    Path = GetRelativePath(file.FullName),
                    Content = File.ReadAllBytes(file.FullName),
                    Url = secure
                          ? ""
                          : appSettingsManager.AppUrl + "FileStorage/" + path.Replace("\\", "/") + "/" + file.Name
                });
        }

        private IEnumerable<FileInfo> GetFileInfos(string path, string searchPattern, bool includeFilesFromSubDirs)
        {
            string dir = GetFullPath(path);
            if (!System.IO.Directory.Exists(dir))
                return new List<FileInfo>();

            return new DirectoryInfo(dir).GetFiles(searchPattern, includeFilesFromSubDirs ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
        }

        public StorageFile GetFile(string path)
        {
            string fullPath = GetFullPath(path);
            var file = new FileInfo(fullPath);
            if (file.Exists)
            {
                return new StorageFile
                {
                    Path = GetRelativePath(fullPath),
                    Content = File.ReadAllBytes(file.FullName),
                    Url = secure ? "" : appSettingsManager.AppUrl + "FileStorage/" + path
                };
            }
            return null;
        }

        public StorageFile MultipleFilesToSingleFile(
            string dirPath,
            string filePattern,
            string destFile,
            bool recursive = true)
        {
            var res = this.MultipleFilesToSingleFile(
                dirPath,
                filePattern,
                destFile,
                new List<string>(),
                recursive);

            return res;
        }

        public StorageFile MultipleFilesToSingleFile(
            string dirPath,
            string filePattern,
            string destFile,
            List<string> exceptFiles,
            bool recursive = true)
        {
            List<string> fileAry = GetFiles(dirPath, filePattern, true)
                .Where(f =>
                {
                    string fileName = f.Path.Substring(f.Path.LastIndexOf("/") + 1);
                    return !f.Path.Contains(destFile)
                           && !exceptFiles.Contains(fileName);
                })
                .Select(file => file.ToString())
                .ToList();

            StringBuilder outputContent = new StringBuilder();
            fileAry.ForEach(f => { outputContent.Append(f); });

            StorageFile savedFile = SaveFile(destFile, outputContent.ToString());

            return new StorageFile
            {
                Path = destFile,
                Content = Encoding.UTF8.GetBytes(outputContent.ToString()),
                Url = savedFile.Url
            };
        }

        private string GetFullPath(string path)
        {
            string updatedPath = Path.Combine(Root, path.Replace("/", "\\").Trim('\\'));
            return updatedPath;
        }

        private string GetRelativePath(string path)
        {
            return path.Substring(Root.Length + 1).Replace("\\", "/");
        }

    }
}
