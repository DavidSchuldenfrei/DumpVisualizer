using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace DumpVisualizer
{
    public class HtmlDlg : Form
    {
        private IContainer components = null;
        private string _html;
        private readonly string _fileName;
        private Panel panel1;
        private Button button1;
        private WebBrowser webBrowser1;

        public HtmlDlg()
        {
            InitializeComponent();
            _fileName = Path.GetTempFileName();
            File.Delete(_fileName);
            _fileName = Path.ChangeExtension(_fileName, "html");
        }

        public void Init(string html)
        {
            _html = html;
            webBrowser1.DocumentText = html;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            File.WriteAllText(_fileName, _html);
            Process.Start(_fileName);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null)
                components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            panel1 = new Panel();
            button1 = new Button();
            webBrowser1 = new WebBrowser();
            panel1.SuspendLayout();
            SuspendLayout();
            panel1.Controls.Add((Control)button1);
            panel1.Dock = DockStyle.Top;
            panel1.Location = new Point(0, 0);
            panel1.Name = "panel1";
            panel1.Size = new Size(700, 51);
            panel1.TabIndex = 1;
            button1.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            button1.Location = new Point(584, 12);
            button1.Name = "button1";
            button1.Size = new Size(104, 23);
            button1.TabIndex = 0;
            button1.Text = "View in Browser";
            button1.UseVisualStyleBackColor = true;
            button1.Click += new EventHandler(button1_Click);
            webBrowser1.Dock = DockStyle.Fill;
            webBrowser1.Location = new Point(0, 51);
            webBrowser1.MinimumSize = new Size(20, 20);
            webBrowser1.Name = "webBrowser1";
            webBrowser1.Size = new Size(700, 374);
            webBrowser1.TabIndex = 2;
            AutoScaleDimensions = new SizeF(6f, 13f);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(700, 425);
            Controls.Add((Control)webBrowser1);
            Controls.Add((Control)panel1);
            MinimizeBox = false;
            Name = nameof(HtmlDlg);
            Text = "Html Viewer";
            panel1.ResumeLayout(false);
            ResumeLayout(false);
        }
    }
}
