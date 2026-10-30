// superintelligentmeter contributors, 2026-10-29: keep native fields and dialog actions inside measured rows. GPL-2.0-only; original notices retained.
// Modified for the Intelmeter fork, 2026-10-16: .NET 10 migration, UI modernization and compatibility fixes. GPL-2.0-only; original notices retained.
using Gurux.DLMS.Objects;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Gurux.DLMS.UI
{
    public partial class GXDLMSRoutingTableDlg : Form
    {
        private GXDLMSRoutingTable Item;
        public GXDLMSRoutingTableDlg(GXDLMSRoutingTable item)
        {
            // Preserve the font metrics used by the fixed-coordinate designer.
            Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F);
            InitializeComponent();
            NativeDialogLayout.Apply(this);
            Item = item;
            DestinationTb.Text = Item.DestinationAddress.ToString();
            NextHopTb.Text = Item.NextHopAddress.ToString();
            RouteCostTb.Text = Item.RouteCost.ToString();
            HopCountTb.Text = Item.HopCount.ToString();
            WeakLinkCountTb.Text = Item.WeakLinkCount.ToString();
            ValidTimeTb.Text = Item.ValidTime.ToString();
        }

        private void OkBtn_Click(object sender, EventArgs e)
        {
            try
            {
                Item.DestinationAddress = UInt16.Parse(DestinationTb.Text);
                Item.NextHopAddress = UInt16.Parse(NextHopTb.Text);
                Item.RouteCost = UInt16.Parse(RouteCostTb.Text);
                Item.HopCount = byte.Parse(HopCountTb.Text);
                Item.WeakLinkCount = byte.Parse(WeakLinkCountTb.Text);
                Item.ValidTime = UInt16.Parse(ValidTimeTb.Text);
            }
            catch (Exception ex)
            {
                this.DialogResult = DialogResult.None;
                MessageBox.Show(this, ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
