using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace SciGit_Client
{
    public partial class SGMain : Form
    {
        public SGMain()
        {
            InitializeComponent();
        }

        private void SGMain_Load(object sender, EventArgs e)
        {
            
        }

        private void notifyIcon_MouseDoubleClick(object sender, MouseEventArgs e)
        {

        }

        private void showAllFiles_Click(object sender, EventArgs e)
        {
            string homePath = (Environment.OSVersion.Platform == PlatformID.Unix ||
                   Environment.OSVersion.Platform == PlatformID.MacOSX)
                ? Environment.GetEnvironmentVariable("HOME")
                : Environment.ExpandEnvironmentVariables("%HOMEDRIVE%%HOMEPATH%");
            System.Diagnostics.Process.Start(homePath + "/SciGit");
        }

        private void update_Click(object sender, EventArgs e)
        {

        }

        private void upload_Click(object sender, EventArgs e)
        {

        }

        private void exit_Click(object sender, EventArgs e)
        {
            Environment.Exit(0);
        }
    }
}
