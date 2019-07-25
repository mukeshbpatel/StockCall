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

    public class RTStockPrice
    {
        private const string SYMBOL = "{{SYMBOL}}";
        private const string INTERVAL = "{{INTERVAL}}";
        private const string TIME_PERIOD = "{{TIME_PERIOD}}";
        private const string APIKEY = "{{APIKEY}}";

        private string URL_StockPrice = @"https://www.alphavantage.co/query?function=TIME_SERIES_INTRADAY&symbol=NSE:{{SYMBOL}}&interval={{INTERVAL}}&apikey={{APIKEY}}&datatype=csv";
        private string URL_EMA = @"https://www.alphavantage.co/query?function=EMA&symbol=NSE:{{SYMBOL}}&interval={{INTERVAL}}&time_period={{TIME_PERIOD}}&series_type=close&apikey={{APIKEY}}&datatype=csv";
        private string URL_VWAP = @"https://www.alphavantage.co/query?function=VWAP&symbol=NSE:{{SYMBOL}}&interval={{INTERVAL}}&apikey={{APIKEY}}&datatype=csv";

        private string _path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Output");

        List<StockCall> call = new List<StockCall>();

        public List<StockCall> GetStockCall(bool RunInThread = true)
        {

            Master.MoveDirectoryFile(_path);

            foreach (var item in Master.Scripts())
            {
                if (RunInThread)
                {
                    Thread childThread = new Thread(() => GetStockPrice(item));
                    childThread.Start();
                }
                else
                {
                    GetStockPrice(item);
                }
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
            System.IO.File.WriteAllText(Path.Combine(_path, script.Code + "_" + script.ProxyDurty.ToString() + "_" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".csv"), s.ToString());
            var call = t.First();
            if (call != null)
            {
                if (!string.IsNullOrEmpty(call.Call) && call.Date.Day == DateTime.Today.Day && call.Date.Month == DateTime.Today.Month)
                {
                    call.AvgVolume = t.Average(a => a.Volume);
                    Master.Sendmail(call);
                    if (!File.Exists(Path.Combine(_path, "Tread_" + DateTime.Now.ToString("yyyyMMdd") + ".txt")))
                    {
                        System.IO.File.AppendAllText(Path.Combine(_path, "Tread_" + DateTime.Now.ToString("yyyyMMdd") + ".txt"), "Stock,Call,Date,Open,High,Low,Close,EMA21,Volume,Size");
                    }
                    System.IO.File.AppendAllText(Path.Combine(_path, "Tread_" + DateTime.Now.ToString("yyyyMMdd") + ".txt"),Environment.NewLine + call.Print());
                }
            }
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
        public Decimal Volume;
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
        private string numStyle = "##,##,##0.00";
        private string _output = @"{{Stock}},{{Call}},{{Date}},{{Open}},{{High}},{{Low}},{{Close}},{{EMA21}},{{Volume}},{{Size}}";

        private string _mail = @"
                    <table style='border-collapse:collapse;font-family:Verdana,Arial;min-width:250px; border: 1px solid silver;'>    
                            <tr class='{{Call}}'><th>{{Stock}}</th><th>{{Call}}</th></tr>
                            <tr><td style='border: 1px solid silver;'>Date</td><td style='border: 1px solid silver;'>{{Date}}</td></tr>
                            <tr><td style='border: 1px solid silver;'>Open</td><td style='border: 1px solid silver;'>{{Open}}</td></tr>
                            <tr><td style='border: 1px solid silver;'>High</td><td style='border: 1px solid silver;'>{{High}}</td></tr>
                            <tr><td style='border: 1px solid silver;'>Low</td><td style='border: 1px solid silver;'>{{Low}}</td></tr>
                            <tr><td style='border: 1px solid silver;'>Close</td><td style='border: 1px solid silver;'>{{Close}}</td></tr>
                            <tr><td style='border: 1px solid silver;'>EMA21</td><td style='border: 1px solid silver;'>{{EMA21}}</td></tr>
                            <tr><td style='border: 1px solid silver;'>Volume</td><td style='border: 1px solid silver;'>{{Volume}}({{Avg}})</td></tr>
                            <tr><td style='border: 1px solid silver;'>Body</td><td style='border: 1px solid silver;'>{{Size}}({{Body}})</td></tr>    
                    </table>";

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
        public String Status { get; set; }
        public Decimal Entry { get; set; }
        public Decimal Exit { get; set; }
        public int LotSize { get; set; }
        public Decimal AvgVolume { get; set; }
        public string Print()
        {
            return _output.Replace("{{Stock}}", Stock)
                .Replace("{{Date}}", this.Date.ToString("dd/MM/yyyy hh:mm tt"))
                .Replace("{{Call}}", Call)
                .Replace("{{Open}}", this.Open.ToString("0.00"))
                .Replace("{{Close}}", this.Close.ToString("0.00"))
                .Replace("{{High}}", this.High.ToString("0.00"))
                .Replace("{{Low}}", this.Low.ToString("0.00"))
                .Replace("{{EMA21}}", this.EMA21.ToString("0.00"))
                .Replace("{{Size}}", this.Size.ToString("0.00"))
                .Replace("{{Volume}}", this.Volume.ToString());
        }

        public string MilSubject()
        {
            string _out = Call + " " + Stock;
            if (Call == "BUY")
            {
                _out += " Above " + High.ToString(numStyle);
            }
            else
            {
                _out += " Below " + Low.ToString(numStyle);
            }
            return _out;
        }

        public string MailBody()
        {
            return _mail.Replace("{{Stock}}", Stock)
                .Replace("{{Date}}", this.Date.ToString("dd/MM/yy hh:mm tt"))
                .Replace("{{Call}}", Call)
                .Replace("{{Open}}", this.Open.ToString(numStyle))
                .Replace("{{Close}}", this.Close.ToString(numStyle))
                .Replace("{{High}}", this.High.ToString(numStyle))
                .Replace("{{Low}}", this.Low.ToString(numStyle))
                .Replace("{{EMA21}}", this.EMA21.ToString(numStyle))
                .Replace("{{Size}}", this.Size.ToString(numStyle))
                .Replace("{{Body}}", (this.Low * 0.003M).ToString(numStyle))
                .Replace("{{Volume}}", Master.FormatVolume(this.Volume))
                .Replace("{{Avg}}", Master.FormatVolume(this.AvgVolume));
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


