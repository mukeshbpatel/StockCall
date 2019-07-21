using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Mail;

namespace RealTimeStockPrice
{
    public static class Master
    {
        public static decimal BodySize = 0.003M;
        private static string API_Proxy = @"http://api.scraperapi.com?api_key=be40c9c3ca77bc8b0632e4e508873845&url={{URL}}";
        private static List<Script> lstScript = new List<Script>();

        public static List<Script> Scripts()
        {
            if (lstScript.Count == 0)
                GetScripts();
            return lstScript;
        }

        private static void GetScripts()
        {
            string[] txtScript = File.ReadAllLines(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "Script.csv"));
            foreach (var item in txtScript)
            {
                string[] a = item.Split(',');
                if (a[0] != "Script")
                    lstScript.Add(new Script { Code = a[0], key = a[1]});
            }
        }

        public static string IsCall(StockValue a, EMAValues b) // DateTime date, decimal open, decimal high, decimal low, decimal close, decimal volume, decimal body, decimal size, decimal ema)
        {
            if ((a.Date.Hour + a.Date.Minute) != 24 && Math.Abs(a.Body) > (a.Low * BodySize))
            {
                if ((a.Open < a.Close) && (b.EMA > a.Open) && (b.EMA < a.Close)) // && (c.EMA > a.Open) && (c.EMA < a.Close))
                    return "BUY";
                if ((a.Open >= a.Close) && (b.EMA > a.Close) && (b.EMA < a.Open)) // && (c.EMA > a.Close) && (c.EMA < a.Open))
                    return "SELL";
            }
            return string.Empty;
        }


        public static string GetData(string url, Script script)
        {
            string results = string.Empty;

            ProxyServer t;
            if (script.ProxyDurty == false && !string.IsNullOrEmpty(script.ProxyURL) && script.ProxyPort > 0)
                t = new ProxyServer { IsDurty = script.ProxyDurty, URL = script.ProxyURL,Port = script.ProxyPort};
            else
                t = MyProxy.GetProxy();
            
            if (t == null)
            {
                url = API_Proxy.Replace("{{URL}}", url);
                script.ProxyDurty = true;
            }
            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
            if (t != null)
                req.Proxy = new WebProxy(t.URL, t.Port);
            try
            {
                using (HttpWebResponse resp = (HttpWebResponse)req.GetResponse())
                {
                    using (StreamReader sr = new StreamReader(resp.GetResponseStream()))
                    {
                        results = sr.ReadToEnd();
                        if (results.Length < 1000)
                        {
                            script.ProxyURL = string.Empty;
                            script.ProxyPort = 0;
                            script.ProxyDurty = true;
                            System.Threading.Thread.Sleep(60010);
                            results = GetData(url, script);
                        }
                        else
                        {
                            script.ProxyURL = t.URL;
                            script.ProxyPort = t.Port;
                            script.ProxyDurty = false;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                script.ProxyURL = string.Empty;
                script.ProxyPort = 0;
                script.ProxyDurty = true;
                results = GetData(url, script);
            }
            return results;
        }

        public static void MoveDirectoryFile(string path)
        {
            DirectoryInfo source = new DirectoryInfo(path);
            DirectoryInfo destination = new DirectoryInfo(path + "\\Backup");

            if (!source.Exists)
            {
                source.Create();
            }

            if (!destination.Exists)
            {
                destination.Create();
            }

            // Copy all files.
            FileInfo[] files = source.GetFiles();
            foreach (FileInfo file in files)
            {
                file.MoveTo(Path.Combine(destination.FullName, file.Name));
            }
        }

        public static void Sendmail(StockCall call)
        {
            using (SmtpClient client = new SmtpClient())
            {
                client.DeliveryMethod = SmtpDeliveryMethod.Network;
                client.EnableSsl = true;
                client.Host = "smtp.mail.yahoo.com";
                client.Port = 587;
                // setup Smtp authentication
                System.Net.NetworkCredential credentials = new System.Net.NetworkCredential("ygcfund@yahoo.com", "y@cfund!@#$");
                client.UseDefaultCredentials = false;
                client.Credentials = credentials;
                using (MailMessage msg = new MailMessage())
                {
                    msg.From = new MailAddress("ygcfund@yahoo.com", "Market Call");
                    msg.To.Add(new MailAddress("mukeshbpatel@gmail.com", "Mukesh Patel"));
                    msg.To.Add(new MailAddress("anjanampatel@gmail.com", "Anjana Patel"));
                    msg.Subject = call.Call + " " + call.Stock;
                    if (call.Call == "BUY")
                    {
                        msg.Subject += " Above " + call.High.ToString("0.00");
                    }
                    else
                    {
                        msg.Subject += " Below " + call.Low.ToString("0.00");
                    }
                    msg.IsBodyHtml = true;
                    msg.Body = call.Print();
                    client.Send(msg);
                }
            }
        }
    }
}
