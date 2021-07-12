using GodSharp.Opc.Da;
using GodSharp.Opc.Da.Options;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Forms;
// ReSharper disable InconsistentNaming

namespace OpcDaBrowser
{
    public partial class FormBrowser : Form
    {
        //System.Windows.Forms.DataGridView dataGridView1;
        System.Windows.Forms.DataGridView dataGridView1;
        private IOpcDaClient _client;
        private int _id = 1;
        private readonly BindingList<ViewModel> _values = new();

        public FormBrowser()
        {
            InitializeComponent();
            tsmiVersion.Text = ProductVersion;
            base.Text += $@" v{ProductVersion}";
            tsslOpcServerStatus.Text = @"Opc Server : Stop";
            tsslComponent.Text = null;

            treeView1.AfterCollapse += treeView1_AfterCollapse;
            treeView1.AfterExpand += treeView1_AfterExpand;
            dataGridView1.AutoGenerateColumns = false;
            dataGridView1.DataSource = _values;
            dataGridView1.RowStateChanged += DataGridView1_RowStateChanged;
            tsmiAutomation.CheckedChanged += ComponentCheckedChanged;
            tsmiOpenNetApi.CheckedChanged += ComponentCheckedChanged;
            OnConnectChanged(false);
            UpdateDataGridState();
        }

        private void DataGridView1_RowStateChanged(object sender, DataGridViewRowStateChangedEventArgs e)
        {
            UpdateDataGridState();
        }

        private void UpdateDataGridState()
        {

            var has = dataGridView1.Rows.Count > 0;
            tsmiRemoveItem.Enabled = has;
            tsmiRemoveAll.Enabled = has;
            dataGridView1.ContextMenuStrip = has ? contextMenuStrip1 : null;
            var selected = dataGridView1.SelectedRows.Count > 0;
            tsmiWriteValueAsync.Enabled = selected;
            tsmiWriteValueSync.Enabled = selected;
            tsmiReadValueAsync.Enabled = selected;
            tsmiReadValueSync.Enabled = selected;
        }

        private void ComponentCheckedChanged(object sender, EventArgs e)
        {
            if (sender is not ToolStripMenuItem { Checked: true } item) return;

            if (item.Name == tsmiAutomation.Name)
            {
                tsmiOpenNetApi.CheckState = CheckState.Unchecked;
            }

            if (item.Name == tsmiOpenNetApi.Name)
            {
                tsmiAutomation.CheckState = CheckState.Unchecked;
            }
        }

        private void tsmiConnect_Click(object sender, EventArgs e)
        {
            new FormServerList(OnServerSelected, tsmiOpenNetApi.Checked).ShowDialog();
        }

        private void OnConnectChanged(bool connect)
        {
            tsmiConnect.Enabled = !connect;
            tsmiDisconnect.Enabled = connect;

            tsmiAutomation.Enabled = !connect;
            tsmiOpenNetApi.Enabled = !connect;

            addSelectedToolStripMenuItem.Enabled = connect;
            tsmiRemoveAll.Enabled = connect;
            tsmiRemoveItem.Enabled = connect;
        }

        private void OnServerSelected(string server)
        {
            if (string.IsNullOrWhiteSpace(server)) return;
            if (_client?.Connected == true)
            {
                _client.Disconnect();
                _client.Dispose();
                GC.Collect();
            }

            _values.Clear();
            treeView1.Nodes.Clear();

            var root = new TreeNode(server){ ImageIndex = 0, SelectedImageIndex = 1};
            treeView1.Nodes.Add(root);

            Func<Action<DaClientOptions>, IOpcDaClient> factory = tsmiAutomation.Checked
                ? DaClientFactory.Instance.CreateOpcAutomationClient
                : DaClientFactory.Instance.CreateOpcNetApiClient;
            
            _client = factory(x =>
            {
                x.Data = new ServerData
                {
                    Host = "localhost",
                    ProgId = server,
                    Name = server
                };
                x.OnDataChangedHandler += OnDataChangedHandler;
                x.OnServerShutdownHandler += OnServerShutdownHandler;
                x.OnAsyncReadCompletedHandler += OnAsyncReadCompletedHandler;
                x.OnAsyncWriteCompletedHandler += OnAsyncWriteCompletedHandler;
            });

            _client.Connect();
            _client.Add(new Group {Name = "default", UpdateRate = 100});

            tsslOpcServerStatus.Text = $@"Opc Server : {(_client.Connected ? "Run" : "Stop")}";
            tsslComponent.Text = $@"Component: {(tsmiAutomation.Checked ? "Automation" : "NetApi")}";
            OnConnectChanged(_client.Connected);

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
            UpdateDataGridState();
        }

        private void OnServerShutdownHandler(Server arg1, string arg2)
        {
            tsslOpcServerStatus.Text = @"Opc Server : Stop";
            OnConnectChanged(false);
            //_client.Disconnect();
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

        private void OnAsyncReadCompletedHandler(AsyncReadCompletedOutput e)
        {
            if (!e.Data.Ok) return;

            var tag = _values.FirstOrDefault(a => a.ClientHandle == e.Data.Result.ClientHandle);
            if (tag == null) return;
            tag.Value = e.Data.Result.Value?.ToString();
            tag.Quality = e.Data.Result.Quality?.ToString();
            tag.Counter += 1;
            tag.Timestamp = e.Data.Result.Timestamp?.ToString("yyyy-MM-dd HH:mm:ss.fff");
        }

        private void OnAsyncWriteCompletedHandler(AsyncWriteCompletedOutput output)
        {
        }

        private void tsmiDisconnect_Click(object sender, EventArgs e)
        {
           var ret = _client.Disconnect();
           _client.Dispose();
           GC.Collect();
           tsslOpcServerStatus.Text = $@"Opc Server : {(!ret ? "Run" : "Stop")}";
           OnConnectChanged(!ret);
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
            if (_client?.Connected == true)
            {
                e.Cancel = MessageBox.Show(@"Whether to quit now?", @"Opc Da Browser", MessageBoxButtons.YesNo, MessageBoxIcon.Question,
                    MessageBoxDefaultButton.Button1) != DialogResult.Yes;
            }
        }

        private void FormBrowser_FormClosed(object sender, FormClosedEventArgs e)
        {
            GC.Collect();
            _client?.Dispose();
        }

        private void FormBrowser_SizeChanged(object sender, EventArgs e)
        {
            dataGridView1.Invalidate();
        }

        private void tsmiWriteValueSync_Click(object sender, EventArgs e)
        {
            WriteValue(true);
        }

        private void tsmiWriteValueAsync_Click(object sender, EventArgs e)
        {
            WriteValue(false);
        }

        private void WriteValue(bool sync)
        {

            try
            {
                var vm = dataGridView1.SelectedRows[0].DataBoundItem as ViewModel;
                new FormValueInput((v) => WriteValue(sync, vm, v)).ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show(@$"Write value failed:{ex.Message}", @"Opc Da Browser", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1);
            }
        }

        private void WriteValue(bool sync, ViewModel vm, string value)
        {

            try
            {
                var result = sync ? _client.Current.Write(vm.ItemName, value) : _client.Current.WriteAsync(vm.ItemName, value);
                if (!result.Ok)
                {
                    MessageBox.Show(@$"Write value failed.", @"Opc Da Browser", MessageBoxButtons.OK, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(@$"Write value failed:{ex.Message}", @"Opc Da Browser", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1);
            }
        }

        private void tsmiReadValueSync_Click(object sender, EventArgs e)
        {
            try
            {
                List<ViewModel> list = new();
                foreach (DataGridViewRow row in dataGridView1.SelectedRows)
                {
                    var vm = row.DataBoundItem as ViewModel;
                    if (vm == null) continue;
                    list.Add(vm);
                }

                var results = _client.Current.Reads(list.Select(x => x.ItemName).ToArray());

                foreach (var result in results)
                {
                    var vm = list.Find(x => x.ClientHandle == result.Result.ClientHandle);
                    if (result.Ok)
                    {
                        vm.Value = result.Result.Value?.ToString();
                        vm.Timestamp = result.Result.Timestamp?.ToString("yyyy-MM-dd HH:mm:ss.fff");
                        vm.Counter++;
                    }
                }

                var errors = results?.Where(x => !x.Ok).ToArray();
                if (errors?.Length > 0)
                {
                    MessageBox.Show(@$"Read value failed.{string.Join("\r\n", errors.Select(x => x.Result.ItemName))}", @"Opc Da Browser", MessageBoxButtons.OK, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(@$"Read value failed:{ex.Message}", @"Opc Da Browser", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1);
            }
        }

        private void tsmiReadValueAsync_Click(object sender, EventArgs e)
        {
            try
            {
                List<ViewModel> list = new();
                foreach (DataGridViewRow row in dataGridView1.SelectedRows)
                {
                    var vm = row.DataBoundItem as ViewModel;
                    if (vm == null) continue;
                    list.Add(vm);
                }

                var result = _client.Current.ReadsAsync(list.Select(x => x.ItemName).ToArray());
                var errors = result?.Where(x => !x.Ok).ToArray();

                if (errors?.Length > 0)
                {
                    MessageBox.Show(@$"Read value failed.{string.Join("\r\n", errors.Select(x => x.Result.ItemName))}", @"Opc Da Browser", MessageBoxButtons.OK, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(@$"Read value failed:{ex.Message}", @"Opc Da Browser", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1);
            }
        }
    }
}