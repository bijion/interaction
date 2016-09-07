using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Mvc;

namespace Interaction.Controllers
{
    public class TrainTicketController : Controller
    {
        // GET: TrainTicket
        public ActionResult QueryTrigger()
        {
            DateTime date = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("China Standard Time"));
            string today = date.ToString("MM月dd日");
            Regex regex = new Regex("0\\d");
            MatchCollection matches = regex.Matches(today);
            foreach (Match match in matches)
            {
                today = today.Replace(match.Value, match.Value.Substring(1, 1));
            }
            ViewBag.Today = today;

            DateTime date_start = DateTime.Parse("2014-12-01");
            DateTime date_end = DateTime.Parse("2014-12-07");
            DateTime date_limitation = new DateTime();
            if (date < date_start)
            {
                date_limitation = date.AddDays(19);
                ViewBag.FirstLine = GetDate(date_limitation, date_limitation.Year);
                date_limitation = date.AddDays(17);
                ViewBag.SecondLine = GetDate(date_limitation, date_limitation.Year);
            }
            else if (date.ToString("yyyy-MM.dd").Equals(date_start.ToString("yyyy-MM.dd")))
            {
                date_limitation = date.AddDays(29);
                ViewBag.FirstLine = GetDate(date_limitation, date_limitation.Year);
                date_limitation = date.AddDays(27);
                ViewBag.SecondLine = GetDate(date_limitation, date_limitation.Year);
            }
            else
            {
                if (date < date_end)
                {
                    date_limitation = date.AddDays(29 + ((date - date_start).Days) * 6);
                    ViewBag.FirstLine = GetDate(date_limitation, date_limitation.Year);
                    date_limitation = date.AddDays(27 + ((date - date_start).Days) * 6);
                    ViewBag.SecondLine = GetDate(date_limitation, date_limitation.Year);
                }
                else
                {
                    date_limitation = date.AddDays(59);
                    ViewBag.FirstLine = GetDate(date_limitation, date_limitation.Year);
                    date_limitation = date.AddDays(57);
                    ViewBag.SecondLine = GetDate(date_limitation, date_limitation.Year);
                }
            }

            Response.Buffer = true;
            Response.ExpiresAbsolute = System.DateTime.Now.AddSeconds(-1);
            Response.Expires = 0;
            Response.CacheControl = "no-cache";   

            return View();
        }

        private static string GetDate(DateTime date, int year)
        {
            // 修复农历与阳历年份不一致时引起的bug
            #region
            Dictionary<int, DateTime> hardcode = new Dictionary<int, DateTime>();
            hardcode.Add(2016, DateTime.Parse("2016-02-08"));
            hardcode.Add(2017, DateTime.Parse("2017-01-28"));
            hardcode.Add(2018, DateTime.Parse("2018-02-16"));
            hardcode.Add(2019, DateTime.Parse("2019-02-05"));
            hardcode.Add(2020, DateTime.Parse("2019-01-25"));
            if (hardcode.ContainsKey(year))
            {
                DateTime dt = hardcode[year];
                if (date < dt)
                    year--;
            }
            #endregion

            Dictionary<string, string> weekDays = new Dictionary<string, string>() { { "Monday", "周一" }, { "Tuesday", "周二" }, { "Wednesday", "周三" } 
            , { "Thursday", "周四" }, { "Friday", "周五" }, { "Saturday", "周六" }, { "Sunday", "周日" }};
            Dictionary<int, string> clcMonths = new Dictionary<int, string>(){{1,"正"},{2,"二"},{3,"三"},{4,"四"},{5,"五"},{6,"六"},{7,"七"},{8,"八"}
                ,{9,"九"},{10,"十"},{11,"十一"},{12,"腊"},};
            Dictionary<int, string> clcDays = new Dictionary<int, string>(){{1,"初一"},{2,"初二"},{3,"初三"},{4,"初四"},{5,"初五"},{6,"初六"},{7,"初七"},{8,"初八"}
                ,{9,"初九"},{10,"初十"},{11,"十一"},{12,"十二"},{13,"十三"},{14,"十四"},{15,"十五"},{16,"十六"},{17,"十七"},{18,"十八"},{19,"十九"},{20,"二十"}
                ,{21,"廿一"},{22,"廿二"},{23,"廿三"},{24,"廿四"},{25,"廿五"},{26,"廿六"},{27,"廿七"},{28,"廿八"},{29,"廿九"},{30,"三十"}};
            StringBuilder sb = new StringBuilder();
            sb.Append(date.ToString("MM月dd日 "));
            sb.Append(weekDays[date.DayOfWeek.ToString()] + " ");
            ChineseLunisolarCalendar clc = new ChineseLunisolarCalendar();
            //判断是否有闰月
            if (clc.GetMonthsInYear(year) == 13)
            {
                //第几个月为闰月
                int leapMonth = clc.GetLeapMonth(date.Year);
                if (clc.GetMonth(date) < leapMonth)
                    sb.Append("农历" + clcMonths[clc.GetMonth(date)] + "月");
                else if (clc.GetMonth(date) == leapMonth)
                    sb.Append("农历闰" + clcMonths[clc.GetMonth(date) - 1] + "月");
                else
                    sb.Append("农历" + clcMonths[clc.GetMonth(date) - 1] + "月");
            }
            else
            {
                sb.Append("农历" + clcMonths[clc.GetMonth(date)] + "月");
            }
            sb.Append(clcDays[clc.GetDayOfMonth(date)]);
            Regex regex = new Regex("0\\d");
            MatchCollection matches = regex.Matches(sb.ToString());
            foreach (Match match in matches)
            {
                sb.Replace(match.Value, match.Value.Substring(1, 1));
            }
            return sb.ToString();
        }
    }
}