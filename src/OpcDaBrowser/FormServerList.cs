using System;
using System.Linq;
using System.Windows.Forms;
using GodSharp.Opc.Da;

namespace OpcDaBrowser
{
    public partial class FormServerList : Form
    {
        private readonly Action<string> _onServerSelected;
        private string _server;
        private IServerDiscovery _discovery;
        
        public FormServerList(Action<string> onServerSelected,bool openNetApi=false)
        {
            _onServerSelected = onServerSelected;
            InitializeComponent();

            _discovery = !openNetApi
                ? DaClientFactory.Instance.CreateOpcAutomationServerDiscovery()
                : DaClientFactory.Instance.CreateOpcNetApiServerDiscovery();
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
                if(lvServer.Items.Count>0) lvServer.Items.Clear();
                rtbText.ResetText();
                var servers = _discovery.GetServers(GetServerSpecification());

                if (servers == null || servers.Length == 0) return;
                foreach (var item in servers)
                {
                    if (item.Any(x=>!char.IsLetterOrDigit(x) && x!='.')) continue;
                    
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

        private ServerSpecification GetServerSpecification()
        {
            return radioButton1.Checked ? ServerSpecification.DA10 : radioButton3.Checked ? ServerSpecification.DA30 : ServerSpecification.DA20;
        }

        private void btnSelect_Click(object sender, EventArgs e)
        {
            _onServerSelected?.Invoke(_server);
            Close();
        }

        private void lvServer_DoubleClick(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_server)) return;
            btnSelect_Click(null, null);
        }
    }
}
