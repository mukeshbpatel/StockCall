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
                txtLog.AppendText(DateTime.Now.ToLongTimeString() + " TimerProxy: Count < Script");
                MyProxy.VerifyProxy();
            }
            else
            {
                DateTime dt = DateTime.Now;
                if (dt.Minute == 10 || dt.Minute == 25 || dt.Minute == 40 || dt.Minute == 55)
                {
                    txtLog.AppendText(DateTime.Now.ToLongTimeString() + " TimerProxy: 10,25,40,55");
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
                txtLog.AppendText(DateTime.Now.ToLongTimeString() + " TimerScript: 00,15,30,45");
                RTStockPrice stockPrice = new RealTimeStockPrice.RTStockPrice();
                var t = stockPrice.GetStockCall();
            }
        }
    }
}
