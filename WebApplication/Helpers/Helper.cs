using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Configuration;
using Microsoft.Azure;
using WebApplication.Models;

namespace WebApplication.Helpers
{
    public class Helper
    {
        CloudBlobContainer blobContainer;
        CloudTable imagesTable = null;
        CloudStorageAccount storageAccount = CloudStorageAccount.Parse(CloudConfigurationManager.GetSetting("StorageConnectionString"));

        public Helper(string containerName)
        {
            if (string.IsNullOrEmpty(containerName))
            {
                throw new ArgumentNullException("ContainerName", "Container Name can't be empty");
            }
            if (containerName == "images")
            {
                GetBlobContainer(containerName);
            }
            else
            {
                GetCloudTable(containerName);
            }

        }

        CloudBlobContainer GetBlobContainer(string containerName)
        {
            try
            {
                CloudBlobClient cloudBlobClient = storageAccount.CreateCloudBlobClient();
                blobContainer = cloudBlobClient.GetContainerReference(containerName);

                if (blobContainer.CreateIfNotExists())
                {
                    blobContainer.SetPermissions(
                        new BlobContainerPermissions
                        {
                            PublicAccess = BlobContainerPublicAccessType.Blob
                        }
                    );
                }
            }
            catch (Exception e)
            {
                throw e;
            }
            return blobContainer;
        }

        CloudTable GetCloudTable(string containerName)
        {
            try
            {
                var tableClient = storageAccount.CreateCloudTableClient();
                imagesTable = tableClient.GetTableReference(containerName);
                if (!imagesTable.Exists())
                {
                    imagesTable.Create();

                }
            }
            catch (Exception e)
            {
                throw e;
            }
            return imagesTable;
        }

        public string UploadFile(HttpPostedFileBase fileToUpload, ImagesTableModel model)
        {
            string absoluteUri;
            if (fileToUpload == null || fileToUpload.ContentLength == 0)
                return null;
            try
            {
                string fileName = Path.GetFileName(fileToUpload.FileName);
                CloudBlockBlob blockBlob;
                blockBlob = blobContainer.GetBlockBlobReference(fileName);
                blockBlob.Properties.ContentType = fileToUpload.ContentType;
                blockBlob.UploadFromStream(fileToUpload.InputStream);

                absoluteUri = blockBlob.Uri.AbsoluteUri;
                SetMetaData(blockBlob, model.Name, model.Description, absoluteUri);
            }
            catch (Exception e)
            {
                throw e;
            }
            return absoluteUri;
        }

        void SetMetaData(CloudBlockBlob blockBlob, string fileName, string description, string absoluteUri)
        {
            blockBlob.Metadata.Clear();
            blockBlob.Metadata.Add("Name", fileName);
            blockBlob.Metadata.Add("Description", description);
            blockBlob.Metadata.Add("Url", absoluteUri);
            blockBlob.SetMetadata();
        }

        public List<string> BlobList()
        {
            List<string> blobList = new List<string>();
            foreach (IListBlobItem item in blobContainer.ListBlobs())
            {
                if (item.GetType() == typeof(CloudBlockBlob))
                {
                    CloudBlockBlob blobpage = (CloudBlockBlob)item;
                    blobList.Add(blobpage.Uri.AbsoluteUri.ToString());
                }
            }
            return blobList;
        }

        public bool DeleteBlob(string absoluteUri)
        {
            try
            {
                Uri uri = new Uri(absoluteUri);
                string blobName = Path.GetFileName(uri.LocalPath);

                CloudBlockBlob blockBlob = blobContainer.GetBlockBlobReference(blobName);

                blockBlob.Delete();
                return true;
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        public IEnumerable<ImageTableEntity> GetAllRecords()
        {
            var query = new TableQuery<ImageTableEntity>();

            var entities = imagesTable.ExecuteQuery(query);

            return entities;
        }

        public IEnumerable<ImageTableEntity> GetExistingPartitionKeys()
        {
            var query = new TableQuery<ImageTableEntity>()
            .Where(TableQuery.GenerateFilterCondition("PartitionKey",QueryComparisons.Equal,"Images"));

            var entities = imagesTable.ExecuteQuery(query);

            return entities;
        }

        public void CreateOrUpdate(ImageTableEntity entity)
        {
            var operation = TableOperation.InsertOrReplace(entity);

            imagesTable.Execute(operation);
        }

        public void Delete(ImageTableEntity entity)
        {
            var operation = TableOperation.Delete(entity);

            imagesTable.Execute(operation);
        }

        public ImageTableEntity Get(string partitionKey, string rowKey)
        {
            var operation = TableOperation.Retrieve<ImageTableEntity>(partitionKey, rowKey);

            var result = imagesTable.Execute(operation);

            return result.Result as ImageTableEntity;
        }
    }
    public class ImageTableEntity : TableEntity
        {
            public string Name { get; set; }
            public string URL { get; set; }
            public string Description { get; set; }
        }
}