using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Script.Serialization;
using System.IO;
using System.Collections.Concurrent;

namespace Interaction.Models
{
    public class VoteModel
    {
        public enum TopicCategory { Default };

        public enum TopicStatus { Pending, Active, Deprecated };

        public enum CommentStatus { Active, Deprecated, Deleted };

        public static readonly string VoteTopicTableName = "VoteTopics";

        public static readonly string VoteOptionTableName = "VoteOptions";

        public static readonly string VoteShareLogTableName = "VoteShareLogs";

        public static readonly string VoteCommentTableName = "VoteComments";

        // All letters in container name must be lowercase
        public static readonly string VoteTopicBkgPicContainerName = "votetopicbkgpic";

        public static readonly string VoteTrafficLogTableName = "VoteTrafficLog";

        public void InsertVoteTopicEntity(VoteTopicEntity entity)
        {
            StorageModel.GetTable(VoteModel.VoteTopicTableName)
                        .Execute(TableOperation.Insert(entity));
        }

        public void InsertVoteOptionEntity(VoteOptionEntity entity)
        {
            StorageModel.GetTable(VoteModel.VoteOptionTableName)
                        .Execute(TableOperation.Insert(entity));
        }

        public void InsertVoteShareLogEntity(VoteShareLogEntity entity)
        {
            StorageModel.GetTable(VoteModel.VoteShareLogTableName)
                        .Execute(TableOperation.Insert(entity));
        }

        public void InsertVoteComment(VoteCommentEntity entity)
        {
            StorageModel.GetTable(VoteModel.VoteCommentTableName)
                        .Execute(TableOperation.Insert(entity));
        }

        public string UploadVoteTopicBkgPic(Stream srcFileStream, string blobfileName)
        {
            using (srcFileStream)
            {
                StorageModel.GetBlobContainer(VoteModel.VoteTopicBkgPicContainerName)
                            .GetBlockBlobReference(blobfileName)
                            .UploadFromStream(srcFileStream);
            }

            return Path.Combine(StorageModel.GetBlobEndPoint(), VoteModel.VoteTopicBkgPicContainerName, blobfileName);
        }

        public void UpdateVoteShareLogEntity(VoteShareLogEntity entity)
        {
            StorageModel.GetTable(VoteModel.VoteShareLogTableName)
                        .Execute(TableOperation.Replace(entity));
        }

        public void UpdateVoteComment(VoteCommentEntity entity)
        {
            StorageModel.GetTable(VoteModel.VoteCommentTableName)
                        .Execute(TableOperation.Replace(entity));
        }

        public void DeleteVoteTopicEntity(VoteTopicEntity entity)
        {
            StorageModel.GetTable(VoteModel.VoteTopicTableName)
                        .Execute(TableOperation.Delete(entity));
        }

        public void DeleteVoteOptionEntity(VoteOptionEntity entity)
        {
            StorageModel.GetTable(VoteModel.VoteOptionTableName)
                        .Execute(TableOperation.Delete(entity));
        }

        public void DeleteVoteShareLogEntity(VoteShareLogEntity entity)
        {
            StorageModel.GetTable(VoteModel.VoteShareLogTableName)
                        .Execute(TableOperation.Delete(entity));
        }

        public void DeleteVoteComment(VoteCommentEntity entity)
        {
            StorageModel.GetTable(VoteModel.VoteCommentTableName)
                        .Execute(TableOperation.Delete(entity));
        }

        public VoteShareLogEntity GetVoteShareLogEntity(string partitionKey, string rowKey)
        {
            TableResult res = StorageModel.GetTable(VoteModel.VoteShareLogTableName)
                                          .Execute(TableOperation.Retrieve<VoteShareLogEntity>(partitionKey, rowKey));

            return (null != res.Result) ? (VoteShareLogEntity)res.Result : null;
        }

        public List<VoteShareLogEntity> GetVoteShareLogEntityByPartition(string pKey)
        {
            TableQuery<VoteShareLogEntity> query = new TableQuery<VoteShareLogEntity>()
                .Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, pKey));

            return StorageModel.GetTable(VoteModel.VoteShareLogTableName)
                               .ExecuteQuery(query).ToList<VoteShareLogEntity>();
        }

        public List<VoteTopic> GetTopicsByCategory(TopicCategory category)
        {
            List<VoteTopic> topics = new List<VoteTopic>();

            // Retrieve the topic/option entities and create the topic instances
            List<VoteTopicEntity> topicEntities = StorageModel.GetTable(VoteTopicTableName)
                .ExecuteQuery(new TableQuery<VoteTopicEntity>()
                .Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, category.ToString())))
                .ToList<VoteTopicEntity>();
            CloudTable optionTbl = StorageModel.GetTable(VoteOptionTableName);
            foreach (VoteTopicEntity topicEntity in topicEntities)
            {
                // Create the topic instance, and set the topic entity, option entities
                VoteTopic topic = new VoteTopic(topicEntity);
                topics.Add(topic);
            }

            topics.Sort(delegate(VoteTopic vta, VoteTopic vtb) 
            {
                if (vta.TopicEntity.Status.Equals("Active"))
                {
                    if (vtb.TopicEntity.Status.Equals("Active"))
                        return vtb.TopicId.CompareTo(vta.TopicId);
                    else
                        return -1;
                }
                else if (vta.TopicEntity.Status.Equals("Pending"))
                {
                    if (vtb.TopicEntity.Status.Equals("Active"))
                        return 1;
                    else if (vtb.TopicEntity.Status.Equals("Pending"))
                        return vtb.TopicId.CompareTo(vta.TopicId);
                    else if (vtb.TopicEntity.Status.Equals("Deprecated"))
                        return -1;
                }
                else if (vta.TopicEntity.Status.Equals("Deprecated"))
                {
                    if (vtb.TopicEntity.Status.Equals("Deprecated"))
                        return vtb.TopicId.CompareTo(vta.TopicId);
                    else
                        return 1;
                }
                return 0;
            });

            return topics;
        }   

        // Check cookie to get the voted option
        // Return : -1, never voted; >=0, voted option
        public int GetVotedOption(string topicId)
        {
            int voteOption = -1;
            HttpCookie cookie = HttpContext.Current.Request.Cookies["TopicOpt_" + topicId];
            
            if (null != cookie)
            { 
                voteOption = int.Parse(cookie.Value);
            }
            return voteOption;
        }

        public bool IsVoted(string topicId)
        {
            return GetVotedOption(topicId) < 0 ? false : true;
        }

        public string CreateTopicId()
        {
            return "T" + DateTime.UtcNow.ToString("yyyyMMddHHmmssfff")
                + "R" + (new Random()).Next(999999).ToString("000000");
        }

        public string CreateCommentId()
        {
            return "C" + DateTime.UtcNow.ToString("yyyyMMddHHmmssfff")
                + "R" + (new Random()).Next(999999).ToString("000000"); 
        }

        // Row key for share log, make sure it's unique.
        public string CreateNewShareLogRowKey()
        {
            return "T" + DateTime.UtcNow.ToString("yyyyMMddHHmmssfff")
                + "R" + (new Random()).Next(999999).ToString("000000");
        }

        public string CapsuleQueryies(List<string> queries)
        {
            string splitStr = "$", qstr = "";

            foreach (string q in queries)
            {
                qstr += splitStr + q;
            }

            return qstr.TrimStart(new char[] { '$' });
        }

        public string CapsuleRelatedNews(List<VoteRelatedNews> rnList)
        {
            string relatedNewsStr = null;

            if (null != rnList)
            {
                relatedNewsStr = (new JavaScriptSerializer()).Serialize(rnList);
            }

            return relatedNewsStr;
        }

        public int GetRandomOptionCount(int mean, int variant)
        {
            return new Random().Next(mean - variant, mean + variant + 1);
        }

        public string GetShareQuery(VoteTopic topic)
        {
            string query = string.Empty;

            if (topic.TriggerQueries != null && topic.TriggerQueries.Count > 0)
            {
                query = HttpUtility.UrlEncode(topic.TriggerQueries[0]);
            }

            return query;
        }

        public string GetShareUrl(VoteTopic topic)
        {
            string url = null;

            if (topic.TopicEntity.HasDetailPage)
            {
                url = string.Format("http://{0}/Vote/Detail?tid={1}&mkt={2}", Util.GetSiteHost(), topic.TopicId, topic.TopicEntity.Market);
            }
            else
            {
                string shareQuery = GetShareQuery(topic);
                if (string.Empty != shareQuery)
                {
                    url = string.Format("http://www.bing.com/search?q={0}&setmkt={1}&form=intact", shareQuery, topic.TopicEntity.Market);
                }
                else
                {
                    url = "http://www.bing.com";
                }
            }
            return url;
        }

        public string GetShareImage(VoteTopic topic)
        {
            string imageUrl = null;

            switch (topic.TopicId)
            {
                case "T20141112033703638R916024":
                    imageUrl = "https://interaction.blob.core.chinacloudapi.cn/voteimages/T20141112033703638R916024_ShareIcon.png";
                    break;

                case "T20141028051724239R851451":
                    imageUrl = "https://interaction.blob.core.chinacloudapi.cn/voteimages/T20141028051724239R851451_ShareIcon.png";
                    break;

                default:
                    imageUrl = topic.HasDetailPage ? topic.PictureLink : "https://interaction.chinacloudsites.cn/images/vote/votepic.jpg";
                    break;
            }

            return imageUrl;
        }

        public string GetSinaweiboV(VoteTopic topic)
        {
            string weibov = "";

            switch (topic.TopicId)
            {
                case "T20141104081642663R018335": 
                    weibov += " @鹿晗全球粉丝后援会 @饭团-鹿晗 ";
                    break;

                case "T20141030064506784R210553":
                    weibov += " @TFBOYS粉丝应援站 @TFBOYS官方后援会 ";
                    break;

                default : break;
            }

            return weibov;
        }

        public string GetDefaultTopicId(string topicCat)
        {
            string topicid = null;

            try
            {
                TableQuery<DynamicTableEntity> projectionQuery = new TableQuery<DynamicTableEntity>()
                    .Where(TableQuery.CombineFilters(
                           TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, topicCat),
                           TableOperators.And,
                           TableQuery.GenerateFilterCondition("Status", QueryComparisons.Equal, VoteModel.TopicStatus.Active.ToString())))
                    .Select(new string[] { "RowKey" });
                EntityResolver<string> resolver = (pk, rk, ts, props, etag) => rk;
                List<string> res = StorageModel.GetTable(VoteModel.VoteTopicTableName)
                                               .ExecuteQuery(projectionQuery, resolver, null, null)
                                               .ToList();
                if (null != res && res.Count > 0)
                {
                    topicid = res.OrderByDescending(e => e).First();
                }
            }
            catch 
            { 
                return null; 
            }

            return topicid;
        }

        public string GetAnswerUrl(string tid, string form)
        {
            return string.Format("http://{0}/Vote/Answer?tid={1}&form={2}", Util.GetSiteHost(), tid, form);
        }

        public Dictionary<string, string> SetAnswerLabelLang(string market)
        {
            return Util.LabeLang.ContainsKey(market.ToLower()) ? Util.LabeLang[market.ToLower()] : Util.LabeLang["zh-cn"];
        }
    }

    public class VoteTopicEntity : TableEntity
    {
        public VoteTopicEntity(string category, string topicID)
        {
            this.PartitionKey = category;
            this.RowKey = topicID;
        }

        public VoteTopicEntity() { }

        public string TopicTitle { get; set; }

        public string Creator { get; set; }

        public string Status { get; set; }

        public string Background { get; set; }

        public string RelatedNewsStr { get; set; }

        public string PictureName { get; set; }

        public bool HasDetailPage { get; set; }

        // JSON
        public string TriggerQueries { get; set; }

        public long PVRequestCount { get; set; }

        public long VotableRequestCount { get; set; }

        public long TotalVoteCount { get; set; }

        public string Market { get; set; }

        //public DateTime StartDate { get; set; }
        //public DateTime EndDate { get; set; }
    }

    public class VoteOptionEntity : TableEntity
    {
        public string OptionText { get; set; }

        public long VoteCount { get; set; }

        public VoteOptionEntity() { }

        public VoteOptionEntity(string topicId, string optPos)
        {
            this.PartitionKey = topicId;
            this.RowKey = optPos;
        }
    }

    public class VoteShareLogEntity : TableEntity
    {
        public VoteShareLogEntity() { }

        public VoteShareLogEntity(string topicId, string rowKey)
        {
            this.PartitionKey = topicId;
            this.RowKey = rowKey;
        }

        public string UserId { get; set; }

        public string Source { get; set; }

        public string OptionPos { get; set; }
    }

    public class VoteCommentEntity : TableEntity
    {
        public VoteCommentEntity() { }

        public VoteCommentEntity(string topicId, string commentId)
        {
            this.PartitionKey = topicId;
            this.RowKey = commentId;   
        }

        public string CommentStatus { get; set; }

        public string Creator { get; set; }

        public string CommentText { get; set; }

        public long PraiseCount { get; set; }
    }

    public class VoteRelatedNews
    {
        public string Title { get; set; }

        public string Url { get; set; }
    }

    public class VoteTopic
    {
        public string TopicCategory { get; set; }

        public string TopicId { get; set; }

        public VoteTopicEntity TopicEntity { get; set; }

        // Schema : <OptionPosition, OptionEntity>
        public Dictionary<int, VoteOptionEntity> Options { get; set; }

        public List<string> TriggerQueries { get; set; }

        public bool HasDetailPage { get; set; }

        public string Background { get; set; }

        public List<VoteRelatedNews> RelatedNewsList { get; set; }

        public string PictureLink { get; set; }

        private static readonly Object Locker = new Object();

        private static ConcurrentDictionary<string, long> VoteCount = new ConcurrentDictionary<string, long>();

        public List<VoteComment> Comments { get; set; }

        public VoteTopic() { }

        public VoteTopic(VoteModel.TopicCategory topicCat, string topicId)
        {
            TopicCategory = topicCat.ToString();
            TopicId = topicId;
            TopicEntity = GetVoteTopicEntity(TopicCategory, TopicId);
            Options = GetVoteOptionEntities(TopicId);
            TriggerQueries = GetTriggerQueries(TopicEntity.TriggerQueries);
            HasDetailPage = TopicEntity.HasDetailPage;
            Background = TopicEntity.Background;
            RelatedNewsList = GetRelatedNews(TopicEntity.RelatedNewsStr);
            PictureLink = GetTopicPictureLink(TopicEntity.PictureName);
            Comments = GetVoteComments(TopicId);
        }

        public VoteTopic(VoteTopicEntity topicEntity)
        {
            TopicCategory = topicEntity.PartitionKey;
            TopicId = topicEntity.RowKey;
            TopicEntity = topicEntity;
            Options = GetVoteOptionEntities(TopicId);
            TriggerQueries = GetTriggerQueries(TopicEntity.TriggerQueries);
            HasDetailPage = TopicEntity.HasDetailPage;
            Background = TopicEntity.Background;
            RelatedNewsList = GetRelatedNews(TopicEntity.RelatedNewsStr);         
            PictureLink = GetTopicPictureLink(TopicEntity.PictureName);
            Comments = GetVoteComments(TopicId);
        }

        public VoteTopicEntity GetVoteTopicEntity(string partitionKey, string rowKey)
        {
            TableResult res = StorageModel.GetTable(VoteModel.VoteTopicTableName)
                                           .Execute(TableOperation.Retrieve<VoteTopicEntity>(partitionKey, rowKey));

            VoteTopicEntity entity = (null != res.Result) ? (VoteTopicEntity)res.Result : null;

            // Set default value for the newly added null value field
            entity.Market = entity.Market == null ? "zh-cn" : entity.Market;

            return entity;
        }

        public VoteOptionEntity GetVoteOptionEntity(string partitionKey, string rowKey)
        {
            TableResult res = StorageModel.GetTable(VoteModel.VoteOptionTableName)
                                          .Execute(TableOperation.Retrieve<VoteOptionEntity>(partitionKey, rowKey));

            return (null != res.Result) ? (VoteOptionEntity)res.Result : null;
        }

        public Dictionary<int, VoteOptionEntity> GetVoteOptionEntities(string topicId)
        {
            TableQuery<VoteOptionEntity> query = new TableQuery<VoteOptionEntity>()
                .Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, topicId));

            List<VoteOptionEntity> optionEntities = StorageModel.GetTable(VoteModel.VoteOptionTableName)
                                                                 .ExecuteQuery(query).ToList<VoteOptionEntity>();

            Dictionary<int, VoteOptionEntity> options = new Dictionary<int, VoteOptionEntity>();
            foreach (VoteOptionEntity voe in optionEntities)
            {
                int optPos = int.Parse(voe.RowKey);
                if (!options.ContainsKey(optPos))
                {
                    // Get vote count from cache
                    string key = GetVoteCountCacheKey(topicId, optPos.ToString());
                    if (VoteCount.ContainsKey(key))
                    {
                        voe.VoteCount = VoteCount[key];
                    }

                    options.Add(optPos, voe);
                }
            }

            return options;
        }

        private string GetVoteCountCacheKey(string topicId, string optPos)
        {
            return topicId + "#" + optPos;
        }

        public void UpdateVoteTopicEntity(VoteTopicEntity entity)
        {
            lock (Locker)
            {
                StorageModel.GetTable(VoteModel.VoteTopicTableName)
                            .Execute(TableOperation.Replace(entity));
            }
        }

        public void UpdateVoteOptionEntity(VoteOptionEntity entity)
        {
            lock (Locker)
            {
                StorageModel.GetTable(VoteModel.VoteOptionTableName)
                            .Execute(TableOperation.Replace(entity));
            }
        }

        public List<string> GetTriggerQueries(string qstr)
        {
            return qstr.Split(new char[] { '$' }, StringSplitOptions.RemoveEmptyEntries).ToList<string>();
        }

        public List<VoteRelatedNews> GetRelatedNews(string rnews)
        {
            List<VoteRelatedNews> relatedNewsList = null;

            if (null != rnews)
            {
                relatedNewsList = (new JavaScriptSerializer()).Deserialize<List<VoteRelatedNews>>(rnews);
            }

            return relatedNewsList;
        }

        public static List<VoteComment> GetVoteComments(string topicId, string upperCommentId = null)
        {
            string filter = null;

            if (null != upperCommentId)
            {
                filter = TableQuery.CombineFilters(
                         TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, topicId)
                         , TableOperators.And
                         , TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.LessThan, upperCommentId));
            }
            else
            {
                filter = TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, topicId);
            }

            TableQuery<VoteCommentEntity> query = new TableQuery<VoteCommentEntity>().Where(filter);
            List<VoteCommentEntity> vceList = StorageModel.GetTable(VoteModel.VoteCommentTableName)
                                                          .ExecuteQuery(query).ToList<VoteCommentEntity>();
            List<VoteCommentEntity> topVCEList = vceList.OrderByDescending(e => e.Timestamp).Take(5).ToList();

            List<VoteComment> comments = new List<VoteComment>();

            foreach(VoteCommentEntity c in topVCEList)
            {
                comments.Add(new VoteComment(c));
            }
          
            return comments;
        }

        public string GetTopicPictureLink(string topicPicName)
        {
            string link = "";

            if (!string.IsNullOrWhiteSpace(topicPicName))
            {
                link = Path.Combine(StorageModel.GetBlobEndPoint(), VoteModel.VoteTopicBkgPicContainerName, topicPicName).Replace(@"\", "/");
            }

            return link;
        }

        public void IncreasePageView(bool canVote)
        {
            if (null != TopicEntity)
            {
                lock(Locker)
                {
                    TableQuery<DynamicTableEntity> projectionQuery = new TableQuery<DynamicTableEntity>()
                        .Where(TableQuery.CombineFilters(
                               TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, TopicCategory),
                               TableOperators.And,
                               TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, TopicId)))
                        .Select(new string[] { "PVRequestCount", "VotableRequestCount" });
                    EntityResolver<MonitorProperties> resolver = (pk, rk, ts, props, etag)
                                                                  => props.ContainsKey("PVRequestCount") && props.ContainsKey("VotableRequestCount")
                                                                     ? new MonitorProperties((long)props["PVRequestCount"].Int64Value,
                                                                                             (long)props["VotableRequestCount"].Int64Value)
                                                                     : null;

                    List<MonitorProperties> res = StorageModel.GetTable(VoteModel.VoteTopicTableName)
                                                              .ExecuteQuery(projectionQuery, resolver, null, null).ToList();
                    if (res.Count > 0 && null != res.First())
                    {
                        TopicEntity.PVRequestCount = ++res.First().PVRequestCount;
                        if (canVote)
                        {
                            TopicEntity.VotableRequestCount = ++res.First().VotableRequestCount;
                        }
                        UpdateVoteTopicEntity(TopicEntity);
                    }
                }          
            }
        }

        public void IncreaseVoteCount(int option)
        {
            TableQuery<DynamicTableEntity> projectionQuery = null;
            EntityResolver<long> resolver = null;
            List<long> res = null;

            // Deal with vote action, update the vote count     
            if (Options.ContainsKey(option) && null != Options[option] && null != TopicEntity)
            {
                lock (Locker)
                { 
                    // Update option's vote count
                    projectionQuery = new TableQuery<DynamicTableEntity>()
                        .Where(TableQuery.CombineFilters(
                               TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, TopicId),
                               TableOperators.And,
                               TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, option.ToString())))
                        .Select(new string[] { "VoteCount" });
                    resolver = (pk, rk, ts, props, etag) => props.ContainsKey("VoteCount") ? (long)props["VoteCount"].Int64Value : -1;
                    res = StorageModel.GetTable(VoteModel.VoteOptionTableName)
                                      .ExecuteQuery(projectionQuery, resolver, null, null).ToList();
                    if (res.Count > 0 && res.First() >= 0)
                    {
                        Options[option].VoteCount = (res.First() + 1);
                        UpdateVoteOptionEntity(Options[option]);
                    }

                    // Update total vote count
                    projectionQuery = new TableQuery<DynamicTableEntity>()
                        .Where(TableQuery.CombineFilters(
                               TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, TopicCategory),
                               TableOperators.And,
                               TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, TopicId)))
                        .Select(new string[] { "TotalVoteCount" });
                    resolver = (pk, rk, ts, props, etag) => props.ContainsKey("TotalVoteCount") ? (long)props["TotalVoteCount"].Int64Value : -1;
                    res = StorageModel.GetTable(VoteModel.VoteTopicTableName)
                                      .ExecuteQuery(projectionQuery, resolver, null, null).ToList();
                    if (res.Count > 0 && res.First() >= 0)
                    {
                        TopicEntity.TotalVoteCount = (res.First() + 1);
                        UpdateVoteTopicEntity(TopicEntity);
                    }
                }

                // Write cookie to tag the topic has been voted
                WriteVotedOptionInCookie(TopicId, option);
            }
        }

        public void IncreaseVoteCountByCache(int option)
        {
            // Deal with vote action, update the vote count     
            if (Options.ContainsKey(option) && null != Options[option] && null != TopicEntity)
            {
                lock (Locker)
                {
                    string key = GetVoteCountCacheKey(TopicId, option.ToString());
                    
                    if (VoteCount.ContainsKey(key))
                    {
                        Options[option].VoteCount = ++VoteCount[key];
                    }
                    else
                    {
                        TableQuery<DynamicTableEntity> projectionQuery = null;
                        EntityResolver<long> resolver = null;
                        List<long> res = null;

                        projectionQuery = new TableQuery<DynamicTableEntity>()
                            .Where(TableQuery.CombineFilters(
                                   TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, TopicId),
                                   TableOperators.And,
                                   TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, option.ToString())))
                            .Select(new string[] { "VoteCount" });
                        resolver = (pk, rk, ts, props, etag) => props.ContainsKey("VoteCount") ? (long)props["VoteCount"].Int64Value : -1;
                        res = StorageModel.GetTable(VoteModel.VoteOptionTableName)
                                          .ExecuteQuery(projectionQuery, resolver, null, null).ToList();
                        if (res.Count > 0 && res.First() >= 0)
                        {
                            Options[option].VoteCount = (res.First() + 1);
                            VoteCount.TryAdd(key, Options[option].VoteCount);
                        }
                    }

                    if (Options[option].VoteCount % 20 == 0)
                    {
                        UpdateVoteOptionEntity(Options[option]);
                    }
                }

                // Write cookie to tag the topic has been voted
                WriteVotedOptionInCookie(TopicId, option);
            }          
        }

        private void WriteVotedOptionInCookie(string topicId, int option)
        {
            HttpCookie cookie = new HttpCookie("TopicOpt_" + topicId, option.ToString());
            cookie.Expires = DateTime.Now.AddMinutes(10);
            HttpContext.Current.Response.Cookies.Add(cookie);
        }
    }

    public class VoteComment
    {
        public VoteCommentEntity CommentEntity { get; set; }

        public string TopicId { get; set; }

        public string CommentId { get; set; }

        public string CommentStatus { get; set; }

        public string Creator { get; set; }

        public string CommentText { get; set; }

        public long PraiseCount { get; set; }

        public DateTime CommentTime { get; set; }

        public string CommentTimeStr { get; set; }

        public VoteComment(VoteCommentEntity commentEntity)
        {
            CommentEntity = commentEntity;
            TopicId = commentEntity.PartitionKey;
            CommentId = commentEntity.RowKey;
            CommentStatus = commentEntity.CommentStatus;
            Creator = commentEntity.Creator;
            CommentText = commentEntity.CommentText;
            PraiseCount = commentEntity.PraiseCount;
            CommentTime = commentEntity.Timestamp.LocalDateTime;
            CommentTimeStr = GetCommentTimeStr(CommentTime, (new VoteTopic()).GetVoteTopicEntity(VoteModel.TopicCategory.Default.ToString(), TopicId).Market);
        }

        public string GetCommentTimeStr(DateTime time, string market)
        {
            string timestr = "";
            TimeSpan timediff = DateTime.Now - time;
            if (timediff.Minutes < 1)
            {
                timestr = Util.LabeLang[market]["LabelJustnow"];
            }
            else if (timediff.Hours < 1)
            {
                timestr = timediff.Minutes + " " + Util.LabeLang[market]["LabelMinute"];
            }
            else if (timediff.Days < 1)
            {
                timestr = timediff.Hours + " " + Util.LabeLang[market]["LabelHour"];
            }
            else if (timediff.Days < 7)
            {
                timestr = timediff.Days + " " + Util.LabeLang[market]["LabelDay"];
            }
            else
            {
                timestr = time.ToString("yyyy-MM-dd");
            }

            return timestr;
        }
    }

    public class VoteAnswerData 
    { 
        public string TopicId { get; set; }

        public int OptionsCount { get; set; }

        public string TriggerQueries { get; set; }

        public string Status { get; set; }

        public string Url { get; set; }

        public string Market { get; set; }
    }

    public class MonitorProperties
    {
        public long PVRequestCount { get; set; }

        public long VotableRequestCount { get; set; }

        public MonitorProperties(long pv, long votableCount)
        {
            PVRequestCount = pv;
            VotableRequestCount = votableCount;
        }
    }
}
