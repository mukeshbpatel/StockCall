using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading;
using System.Threading.Tasks;


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
                    lstScript.Add(new Script { Code = a[0], key = a[1], ProxyURL = a[2], ProxyPort = int.Parse(a[3]) });
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
            var t = MyProxy.GetProxy();
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
                            System.Threading.Thread.Sleep(60010);
                            results = GetData(url, script);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
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


    public class RTStockPrice
    {
        private const string SYMBOL = "{{SYMBOL}}";
        private const string INTERVAL = "{{INTERVAL}}";
        private const string TIME_PERIOD = "{{TIME_PERIOD}}";
        private const string APIKEY = "{{APIKEY}}";

        private string URL_StockPrice = @"https://www.alphavantage.co/query?function=TIME_SERIES_INTRADAY&symbol=NSE:{{SYMBOL}}&interval={{INTERVAL}}&apikey={{APIKEY}}&datatype=csv";
        private string URL_EMA = @"https://www.alphavantage.co/query?function=EMA&symbol=NSE:{{SYMBOL}}&interval={{INTERVAL}}&time_period={{TIME_PERIOD}}&series_type=close&apikey={{APIKEY}}&datatype=csv";
        private string URL_VWAP = @"https://www.alphavantage.co/query?function=VWAP&symbol=NSE:{{SYMBOL}}&interval={{INTERVAL}}&apikey={{APIKEY}}&datatype=csv";


        List<StockCall> call = new List<StockCall>();

        public List<StockCall> GetStockCall()
        {

            Master.MoveDirectoryFile(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Output"));
            foreach (var item in Master.Scripts())
            {
                //GetStockPrice(item);

                Thread childThread = new Thread(() => GetStockPrice(item));
                childThread.Start();
            }
            return call;
        }

        public List<StockCall> GetStockPrice(Script script)
        {
            string CSV_Price = Master.GetData(URL_StockPrice.Replace(APIKEY, script.key).Replace(SYMBOL, script.Code).Replace(INTERVAL, "15min"), script);
            string CSV_EMA_21 = Master.GetData(URL_EMA.Replace(APIKEY, script.key).Replace(SYMBOL, script.Code).Replace(INTERVAL, "15min").Replace(TIME_PERIOD, "21"), script);
            List<StockValue> Price = CSV_Price
                                       .Replace('\n', '\r')
                                       .Split(new char[] { '\r' }, StringSplitOptions.RemoveEmptyEntries)
                                       .Skip(1)
                                       .Select(v => StockValue.FromCsv(v))
                                       .ToList();

            List<EMAValues> EMA_21 = CSV_EMA_21
                                    .Replace('\n', '\r')
                                    .Split(new char[] { '\r' }, StringSplitOptions.RemoveEmptyEntries)
                                    .Skip(1)
                                    .Select(v => EMAValues.FromCsv(v))
                                    .ToList();

            string easternZoneId = "Eastern Standard Time";
            TimeZoneInfo easternZone = TimeZoneInfo.FindSystemTimeZoneById(easternZoneId);
            var t = from a in Price
                    join b in EMA_21 on a.Date equals b.Date
                    //join c in EMA_9 on a.Date equals c.Date
                    //join d in VWAP on a.Date equals d.Date
                    select new StockCall
                    {
                        Stock = script.Code,
                        Date = a.Date,
                        Open = a.Open,
                        High = a.High,
                        Low = a.Low,
                        Close = a.Close,
                        Volume = a.Volume,
                        Body = a.Body,
                        Size = a.Size,
                        //EMA9 = c.EMA,
                        EMA21 = b.EMA,
                        //VWAP= d.VWAP,
                        Call = Master.IsCall(a, b)
                    };


            StringBuilder s = new StringBuilder();
            s.Append("Date,Open,High,Low,Close,Volume,Body,Size,EMA9,EMA21,VWAP,Call");
            foreach (var item in t)
            {
                s.Append(Environment.NewLine + item.Date.ToString() + "," + item.Open.ToString("0.00") + "," + item.High.ToString("0.00") + "," + item.Low.ToString("0.00") + "," + item.Close.ToString("0.00") + "," + item.Volume.ToString("0.00") + "," + item.Body.ToString("0.00") + "," + item.Size.ToString("0.00") + "," + item.EMA9.ToString("0.00") + "," + item.EMA21.ToString("0.00") + "," + item.VWAP.ToString("0.00") + "," + item.Call);
            }
            System.IO.File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Output", script.Code + "_" + script.ProxyDurty.ToString() + "_" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".csv"), s.ToString());

            var call = t.FirstOrDefault();
            if (call.Call != string.Empty)
                Master.Sendmail(call);
            return t.ToList();
        }
    }

    public class StockValue
    {
        public DateTime Date;
        public decimal Open;
        public decimal High;
        public decimal Low;
        public decimal Close;
        public decimal Volume;
        public decimal Body;
        public decimal Size;
        public bool tread;
        public static StockValue FromCsv(string csvLine)
        {
            string[] values = csvLine.Split(',');
            StockValue dailyValues = new StockValue();
            dailyValues.Date = Convert.ToDateTime(values[0]).AddMinutes(570);
            dailyValues.Open = Convert.ToDecimal(values[1]);
            dailyValues.High = Convert.ToDecimal(values[2]);
            dailyValues.Low = Convert.ToDecimal(values[3]);
            dailyValues.Close = Convert.ToDecimal(values[4]);
            dailyValues.Volume = Convert.ToDecimal(values[5]);
            dailyValues.Body = dailyValues.Close - dailyValues.Open;
            dailyValues.Size = dailyValues.High - dailyValues.Low;
            return dailyValues;
        }
    }

    public class EMAValues
    {
        public DateTime Date;
        public decimal EMA;

        public static EMAValues FromCsv(string csvLine)
        {
            string[] values = csvLine.Split(',');
            EMAValues dailyValues = new EMAValues();
            dailyValues.Date = Convert.ToDateTime(values[0]).AddMinutes(570);
            dailyValues.EMA = Convert.ToDecimal(values[1]);
            return dailyValues;
        }
    }

    public class VWAPValues
    {
        public DateTime Date;
        public decimal VWAP;

        public static VWAPValues FromCsv(string csvLine)
        {
            string[] values = csvLine.Split(',');
            VWAPValues dailyValues = new VWAPValues();
            dailyValues.Date = Convert.ToDateTime(values[0]).AddMinutes(570);
            dailyValues.VWAP = Convert.ToDecimal(values[1]);
            return dailyValues;
        }
    }

    public class StockCall
    {
        private string _output = @"
            {{Stock}}: {{Call}} <br/>
            Date: {{Date}} <br/>
            Open: {{Open}} <br/>
            High: {{High}} <br/>
            Low: {{Low}} <br/>
            Close: {{Close}} <br/>
            EMA21: {{EMA21}} <br/>
            Volume: {{Volume}} <br/>
            Size: {{Size}} <br/>";
        public string Stock { get; set; }
        public DateTime Date { get; set; }
        public Decimal Open { get; set; }
        public Decimal High { get; set; }
        public Decimal Low { get; set; }
        public Decimal Close { get; set; }
        public Decimal Volume { get; set; }
        public Decimal Body { get; set; }
        public Decimal Size { get; set; }
        public Decimal EMA9 { get; set; }
        public Decimal EMA21 { get; set; }
        public Decimal VWAP { get; set; }
        public String Call { get; set; }

        public string Print()
        {
            return _output.Replace("{{Stock}}", Stock)
                .Replace("{{Date}}", this.Date.ToLongDateString() + " " + this.Date.ToLongTimeString())
                .Replace("{{Call}}", Call)
                .Replace("{{Open}}", this.Open.ToString("0.00"))
                .Replace("{{Close}}", this.Close.ToString("0.00"))
                .Replace("{{High}}", this.High.ToString("0.00"))
                .Replace("{{Low}}", this.Low.ToString("0.00"))
                .Replace("{{EMA21}}", this.EMA21.ToString("0.00"))
                .Replace("{{Size}}", this.Size.ToString("0.00"))
                .Replace("{{Volume}}", this.Volume.ToString("0.00"));
        }
    }

    public class ProxyServer
    {
        public ProxyServer()
        {
            LastVerified = DateTime.Now;
            IsDurty = false;
        }
        public string URL { get; set; }
        public int Port { get; set; }

        public Boolean IsDurty { get; set; }
        public DateTime LastVerified { get; set; }

        public int ResponseTime { get; set; }
    }

    public class Script
    {
        public string Code { get; set; }
        public string key { get; set; }
        public string ProxyURL { get; set; }
        public int ProxyPort { get; set; }
        public bool ProxyDurty { get; set; }
    }
}


