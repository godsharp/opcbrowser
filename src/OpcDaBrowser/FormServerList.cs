using OPCAutomation;

using System;
using System.Windows.Forms;

namespace OpcDaBrowser
{
    public partial class FormServerList : Form
    {
        private readonly Action<string> _onServerSelected;
        private string _server;
        
        public FormServerList(Action<string> onServerSelected)
        {
            _onServerSelected = onServerSelected;
            InitializeComponent();
        }

        private void FormServerList_Load(object sender, EventArgs e)
        {
            btnRefresh_Click(btnRefresh, null);
        }

        private void lvServer_SelectedIndexChanged(object sender, EventArgs e)
        {
            _server = null;
            if (lvServer.SelectedIndices.Count==0 || lvServer.SelectedIndices[0]<0) return;
            var item = lvServer.SelectedItems[0];
            _server = item.Text;
            rtbText.Text = null;
            rtbText.AppendText($"Server: {item.Text}\r\n");
            rtbText.AppendText($"CLSID: {item.SubItems[1].Text}\r\n");
        }

        private void btnRefresh_Click(object sender, EventArgs e)
        {
            try
            {
                rtbText.ResetText();
                var server = new OPCServer();

                if (server.GetOPCServers("localhost") is not Array servers) return;
                foreach (string item in servers)
                {
                    var type = Type.GetTypeFromProgID(item);
                    var lvi = new ListViewItem(item);
                    lvi.SubItems.Add(type.GUID.ToString("D").ToUpper());
                    lvServer.Items.Add(lvi);
                }
            }
            catch (Exception exception)
            {
                MessageBox.Show($@"Enumerate opc server throw exception:{exception.Message}\r\n{exception.StackTrace}");
            }
        }

        private void btnSelect_Click(object sender, EventArgs e)
        {
            _onServerSelected?.Invoke(_server);
            Close();
        }
    }
}
