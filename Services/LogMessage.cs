using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace NaukriProfileUpdater.Services
{
    public static class LogMessage 
    {
        public static void Log(string ControllerName, string actionName, string url, string Message, bool IsFile = false, bool Isrequest = false, [CallerMemberName] string ActionName = null)
        {
            string path = System.IO.Directory.GetCurrentDirectory();
            //string path = _appSettings.LocationPath;
            path = path.Replace("\\", "/");
            string RequestResponseLogDirectory = path + "/Logger/RequestResponseLog/";
            //Tools.LogToDB(2, url, ControllerName, Message, "Need to check", 0, 0, "Need to check", "Need to check", "ADD", null, actionName);
            if (IsFile)
            {
                DateTime nowDate = DateTime.Now;
                RequestResponseLogDirectory = RequestResponseLogDirectory + nowDate.Day + "_" + nowDate.Month + "_" + nowDate.Year;
                if (!Directory.Exists(RequestResponseLogDirectory))
                {
                    Directory.CreateDirectory(RequestResponseLogDirectory);
                }
                try
                {
                    if (Isrequest)
                    {
                        File.AppendAllText((RequestResponseLogDirectory + "/" + ControllerName + "Log.txt"), string.Format("\r\n\r\n====Url : {0} -----@{1} \r\n --- In ActionName -> {2}\r\n-->Request :{3}", url, DateTime.Now.ToString(), actionName, Message));
                    }
                    else
                    {
                        File.AppendAllText((RequestResponseLogDirectory + "/" + ControllerName + "Log.txt"), string.Format("\r\n\r\n====Url : {0} -----@{1}\r\n --- In ActionName -> {2}\r\n-->Response :{3}", url, DateTime.Now.ToString(), actionName, Message));
                    }
                    // write request log in the file
                }
                catch (Exception ex)
                {
                }
            }
            // Create date wise folder for the logs
        }
        
    }
}
