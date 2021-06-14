using GodSharp.Opc.Da;
using GodSharp.Opc.Da.Options;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Forms;

namespace OpcDaBrowser
{
    public partial class FormBrowser : Form
    {
        //private DataGridView dataGridView1;
        private DataGridView dataGridView1;
        private IOpcDaClient _client;
        private int _id = 1;
        private readonly BindingList<ViewModel> _values = new();

        public FormBrowser()
        {
            InitializeComponent();
            tsmiVersion.Text = ProductVersion;
            base.Text += $@" v{ProductVersion}";
            tsslOpcServerStatus.Text = @"Opc Server : Stop";

            treeView1.AfterCollapse += treeView1_AfterCollapse;
            treeView1.AfterExpand += treeView1_AfterExpand;
            dataGridView1.AutoGenerateColumns = false;
            dataGridView1.DataSource = _values;
            OnConnectChanged(true);
        }

        private void tsmiConnect_Click(object sender, EventArgs e)
        {
            new FormServerList(OnServerSelected).ShowDialog();
        }

        private void OnConnectChanged(bool connect)
        {
            tsmiConnect.Enabled = connect;
            tsmiDisconnect.Enabled = !connect;
        }

        private void OnServerSelected(string server)
        {
            if (string.IsNullOrWhiteSpace(server)) return;
            _client?.Disconnect();
            _client?.Dispose();
            _values.Clear();
            treeView1.Nodes.Clear();

            var root = new TreeNode(server){ ImageIndex = 0, SelectedImageIndex = 1};
            treeView1.Nodes.Add(root);

            _client = DaClientFactory.Instance.CreateOpcAutomationClient(x =>
            {
                x.Data = new ServerData
                {
                    Host = "localhost",
                    ProgId = server,
                    Name = server
                };
                x.OnDataChangedHandler += OnDataChangedHandler;
                x.OnServerShutdownHandler += OnServerShutdownHandler;
            });

            _client.Connect();
            _client.Add(new Group {Name = "default", UpdateRate = 100});

            tsslOpcServerStatus.Text = $@"Opc Server : {(_client.Connected ? "Run" : "Stop")}";
            OnConnectChanged(!_client.Connected);

            var list = _client.BrowseNodes();

            if (list == null) return;

            Dictionary<string, TreeNode> tree = new();
            var one = new List<string>();

            foreach (var item in list)
            {
                var array = item.Split('.');
                string name = null;
                for (var i = 0; i < array.Length; i++)
                {
                    var tmp = array[i];
                    var parent = name;
                    if (i == 0)
                    {
                        name = tmp;
                        if (!one.Contains(name)) one.Add(name);
                    }
                    else
                    {
                        name += $".{tmp}";
                    }

                    TreeNode node;
                    if (!tree.ContainsKey(name))
                    {
                        node = new TreeNode(tmp) {Name = tmp, Tag = name, ImageIndex = 0, SelectedImageIndex = 1};
                        tree.Add(name, node);
                    }
                    else
                    {
                        node = tree[name];
                    }

                    if (parent == null) continue;
                    if (tree[parent].Nodes.ContainsKey(tmp)) continue;

                    tree[parent].Nodes.Add(node);
                    tree[parent].ImageIndex = 2;
                    tree[parent].SelectedImageIndex = 3;
                }
            }

            var nodes = tree.Where(x => one.Contains(x.Key)).Select(x => x.Value).ToArray();
            root.Nodes.AddRange(nodes);
            treeView1.ExpandAll();
        }

        private void OnServerShutdownHandler(Server arg1, string arg2)
        {
            tsslOpcServerStatus.Text = @"Opc Server : Stop";
            OnConnectChanged(true);
            _client.Disconnect();
        }

        private void treeView1_AfterCollapse(object sender, TreeViewEventArgs e)
        {
            if (e.Node.Nodes.Count == 0) return;
            e.Node.ImageIndex = 2;
            e.Node.SelectedImageIndex = 3;
        }

        private void treeView1_AfterExpand(object sender, TreeViewEventArgs e)
        {
            if (e.Node.Nodes.Count == 0) return;
            e.Node.ImageIndex = 4;
            e.Node.SelectedImageIndex = 5;
        }

        private void treeView1_DoubleClick(object sender, EventArgs e)
        {
            if (treeView1.SelectedNode == null || treeView1.SelectedNode.Nodes.Count > 0) return;
            Monitor(treeView1.SelectedNode);
        }

        private void Monitor(TreeNode node)
        {
            if(node.Nodes.Count>0)
            {
                foreach (TreeNode item in node.Nodes) Monitor(item);
            }
            else
            {
                var tagName = node.Tag?.ToString();
                if (string.IsNullOrWhiteSpace(tagName)) return;
                if (_client.Current.Tags?.ContainsKey(tagName) == true) return;
                var tag = new Tag(tagName, _id++);
                _client.Current.Add(tag);
                _values.Add(new ViewModel(tag.ItemName, tag.ClientHandle));
            }
        }

        private void OnDataChangedHandler(DataChangedOutput e)
        {
            var tag = _values.FirstOrDefault(a => a.ClientHandle == e.Data.ClientHandle);
            if (tag == null) return;
            tag.Value = e.Data.Value?.ToString();
            tag.Quality = e.Data.Quality?.ToString();
            tag.Counter += 1;
            tag.Timestamp = e.Data.Timestamp?.ToString("yyyy-MM-dd HH:mm:ss.fff");
        }

        private void tsmiDisconnect_Click(object sender, EventArgs e)
        {
           var ret = _client.Disconnect();
           tsslOpcServerStatus.Text = $@"Opc Server : {(!ret ? "Run" : "Stop")}";
           OnConnectChanged(ret);
           _values.Clear();
        }

        private void tsmiNewClient_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void addSelectedToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (treeView1.SelectedNode == null) return;
            Monitor(treeView1.SelectedNode);
        }

        private void removeItemsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (dataGridView1.SelectedRows.Count < 0) return;

            List<string> removes = new();
            foreach (DataGridViewRow row in dataGridView1.SelectedRows)
            {
                var vm = row.DataBoundItem as ViewModel;
                if (vm == null) continue;
                removes.Add(vm.ItemName);
                _values.Remove(vm);
            }

            _client.Current.Remove(removes.ToArray());
        }

        private void removeAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _client.Current.RemoveAll();
            _values.Clear();
        }

        private void FormBrowser_FormClosing(object sender, FormClosingEventArgs e)
        {
            e.Cancel = MessageBox.Show(@"Whether to quit now?", @"Opc Da Browser", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button1) != DialogResult.Yes;
        }

        private void FormBrowser_FormClosed(object sender, FormClosedEventArgs e)
        {
            _client?.Dispose();
        }
    }
}