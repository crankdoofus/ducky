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
using System.Windows.Forms;
using DuckDNS.NET.Properties;

namespace DuckDNS.NET
{
    public partial class frmSettings : Form
    {
        public frmSettings()
        {
            InitializeComponent();
        }

        private void frmSettings_Load(object sender, EventArgs e)
        {
           numTime.Value = (decimal) Settings.Default.timeLapse/60000;
        }

        private void frmSettings_FormClosing(object sender, FormClosingEventArgs e)
        {
            Settings.Default.timeLapse = (int) numTime.Value*60000;
            MessageBox.Show("You must restart the app for changes to take effect...");
        }
    }
}
