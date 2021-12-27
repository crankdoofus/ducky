/*This file is part of DuckDNS.NET
    2014 Max J. Rodríguez Beltran ing.maxjrb[at]gmail.com

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/

using System;
using System.Data;
using System.Net;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using DuckDNS.NET.Properties;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Reflection;

namespace DuckDNS.NET
{
    public partial class frmMain : Form
    {
        public frmMain()
        {
            InitializeComponent();
        }

        private DataTable dtDomains = new DataTable();

        private static void Download(string cDomain)
        {
            var wcResponse = "";
            using (var wc = new WebClient())
            {
                wc.DownloadStringCompleted += (sender, e) =>
                {
                    using (var compWc = (WebClient)sender)
                    {
                        var url = e.UserState as string;
                        Console.WriteLine(compWc.ResponseHeaders[HttpResponseHeader.Server]);
                        Console.WriteLine(url);
                    }   
                };
                // if successful wcResponse is OK, else KO
                wcResponse = wc.DownloadString(new Uri(cDomain));

            }             
        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            if (txtDomain.TextLength <= 0 || txtToken.TextLength <= 0) return;
            if (txtIP.Text.Length > 0)
            {
                var validIp = new Regex(@"\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}").IsMatch(txtIP.Text.Trim());
                if (!validIp)
                {
                    MessageBox.Show("Not valid IP Format, please check...","Format not valid", MessageBoxButtons.OK,MessageBoxIcon.Error);
                    return;
                }
            }

            dtDomains.Rows.Add(txtDomain.Text.Trim(), txtToken.Text.Trim(), txtIP.Text.Trim());
            txtDomain.Text = string.Empty;
            txtToken.Text = string.Empty;
            txtIP.Text = string.Empty;
        }

        private void frmMain_Load(object sender, EventArgs e)
        {
            dtDomains.Columns.Add("Domain");
            dtDomains.Columns.Add("Token");
            dtDomains.Columns.Add("IP");
            dtDomains.Rows.Clear();
            
            timer1.Interval = Settings.Default.timeLapse * 60000; //Interval is in ms, settings are in minutes, 1 min = 60000 ms
            loadGrid();
            dgDomains.DataSource = dtDomains;
            timer1.Enabled = true;
            timer1.Start();

            updateDomains();
        }
        private void updateDomains()
        {
            int minutes = Settings.Default.timeLapse;


            lblExtIP.Text = "Your IP: " + getExternalIP();

            if (dgDomains.RowCount <= 0) return;
            foreach (DataGridViewRow row in dgDomains.Rows)
            {
                var getdom = string.Format("https://www.duckdns.org/update?domains={0}&token={1}&ip={2}",
                    row.Cells[0].Value, row.Cells[1].Value, row.Cells[2].Value);
                Download(getdom);
            }
            lblStatus.Text = "All domains updated...";
            myNotifyIcon.BalloonTipText = "Next update: " + DateTime.Now.AddMinutes(minutes).ToShortTimeString();
            myNotifyIcon.Text = "DuckDNS.NET \r\n Next Update: " +
                                DateTime.Now.AddMinutes(minutes).ToShortTimeString();

        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            updateDomains();
        }

        private void settingsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var fSettings = new frmSettings();
            fSettings.ShowDialog();
        }

        private void loadGrid()
        {
            if (Settings.Default.domains != "default")  
            dtDomains = (DataTable)JsonConvert.DeserializeObject(Settings.Default.domains, (typeof(DataTable)));  
        }

        private void frmMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            var json = JsonConvert.SerializeObject(dtDomains);
            if (json == "[]") json = "default";
            
            Settings.Default.domains = json;
            
            Settings.Default.Save();
        }

        private void deleteDomainToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (dgDomains.RowCount <= 0) return;
            var row = new string[3];
            int nCell = 0;

            if (dgDomains.CurrentRow == null) return;
            foreach (DataGridViewCell cell in dgDomains.CurrentRow.Cells)
            {
                row[nCell] = cell.Value.ToString();
                nCell++;
            }

            txtDomain.Text = row[0];
            txtToken.Text = row[1];
            txtIP.Text = row[2];

            dgDomains.Rows.RemoveAt(dgDomains.CurrentRow.Index);
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            
            //get build timestamp
            var timestamp = Properties.Resources.BuildTimeStamp;

            MessageBox.Show("DuckDNS.NET a GUI client for DuckDNS service" +
                            "\r\nBuild timestamp: " + timestamp +
                            "\r\nBased on version by Max Rodríguez (2014)." +
                            "\r\nCheck Source: https://bitbucket.org/Jaxmetalmax/duckdns.net",
                            "DuckDNS.NET GUI Client." , 
                            MessageBoxButtons.OK,MessageBoxIcon.Information);
        }

        private void frmMain_Resize(object sender, EventArgs e)
        {
            if (FormWindowState.Minimized != this.WindowState) return;
            myNotifyIcon.Visible = true;
            this.ShowInTaskbar = false;
        }

        private void myNotifyIcon_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            this.WindowState = FormWindowState.Normal;
            this.ShowInTaskbar = true;
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private string getExternalIP()
        {
            try
            {
                var externalIP = string.Empty;
                externalIP = (new WebClient()).DownloadString("http://www.showmemyip.com/");
                externalIP = (new Regex(@"\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}"))
                             .Matches(externalIP)[0].ToString();
                return externalIP;
            }
            catch { return null; }
        }

        private void btnAutoIP_Click(object sender, EventArgs e)
        {
            txtIP.Text = getExternalIP();
        }
    }
}
