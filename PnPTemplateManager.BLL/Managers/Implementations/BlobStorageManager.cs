using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using PnPTemplateManager.Managers.Contracts;
using PnPTemplateManager.Models;

namespace PnPTemplateManager.Managers.Implementations
{


    public class BlobStorageManager : IFileStorageManager
    {
        internal static Lazy<CloudBlobClient> lazyClient = new Lazy<CloudBlobClient>(() =>
        {
            var BlobStorageConnection =
                System.Configuration.ConfigurationManager.ConnectionStrings[
                    Environment.MachineName + "-BlobStorageConnection"] ??
                System.Configuration.ConfigurationManager.ConnectionStrings["BlobStorageConnection"];

            CloudStorageAccount account = CloudStorageAccount.Parse(BlobStorageConnection.ConnectionString);
            var client2 = account.CreateCloudBlobClient();
            var secureContainer = client2.GetContainerReference("wizdom365secure");
            if (secureContainer.CreateIfNotExists())
            {
                secureContainer.SetPermissions(
                new BlobContainerPermissions
                {
                    PublicAccess = BlobContainerPublicAccessType.Off
                });
                
            }

            var publicContainer = client2.GetContainerReference("wizdom365public");
            if (publicContainer.CreateIfNotExists())
            {
                publicContainer.SetPermissions(
                new BlobContainerPermissions
                {
                    PublicAccess = BlobContainerPublicAccessType.Blob
                });

            }

            return client2;
        });
        public static CloudBlobClient client
        {
            get
            {
                return lazyClient.Value;
            }
        }
        private BlobRequestOptions blobOptions = new BlobRequestOptions() { ServerTimeout = new TimeSpan(0, 2, 0) }; // 2 min timeout
        private string containerReferenceName = "wizdom365public";
        //        private bool _secure = false;

        public StorageFile MultipleFilesToSingleFile(string dirPath, string filePattern, string destFile, List<string> exceptFiles,
            bool recursive = true)
        {
            throw new NotImplementedException();
        }

        public bool Secure
        {
            set { containerReferenceName = value ? "wizdom365secure" : "wizdom365public"; }
            get { return containerReferenceName == "wizdom365secure"; }
        }

        public StorageFile SaveFile(string path, string content) // interface
        {
            return SaveFile(path, Encoding.UTF8.GetBytes(content), null);
        }
        public StorageFile SaveFile(string path, byte[] content) // interface
        {
            return SaveFile(path, content, null);
        }
        public StorageFile SaveFile(string path, string content, string cacheControl) // implementation specific override
        {
            return SaveFile(path, Encoding.UTF8.GetBytes(content), cacheControl);
        }
        public StorageFile SaveFile(string path, byte[] content, string cacheControl) // implementation specific override
        {
            CloudBlobContainer container = client.GetContainerReference(containerReferenceName);
            var blob = container.GetBlockBlobReference(containerReferenceName + "/" + path);
            blob.UploadFromByteArray(content, 0, content.Length);
            blob.FetchAttributes();

            blob.Properties.ContentType = GetContentType(Path.GetExtension(path));

            if (!string.IsNullOrEmpty(cacheControl))
                blob.Properties.CacheControl = cacheControl;

            blob.SetProperties();


            return new StorageFile()
            {
                Path = blob.Name,
                Content = content,
                Url = blob.Uri.ToString()
            };
        }



        public void DeleteFile(string path)
        {
            try
            {
                IEnumerable<CloudBlob> blobs = client.GetContainerReference(containerReferenceName).ListBlobs(null, true, BlobListingDetails.None, blobOptions)
                .Where(b => b is CloudBlob) // exclude directory objects. When includeFilesFromSubDirs is false, useFlatBlobListing, will return directories
                .Cast<CloudBlob>();

                foreach (var blob in blobs)
                {
                    if(blob.Name.Equals($"{containerReferenceName}/{path}",StringComparison.InvariantCultureIgnoreCase))
                        blob.Delete();

                }
               
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public IEnumerable<StorageFile> GetFileWithoutContent(string path, string searchPattern, bool includeFilesFromSubDirs)
        {
            throw new NotImplementedException();
        }
        
        public IEnumerable<StorageFile> GetFiles(string path, string searchPattern, bool includeFilesFromSubDirs)
        {
            var storageFiles = new List<StorageFile>();
            IEnumerable<CloudBlob> blobs = GetBlobs(path, searchPattern, includeFilesFromSubDirs);
            foreach (var item in blobs)
            {
                using (var memoryStream = new MemoryStream())
                {
                    item.DownloadToStream(memoryStream);
                    storageFiles.Add(new StorageFile()
                    {
                        Path = item.Name,
                        Content = ReadFully(memoryStream),
                        Url = item.Uri.ToString()
                    });
                }


            }
            return storageFiles;
        }

        private IEnumerable<CloudBlob> GetBlobs(string path, string searchPattern, bool includeFilesFromSubDirs)
        {

            IEnumerable<CloudBlob> blobs = client.GetContainerReference(containerReferenceName).ListBlobs(null, includeFilesFromSubDirs, BlobListingDetails.None, blobOptions)
                .Where(b => b is CloudBlob) // exclude directory objects. When includeFilesFromSubDirs is false, useFlatBlobListing, will return directories
                .Cast<CloudBlob>();

            if (!string.IsNullOrEmpty(searchPattern))
            {
                // searchPattern to regex
                var pattern = new Regex("^" + Regex.Escape(searchPattern).Replace("\\*", ".*").Replace("\\?", ".") + "$", RegexOptions.IgnoreCase);
                if (!string.IsNullOrEmpty(path))
                {
                    blobs =
                        blobs.Where(
                            blob =>
                                blob.Parent.Prefix == $"{containerReferenceName}/{path}/" &&
                                pattern.IsMatch(Path.GetFileName(blob.Uri.ToString())));
                }
                else
                {
                    blobs = blobs.Where(blob => pattern.IsMatch(Path.GetFileName(blob.Uri.ToString())));
                }

            }

            return blobs;
        }
        private byte[] ReadFully(Stream input)
        {
            byte[] buffer = new byte[16 * 1024];
            using (MemoryStream ms = new MemoryStream())
            {
                int read;
                while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
                {
                    ms.Write(buffer, 0, read);
                }
                return ms.ToArray();
            }
        }
        public StorageFile GetFile(string path)
        {
            try
            {
                IEnumerable<CloudBlob> blobs = client.GetContainerReference(containerReferenceName).ListBlobs(null, true, BlobListingDetails.None, blobOptions)
                .Where(b => b is CloudBlob) // exclude directory objects. When includeFilesFromSubDirs is false, useFlatBlobListing, will return directories
                .Cast<CloudBlob>();

                foreach (var blob in blobs)
                {
                    if (blob.Name.Equals($"{containerReferenceName}/{path}", StringComparison.InvariantCultureIgnoreCase))
                    {
                        using (var memoryStream = new MemoryStream())
                        {
                            blob.DownloadToStream(memoryStream);
                            return new StorageFile()
                            {
                                Path = blob.Name,
                                Content = memoryStream.ToArray(),
                                Url = blob.Uri.ToString()
                            };
                        }
                    }

                }
               

            }
            catch (Exception ex)
            {
                //if (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
                //    return null;
                throw ex;
            }
            return null;

        }

        public StorageFile MultipleFilesToSingleFile(string dirPath, string filePattern, string destFile, bool recursive = true)
        {
            throw new NotImplementedException();
        }


        internal string GetContentType(string extension)
        {
            if (extension.Equals(".js", StringComparison.InvariantCultureIgnoreCase))
                return "application/javascript";
            if (extension.Equals(".css", StringComparison.InvariantCultureIgnoreCase))
                return "text/css";

            return "application/octet-stream";
        }
    }


}
