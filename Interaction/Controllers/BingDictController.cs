using Interaction.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Web.Script.Serialization;

namespace Interaction.Controllers
{
    public class BingDictController : Controller
    {
        private BingDictModel feedbackModel { get; set; }

        public BingDictController()
        {
            feedbackModel = new BingDictModel();
        }

        // GET: Academic
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult Feedback()
        {
            ViewBag.Formcode = "bingdict";
            ViewBag.Title = "反馈";            

            feedbackModel.Title = "意见反馈";
            feedbackModel.Contact = Request.QueryString.Get("contact");
            feedbackModel.CommentPlaceHolder = string.IsNullOrWhiteSpace(feedbackModel.Contact) ? "您的反馈是我们持续改进的动力!(512字以内)" :
                "您好, 您的反馈是我们持续改进的动力!(512字以内)";
            feedbackModel.Event = Request.QueryString.Get("device");
            string queryStr = Request.QueryString.Get("query");            
            if (!string.IsNullOrWhiteSpace(queryStr))
            {
                queryStr = HttpUtility.UrlDecode(queryStr.Trim());
                if (queryStr.Trim() != "null" && queryStr.Trim() != "(null)")
                {                    
                    feedbackModel.Query =  queryStr;
                }
            }

            return View(feedbackModel);
        }
                
        public ActionResult AddFeedback()
        {
            bool isFromWeb = Request["source"] == null;
            string url = Request["url"] == null ? "" : Request["url"].Trim(), cmttext = Request["cmttext"] == null ? "" : Request["cmttext"].Trim(), 
                clientInfo = Request["clientInfo"] == null ? "" : Request["clientInfo"].Trim();
            clientInfo = string.IsNullOrEmpty(clientInfo) ? (Request["userAgent"] == null ? "" : Request["userAgent"].Trim()) : clientInfo;
            string contact = Request["contact"] == null ? "" : Request["contact"].Trim();
            string query = Request["query"] == null ? "" : Request["query"].Trim();
            Dictionary<string, string> clippicnames = new Dictionary<string, string>(), clippiclinks = new Dictionary<string, string>();

            string catId = Util.ByteArrayToString(new MD5CryptoServiceProvider().ComputeHash(ASCIIEncoding.ASCII.GetBytes(url)));
            string fbId = "C" + DateTime.UtcNow.ToString("yyyyMMddHHmmssfff") + "R" + (new Random()).Next(999999).ToString("000000");

            DictFeedsEntity comment = new DictFeedsEntity(catId, fbId);
            if (!string.IsNullOrEmpty(cmttext))
            {
                try
                {
                    comment.CommentStatus = "Active";
                    comment.Url = url.Substring(0, Math.Min(url.Length, 512));
                    comment.ClientInfo = clientInfo;
                    string linkstr = "";

                    var files = Request.Files;
                    if (files != null && Request["hiddenImageName"] != null)
                    {
                        string uploadedImageNames = Request["hiddenImageName"].Trim();
                        for (int i = 0; i < files.AllKeys.Length; i++)
                        {
                            if (!files.AllKeys[i].Equals("uploadImage"))
                            {
                                continue;
                            }
                            string fileName = files[i].FileName;
                            if (!string.IsNullOrEmpty(fileName) && uploadedImageNames.Contains(fileName))
                            {
                                // Given clip picture name
                                clippicnames.Add("clippic" + (i + 1), string.Format("Pic_{0}_{1}{2}", fbId, (i + 1), Path.GetExtension(fileName)));

                                // Put the picture into storage blob
                                clippiclinks.Add("clippiclink" + (i + 1), feedbackModel.UploadPic(files[i].InputStream, clippicnames["clippic" + (i + 1)]));

                                linkstr += clippiclinks["clippiclink" + (i + 1)];
                            }
                        }
                    }
                    
                    comment.CommentText = string.Format("#Comment#: {0};  #CLIPPICTURELINK#:{1}; #Email#:{2}", cmttext.Substring(0, Math.Min(cmttext.Length, 1024)),
                        linkstr, contact);
                    if (!string.IsNullOrEmpty(query)) {
                        comment.CommentText += "#Query#:" + query;
                    }

                    feedbackModel.InsertFeedback(comment);
                }
                catch (Exception e)
                {
                    return Content(e.StackTrace + e.Message);
                }
            }
            if (!isFromWeb)
            {                
                return Content("success");
            }
            System.Threading.Thread.Sleep(1000);
            return RedirectToAction("Feedback", "BingDict", new { contact = contact });
        }

        public string DumpFeedbacks(string k = "", string date = "", string url= "/BingDict/AddFeedback")
        {
            try
            {
                if (!k.Equals(Util.AccessKey))
                    return "Failed";

                List<DictFeedsEntity> feedbackList = feedbackModel.FetchAllFeedbackEntities();

                feedbackList.Sort(delegate (DictFeedsEntity vta, DictFeedsEntity vtb)
                {
                    return vtb.Timestamp.CompareTo(vta.Timestamp);
                });

                StringBuilder sb = new StringBuilder();
                sb.AppendLine("Timestamp\tUrl\tCommentText\tDevice <br/>");
                foreach (DictFeedsEntity fde in feedbackList)
                {
                    if (fde.Url.Equals(url, StringComparison.CurrentCultureIgnoreCase))
                    {
                        DateTime localdt = fde.Timestamp.AddHours(8).DateTime;
                        if (date == "")
                        {
                            sb.AppendLine(localdt + "\t" + HttpUtility.UrlDecode(fde.Url) + "\t" + fde.CommentText + "\t" + fde.ClientInfo + "<br/>");
                        }
                        else {
                            
                            if (localdt.ToString("yyyyMMdd").CompareTo(string.IsNullOrEmpty(date) ? DateTime.UtcNow.AddHours(8).ToString("yyyyMMdd") : DateTime.Parse(date).ToString("yyyyMMdd")) == 0)
                            {
                                sb.AppendLine(localdt + "\t" + HttpUtility.UrlDecode(fde.Url) + "\t" + fde.CommentText + "\t" + fde.ClientInfo + "<br/>");
                            }
                        }
                    }
                }

                return sb.ToString();
            }
            catch (Exception ex)
            {
                return ex.Message.ToString();
            }
        }

        [HttpPost]
        public bool UploadFile(){

            return false;
        }
    }
}
