using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using WebMatrix.WebData;

namespace Interaction.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            if (WebSecurity.IsAuthenticated)
                ViewBag.Verification = true;
            else
                ViewBag.Verification = false;
            return View();
        }

        public ActionResult List()
        {
            return View();
        }

        public ActionResult About()
        {
            ViewBag.Title = "\"用搜索来表达你的观点！\"";
            ViewBag.Message = "-- 必应互动的愿景";
            ViewBag.AdditionalInfo = "长期以来，搜索是一个独立的行为，搜索同一个或者同一类查询词的人没有机会去表达彼此的观点并和这些可能的朋友互动交流。" + 
                                     "而现有的社交网络要么基于线下的社交关系如微信，要么基于特定的实体（书、影、音等）如豆瓣，要么纯粹只是地理位置上的相近如陌陌。" +
                                     "让搜索不再只是一个信息查询的渠道，让用户通过搜索表达自己的观点，让观点类似的人彼此感知并成为朋友，是必应互动的奋斗目标。";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Title = "联系我们";
            ViewBag.Message = "欢迎您提供各种反馈信息";

            return View();
        }
    }
}