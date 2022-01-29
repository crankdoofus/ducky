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

using DuckDNS.NET.Properties;
using Newtonsoft.Json;
using System;
using System.Data;
using System.Drawing;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;


namespace DuckDNS.NET
{
    public partial class frmMain : Form
    {
        public frmMain()
        {
            InitializeComponent();
        }

        private DataTable dtDomains = new DataTable();
        private string mExternalIP = "";

        private static bool UpdateDuckDNS(string cDomain)
        {
            string wcResponse = "";
            bool returnValue = false;

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
                try
                {
                    // if successful wcResponse is OK, else KO
                    wcResponse = wc.DownloadString(new Uri(cDomain));
                    returnValue = wcResponse.Equals("OK") ? true : false;
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.Message);
                    Console.WriteLine($"Exception : {e}");
                    return returnValue;
                }
            }
            return returnValue;
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

            dtDomains.Rows.Add(txtDomain.Text.Trim(), txtToken.Text.Trim(), txtIP.Text.Trim(), "","");
            txtDomain.Text = string.Empty;
            txtToken.Text = string.Empty;
            txtIP.Text = string.Empty;
        }

        private void frmMain_Load(object sender, EventArgs e)
        {
            this.Text = "Ducky";
            dgDomains.ColumnHeadersDefaultCellStyle.BackColor = Color.Navy;
            dgDomains.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgDomains.ColumnHeadersDefaultCellStyle.Font = new Font(dgDomains.Font, FontStyle.Bold);

            dgDomains.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.DisplayedCellsExceptHeaders;
            dgDomains.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;
            dgDomains.CellBorderStyle = DataGridViewCellBorderStyle.Single;
            dgDomains.GridColor = SystemColors.ActiveBorder;
            dgDomains.RowHeadersVisible = false;
            dgDomains.ColumnHeadersVisible = true;
            dgDomains.AutoResizeColumnHeadersHeight();
            dgDomains.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgDomains.MultiSelect = false;

            dtDomains.Columns.Add("Domain");
            dtDomains.Columns.Add("Token");
            dtDomains.Columns.Add("IP");
            dtDomains.Columns.Add("Last Check");
            dtDomains.Columns.Add("Status");
            dtDomains.Rows.Clear();
            dgDomains.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            loadGrid();
            dgDomains.DataSource = dtDomains;

            //get build timestamp
            var timestamp = Properties.Resources.BuildTimeStamp;
            lblAbout.Text = "Build Timestamp : " + timestamp;

            // this triggers timer right away, so external IP and domains are checked
            // the actual timer is set in updateDomains
            timer1.Interval = 1000;
            timer1.Enabled = true;
            timer1.Start();

            dgDomains.Update();
            Application.DoEvents();
        }


        private void updateDomains()
        {
            int minutes = Settings.Default.timeLapse;
            timer1.Interval = Settings.Default.timeLapse * 60000; //Interval is in ms, settings are in minutes, 1 min = 60000 ms

            bool bAtLeastOneFailure = false;
            mExternalIP = getExternalIP();
            if (mExternalIP==null)
            {
                return;
            }
            lblExtIP.Text = mExternalIP;

            if (dgDomains.RowCount <= 0) return;
            foreach (DataGridViewRow row in dgDomains.Rows)
            {
                string strIPToUse = row.Cells[2].Value.ToString();
                if (row.Cells[2].Value.ToString().Contains("Auto"))
                {
                    row.Cells[2].Value = "Auto (" + mExternalIP + ")";
                    strIPToUse = mExternalIP;
                }
                var getdom = string.Format("https://www.duckdns.org/update?domains={0}&token={1}&ip={2}",
                    row.Cells[0].Value, row.Cells[1].Value, strIPToUse);
                if (UpdateDuckDNS(getdom))
                {
                    row.Cells[4].Value = "Success";
                    row.Cells[4].Style.ForeColor = Color.White;
                    row.Cells[4].Style.BackColor = Color.Green;
                }
                else
                {
                    row.Cells[4].Value = "Failure";
                    row.Cells[4].Style.ForeColor = Color.Red;
                    row.Cells[4].Style.BackColor = Color.Yellow;
                    bAtLeastOneFailure = true;
                }
                row.Cells[3].Value = DateTime.Now.ToShortTimeString() + " " + DateTime.Now.ToShortDateString(); 
            }
            if (bAtLeastOneFailure)
            {
                myNotifyIcon.Icon = Properties.Resources.IconRed; 
            }
            else
            {
                myNotifyIcon.Icon = Properties.Resources.IconYellow;
            }
            lblStatus.Text = "All domains checked..." + "Next check: " + DateTime.Now.ToShortDateString() + " " + DateTime.Now.AddMinutes(minutes).ToShortTimeString(); ;
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
/*
            foreach (DataGridViewCell cell in dgDomains.CurrentRow.Cells)
            {
                row[nCell] = cell.Value.ToString();
                nCell++;
            }

            txtDomain.Text = row[0];
            txtToken.Text = row[1];
            txtIP.Text = row[2];
*/
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
            catch (Exception e)
            {
                MessageBox.Show(e.Message + ", exiting ...");
                Console.WriteLine($"Exception : {e}");
                Application.Exit();
                return null;
            }
        }

        private void btnAutoIP_Click(object sender, EventArgs e)
        {
            txtIP.Text = "Auto (" + mExternalIP + ")";
        }

        private void linkLabel2_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            System.Diagnostics.Process.Start("https://bitbucket.org/Jaxmetalmax/duckdns.net");
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            System.Diagnostics.Process.Start("https://www.duckdns.org/");
        }
    }
}
