
namespace OpcDaBrowser
{
    public partial class FormServerList
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.lvServer = new System.Windows.Forms.ListView();
            this.columnHeader1 = new System.Windows.Forms.ColumnHeader();
            this.columnHeader2 = new System.Windows.Forms.ColumnHeader();
            this.btnRefresh = new System.Windows.Forms.Button();
            this.rtbText = new System.Windows.Forms.RichTextBox();
            this.btnSelect = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // lvServer
            // 
            this.lvServer.BackgroundImageTiled = true;
            this.lvServer.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {this.columnHeader1, this.columnHeader2});
            this.lvServer.FullRowSelect = true;
            this.lvServer.GridLines = true;
            this.lvServer.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.Nonclickable;
            this.lvServer.HideSelection = false;
            this.lvServer.LabelEdit = true;
            this.lvServer.Location = new System.Drawing.Point(12, 12);
            this.lvServer.MultiSelect = false;
            this.lvServer.Name = "lvServer";
            this.lvServer.ShowItemToolTips = true;
            this.lvServer.Size = new System.Drawing.Size(412, 226);
            this.lvServer.TabIndex = 0;
            this.lvServer.TabStop = false;
            this.lvServer.UseCompatibleStateImageBehavior = false;
            this.lvServer.View = System.Windows.Forms.View.Details;
            this.lvServer.SelectedIndexChanged += new System.EventHandler(this.lvServer_SelectedIndexChanged);
            // 
            // columnHeader1
            // 
            this.columnHeader1.Text = "Server Name";
            this.columnHeader1.Width = 367;
            // 
            // columnHeader2
            // 
            this.columnHeader2.Text = "CLSID";
            this.columnHeader2.Width = 0;
            // 
            // btnRefresh
            // 
            this.btnRefresh.Location = new System.Drawing.Point(12, 244);
            this.btnRefresh.Name = "btnRefresh";
            this.btnRefresh.Size = new System.Drawing.Size(75, 26);
            this.btnRefresh.TabIndex = 1;
            this.btnRefresh.Text = "Refresh";
            this.btnRefresh.UseVisualStyleBackColor = true;
            this.btnRefresh.Click += new System.EventHandler(this.btnRefresh_Click);
            // 
            // rtbText
            // 
            this.rtbText.Location = new System.Drawing.Point(12, 276);
            this.rtbText.Name = "rtbText";
            this.rtbText.ReadOnly = true;
            this.rtbText.Size = new System.Drawing.Size(412, 154);
            this.rtbText.TabIndex = 3;
            this.rtbText.Text = "";
            // 
            // btnSelect
            // 
            this.btnSelect.Location = new System.Drawing.Point(350, 244);
            this.btnSelect.Name = "btnSelect";
            this.btnSelect.Size = new System.Drawing.Size(75, 26);
            this.btnSelect.TabIndex = 4;
            this.btnSelect.Text = "Select";
            this.btnSelect.UseVisualStyleBackColor = true;
            this.btnSelect.Click += new System.EventHandler(this.btnSelect_Click);
            // 
            // FormServerList
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(434, 441);
            this.Controls.Add(this.btnSelect);
            this.Controls.Add(this.rtbText);
            this.Controls.Add(this.btnRefresh);
            this.Controls.Add(this.lvServer);
            this.DoubleBuffered = true;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "FormServerList";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Servers";
            this.Load += new System.EventHandler(this.FormServerList_Load);
            this.ResumeLayout(false);
        }

        private System.Windows.Forms.Button btnSelect;

        #endregion

        private System.Windows.Forms.ListView lvServer;
        private System.Windows.Forms.ColumnHeader columnHeader1;
        private System.Windows.Forms.ColumnHeader columnHeader2;
        private System.Windows.Forms.Button btnRefresh;
        private System.Windows.Forms.RichTextBox rtbText;
    }
}