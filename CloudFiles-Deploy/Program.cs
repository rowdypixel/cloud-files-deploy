using net.openstack.Core.Domain;
using net.openstack.Providers.Rackspace;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CloudFiles_Deploy
{
    class Program
    {
        static void Main(string[] args)
        {
            if(args.Length < 4)
            {
                Console.WriteLine("Usage: CloudFiles-Deploy.exe ContainerName \"C:\\Folder name\" username apikey");
                return;
             }

            string containerName = args[0];
            string folderToDeploy = args[1];
            var username = args[2];
            string apiKey = args[3];

            SmartRackspaceUpload.SmartUploadFolderToRackspace(username, apiKey, containerName, folderToDeploy);

           // UploadFolderToRackspace(username, apiKey, containerName, folderToDeploy);

            Console.WriteLine("Done");
#if DEBUG
            Console.Read();
#endif
        }

        public static void UploadFolderToRackspace(string username, string apiKey, string containerName, string folderToDeploy)
        {    
            var cloudIdentity = new CloudIdentity() { Username = username, APIKey = apiKey };
            var cloudFilesProvider = new CloudFilesProvider(cloudIdentity);
            Console.WriteLine("Preparing to deploy {0}", folderToDeploy );
            Console.WriteLine("\t deploy to {0}", containerName);
            UploadFilesInFolder(cloudFilesProvider, containerName, folderToDeploy);
        }

        private static void UploadFilesInFolder(CloudFilesProvider provider, string containerName, string path, string basePath = null)
        {
            var baseFolder = basePath;
            if(string.IsNullOrEmpty(basePath))
                baseFolder = path;

            var files = Directory.GetFiles(path);
            foreach (var file in files)
            {

                var cloudPath = file.Substring(baseFolder.Length); 
                cloudPath = cloudPath.Trim(@"\".ToCharArray());
                cloudPath = cloudPath.Replace(@"\", "/");

                var objects = provider.ListObjects(containerName);

                 provider.CreateObjectFromFile(containerName, file, cloudPath);
                
                 Console.WriteLine("\t Deployed {0}", file);
            }

            foreach(var folder in Directory.GetDirectories(path))
            {
                Console.WriteLine("\t Creating folder {0}", folder);

                var cloudPath = folder.Substring(baseFolder.Length); 
                cloudPath = cloudPath.Trim(@"\".ToCharArray());
                cloudPath = cloudPath.Replace(@"\", "/");

                provider.CreateObject(containerName, new MemoryStream(), cloudPath, "application/directory");
                UploadFilesInFolder(provider, containerName, folder, baseFolder);
            }
        }
    }
}
