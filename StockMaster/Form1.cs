using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using RealTimeStockPrice;

namespace StockMaster
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            MyProxy.VerifyProxy();
        }

        private void TimerProxy_Tick(object sender, EventArgs e)
        {
            if (MyProxy.GoodProxyServer.Count <= (Master.Scripts().Count() * 2))
            {
                MyProxy.VerifyProxy();
            }
            else
            {
                DateTime dt = DateTime.Now;
                if (dt.Minute == 32 || dt.Minute == 25 || dt.Minute == 42 || dt.Minute == 55)
                {
                    MyProxy.GoodProxyServer.Clear();
                    MyProxy.VerifyProxy();
                }
            }
        }

        private void TimerScript_Tick(object sender, EventArgs e)
        {
            DateTime dt = DateTime.Now;
            if (dt.Minute == 00 || dt.Minute == 15 || dt.Minute == 30 || dt.Minute == 45)
            {
                RTStockPrice stockPrice = new RealTimeStockPrice.RTStockPrice();
                var t = stockPrice.GetStockCall();
            }
        }
    }
}
