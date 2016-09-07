using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;

namespace Interaction.Models
{
    public enum DeviceType { Mobile, PC, Other };

    public static class Util
    {
        public static readonly string AccessKey = "5f6e67486e34b96315a08cca9f5bffa8d70b1645";

        public static Dictionary<string, Dictionary<string, string>> LabeLang { get; set; }

        static Util()
        {
            InitLabelLang();   
        }

        public static string GetSiteHost()
        {
            return HttpContext.Current.Request.Url.Host;
        }

        public static string GetScheme()
        {
            return HttpContext.Current.Request.Url.Scheme;
        }

        public static DeviceType GetDeviceType()
        {
            string userAgent = HttpContext.Current.Request.UserAgent.ToLower();

            string[] mobileUserAgentKeywords = new string[] 
                { 
                    "android",
                    "iphone",
                    "ipad",
                    "windows phone",
                    "windows ce",
                    "windows mobile",
                    "midp",
                    "rv:1.2.3.4", // UC7
                    "ucweb"
                };

            foreach (string keyword in mobileUserAgentKeywords)
            {
                if (userAgent.Contains(keyword))
                    return DeviceType.Mobile;
            }

            return DeviceType.PC;
        }

        public static string UrlEncode(string url)
        {
            if (string.IsNullOrEmpty(url))
            {
                return "";
            }

            return HttpUtility.UrlEncode(url);
        }

        public static string GetRequestIp()
        {
            HttpRequest request = HttpContext.Current.Request;

            string reqIpStr = request.ServerVariables["HTTP_X_FORWARDED_FOR"] + ";"
                             + request.ServerVariables["REMOTE_ADDR"] + ";"
                             + request.UserHostAddress;

            return reqIpStr;
        }

        public static string GetRemoteAddr()
        {
            return HttpContext.Current.Request.ServerVariables["REMOTE_ADDR"];
        }

        public static string GetRequestParam(HttpRequestBase request, string name)
        {
            if (request != null && name != null && request.QueryString != null)
            {
                return request.QueryString[name] ?? string.Empty;
            }

            return null;
        }

        public static void InitLabelLang()
        {
            LabeLang = new Dictionary<string, Dictionary<string, string>>()
                {
                    {"ja-jp", new Dictionary<string, string>()
                        {
                            {"LabelEngagementCount1", "投票数："},
                            {"LabelEngagementCount2", ""},
                            {"SupportRed", "赤の票数"},
                            {"SupportBlue", "青の票数"},
                            {"LabelCommentArea", "コメントを書く"},
                            {"LabelSeeRelatedComments", "他のコメントを読む"},
                            {"LabelCommit", "送信"},
                            {"LabelBackground", "背景"},
                            {"LabelRelatednews", "関連する記事"},
                            {"LabelMore", "更に読む..."},
                            {"LabelCommentSection", "コメント"},
                            {"LabelMoreComments", "更にコメントを読む"},
                            {"LabelCommentLimitation", "200文字以内"},
                            {"LabelComment", "ここにコメントを書いてください"},                                                       
                            {"LabelJustnow", "たった今"},
                            {"LabelDay", "日前"},
                            {"LabelHour", "時間前"},
                            {"LabelMinute", "分前"}
                        }
                    },
                    {"zh-cn", new Dictionary<string, string>()
                        {
                            {"LabelEngagementCount1", "已有"},
                            {"LabelEngagementCount2", "人参与"},
                            {"SupportRed", "支持红方"},
                            {"SupportBlue", "支持蓝方"},
                            {"LabelCommentArea", "我来说两句"},
                            {"LabelSeeRelatedComments", "查看相关评论"},
                            {"LabelCommit", "提交"},
                            {"LabelBackground", "背景介绍"},
                            {"LabelRelatednews", "相关新闻"},
                            {"LabelMore", "更多..."},
                            {"LabelCommentSection", "评论"},
                            {"LabelMoreComments", "查看更多评论"},
                            {"LabelCommentLimitation", "最多输入200字"},
                            {"LabelComment", "发表评论"},                                                       
                            {"LabelJustnow", "刚刚"},
                            {"LabelDay", "天前"},
                            {"LabelHour", "小时前"},
                            {"LabelMinute", "分钟前"}
                        }
                    }
                };
        }

        public static string ByteArrayToString(byte[] arrInput)
        {  
            StringBuilder sOutput = new StringBuilder(arrInput.Length);  
            for (int i = 0; i < arrInput.Length; i++)
            {  
                sOutput.Append(arrInput[i].ToString("X2"));  
            }  
            return sOutput.ToString();  
        }
    }

    public static class Armor
    {
        public static ConcurrentDictionary<string, int> IPCountCache = new ConcurrentDictionary<string, int>();

        public static List<string> IPBlackList = new List<string>();

        public static readonly int IPCountThreshold = 3;

        public static readonly int IPCountCacheSizeThreshold = 1000;

        public static readonly string IPBlacklistTag = "IPBlackList";

        public static bool IsBot(string reqIP)
        {
            bool isBot = false;

            if (IPBlackList.Contains(reqIP))
            {
                isBot = true;
            }
            else
            {
                string key = DateTime.UtcNow.ToString("yyyyMMddHHmmss") + reqIP;

                if (IPCountCache.Count > IPCountCacheSizeThreshold)
                {
                    IPCountCache.Clear();
                }

                if (IPCountCache.ContainsKey(key))
                {
                    IPCountCache[key]++;

                    if (IPCountCache[key] > IPCountThreshold)
                    {
                        IPBlackList.Add(reqIP);

                        LogBlackList();

                        isBot = true;
                    }
                }
                else
                {
                    IPCountCache.TryAdd(key, 1);
                }
            }

            return isBot;
        }

        public static bool IsDDos(string tid)
        {
            // Get the request ip
            string reqipstr = Util.GetRequestIp();
            string[] IPSe = reqipstr.Split(new char[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string ip in IPSe)
            {
                if (IsBot(ip.Trim()))
                {
                    return true;
                }
            }

            TrafficLogEntity logEntity = new TrafficLogEntity(tid, DateTime.UtcNow.ToString("yyyyMMddHHmmssfff") + "R" + (new Random()).Next(999999999).ToString("000000000"));
            logEntity.RequestIP = reqipstr;
            InsertTrafficLog(logEntity);

            return false;
        }

        public static void LogBlackList()
        {
            TrafficLogEntity logEntity = new TrafficLogEntity(IPBlacklistTag, DateTime.UtcNow.ToString("yyyyMMddHHmmssfff") + "R" + (new Random()).Next(999999999).ToString("000000000"));
            logEntity.RequestIP = "";

            foreach (string ip in IPBlackList)
            {
                logEntity.RequestIP += ip + ";";
            }

            InsertTrafficLog(logEntity);
        }

        public static void InsertTrafficLog(TrafficLogEntity entity)
        {
            StorageModel.GetTable(VoteModel.VoteTrafficLogTableName)
                        .Execute(TableOperation.Insert(entity));
        }

        public static List<TrafficLogEntity> QueryIPBlacklist()
        {
            List<TrafficLogEntity> IPBlacklistEntities = StorageModel.GetTable(VoteModel.VoteTrafficLogTableName)
                .ExecuteQuery(new TableQuery<TrafficLogEntity>()
                .Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, IPBlacklistTag)))
                .ToList<TrafficLogEntity>();

            List<TrafficLogEntity> toplist = IPBlacklistEntities.OrderByDescending(e => e.Timestamp).Take(20).ToList();

            return toplist;
        }
    }

    public class TrafficLogEntity : TableEntity
    {
        public string RequestIP { get; set; }

        public TrafficLogEntity() { }

        public TrafficLogEntity(string pKey, string rKey)
        {
            this.PartitionKey = pKey;
            this.RowKey = rKey;
        }
    }
}