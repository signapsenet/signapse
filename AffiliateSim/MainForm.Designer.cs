namespace AffiliateSim
{
    partial class MainForm
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.lvAffiliates = new System.Windows.Forms.ListView();
            this.panelGraph = new System.Windows.Forms.Panel();
            this.transactionGraph1 = new AffiliateSim.TransactionGraph();
            this.vScrollBar1 = new System.Windows.Forms.VScrollBar();
            this.comboBox1 = new System.Windows.Forms.ComboBox();
            this.btnJoin = new System.Windows.Forms.Button();
            this.btnAddAffiliate = new System.Windows.Forms.Button();
            this.tbSimSpeed = new System.Windows.Forms.TrackBar();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.panelGraph.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.tbSimSpeed)).BeginInit();
            this.SuspendLayout();
            // 
            // lvAffiliates
            // 
            this.lvAffiliates.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
            this.lvAffiliates.FullRowSelect = true;
            this.lvAffiliates.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.Nonclickable;
            this.lvAffiliates.Location = new System.Drawing.Point(12, 72);
            this.lvAffiliates.Name = "lvAffiliates";
            this.lvAffiliates.Size = new System.Drawing.Size(205, 352);
            this.lvAffiliates.TabIndex = 0;
            this.lvAffiliates.UseCompatibleStateImageBehavior = false;
            this.lvAffiliates.View = System.Windows.Forms.View.Details;
            this.lvAffiliates.ItemSelectionChanged += new System.Windows.Forms.ListViewItemSelectionChangedEventHandler(this.lvAffiliates_ItemSelectionChanged);
            this.lvAffiliates.SelectedIndexChanged += new System.EventHandler(this.lvAffiliates_SelectedIndexChanged);
            // 
            // panelGraph
            // 
            this.panelGraph.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.panelGraph.Controls.Add(this.transactionGraph1);
            this.panelGraph.Controls.Add(this.vScrollBar1);
            this.panelGraph.Location = new System.Drawing.Point(223, 36);
            this.panelGraph.Name = "panelGraph";
            this.panelGraph.Size = new System.Drawing.Size(607, 423);
            this.panelGraph.TabIndex = 1;
            // 
            // transactionGraph1
            // 
            this.transactionGraph1.Simulator = null;
            this.transactionGraph1.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.transactionGraph1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.transactionGraph1.Location = new System.Drawing.Point(0, 0);
            this.transactionGraph1.Name = "transactionGraph1";
            this.transactionGraph1.Size = new System.Drawing.Size(590, 423);
            this.transactionGraph1.TabIndex = 2;
            // 
            // vScrollBar1
            // 
            this.vScrollBar1.Dock = System.Windows.Forms.DockStyle.Right;
            this.vScrollBar1.Location = new System.Drawing.Point(590, 0);
            this.vScrollBar1.Name = "vScrollBar1";
            this.vScrollBar1.Size = new System.Drawing.Size(17, 423);
            this.vScrollBar1.TabIndex = 1;
            // 
            // comboBox1
            // 
            this.comboBox1.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBox1.FormattingEnabled = true;
            this.comboBox1.Location = new System.Drawing.Point(71, 14);
            this.comboBox1.Name = "comboBox1";
            this.comboBox1.Size = new System.Drawing.Size(146, 23);
            this.comboBox1.TabIndex = 2;
            this.comboBox1.SelectedIndexChanged += new System.EventHandler(this.comboBox1_SelectedIndexChanged);
            // 
            // btnJoin
            // 
            this.btnJoin.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnJoin.Location = new System.Drawing.Point(12, 430);
            this.btnJoin.Name = "btnJoin";
            this.btnJoin.Size = new System.Drawing.Size(205, 29);
            this.btnJoin.TabIndex = 3;
            this.btnJoin.Text = "Join";
            this.btnJoin.UseVisualStyleBackColor = true;
            this.btnJoin.Click += new System.EventHandler(this.btnJoin_Click);
            // 
            // btnAddAffiliate
            // 
            this.btnAddAffiliate.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnAddAffiliate.Location = new System.Drawing.Point(12, 43);
            this.btnAddAffiliate.Name = "btnAddAffiliate";
            this.btnAddAffiliate.Size = new System.Drawing.Size(202, 23);
            this.btnAddAffiliate.TabIndex = 4;
            this.btnAddAffiliate.Text = "Add Affiliate";
            this.btnAddAffiliate.UseVisualStyleBackColor = true;
            this.btnAddAffiliate.Click += new System.EventHandler(this.btnAddAffiliate_Click);
            // 
            // tbSimSpeed
            // 
            this.tbSimSpeed.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tbSimSpeed.Location = new System.Drawing.Point(328, 14);
            this.tbSimSpeed.Maximum = 100;
            this.tbSimSpeed.Name = "tbSimSpeed";
            this.tbSimSpeed.Size = new System.Drawing.Size(499, 45);
            this.tbSimSpeed.TabIndex = 5;
            this.tbSimSpeed.TickStyle = System.Windows.Forms.TickStyle.None;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 18);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(53, 15);
            this.label1.TabIndex = 6;
            this.label1.Text = "Affiliates";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(223, 18);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(99, 15);
            this.label2.TabIndex = 7;
            this.label2.Text = "Simulation Speed";
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(842, 471);
            this.Controls.Add(this.panelGraph);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.tbSimSpeed);
            this.Controls.Add(this.btnAddAffiliate);
            this.Controls.Add(this.btnJoin);
            this.Controls.Add(this.comboBox1);
            this.Controls.Add(this.lvAffiliates);
            this.Name = "MainForm";
            this.Text = "Affiliate Simulator";
            this.panelGraph.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.tbSimSpeed)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private ListView lvAffiliates;
        private Panel panelGraph;
        private VScrollBar vScrollBar1;
        private ComboBox comboBox1;
        private Button btnJoin;
        private Button btnAddAffiliate;
        private TrackBar tbSimSpeed;
        private Label label1;
        private Label label2;
        private TransactionGraph transactionGraph1;
    }
}