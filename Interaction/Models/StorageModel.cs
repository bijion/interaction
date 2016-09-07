using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using Microsoft.WindowsAzure.Storage.Blob;
using System.Collections.Generic;

namespace Interaction.Models
{
    public class StorageModel
    {        
        private static CloudStorageAccount StorageAccount { get; set; }

        private static CloudTableClient TableClient { get; set; }

        private static CloudBlobClient BlobClient { get; set; }

        private static Dictionary<string, CloudTable> CloudTableCache = new Dictionary<string, CloudTable>();

        private static Dictionary<string, CloudBlobContainer> CloudBlobContainerCache = new Dictionary<string, CloudBlobContainer>();

        static StorageModel() 
        {
            // Retrieve the storage account from the connection string.
            StorageAccount = CloudStorageAccount.Parse(CloudConfigurationManager
                                                .GetSetting(CloudConfigurationManager
                                                .GetSetting("StorageConnectionSelector")));
            // Create the table/blob client.
            TableClient = StorageAccount.CreateCloudTableClient();
            BlobClient = StorageAccount.CreateCloudBlobClient();
        }

        public static CloudTable GetTable(string tableName)
        {
            CloudTable table = null;

            if (CloudTableCache.ContainsKey(tableName))
            {
                table = CloudTableCache[tableName];
            }
            else 
            {         
                table = TableClient.GetTableReference(tableName);

                // Create the table if it doesn't exist.
                table.CreateIfNotExists();

                CloudTableCache.Add(tableName, table);
            }

            return table;
        }

        public static CloudBlobContainer GetBlobContainer(string containerName)
        {
            CloudBlobContainer container = null;

            if (CloudBlobContainerCache.ContainsKey(containerName))
            {
                container = CloudBlobContainerCache[containerName];
            }
            else
            {
                container = BlobClient.GetContainerReference(containerName);

                // Create the table if it doesn't exist.
                container.CreateIfNotExists();

                container.SetPermissions(new BlobContainerPermissions
                {
                    PublicAccess = BlobContainerPublicAccessType.Blob
                });

                CloudBlobContainerCache.Add(containerName, container);
            }

            return container;
        }

        public static string GetBlobEndPoint()
        {
            return StorageAccount.BlobEndpoint.ToString();
        }
    }
}
