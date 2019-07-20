using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.IO;
using System.Net.NetworkInformation;
using System.Net;

namespace RealTimeStockPrice
{
    public static class MyProxy
    {
        private static List<ProxyServer> lstProxyServer = new List<ProxyServer>();
        public static List<ProxyServer> GoodProxyServer = new List<ProxyServer>();

        public static ProxyServer GetProxy()
        {
           
            if (GoodProxyServer.Count>0)
            {
                var last = GoodProxyServer.OrderBy(o=>o.ResponseTime).First();
                if (last != null)
                {
                    GoodProxyServer.Remove(last);
                }
                return last;
            }
            return null;
        }

        public static List<ProxyServer> AllProxy()
        {
            if (lstProxyServer.Count == 0)
                InitializeProxyServers();
            return lstProxyServer;
        }

        private static void InitializeProxyServers()
        {
            string[] txtProxyServer = File.ReadAllLines(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "ProxyServers.csv"));
            foreach (var item in txtProxyServer)
            {
                string[] a = item.Split(',');
                lstProxyServer.Add(new ProxyServer { URL = a[0], Port = int.Parse(a[1]) });
            }
        }

        public static void VerifyProxy()
        {
            foreach (var item in AllProxy())
            {
               Thread childThread = new Thread(() => ConnectToNet(item));
               childThread.Start();
            }
        }

        private static bool CanPing(string address)
        {
            Ping ping = new Ping();
            try
            {
                PingReply reply = ping.Send(address, 2000);
                if (reply == null) return false;

                return (reply.Status == IPStatus.Success);
            }
            catch (PingException)
            {
                return false;
            }
        }

        private static void ConnectToNet(ProxyServer proxy)
        {
            string URL = @"http://www.nseindia.com/live_market/dynaContent/live_analysis/gainers/niftyGainers1.json";
            string results = string.Empty;
            try
            {
                if (CanPing(proxy.URL))
                {
                    HttpWebRequest req = (HttpWebRequest)WebRequest.Create(URL);
                    req.Proxy = new WebProxy(proxy.URL, proxy.Port);
                    DateTime t = DateTime.Now;
                    using (HttpWebResponse resp = (HttpWebResponse)req.GetResponse())
                    {
                        using (StreamReader sr = new StreamReader(resp.GetResponseStream()))
                        {
                            results = sr.ReadToEnd();
                            proxy.ResponseTime = DateTime.Now.Subtract(t).Milliseconds;
                            proxy.IsDurty = false;
                            proxy.LastVerified = DateTime.Now;
                            GoodProxyServer.Add(proxy);
                        }
                    }
                }
            }
            catch (Exception)
            {
            }           
        }
    }
}
