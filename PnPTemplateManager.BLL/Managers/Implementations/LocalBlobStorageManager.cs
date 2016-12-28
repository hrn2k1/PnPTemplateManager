using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MoreLinq;
using PnPTemplateManager.Managers.Contracts;
using PnPTemplateManager.Models;

namespace PnPTemplateManager.Managers.Implementations
{
    public class LocalBlobStorageManager : IFileStorageManager
    {
        private IAppSettingsManager appSettingsManager;

        private string root = null;
        private string publicPath = null;
        private string securePath = null;

        internal string Root
        {
            get { return root; }
            set { root = value; }
        }

        public bool Secure { get; set; }

        private void InitPaths()
        {
            if (!string.IsNullOrEmpty(root))
                return;
            root = appSettingsManager.GetAppSetting("LocalBlobPath");
            if (string.IsNullOrWhiteSpace(root))
                throw new Exception("AppSetting LocalBlobPath is not defined");

            if (!Directory.Exists(root))
                throw new Exception(string.Format("LocalBlobPath '{0}' does not exists", root));

            publicPath = Path.Combine(root, "public");
            if (!Directory.Exists(publicPath))
                Directory.CreateDirectory(publicPath);

            securePath = Path.Combine(root, "secure");
            if (!Directory.Exists(securePath))
                Directory.CreateDirectory(securePath);
        }

        public LocalBlobStorageManager(IAppSettingsManager appSettingsManager)
        {
            this.appSettingsManager = appSettingsManager;
            InitPaths();
        }

        public StorageFile SaveFile(string path, string content, string cacheControl = null)
        {
            return SaveFile(path, Encoding.UTF8.GetBytes(content));
        }

        public StorageFile SaveFile(string path, byte[] content, string cacheControl = null)
        {
            string fullPath = GetFullPath(path);
            EnsureFolders(fullPath);
            System.IO.File.WriteAllBytes(fullPath, content);
            return new StorageFile()
            {
                Path = GetRelativePath(fullPath),
                Content = content,
                Url = Secure ? "" : appSettingsManager.BlobUrl + path
            };
        }

        private void EnsureFolders(string fullPath)
        {
            string currentPath = fullPath.Split('\\')[0] + "\\"; // drive +

            fullPath
                .Split('\\')
                .Skip(1)
                .Reverse()
                .Skip(1)
                .Reverse()
                .ForEach(part =>
                {
                    try
                    {
                        currentPath = Path.Combine(currentPath, part);
                        if (!System.IO.Directory.Exists(currentPath))
                            System.IO.Directory.CreateDirectory(currentPath);
                    }
                    catch (Exception)
                    {
                        throw new Exception("could not create directory '" + currentPath + "'");
                    }

                });
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
                    return !f.Path.Contains(destFile) && !exceptFiles.Contains(fileName);
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

        public void DeleteFile(string path)
        {
            string fullPath = GetFullPath(path);
            if (File.Exists(fullPath))
                File.Delete(fullPath);
        }

        public IEnumerable<StorageFile> GetFileWithoutContent(string path, string searchPattern, bool includeFilesFromSubDirs)
        {
            return GetFilesPaths(path, searchPattern, includeFilesFromSubDirs).Select(filePath =>
            {
                return new StorageFile()
                {
                    Path = GetRelativePath(filePath), // filePath.Substring(GetFullPath("").Length + 1).Replace("\\", "/"),
                    Url =
                        Secure
                            ? ""
                            : appSettingsManager.BlobUrl + GetRelativePath(filePath) //.Substring(root.Length + 1).Replace("\\", "/")
                };
            });
        }

        public IEnumerable<StorageFile> GetFiles(string path, string searchPattern, bool includeFilesFromSubDirs)
        {
            return GetFilesPaths(path, searchPattern, includeFilesFromSubDirs).Select(filePath =>
            {
                return new StorageFile()
                {
                    Content = System.IO.File.ReadAllBytes(filePath),
                    Path = GetRelativePath(filePath), // filePath.Substring(GetFullPath("").Length + 1).Replace("\\", "/"),
                    Url =
                        Secure
                            ? ""
                            : appSettingsManager.BlobUrl + GetRelativePath(filePath) //.Substring(root.Length + 1).Replace("\\", "/")
                };
            });
        }

        private IEnumerable<string> GetFilesPaths(string path, string searchPattern, bool includeFilesFromSubDirs)
        {
            string fullPath = GetFullPath(path);
            if (!Directory.Exists(fullPath))
                return new List<string>();

            return System.IO.Directory.GetFiles(fullPath, searchPattern,
                includeFilesFromSubDirs ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
        }

        public StorageFile GetFile(string path)
        {
            string fullPath = GetFullPath(path);
            if (File.Exists(fullPath))
            {
                return new StorageFile
                {
                    Path = GetRelativePath(fullPath),
                    Content = System.IO.File.ReadAllBytes(fullPath),
                    Url = Secure ? "" : appSettingsManager.BlobUrl + GetRelativePath(fullPath)
                };
            }
            return null;
        }

        private string GetFullPath(string path)
        {
            var fullPath = Path.Combine(Secure ? securePath : publicPath, path).Replace("/", "\\");
            return fullPath;
        }

        private string GetRelativePath(string path)
        {
            return path.Substring(Secure ? securePath.Length + 1 : publicPath.Length + 1).Replace("\\", "/");
        }

    }
}
