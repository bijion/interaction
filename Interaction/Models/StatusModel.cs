using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Interaction.Models
{
    public class StatusModel
    {
        public string Error_Code { get; set; }
        public string Error { get; set; }

        public StatusModel()
        {

        }

        public StatusModel(string statusCode, string statusInfo)
        {
            Error_Code = statusCode;
            Error = statusInfo;
        }

        public string JsonFormatter()
        {
            return JsonFormatter(string.Empty);
        }

        public string JsonFormatter(string callback)
        {
            StringBuilder sf = new StringBuilder();
            if (!string.IsNullOrEmpty(callback))
            {
                sf.Append(callback).Append("([");
            }
            sf.Append("{\"error_code\":").Append("\"").Append(Error_Code).Append("\", \"error\":\"").Append(Error).Append("\"}");
            if (!string.IsNullOrEmpty(callback))
            {
                sf.Append("]);");
            }
            return sf.ToString();
        }
    }
}
