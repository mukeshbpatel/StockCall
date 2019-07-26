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
            DateTime dt = DateTime.Now;
            if (dt.Hour >= 9 && dt.Hour < 15)
            {
                if (MyProxy.GoodProxyServer.Count <= (Master.Scripts().Count() * 2))
                {
                    AddLog("Proxy: Count(" + MyProxy.GoodProxyServer.Count.ToString() + ") < Script");
                    MyProxy.VerifyProxy();
                }
                else
                {
                    if (dt.Minute == 10 || dt.Minute == 25 || dt.Minute == 40 || dt.Minute == 55)
                    {
                        AddLog("Proxy: 10,25,40,55 Count(" + MyProxy.GoodProxyServer.Count.ToString() + ")");
                        MyProxy.GoodProxyServer.Clear();
                        MyProxy.VerifyProxy();
                    }
                }
            }
        }

        private void TimerScript_Tick(object sender, EventArgs e)
        {
            DateTime dt = DateTime.Now;
            var t = dt.Hour + dt.Minute;
            if ((dt.Hour > 9 && dt.Hour < 15) || t==54)
            {               
                if (dt.Minute == 00 || dt.Minute == 15 || dt.Minute == 30 || dt.Minute == 45)
                {
                    AddLog("Script: 00,15,30,45 Count(" + MyProxy.GoodProxyServer.Count.ToString() + ")");
                    RTStockPrice stockPrice = new RealTimeStockPrice.RTStockPrice();
                    stockPrice.GetStockCall();
                }
            } else if (dt.Hour==15 && dt.Minute==0)
            {
                Master.SendFile();
                AddLog("Market Calls Summary sent.");
            }
        }

        private void AddLog(string log)
        {
            txtLog.AppendText(DateTime.Now.ToString("hh:mm:ss tt") + " " + log + Environment.NewLine);
        }

        private void BtnStart_Click(object sender, EventArgs e)
        {
            if (timerScript.Enabled)
            {
                timerScript.Enabled = false;
                timerProxy.Enabled = false;
                btnStart.Text = "&Start";
                AddLog("Timer Stopped");
            }
            else
            {
                timerScript.Enabled = true;
                timerProxy.Enabled = true;
                btnStart.Text = "&Stop";
                AddLog("Timer Started");
            }
        }

        private void Button2_Click(object sender, EventArgs e)
        {
            AddLog("Test run started. Count(" + MyProxy.GoodProxyServer.Count.ToString() + ")");
            RTStockPrice stockPrice = new RealTimeStockPrice.RTStockPrice();
            var t = stockPrice.GetStockCall(false);
            //Master.SendFile();
        }
    }
}
