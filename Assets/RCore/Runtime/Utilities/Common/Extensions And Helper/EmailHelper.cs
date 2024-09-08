/***
 * Author RadBear - nbhung71711 @gmail.com - 2017
 **/

using System;
using System.Net;
using System.Net.Mail;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using UnityEngine;
using UnityEngine.Networking;

namespace RCore.Common
{
    public class EmailHelper
    {
        private const string PROJECT_NAME = "";

        public static void SendEmailByDefaultApp(string pSubject, string pBody, string pEmailTo, string[] pCCTo)
        {
            string subject = EscapeURL(pSubject);
            string body = EscapeURL(pBody);
            string cc = "";
            string url = "mailto:{0}?subject={1}&body={2}";
            if (pCCTo != null && pCCTo.Length > 0)
            {
                url = "mailto:{0}?subject={1}&body={2}&cc={3}";
                cc = string.Join(";", pCCTo);
            }
            url = string.Format(url, pEmailTo, pSubject, pBody, cc);

            Application.OpenURL(url);
        }

        private static string EscapeURL(string url)
        {
            return UnityWebRequest.EscapeURL(url).Replace("+", "%20");
        }

        public static void SendEmailBySMTP(string pSubject, string pBody, string pEmailTo, string[] pCCTo, string pTemplateEmail, string pTemplatePassword)
        {
            var mail = new MailMessage();

            mail.From = new MailAddress(pTemplateEmail);
            mail.To.Add(pEmailTo);
            mail.Subject = pSubject;
            mail.Body = pBody;
            if (pCCTo != null && pCCTo.Length > 0)
            {
                for (int i = 0; i < pCCTo.Length; i++)
                {
                    mail.CC.Add(pCCTo[i]);
                }
            }

            var smtpServer = new SmtpClient("smtp.gmail.com");
            smtpServer.Port = 587;
            smtpServer.Credentials = new NetworkCredential(pTemplateEmail, pTemplatePassword) as ICredentialsByHost;
            smtpServer.EnableSsl = true;

            ServicePointManager.ServerCertificateValidationCallback = delegate (object s, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
            {
                return true;
            };

            smtpServer.Send(mail);
        }

        public static string CreateEmailReportSubject()
        {
            var dateTime = DateTime.Now;
            string subject = PROJECT_NAME + "_CRASH_" + dateTime.ToString("MM'_'dd'_'yyyy_HH'_'mm'_'ss.fff");
            return subject;
        }

        public static string CreateEmailFeedbackSubject()
        {
            string version = "Version: " + Application.version + "_" + RNative.getVersionCode();
            string sujbect = PROJECT_NAME + " FEEDBACK " + version;
            return sujbect;
        }

        public static string CreateEmailReportContent(string pMessage, string pUserId = "EMPTY")
        {
            string content = "\n\n\n-------- PLEASE DO NOT MODIFY THIS ----------\n";
            content += "Model: " + SystemInfo.deviceModel + "\n";
            content += "OS: " + SystemInfo.operatingSystem + "\n";
            content += "Version: " + Application.version + "_" + RNative.getVersionCode() + "\n";
            content += "UserId: " + pUserId + "\n";
            content += "---------------------------------------\n";
            content += pMessage;
            content += "\n\n";
            return content;
        }
    }
}