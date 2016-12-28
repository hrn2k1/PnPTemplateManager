using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PnPTemplateManager.Models;

namespace PnPTemplateManager.Managers.Contracts
{
    public interface IFileStorageManager
    {
        StorageFile SaveFile(string path, string content, string cacheControl = null);

        StorageFile SaveFile(string path, byte[] content, string cacheControl = null);

        void DeleteFile(string path);

        IEnumerable<StorageFile> GetFileWithoutContent(string path, string searchPattern, bool includeFilesFromSubDirs);

        IEnumerable<StorageFile> GetFiles(string path, string searchPattern, bool includeFilesFromSubDirs);

        StorageFile GetFile(string path);



        StorageFile MultipleFilesToSingleFile(
            string dirPath,
            string filePattern,
            string destFile,
            bool recursive = true);

        StorageFile MultipleFilesToSingleFile(
            string dirPath,
            string filePattern,
            string destFile,
            List<string> exceptFiles,
            bool recursive = true);

        Boolean Secure
        {
            set; get;
        }
    }
}
