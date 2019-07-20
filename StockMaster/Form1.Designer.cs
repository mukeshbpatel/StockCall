namespace StockMaster
{
    partial class Form1
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.timerProxy = new System.Windows.Forms.Timer(this.components);
            this.timerScript = new System.Windows.Forms.Timer(this.components);
            this.txtLog = new System.Windows.Forms.RichTextBox();
            this.SuspendLayout();
            // 
            // timerProxy
            // 
            this.timerProxy.Enabled = true;
            this.timerProxy.Interval = 60000;
            this.timerProxy.Tick += new System.EventHandler(this.TimerProxy_Tick);
            // 
            // timerScript
            // 
            this.timerScript.Enabled = true;
            this.timerScript.Interval = 60000;
            this.timerScript.Tick += new System.EventHandler(this.TimerScript_Tick);
            // 
            // txtLog
            // 
            this.txtLog.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtLog.Location = new System.Drawing.Point(0, 0);
            this.txtLog.Name = "txtLog";
            this.txtLog.Size = new System.Drawing.Size(477, 286);
            this.txtLog.TabIndex = 0;
            this.txtLog.Text = "";
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(477, 286);
            this.Controls.Add(this.txtLog);
            this.Name = "Form1";
            this.Text = "Stock Master";
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Timer timerProxy;
        private System.Windows.Forms.Timer timerScript;
        private System.Windows.Forms.RichTextBox txtLog;
    }
}

