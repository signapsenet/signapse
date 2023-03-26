using AffiliateSim.Services;

namespace AffiliateSim
{
    public partial class MainForm : Form
    {
        private readonly Simulator simulator;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        private MainForm() { }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

        private AffiliateInstance? FirstAffiliate = null;

        private AffiliateInstance? CurrentAffiliate => comboBox1
            .SelectedItem as AffiliateInstance;

        internal MainForm(Simulator simulator)
        {
            this.simulator = simulator;
            InitializeComponent();

            this.lvAffiliates.Columns.Add("Name", -1);
            lvAffiliates.AutoResizeColumns(ColumnHeaderAutoResizeStyle.HeaderSize);

            this.transactionGraph1.Simulator = simulator;
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            simulator?.Dispose();
            lvAffiliates.Items.Clear();

            base.OnFormClosing(e);
        }

        private void btnAddAffiliate_Click(object sender, EventArgs e)
        {
            lvAffiliates.Items.Add(CreateAffiliateItem());
            lvAffiliates.AutoResizeColumns(ColumnHeaderAutoResizeStyle.HeaderSize);

            ResetAffiliateDropdown();
            HighlightJoinedAffiliates();
        }

        private ListViewItem CreateAffiliateItem()
        {
            var affiliate = simulator.GenerateAffiliate();

            return new ListViewItem(affiliate.Descriptor.Name)
            {
                Tag = affiliate
            };
        }

        private void HighlightJoinedAffiliates()
        {
            if (this.CurrentAffiliate is AffiliateInstance inst)
            {
                foreach (ListViewItem it in lvAffiliates.Items)
                {
                    if (it.Tag is AffiliateInstance other
                        && inst.AllAffiliates.Contains(other))
                    {
                        it.BackColor = Color.LightCoral;
                    }
                    else
                    {
                        it.BackColor = Color.White;
                    }
                }
            }
            else
            {
                foreach (ListViewItem it in lvAffiliates.Items)
                {
                    it.BackColor = Color.White;
                }
            }
        }

        private void ResetAffiliateDropdown()
        {
            comboBox1.Items.Clear();
            comboBox1.Items.AddRange(simulator.Affiliates.ToArray());
        }

        private void lvAffiliates_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (this.CurrentAffiliate is AffiliateInstance inst)
            {
            }

            HighlightJoinedAffiliates();
        }

        private void btnJoin_Click(object sender, EventArgs e)
        {
            if (this.CurrentAffiliate is AffiliateInstance inst
                && this.FirstAffiliate is AffiliateInstance other)
            {
                if (inst.AllAffiliates.Contains(other))
                {
                    inst.LeaveAffiliation();
                }
                else
                {
                    inst.JoinAffiliate(other);
                }

                HighlightJoinedAffiliates();
            }
        }

        private void lvAffiliates_ItemSelectionChanged(object sender, ListViewItemSelectionChangedEventArgs e)
        {
            this.FirstAffiliate = null;
            if (lvAffiliates.SelectedItems.Count == 1 && e.Item.Tag is AffiliateInstance inst)
            {
                this.FirstAffiliate = inst;
            }
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            this.HighlightJoinedAffiliates();
        }
    }
}