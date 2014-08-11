using net.openstack.Core.Domain;
using net.openstack.Providers.Rackspace;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace CloudFiles_Deploy
{
    class SmartRackspaceUpload
    {
        public static void SmartUploadFolderToRackspace(string username, string apiKey, string containerName, string folderToDeploy)
        {
            var cloudIdentity = new CloudIdentity() { Username = username, APIKey = apiKey };
            var cloudFilesProvider = new CloudFilesProvider(cloudIdentity);
            Console.WriteLine("Preparing to Deploy");
            Console.WriteLine("Deploy from {0}", folderToDeploy);
            Console.WriteLine("Deploy to {0}", containerName);

            Console.WriteLine("Scanning for differences.");

            var localmd5 = GetMD5ResultsForLocalFolder(folderToDeploy);
            var cloudmd5 = GetMD5ResultsForCloudContainer(cloudFilesProvider, containerName);

            
            var toUpload = GetFilesToUpload(localmd5, cloudmd5);


            Console.WriteLine("Detected {0} new or changed items", toUpload.Length);
            foreach (var item in toUpload)
            {
                UploadObject(cloudFilesProvider, containerName, item);
            } 
            Console.WriteLine("Uploaded {0} items", toUpload.Length);
        }

        private static void UploadObject(CloudFilesProvider provider, string containerName, MD5Result item)
        {
            if (Directory.Exists(item.LocalPath))
                provider.CreateObject(containerName, new MemoryStream(), item.CloudPath, "application/directory");
            else
                provider.CreateObjectFromFile(containerName, item.LocalPath, item.CloudPath);
        }

     
        private static MD5Result[] GetFilesToUpload(MD5Result[] localResults, MD5Result[] cloudResults)
        {
            // is hash different and not blank
            List<MD5Result> results = new List<MD5Result>();
            var toUpdate = localResults.Where(x => !cloudResults.Contains(x));
            // combine and away we go!
            results.AddRange(toUpdate);
            return results.ToArray();
        }

        private static MD5Result[] GetMD5ResultsForLocalFolder(string path, string basePath = null)
        {
            var baseFolder = basePath;
            if (string.IsNullOrEmpty(basePath))
                baseFolder = path;

            List<MD5Result> resultsForThisCall = new List<MD5Result>();
            

            var files = Directory.GetFiles(path);
            foreach (var file in files)
            {

                var cloudPath = file.Substring(baseFolder.Length);
                cloudPath = cloudPath.Trim(@"\".ToCharArray());
                cloudPath = cloudPath.Replace(@"\", "/");

                var result = new MD5Result() { MD5 = GetMD5ForFile(file), CloudPath = cloudPath, LocalPath = file };
                resultsForThisCall.Add(result);
            }

            foreach (var folder in Directory.GetDirectories(path))
            {
                var cloudPath = folder.Substring(baseFolder.Length);
                cloudPath = cloudPath.Trim(@"\".ToCharArray());
                cloudPath = cloudPath.Replace(@"\", "/");

                var result = new MD5Result() { MD5 = GetMD5ForStream(new MemoryStream()), CloudPath = cloudPath, LocalPath = folder };
                resultsForThisCall.Add(result);


                resultsForThisCall.AddRange(GetMD5ResultsForLocalFolder(folder, baseFolder));
            }

            return resultsForThisCall.OrderBy(x=>x.CloudPath).ToArray();
        }

        private static MD5Result[] GetMD5ResultsForCloudContainer(CloudFilesProvider provider, string containerName)
        {
            List<MD5Result> results = new List<MD5Result>();
            var cloudObjects = provider.ListObjects(containerName);
            foreach (var cloudObject in cloudObjects)
            {
                var result = new MD5Result() { MD5 = cloudObject.Hash, CloudPath = cloudObject.Name, LocalPath = null };
                results.Add(result);
            }

            return results.OrderBy(x=>x.CloudPath).ToArray();
        }

        private static string GetMD5ForFile(string path)
        {
            using (var md5 = MD5.Create())
            {
                using (var stream = File.OpenRead(path))
                {
                    return BitConverter.ToString(md5.ComputeHash(stream)).Replace("-", "").ToLower();
                }
            }
        }
        private static string GetMD5ForStream(Stream stream)
        {
            using (var md5 = MD5.Create())
            {
                    return BitConverter.ToString(md5.ComputeHash(stream)).Replace("-", "").ToLower();
            }
        }

    }
}
