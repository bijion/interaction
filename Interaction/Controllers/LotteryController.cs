﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Script.Serialization;
using WebMatrix.WebData;

namespace Interaction.Models
{
    public class LotteryController : Controller
    {
        private static LotteryModel lotteryModel = new LotteryModel();
        private const string Category = "Lottery";
        private string DetailUrl = "http://www.bing.com/knows/%e7%b2%be%e5%93%81%e6%b5%b7%e6%b7%98%e6%8c%87%e5%8d%97?mkt=zh-cn";

        // GET: Lottery 
        public ActionResult List()
        {
            if (!WebSecurity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Home");
            }
            // test 
            AwardEntity award = lotteryModel.GetAwardEntity(Category, "A20141017150622824P965552");
            ViewBag.award = award;
            return View();
        }

        public string Check(string k = "") 
        {
            if (!k.Equals("5f6e67486e34b96315a08cca9f5bffa8d70b1645"))
                return null;
            List<CheckResult> checkResults = new List<CheckResult>();

            List<List<PhoneNumberEntity>> logs = lotteryModel.GetPhoneNumberOrderbyAward(true);
            DateTime now = DateTime.UtcNow;
            foreach (var log in logs)
            {
                long effectCount = 0;
                string rowKey = string.Empty;
                foreach (PhoneNumberEntity entity in log)
                {
                    if (entity.Validity == true || ((now - entity.Timestamp.UtcDateTime)).TotalMinutes <= 45 && entity.Validity == false)
                        effectCount++;
                    rowKey = entity.PartitionKey;
                }
                if (string.IsNullOrWhiteSpace(rowKey))
                    continue;
                AwardEntity award = lotteryModel.GetAwardByRowId(Interaction.Models.LotteryModel.AwardsCategory.Lottery, rowKey);
                if (effectCount != award.TotalVolume)
                {
                    CheckResult checkResult = new CheckResult(award.AwardName, award.TotalVolume, effectCount);
                    checkResults.Add(checkResult);

                    award.TotalVolume = effectCount;
                    lotteryModel.UpdateLotteryEntity(award);                 
                }
            }
            return (new JavaScriptSerializer()).Serialize(checkResults);
        }

        public string GetLotteryData(string k = "")
        {
            if (!k.Equals("5f6e67486e34b96315a08cca9f5bffa8d70b1645"))
                return null;

            List<AwardEntity> awardsList = lotteryModel.GetAwardsByCategory(LotteryModel.AwardsCategory.Lottery);


            List<LotteryAnswerData> ans = new List<LotteryAnswerData>();
            foreach (AwardEntity award in awardsList)
            {
                LotteryAnswerData item = new LotteryAnswerData();
                item.AwardId = award.RowKey;
                item.TriggerQueries = award.TriggerQueries;
                DateTime now = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("China Standard Time"));
                if (now < award.StartDate)
                    item.Status = "InFuture";
                else if (now > award.EndDate.AddDays(1))
                    item.Status = "OutDate";
                else
                    item.Status="Live";

                ans.Add(item);
            }

            return (new JavaScriptSerializer()).Serialize(ans);
        }

        public ActionResult QueryTrigger() 
         { 
             string rowKey = string.IsNullOrWhiteSpace(Request["key"]) ? null : Request["key"].Trim(); 
             ViewBag.Result = "lose"; 
             ViewBag.DetailUrl = DetailUrl; 
  
             ViewBag.JustVoteBlock = 
                 string.IsNullOrWhiteSpace(Request["JustVoteBlock"]) ? false : bool.Parse(Request["JustVoteBlock"]); 
  
             // if user's input is null 
             if (string.IsNullOrWhiteSpace(rowKey)) 
             { 
                 ViewBag.Root = "space"; 
                 return View(); 
             } 
  
             lock (lotteryModel) 
             { 
                 AwardEntity award = lotteryModel.GetAwardEntity(Category, rowKey); 
                 // if can't find this award in storage 
                 if (award == null) 
                 { 
                     ViewBag.Root = "notFind"; 
                     return View(); 
                 } 
  
                 DateTime now = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("China Standard Time")); 
  
                 if (!now.ToShortDateString().Equals(award.StartDate.ToShortDateString()) && !now.ToShortDateString().Equals(award.EndDate.ToShortDateString())) 
                 { 
                     if (now < award.StartDate) 
                     { 
                         ViewBag.Root = "notBegin"; 
                         return View(); 
                     } 
                     else if (now > award.EndDate) 
                     { 
                         ViewBag.Root = "hasEnd"; 
                         return View(); 
                     } 
                 } 
  
                 TimeSpan timespan = now - award.StartDate;
                 int days = timespan.Days + 1;
                 long quato = days * award.AwardQuota; 
                 // if reach the daily quato 
                 if (award.TotalVolume >= quato) 
                 { 
                     ViewBag.Root = "reachQuato";
                     award.PVCount++;
                     lotteryModel.UpdateLotteryEntity(award); 
                     return View(); 
                 } 
                 else 
                 { 
                     Random random = new Random(); 
                     int randomMax = (int)(1 / award.AwardRate); 
                     if (random.Next(randomMax) == 0) 
                     { 
                         ViewBag.Award = award; 
                         ViewBag.Root = "success"; 
                         ViewBag.Result = "success"; 
  
                         award.TotalVolume++; 
                         award.PVCount++; 
                         lotteryModel.UpdateLotteryEntity(award); 
  
                         string RowKey = lotteryModel.CreateNewPhoneNumberId(); 
                         PhoneNumberEntity phoneNumber = new PhoneNumberEntity(award.RowKey, RowKey); 
                         phoneNumber.InsertTime = DateTime.Parse("1970-01-01"); 
                         phoneNumber.PhoneNumber = string.Empty; 
                         phoneNumber.AwardName = award.AwardName; 
                         lotteryModel.InsertPhoneNameEntity(phoneNumber); 
  
                         ViewBag.PhoneNumber = phoneNumber; 
  
                         ViewBag.AwardId = award.RowKey; 
  
                         // Get trigger queries 
                         List<string> triggerQueries = lotteryModel.ExtractQueries(award.TriggerQueries); 
                         // Get the first trigger query 
                         ViewBag.FirstTriggerQuery = string.Empty; 
                         if (triggerQueries != null && triggerQueries.Count > 0) 
                         { 
                             ViewBag.FirstTriggerQuery = HttpUtility.UrlEncode(triggerQueries[0]); 
                         }

                         ViewBag.ShareMessage = "#海淘妈咪来挖宝#我在必应搜索到了不少海外母婴品牌产品的信息，最近还有挖宝活动，点击查看商品信息就有可能获得同款原装正品，我中奖啦，你也来试试。（分享自 @必应搜索）"; 
  
                         return View(); 
                     } 
                     else 
                     { 
                         ViewBag.Root = "failed"; 
                         award.PVCount++; 
                         lotteryModel.UpdateLotteryEntity(award);

                         ViewBag.FirstTriggerQuery = HttpUtility.UrlEncode("海淘妈咪来挖宝");
                         ViewBag.ShareMessage = "#海淘妈咪来挖宝#我在必应搜索到了不少海外母婴品牌产品的信息，最近还有挖宝活动，点击查看商品信息就有可能获得同款原装正品，你也可以来看看。（分享自 @必应搜索）"; 

                         return View(); 
                     } 
                 } 
             } 
         }

        public ActionResult AwardsList()
        {
            if (!WebSecurity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Home");
            }
            List<AwardEntity> awardsList = lotteryModel.GetAwardsByCategory(LotteryModel.AwardsCategory.Lottery);
            ViewBag.Debug = awardsList.Count;
            if (awardsList != null && awardsList.Count > 0)
            {
                ViewBag.HasResult = true;
                ViewBag.AwardsList = awardsList;
            }
            else
            {
                ViewBag.HasResult = false;
            }
            return View();
        }

        [HttpPost]
        public ActionResult Delete(AwardEntity award)
        {
            if (Request.HttpMethod != "POST")
            {
                return this.Redirect("AwardsList");
            }
            else
            {
                award.PartitionKey = Category;
                award.ETag = "*";
                lotteryModel.DeleteLotteryEntity(award);
                return Json(award, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public ActionResult InsertPhoneNumber(PhoneNumberEntity entity) 
         { 
             if (Request.HttpMethod != "POST") 
             { 
                 return View(); 
             } 
             else 
             {
                 if(entity.PhoneNumber == null||entity.PhoneNumber.Equals(""))
                 {
                     return Json(new CallbackObject("手机号码不能为空", "5"), JsonRequestBehavior.AllowGet);
                 }
                 if (lotteryModel.IdentifyPhoneNumber(entity.PhoneNumber)) 
                 { 
                     if (lotteryModel.IdentifyDuplicatePhoneNumber(entity.PhoneNumber)) 
                     { 
                         entity.PhoneNumber = string.Empty; 
                         return Json(new CallbackObject("很抱歉，每个手机号码只能参与一次抽奖活动", "2"), JsonRequestBehavior.AllowGet); 
                     } 
                     else 
                     {
                         DateTime now = DateTime.UtcNow;
                         if ((now - entity.Timestamp.UtcDateTime).TotalMinutes > 45) 
                         { 
                             entity.PhoneNumber = string.Empty; 
                             return Json(new CallbackObject("很抱歉，您的会话已超时，欢迎您下次再来", "4"), JsonRequestBehavior.AllowGet); 
                         } 
                         else 
                         {
                             entity.InsertTime = TimeZoneInfo.ConvertTimeFromUtc(now, TimeZoneInfo.FindSystemTimeZoneById("China Standard Time")); ; 
                             entity.Validity = true; 
                             entity.ETag = "*"; 
                             lotteryModel.UpdatePhoneNameEntity(entity);

                             return Json(new CallbackObject("您的手机号已记录，必应客服专员将在3个工作日内和您联系取得您的地址，给您寄送奖品", "1"), JsonRequestBehavior.AllowGet);
                         } 
                     } 
                 } 
                 else 
                 { 
                     entity.PhoneNumber = string.Empty; 
                     return Json(new CallbackObject("您输入的号码有误，请重新输入11位手机号码", "3"), JsonRequestBehavior.AllowGet); 
                 } 
             } 
         }

        private IEnumerable<SelectListItem> GetAwardNameForDropdownList()
        {
            var list = lotteryModel.GetAwardsByCategory(LotteryModel.AwardsCategory.Lottery).Select(x =>
                        new SelectListItem
                        {
                            Value = x.RowKey,
                            Text = x.AwardName
                        });

            return new SelectList(list, "Value", "Text");
        }

        public ActionResult Review()
        {
            if (!WebSecurity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Home");
            }
            AwardNameDropDownListViewModel model = new AwardNameDropDownListViewModel
            {
                award = GetAwardNameForDropdownList()
            };
            List<List<PhoneNumberEntity>> allOfPhonenumbers = new List<List<PhoneNumberEntity>>();
            if (Request.HttpMethod != "POST")
            {
                allOfPhonenumbers = lotteryModel.GetPhoneNumberOrderbyAward(false);
                if (allOfPhonenumbers.Count != 0)
                {
                    ViewBag.Phonenumbers = allOfPhonenumbers;
                    ViewBag.HasResult = true;
                }
                else
                {
                    ViewBag.HasResult = false;
                }
                return View(model);
            }
            else
            {
                if (Request["submit"] == null)
                {
                    allOfPhonenumbers = lotteryModel.GetPhoneNumberOrderbyAward(false);
                }
                else
                {
                    string RowKey = Request["awardRowKey"];
                    ViewBag.SelectedKey = RowKey;
                    DateTime dt = new DateTime();
                    try
                    {
                        dt = DateTime.Parse(Request["date"]);
                        ViewBag.SelectedDate = dt;
                    }
                    catch (Exception)
                    {
                        allOfPhonenumbers.Add(lotteryModel.GetPhoneNumberbyAward(RowKey,false));
                        ViewBag.Phonenumbers = allOfPhonenumbers;
                        if (allOfPhonenumbers.Count != 0)
                            ViewBag.HasResult = true;
                        else
                            ViewBag.HasResult = false;

                        return View(model);
                    }
                    allOfPhonenumbers.Add(lotteryModel.GetPhoneNumberbyAwardAndDate(RowKey, dt));
                }
                if (allOfPhonenumbers.Count != 0)
                {
                    ViewBag.Phonenumbers = allOfPhonenumbers;
                    ViewBag.HasResult = true;
                }
                else
                {
                    ViewBag.HasResult = false;
                }

                return View(model);
            }
        }

        public ActionResult Create(AwardEntity award)
        {
            if (!WebSecurity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Home");
            }
            if (award.RowKey != null)
            {
                @ViewBag.award = award;
                @ViewBag.update = true;
            }
            else
                @ViewBag.update = false;
            return View();
        }

        public ActionResult CreateFinish() 
         { 
             if (Request.HttpMethod != "POST") 
             { 
                 return this.Redirect("Create"); 
             } 
             else 
             { 
                 bool updateOrCreate = bool.Parse(Request["updateOrCreate"].ToLower()); 
                 AwardEntity award = new AwardEntity(); 
                 string RowKey = string.Empty; 
                 if(updateOrCreate==true) 
                 { 
                     RowKey = Request["Key"]; 
                     award = lotteryModel.GetAwardEntity(Category, RowKey); 
                 } 
                 else 
                 { 
                     RowKey = lotteryModel.CreateNewAwardId(); 
                     award = new AwardEntity(Category, RowKey); 
                     award.AwardName = Request["name"]; 
                 } 
              
                 award.Url = Request["url"]; 
                 award.AwardQuota = long.Parse(Request["quota"]); 
                 award.AwardRate = double.Parse(Request["rate"]); 
                 award.StartDate = DateTime.Parse(Request["startdate"]); 
                 award.EndDate = DateTime.Parse(Request["enddate"]); 
  
                 // Get the trigger query list 
                 List<string> qList = new List<string>(); 
                 for (int i = 1; i <= 10; i++) 
                 {
                     string qName = "query" + i;
                     if (!string.IsNullOrWhiteSpace(Request[qName])) 
                     { 
                         qList.Add(Request[qName]); 
                     } 
                 } 
                 award.TriggerQueries = lotteryModel.CapsuleQueryies(qList); 
  
                 if (updateOrCreate == true) 
                 { 
                     lotteryModel.UpdateLotteryEntity(award); 
                     ViewBag.updateOrCreate = true; 
                 } 
                 else 
                 { 
                     award.TotalVolume = 0; 
                     award.PVCount = 0; 
                     // Insert the award entity into storage 
                     lotteryModel.InsertLotteryEntity(award); 
                     ViewBag.updateOrCreate = false; 
                 }                 
                           
                 // Set ViewBag. 
                 ViewBag.AwardEntity = award; 
  
             } 
             return View(); 
         }

        class CallbackObject
        {
            public string Tips { set; get; }

            public string encode { set; get; }

            public CallbackObject() { }

            public CallbackObject(string Tips, string encode)
            {
                this.Tips = Tips;
                this.encode = encode;
            }
        }
    }
}
