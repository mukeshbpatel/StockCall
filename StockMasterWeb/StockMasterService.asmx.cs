using RealTimeStockPrice;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Services;

namespace StockMasterWeb
{
    /// <summary>
    /// Summary description for StockMasterService
    /// </summary>
    [WebService(Namespace = "http://tempuri.org/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [System.ComponentModel.ToolboxItem(false)]
    // To allow this Web Service to be called from script, using ASP.NET AJAX, uncomment the following line. 
    // [System.Web.Script.Services.ScriptService]
    public class StockMasterService : System.Web.Services.WebService
    {

        [WebMethod]
        public List<StockCall> GetScriptData(string script)
        {
            List<StockCall> calls = new List<StockCall>();
            using (StockMasterEntities et = new StockMasterEntities())
            {
                foreach (var call in et.ScriptDatas.Where(s => s.Code == script).OrderBy(t => t.tDate))
                {
                    calls.Add(new StockCall
                    {
                        Stock = call.Code,
                        Date = call.tDate,
                        Open = call.tOpen,
                        High = call.tHigh,
                        Low = call.tLow,
                        Close = call.tClose,
                        Volume = Convert.ToDecimal(call.Volume),
                        EMA9 = call.EMA9,
                        EMA21 = call.EMA21,
                        Call = call.tCall,
                        Status = call.tStatus,
                        Entry = call.tEntry,
                        Exit = call.tExit,
                        LotSize = call.LotSize
                    });
                }
            }
            return calls;
        }

        [WebMethod]
        public List<StockCall> GetScriptCalls(string script)
        {
            List<StockCall> calls = new List<StockCall>();
            using (StockMasterEntities et = new StockMasterEntities())
            {
                foreach (var call in et.ScriptDatas.Where(s => s.Code == script && (s.tCall=="BUY" || s.tCall=="SELL")).OrderByDescending(t => t.tDate))
                {
                    calls.Add(new StockCall
                    {
                        Stock = call.Code,
                        Date = call.tDate,
                        Open = call.tOpen,
                        High = call.tHigh,
                        Low = call.tLow,
                        Close = call.tClose,
                        Volume = Convert.ToDecimal(call.Volume),
                        EMA9 = call.EMA9,
                        EMA21 = call.EMA21,
                        Call = call.tCall,
                        Status = call.tStatus,
                        Entry = call.tEntry,
                        Exit = call.tExit,
                        LotSize = call.LotSize
                    });
                }
            }
            return calls;
        }

        [WebMethod]
        public bool AddScriptData(List<StockCall> calls)
        {
            using (StockMasterEntities et = new StockMasterEntities())
            {
                foreach (var call in calls)
                {
                    et.ScriptDatas.Add(new ScriptData
                    {
                        Code = call.Stock,
                        tDate = call.Date,
                        tOpen = call.Open,
                        tHigh = call.High,
                        tLow = call.Low,
                        tClose = call.Close,
                        Volume = Convert.ToInt64(call.Volume),
                        EMA9 = call.EMA9,
                        EMA21 = call.EMA21,
                        tCall = call.Call,
                        tStatus = "Initial",
                        tEntry = 0,
                        tExit = 0,
                        LotSize = 0
                    });
                }
                et.SaveChangesAsync();
            }
            return true;
        }
    }
}
