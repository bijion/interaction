using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Interaction.Models
{
    public class AcademicModel:BaseModel
    {
        public static readonly string AcademicFeedbackTableName = "AcademicFeedback";

        public static readonly string AcademicFeedbackClipPicContainerName = "academicfeedbackpic";

        public void InsertFeedback(FeedbackEntity entity)
        {            
            StorageModel.GetTable(AcademicModel.AcademicFeedbackTableName)
                        .Execute(TableOperation.Insert(entity));
        }

        public List<FeedbackEntity> FetchAllFeedbackEntities()
        {
            CloudTable table = StorageModel.GetTable(AcademicModel.AcademicFeedbackTableName);
            TableContinuationToken token = null;
            List<FeedbackEntity> entityList = new List<FeedbackEntity>();

            do
            {
                var queryResult = table.ExecuteQuerySegmented(new TableQuery<FeedbackEntity>(), token);
                entityList.AddRange(queryResult.Results);
                token = queryResult.ContinuationToken;
            } while (token != null);

            return entityList;
        }

        public string UploadPic(Stream srcFileStream, string blobfileName)
        {
            return base.UploadPic(srcFileStream, blobfileName, AcademicModel.AcademicFeedbackClipPicContainerName);
        }

        public string GetFeedbackPicLink(string picName)
        {
            return base.GetFeedbackPicLink(picName, AcademicModel.AcademicFeedbackClipPicContainerName);
        }
    }

    public class FeedbackEntity : TableEntity
    {
        public FeedbackEntity() { }

        public FeedbackEntity(string CatId, string commentId)
        {
            this.PartitionKey = CatId;
            this.RowKey = commentId;
        }

        public string CommentStatus { get; set; }
        
        public string Contact { get; set; }

        public string Url { get; set; }

        public string CommentText { get; set; }
    }
}
