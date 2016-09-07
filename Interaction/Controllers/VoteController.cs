using Interaction.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Web.Mvc;
using System.Web.Script.Serialization;
using WebMatrix.WebData;

namespace Interaction.Controllers
{
    public class VoteController : Controller
    {
        private static VoteModel Vote = new VoteModel();

        public string LogShare()
        {
            string topicid = string.IsNullOrWhiteSpace(Request["tid"]) ? "0" : Request["tid"];
            string source = string.IsNullOrWhiteSpace(Request["source"]) ? "unknown" : Request["source"];
            string optionPos = string.IsNullOrWhiteSpace(Request["opt"]) ? "-1" : Request["opt"];
            string userid = string.IsNullOrWhiteSpace(Request["uid"]) ? "unknown" : Request["uid"];

            if (!topicid.Equals("0"))
            {
                lock (Vote)
                {
                    string voteShareLogRowKey = Vote.CreateNewShareLogRowKey();
                    VoteShareLogEntity voteShareLogEntity = new VoteShareLogEntity(topicid, voteShareLogRowKey);
                    voteShareLogEntity.Source = source;
                    voteShareLogEntity.OptionPos = optionPos;
                    voteShareLogEntity.UserId = userid;
                    Vote.InsertVoteShareLogEntity(voteShareLogEntity);
                }
            }

            return "投票 " + topicid + " 在 " + source + " 被分享了！";
        }

        public ActionResult Monitor()
        {
            // Only the login user can see the monitor info
            if (!WebSecurity.IsAuthenticated)
            {
                return RedirectToAction("List", "Vote");
            }

            ViewBag.HasResult = false;

            string topicid = string.IsNullOrWhiteSpace(Request["tid"]) ? null : Request["tid"].Trim();
            if (!string.IsNullOrEmpty(topicid))
            {
                ViewBag.TopicId = topicid;
                List<VoteShareLogEntity> voteShareLogEntityList = Vote.GetVoteShareLogEntityByPartition(topicid);
                ViewBag.HasResult = voteShareLogEntityList.Count == 0 ? false : true;
                Dictionary<string, int> sourceMap = new Dictionary<string, int>();
                int totalShareCount = 0;
                foreach (VoteShareLogEntity voteShareLogEntity in voteShareLogEntityList)
                {
                    string source = voteShareLogEntity.Source;
                    if (!sourceMap.ContainsKey(source))
                    {
                        sourceMap.Add(source, 0);
                    }
                    sourceMap[source]++;
                    totalShareCount++;
                }
                ViewBag.SourceMap = sourceMap;
                ViewBag.TotalShareCount = totalShareCount;
            }

            return View();
        }

        public ActionResult List()
        {
            List<VoteTopic> topicList = Vote.GetTopicsByCategory(VoteModel.TopicCategory.Default);
            ViewBag.Debug = topicList.Count;
            if (topicList != null && topicList.Count > 0)
            {
                ViewBag.HasResult = true;
                ViewBag.VoteTopicList = topicList;
            }
            else
            {
                ViewBag.HasResult = false;
            }

            // Disable vote action.
            ViewBag.ShowVoteButton = false;

            ViewBag.ShowTopicTitleLink = true;

            string category = string.IsNullOrWhiteSpace(Request["cat"]) ? VoteModel.TopicCategory.Default.ToString() : Request["cat"];
            string topicid = string.IsNullOrWhiteSpace(Request["tid"]) ? "0" : Request["tid"];
            string action = string.IsNullOrWhiteSpace(Request["act"]) ? "" : Request["act"].Trim();

            // Only the logging account can deal with the action
            if (ViewBag.HasResult && WebSecurity.IsAuthenticated)
            {
                foreach (VoteTopic topic in topicList)
                {
                    if (topicid.Equals(topic.TopicEntity.RowKey) && category.Equals(topic.TopicEntity.PartitionKey))
                    {                 
                        switch (action)
                        {
                            case "del":
                                if (!topic.TopicEntity.Status.Equals(VoteModel.TopicStatus.Active.ToString()))
                                {
                                    // Delete topic entity from storage
                                    Vote.DeleteVoteTopicEntity(topic.TopicEntity);

                                    // Delete option entities from storage
                                    foreach (VoteOptionEntity optEntity in topic.Options.Values)
                                    {
                                        Vote.DeleteVoteOptionEntity(optEntity);
                                    }

                                    topicList.Remove(topic);
                                }
                                break;
                            
                            case "online" :
                                topic.TopicEntity.Status = VoteModel.TopicStatus.Active.ToString();
                                topic.UpdateVoteTopicEntity(topic.TopicEntity);
                                break;
                            
                            case "offline" :
                                topic.TopicEntity.Status = VoteModel.TopicStatus.Deprecated.ToString();
                                topic.UpdateVoteTopicEntity(topic.TopicEntity);
                                break;

                            // Take no action
                            default: break;
                        }
                        return RedirectToAction("List", "Vote");
                    }
                }
                ViewBag.VoteTopicList = topicList;
            }

            // Don't show author and query.
            ViewBag.ShowQuery = true;
            
            ViewBag.Formcode = "";

            ViewBag.LabelLang = Vote.SetAnswerLabelLang("");

            return View();
        }

        public ActionResult Search()
        {
            Answer(Request["tid"], Request["form"], Request["mkt"]);
            
            ViewBag.ShowVoteButton = false;

            return View();
        }

        // Return a fixed topic url for bing app card
        public JsonResult Random(string form = "")
        {
            Hashtable data = new Hashtable();
            try
            {
                string url = Vote.GetAnswerUrl(Vote.GetDefaultTopicId(VoteModel.TopicCategory.Default.ToString()), form);
                data.Add("url", url);
            }
            catch 
            { 
                return this.Json(new { }, JsonRequestBehavior.AllowGet); 
            }
            return this.Json(data, JsonRequestBehavior.AllowGet);          
        }

        public ActionResult Answer(string tid = "", string form = "", string mkt = "")
        {
            try 
            {
                ViewBag.PageCategory = "answer";

                ViewBag.HasResult = false;

                // Don't show trigger queries
                ViewBag.ShowQuery = false;

                // Disable share for bingapp
                ViewBag.EnableShare = (form == "bingapp" || mkt.ToLower() == "ja-jp") ? false : true;

                ViewBag.ShowVoteButton = true;

                ViewBag.Formcode = form;

                // Get a default topic id if the tid is empty
                if (string.IsNullOrWhiteSpace(tid))
                {
                    tid = Vote.GetDefaultTopicId(VoteModel.TopicCategory.Default.ToString());
                }

                // Get the fixed url for bing app
                if (form == "bingapp")
                {
                    ViewBag.BingAppRedirection = Vote.GetAnswerUrl(tid, form);
                }

                ViewBag.TopicId = tid;

                // Create the vote topic instance and handle the vote action
                VoteTopic topic = new VoteTopic(VoteModel.TopicCategory.Default, tid);

                bool canVote = !Vote.IsVoted(tid);

                // Count PV
                //topic.IncreasePageView(canVote);

                if (null != topic.TopicEntity)
                {
                    ViewBag.HasResult = true;

                    // Set share url                     
                    ViewBag.ShareUrl = Vote.GetShareUrl(topic);

                    // Set the share image
                    ViewBag.ShareImage = Vote.GetShareImage(topic);

                    // Set the sina weibo @XXX 
                    ViewBag.SinaweiboV = Vote.GetSinaweiboV(topic);

                    ViewBag.ShowTopicTitleLink = topic.TopicEntity.HasDetailPage;

                    // Get voted option
                    ViewBag.VoteOption = Vote.GetVotedOption(tid);

                    ViewBag.ShowTitleUnderline = (Util.GetDeviceType() == DeviceType.Mobile) ? false : true;
                }

                ViewBag.VoteTopic = topic;

                ViewBag.Title = topic.TopicEntity.TopicTitle;

                ViewBag.Market = mkt.ToLower();

                ViewBag.LabelLang = Vote.SetAnswerLabelLang(mkt);
            }
            catch 
            {
                return RedirectToAction("Error", "Vote");
            }          
            
            return View();
        }

        public JsonResult Shoot(string tid = "", int opt = -1)
        {
            Hashtable data = new Hashtable();

            try
            {
                if (Armor.IsDDos(tid))
                {
                    throw new Exception();
                }    

                if (string.IsNullOrWhiteSpace(tid))
                {
                    return this.Json(new { }, JsonRequestBehavior.AllowGet);
                }

                bool canVote = !Vote.IsVoted(tid);
                data.Add("CanVote", canVote);
                
                if (canVote)
                {
                    VoteTopic topic = new VoteTopic(VoteModel.TopicCategory.Default, tid);

                    // Handle the vote action
                    //topic.IncreaseVoteCount(opt);
                    topic.IncreaseVoteCountByCache(opt);

                    // Get the vote data and serialize it into json format
                    long optionVoteCountSum = 0, optionVoteCountMax = 0;
                    foreach (KeyValuePair<int, VoteOptionEntity> option in topic.Options)
                    {
                        optionVoteCountSum += option.Value.VoteCount;
                        optionVoteCountMax = (optionVoteCountMax > option.Value.VoteCount) ? optionVoteCountMax : option.Value.VoteCount;
                    }
                    data.Add("OptionVoteCountSum", optionVoteCountSum);
                    data.Add("OptionVoteCountMax", optionVoteCountMax);

                    foreach (KeyValuePair<int, VoteOptionEntity> option in topic.Options)
                    {
                        data.Add("OptionVoteCount" + option.Key, option.Value.VoteCount);

                        string optpert = ((0 == optionVoteCountSum) ? "-"
                                        : Math.Round((double)option.Value.VoteCount / optionVoteCountSum * 100, 0).ToString()) + "%";
                        data.Add("OptionPercentage" + option.Key, optpert);

                        string optwidth = ((0 == optionVoteCountMax) ? "20"
                                        : Math.Round(20 + (70 * (double)option.Value.VoteCount / optionVoteCountMax), 0).ToString()) + "%";
                        data.Add("ProgressBarWidth" + option.Key, optwidth);
                    }
                }               
            }catch
            {
                return this.Json(new { }, JsonRequestBehavior.AllowGet);        
            }

            // ajax cross-domain
            Response.AppendHeader("Access-Control-Allow-Origin", "*");

            return this.Json(data, JsonRequestBehavior.AllowGet);
        }

        public string GetVoteData(string k = "")
        {
            if (!k.Equals(Util.AccessKey))
                return null; 
         
            List<VoteTopic> topics = Vote.GetTopicsByCategory(VoteModel.TopicCategory.Default);
            List<VoteAnswerData> ans = new List<VoteAnswerData>();
            foreach (VoteTopic topic in topics)
            {
                VoteAnswerData vote = new VoteAnswerData();
                vote.TopicId = topic.TopicId;
                vote.OptionsCount = topic.Options.Count;
                vote.TriggerQueries = topic.TopicEntity.TriggerQueries;
                vote.Status = topic.TopicEntity.Status;
                vote.Market = topic.TopicEntity.Market;

                vote.Url = string.Format("{0}://{1}/Vote/Answer?tid={2}", Util.GetScheme(), Util.GetSiteHost(), topic.TopicId);
                //vote.Url = HttpUtility.UrlEncode(vote.Url);
                ans.Add(vote);
            }

            return (new JavaScriptSerializer()).Serialize(ans);
        }

        public ActionResult Create(string tid = "")
        {
            // Get the topic instance
            VoteTopic topic = null;
            try
            {
                if (tid != "")
                {
                    topic = new VoteTopic(VoteModel.TopicCategory.Default, tid); 
                }              
            }
            catch { }
            
            ViewBag.Title = (topic == null) ? "创建投票" : "编辑投票";
            
            // topic id
            ViewBag.TopicId = topic != null ? topic.TopicId : "";

            // topic title
            ViewBag.TopicTitle =  topic != null ? topic.TopicEntity.TopicTitle : "";
                
            // topic options
            ViewBag.OptionText = new Dictionary<int, string>();
            for (int optid = 1; optid <= 10; optid++)
            {
                ViewBag.OptionText.Add(optid, topic != null && topic.Options.ContainsKey(optid) ? topic.Options[optid].OptionText : "");
            }

            // topic background
            ViewBag.Background = (topic != null) ? topic.Background : "";

            // related news
            ViewBag.RelatedNewsTitle = new Dictionary<int, string>();
            ViewBag.RelatedNewsUrl = new Dictionary<int, string>();
            int rnid = 1;
            if (topic != null)
            {
                foreach (VoteRelatedNews rn in topic.RelatedNewsList)
                {
                    ViewBag.RelatedNewsTitle.Add(rnid, rn.Title);
                    ViewBag.RelatedNewsUrl.Add(rnid, rn.Url);
                    rnid++;
                }
            }
            for (; rnid <= 4; rnid++)
            {
                ViewBag.RelatedNewsTitle.Add(rnid, "");
                ViewBag.RelatedNewsUrl.Add(rnid, "");    
            }

            // Triggering queries
            ViewBag.TriggeringQuery = new Dictionary<int, string>();
            int tqid = 1;
            if (topic != null)
            {
                foreach (string q in topic.TriggerQueries)
                {
                    ViewBag.TriggeringQuery.Add(tqid, q);
                    tqid++;
                }
            }
            for (; tqid <= 20; tqid++)
            {
                ViewBag.TriggeringQuery.Add(tqid, "");
            }
           
            // Creator
            ViewBag.Creator = (topic != null) ? topic.TopicEntity.Creator : "";

            ViewBag.Market = (topic != null) ? topic.TopicEntity.Market : "";

            ViewBag.IsUpdate = (topic != null) ? true : false;

            return View();
        }

        public ActionResult CreateFinish()
        {    
            try
            {
                bool isUpdate = false;
                VoteTopic topic = null;

                // Only the login user can see the monitor info
                if (!WebSecurity.IsAuthenticated)
                {
                    return RedirectToAction("List", "Vote");
                }

                if (!string.IsNullOrWhiteSpace(Request["topicid"]))
                {
                    topic = new VoteTopic(VoteModel.TopicCategory.Default, Request["topicid"].Trim());
                    
                    ViewBag.Title = "投票修改成功";
                    isUpdate = true;
                }
                else
                {
                    topic = new VoteTopic();
                    
                    topic.TopicCategory = VoteModel.TopicCategory.Default.ToString();
                    topic.TopicId = Vote.CreateTopicId(); ;
                    
                    topic.Options = new Dictionary<int, VoteOptionEntity>();
                    topic.TopicEntity = new VoteTopicEntity(topic.TopicCategory, topic.TopicId);

                    // topic status
                    topic.TopicEntity.Status = VoteModel.TopicStatus.Pending.ToString();

                    // page view
                    topic.TopicEntity.PVRequestCount = 1;
                    topic.TopicEntity.VotableRequestCount = 0;
                    topic.TopicEntity.TotalVoteCount = 0;

                    ViewBag.Title = "投票创建成功";
                    isUpdate = false;
                }

                // The topic can be updated under two conditions : 1. the login user; 2. The topic status is "Pending"
                if (isUpdate && !topic.TopicEntity.Status.Equals("Pending") && !WebSecurity.IsAuthenticated)
                {
                    return this.Redirect("Create");
                }

                // topic entity
                topic.TopicEntity.TopicTitle = Request["topictitle"].Trim();
                topic.TopicEntity.Creator = Request["author"].Trim();
                topic.TopicEntity.Market = Request["market"].Trim();
                
                // Has detail page or not
                topic.TopicEntity.HasDetailPage = topic.HasDetailPage = Request["topichasdetailpage"].Equals("1") ? true : false;

                // Get the background info
                topic.Background = Request["topicbackground"].Trim();
                topic.Background = topic.TopicEntity.Background = topic.Background.Substring(0, Math.Min(topic.Background.Length, 500));

                // Get the related news
                topic.RelatedNewsList = new List<VoteRelatedNews>();
                for (int i = 1; i <= 4; i++)
                {
                    string rntitle = "relatednewstitle" + i, rnurl = "relatednewsurl" + i;
                    if (!string.IsNullOrWhiteSpace(Request[rntitle]) && !string.IsNullOrWhiteSpace(Request[rnurl]))
                    {
                        VoteRelatedNews rn = new VoteRelatedNews();
                        rn.Title = Request[rntitle].Trim();
                        rn.Url = Request[rnurl].Trim();
                        topic.RelatedNewsList.Add(rn);
                    }
                }
                topic.TopicEntity.RelatedNewsStr = Vote.CapsuleRelatedNews(topic.RelatedNewsList);

                // Handle topic's picture
                if (null != Request.Files["topicbackgroundpic"]
                    && !string.IsNullOrEmpty(Request.Files["topicbackgroundpic"].FileName))
                {
                    // topic's picture name
                    topic.TopicEntity.PictureName = "Pic_" + topic.TopicId + ".jpg";
                    
                    // Put the picture into storage blob
                    Vote.UploadVoteTopicBkgPic(Request.Files["topicbackgroundpic"].InputStream, topic.TopicEntity.PictureName);
                }
                topic.PictureLink = topic.GetTopicPictureLink(topic.TopicEntity.PictureName);

                // Get the trigger query list
                List<string> qList = new List<string>();
                for (int i = 1; i <= 20; i++)
                {
                    string qName = "query" + i;
                    if (!string.IsNullOrWhiteSpace(Request[qName]))
                    {
                        qList.Add(Request[qName]);
                    }
                }
                topic.TopicEntity.TriggerQueries = Vote.CapsuleQueryies(qList);
                topic.TriggerQueries = qList;

                // Insert/Update the topic entity
                if (!isUpdate)
                {
                    Vote.InsertVoteTopicEntity(topic.TopicEntity);
                }
                else 
                {
                    topic.UpdateVoteTopicEntity(topic.TopicEntity);   
                }

                // topic options
                for (int optid = 1; optid <= 10; optid++)
                {
                    string opttext = Request["optiontext" + optid].Trim();
                    if (!string.IsNullOrWhiteSpace(opttext))
                    {
                        if (topic.Options.ContainsKey(optid))
                        {
                            topic.Options[optid].OptionText = opttext;

                            topic.UpdateVoteOptionEntity(topic.Options[optid]);
                        }
                        else
                        {
                            topic.Options.Add(optid, new VoteOptionEntity(topic.TopicId, optid.ToString()));
                            topic.Options[optid].OptionText = opttext;
                            topic.Options[optid].VoteCount = Vote.GetRandomOptionCount(20, 5);

                            // Insert the vote option entity into storage
                            Vote.InsertVoteOptionEntity(topic.Options[optid]);
                        }
                    }
                }

                // Set ViewBag.
                ViewBag.VoteTopic = topic;

                // Disable vote action.
                ViewBag.ShowVoteButton = false;

                // Show author and query.
                ViewBag.ShowQuery = true;

                ViewBag.Formcode = "";

                ViewBag.ShowTopicTitleLink = topic.TopicEntity.HasDetailPage;

                // Set the label language according to the market
                ViewBag.LabelLang = Vote.SetAnswerLabelLang(Request["market"].Trim().ToLower());
            }
            catch 
            {
                return this.Redirect("Create");
            }
            return View();
        }

        public ActionResult Detail(string tid = "", string form = "", string mkt = "")
        {
            try 
            {
                if (string.IsNullOrWhiteSpace(tid))
                {
                    return RedirectToAction("Error", "Vote");
                }

                VoteTopic topic = new VoteTopic(VoteModel.TopicCategory.Default, tid);

                ViewBag.VoteTopic = topic;

                ViewBag.ShowVoteButton = true;
                ViewBag.ShowQuery = false;
                ViewBag.EnableShare = (form == "bingapp" || mkt.ToLower() == "ja-jp") ? false : true;
                ViewBag.PageCategory = "detail";

                // html document title
                ViewBag.Title = topic.TopicEntity.TopicTitle;

                // Get voted option
                ViewBag.VoteOption = Vote.GetVotedOption(tid);

                // Set share url                     
                ViewBag.ShareUrl = Vote.GetShareUrl(topic);

                // Set the share image
                ViewBag.ShareImage = Vote.GetShareImage(topic);

                // Set the sina weibo @XXX 
                ViewBag.SinaweiboV = Vote.GetSinaweiboV(topic);

                ViewBag.BingShareUrl = string.Format("http://www.bing.com/search?q={0}&setmkt={1}&form=intact", Vote.GetShareQuery(topic), topic.TopicEntity.Market);

                ViewBag.ShowTopicTitleLink = false;

                ViewBag.TopicId = topic.TopicId;

                ViewBag.Formcode = form;

                // Count PV
                bool canVote = !Vote.IsVoted(topic.TopicId);
                //topic.IncreasePageView(canVote);

                ViewBag.Market = mkt.ToLower();

                // Set the label language according to the market
                ViewBag.LabelLang = Vote.SetAnswerLabelLang(mkt);
            }
            catch
            {
                return RedirectToAction("Error", "Vote");
            }

            return View();
        }

        public JsonResult AddComment(string tid, string cmttext)
        {
            VoteComment comment = null;

            try
            {
                // DDos
                if (Armor.IsDDos(tid))
                {
                    throw new Exception();
                }

                if (string.IsNullOrWhiteSpace(tid))
                {
                    return this.Json(new { }, JsonRequestBehavior.AllowGet);
                }

                tid = tid.Trim();

                if (!string.IsNullOrWhiteSpace(cmttext))
                {
                    VoteCommentEntity vce = new VoteCommentEntity(tid, Vote.CreateCommentId());
                    vce.Creator = (new Regex(@"\.[0-9]{1,3}\.[0-9]{1,3}$")).Replace(Util.GetRemoteAddr(), ".*.*");

                    // Handle the comment text
                    vce.CommentText = cmttext.Trim();
                    vce.CommentText = vce.CommentText.Substring(0, Math.Min(vce.CommentText.Length, 200));
                
                    vce.CommentStatus = VoteModel.CommentStatus.Active.ToString();
                    vce.PraiseCount = 0;
                    Vote.InsertVoteComment(vce);

                    comment = new VoteComment(vce);
                }
            }
            catch
            {
                return this.Json(new { }, JsonRequestBehavior.AllowGet);
            }

            // ajax cross-domain
            Response.AppendHeader("Access-Control-Allow-Origin", "*");

            return this.Json(comment, JsonRequestBehavior.AllowGet);    
        }

        public JsonResult GetComments(string tid, string tcid)
        {
            Hashtable data = new Hashtable();

            try
            {
                if (string.IsNullOrWhiteSpace(tid))
                {
                    return this.Json(new { }, JsonRequestBehavior.AllowGet);
                }

                List<VoteComment> comments = VoteTopic.GetVoteComments(tid, tcid);

                for (int i = 0; i < comments.Count; i++ )
                {
                    data.Add(i.ToString(), comments[i]);
                }              
            }
            catch
            {
                return this.Json(new { }, JsonRequestBehavior.AllowGet);
            }

            // ajax cross-domain
            Response.AppendHeader("Access-Control-Allow-Origin", "*");

            return this.Json(data, JsonRequestBehavior.AllowGet);     
        }

        public JsonResult ViewIPBlackList(string k)
        {
            Hashtable data = new Hashtable();

            try
            {
                if (!k.Equals(Util.AccessKey))
                    return null;

                List<TrafficLogEntity> list = Armor.QueryIPBlacklist();

                foreach(TrafficLogEntity log in list)
                {
                    data.Add(log.RowKey + "-" + log.Timestamp, log.RequestIP);   
                }
            }
            catch
            {
                return this.Json(new { }, JsonRequestBehavior.AllowGet); 
            }

            return this.Json(data, JsonRequestBehavior.AllowGet);
        }

        public ActionResult Error()
        {
            return View();
        }
    }
}
