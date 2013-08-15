using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace TowerAnimator
{
    public partial class About : Form
    {
        public About()
        {
            InitializeComponent();
        }

        private void websiteLabel_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            System.Diagnostics.Process.Start("http://www.towerlights.uiacm.org");
        }

        public void SetVersion(double versionNumber)
        {
            versionLabel.Text = "File version: " + versionNumber.ToString();
            Invalidate();
        }
    }
}
