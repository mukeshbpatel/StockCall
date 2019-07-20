using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RealTimeStockPrice;
using System.Net.NetworkInformation;
using System.IO;

namespace StockPriceTest
{

    class Program
    {

    
        static void Main(string[] args)
        {

            RealTimeStockPrice.MyProxy.VerifyProxy();

            System.Threading.Thread.Sleep(200000);

            RTStockPrice stockPrice = new RealTimeStockPrice.RTStockPrice();
            var t = stockPrice.GetStockCall();     
        }
    }
}
