using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Interaction.Models
{
    public class BingDictModel:BaseModel
    {
        public string ClientInfo { get; set; }

        public static readonly string AcademicFeedbackTableName = "BingDictFeedback";

        public static readonly string AcademicFeedbackClipPicContainerName = "bingdictfeedbackpic";

        public void InsertFeedback(DictFeedsEntity entity)
        {
            StorageModel.GetTable(BingDictModel.AcademicFeedbackTableName)
                        .Execute(TableOperation.Insert(entity));
        }

        public List<DictFeedsEntity> FetchAllFeedbackEntities()
        {
            CloudTable table = StorageModel.GetTable(BingDictModel.AcademicFeedbackTableName);
            TableContinuationToken token = null;
            List<DictFeedsEntity> entityList = new List<DictFeedsEntity>();

            do
            {
                var queryResult = table.ExecuteQuerySegmented(new TableQuery<DictFeedsEntity>(), token);
                entityList.AddRange(queryResult.Results);
                token = queryResult.ContinuationToken;
            } while (token != null);

            return entityList;
        }

        public string UploadPic(Stream srcFileStream, string blobfileName)
        {
            return base.UploadPic(srcFileStream, blobfileName, BingDictModel.AcademicFeedbackClipPicContainerName);
        }

        public string GetFeedbackPicLink(string picName)
        {
            return base.GetFeedbackPicLink(picName, BingDictModel.AcademicFeedbackClipPicContainerName);
        }
    }

    public class DictFeedsEntity : TableEntity
    {
        public DictFeedsEntity() { }

        public DictFeedsEntity(string CatId, string commentId)
        {
            this.PartitionKey = CatId;
            this.RowKey = commentId;
        }

        public string CommentStatus { get; set; }
        
        public string Contact { get; set; }

        public string Url { get; set; }

        public string CommentText { get; set; }

        public string ClientInfo { get; set; }
    }
}
