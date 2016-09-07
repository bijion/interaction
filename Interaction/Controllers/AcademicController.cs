using Interaction.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Web.Mvc;

namespace Interaction.Controllers
{
    public class AcademicController : Controller
    {
        private AcademicModel feedbackModel { get; set; }

        public AcademicController()
        {
            feedbackModel = new AcademicModel();
        }

        // GET: Academic
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult Feedback(string msid = "", string url = "", string iframeId = "", string et = "")
        {
            ViewBag.Formcode = "academic";
            ViewBag.Title = "反馈";

            // Microsoft id
            ViewBag.MsId = msid;
            ViewBag.Url = url;
            ViewBag.IframeId = iframeId;
            switch (et.ToLower())
            {
                //case "wsdm":
                //    feedbackModel.Title = "WSDM 2016挑战赛评测反馈";
                //    feedbackModel.CommentPlaceHolder = "本次评测仅针对“论文搜索排序”，欢迎反馈您对排序结果的满意度，或提出对于评测活动的建议。";                    
                //    feedbackModel.ContactPlaceHolder = "请留下您的有效联系方式，参与活动抽奖。";
                //    break;
                case "profile":
                    feedbackModel.Title = "意见反馈";
                    feedbackModel.CommentPlaceHolder = "欢迎提出您在使用过程中遇到的任何问题或意见,您的反馈是我们持续改进的动力!(512字以内)";
                    feedbackModel.ContactPlaceHolder = "请留下您的联系方式, 以便我们及时回复。";
                    feedbackModel.StyleFileName = "Feedback_Profile.css";
                    break;
                default:
                    feedbackModel.Title = "意见反馈";
                    feedbackModel.CommentPlaceHolder = "欢迎提出您在使用过程中遇到的任何问题或意见,您的反馈是我们持续改进的动力!(512字以内)";
                    feedbackModel.ContactPlaceHolder = "请留下您的联系方式, 以便我们及时回复。";
                    break;
            }
            feedbackModel.Event = et.ToLower().Equals("wsdm") ? string.Empty : et.ToLower();
            return View(feedbackModel);
        }

        public ActionResult AddFeedback()
        {
            string url = Request["url"].Trim(), cmttext = Request["cmttext"].Trim(), contact = Request["contact"].Trim(), msid = Request["msid"].Trim(), iframeId = Request["iframeId"].Trim(), et = Request["event"].Trim();
            Dictionary<string, string> clippicnames = new Dictionary<string, string>(), clippiclinks = new Dictionary<string, string>();

            string catId = Util.ByteArrayToString(new MD5CryptoServiceProvider().ComputeHash(ASCIIEncoding.ASCII.GetBytes(url)));
            string fbId = "C" + DateTime.UtcNow.ToString("yyyyMMddHHmmssfff") + "R" + (new Random()).Next(999999).ToString("000000");

            FeedbackEntity comment = new FeedbackEntity(catId, fbId);

            try
            {
                comment.CommentStatus = "Active";
                comment.Url = url.Substring(0, Math.Min(url.Length, 512));
                comment.Contact = contact.Substring(0, Math.Min(contact.Length, 128));

                string linkstr = "";

                var files = Request.Files;
                if (files != null)
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

                comment.CommentText = et + (string.IsNullOrEmpty(et) ? "" : ": ") + cmttext.Substring(0, Math.Min(cmttext.Length, 1024)) + (string.IsNullOrEmpty(linkstr) ? "" : " #CLIPPICTURELINK# : " + linkstr);

                feedbackModel.InsertFeedback(comment);
            }
            catch(Exception e)
            {
                return RedirectToAction("Feedback", "Academic");
            }

            return RedirectToAction("Feedback", "Academic", new { msid = msid, url = url, iframeId = iframeId, et = et });
        }

        public string DumpFeedbacks(string k = "", string date = "")
        {
            try
            {
                if (!k.Equals(Util.AccessKey))
                    return "Failed";

                List<FeedbackEntity> feedbackList = feedbackModel.FetchAllFeedbackEntities();

                feedbackList.Sort(delegate (FeedbackEntity vta, FeedbackEntity vtb)
                {
                    return vtb.Timestamp.CompareTo(vta.Timestamp);
                });

                StringBuilder sb = new StringBuilder();
                sb.AppendLine("Timestamp\tContact\tUrl\tCommentText");
                foreach (FeedbackEntity fde in feedbackList)
                {
                    DateTime localdt = fde.Timestamp.AddHours(8).DateTime;
                    if (localdt.ToString("yyyyMMdd").CompareTo(string.IsNullOrEmpty(date) ? DateTime.UtcNow.AddHours(8).ToString("yyyyMMdd") : DateTime.Parse(date).ToString("yyyyMMdd")) == 0)
                    {
                        sb.AppendLine(localdt + "\t" + fde.Contact + "\t" + HttpUtility.UrlDecode(fde.Url) + "\t" + fde.CommentText);
                    }
                }

                return sb.ToString();
            }
            catch (Exception ex)
            {
                return ex.Message.ToString();
            }
        }
    }
}
