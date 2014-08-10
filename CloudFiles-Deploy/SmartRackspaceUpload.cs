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
            Console.WriteLine("Preparing to deploy {0}", folderToDeploy);
            Console.WriteLine("\t deploy to {0}", containerName);
            var localmd5 = GetMD5ResultsForLocalFolder(folderToDeploy);
            var cloudmd5 = GetMD5ResultsForCloudContainer(cloudFilesProvider, containerName);
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

                var result = new MD5Result() { MD5 = GetMD5ForFile(file), Path = cloudPath };
                resultsForThisCall.Add(result);
            }

            foreach (var folder in Directory.GetDirectories(path))
            {
                Console.WriteLine("\t Creating folder {0}", folder);

                var cloudPath = folder.Substring(baseFolder.Length);
                cloudPath = cloudPath.Trim(@"\".ToCharArray());
                cloudPath = cloudPath.Replace(@"\", "/");

                var result = new MD5Result() { MD5 = "", Path = cloudPath };
                resultsForThisCall.Add(result);


                resultsForThisCall.AddRange(GetMD5ResultsForLocalFolder(folder, baseFolder));
            }

            return resultsForThisCall.OrderBy(x=>x.Path).ToArray();
        }

        private static MD5Result[] GetMD5ResultsForCloudContainer(CloudFilesProvider provider, string containerName)
        {
            List<MD5Result> results = new List<MD5Result>();
            var cloudObjects = provider.ListObjects(containerName);
            foreach (var cloudObject in cloudObjects)
            {
                var result = new MD5Result() { MD5 = cloudObject.Hash, Path = cloudObject.Name };
                results.Add(result);
            }

            return results.OrderBy(x=>x.Path).ToArray();
        }

    }
}
