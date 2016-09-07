using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Interaction.Models
{
    public class BaseModel
    {
        public string Event { get; set; }

        public string Query { get; set; }

        public string Contact { get; set; }
        
        public string Title { get; set; }

        public string CommentPlaceHolder { get; set; }

        public string ContactPlaceHolder { get; set; }

        public string StyleFileName { get; set; }        

        public string UploadPic(Stream srcFileStream, string blobfileName, string picContainerName)
        {
            using (srcFileStream)
            {
                StorageModel.GetBlobContainer(picContainerName)
                            .GetBlockBlobReference(blobfileName)
                            .UploadFromStream(srcFileStream);
            }

            return Path.Combine(StorageModel.GetBlobEndPoint(), picContainerName, blobfileName);
        }

        public string GetFeedbackPicLink(string picName, string picContainerName)
        {
            string link = "";

            if (!string.IsNullOrWhiteSpace(picName))
            {
                link = Path.Combine(StorageModel.GetBlobEndPoint(), picContainerName, picName).Replace(@"\", "/");
            }

            return link;
        }
    }
}
