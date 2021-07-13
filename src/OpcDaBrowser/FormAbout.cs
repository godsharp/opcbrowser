using System.Diagnostics;
using System.Windows.Forms;

namespace OpcDaBrowser
{
    public partial class FormAbout : Form
    {
        public FormAbout()
        {
            InitializeComponent();
        }

        private void button2_Click(object sender, System.EventArgs e)
        {
            Close();
        }

        private void linkLabel3_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            OpenLink(sender);
        }

        private void linkLabel2_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            OpenLink(sender);
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            OpenLink(sender);
        }

        private void OpenLink(object sender)
        {
            LinkLabel linkLabel= sender as LinkLabel;
            if (linkLabel is null) return;
            Process.Start(linkLabel.Text);
        }
    }
}
