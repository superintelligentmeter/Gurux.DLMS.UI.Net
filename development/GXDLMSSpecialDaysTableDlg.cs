// Modified for the superintelligentmeter fork, 2026-10-16: .NET 10 migration, UI modernization and compatibility fixes. GPL-2.0-only; original notices retained.
//
// --------------------------------------------------------------------------
//  Gurux Ltd
//
//
//
// Filename:        $HeadURL: $
//
// Version:         $Revision: $,
//                  $Date: $
//                  $Author: gurux01 $
//
// Copyright (c) Gurux Ltd
//
//---------------------------------------------------------------------------
//
//  DESCRIPTION
//
// This file is a part of Gurux Device Framework.
//
// Gurux Device Framework is Open Source software; you can redistribute it
// and/or modify it under the terms of the GNU General Public License
// as published by the Free Software Foundation; version 2 of the License.
// Gurux Device Framework is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
// See the GNU General Public License for more details.
//
// More information of Gurux DLMS/COSEM Director: https://www.gurux.org/GXDLMSDirector
//
// This code is licensed under the GNU General Public License v2.
// Full text may be retrieved at http://www.gnu.org/licenses/gpl-2.0.txt
//---------------------------------------------------------------------------

using Gurux.DLMS.Objects;
using System;
using System.Windows.Forms;

namespace Gurux.DLMS.UI
{
    public partial class GXDLMSSpecialDaysTableDlg : Form
    {
        /// <summary>
        /// Selected item.
        /// </summary>
        internal GXDLMSSpecialDay Item;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="item"></param>
        /// <param name="readOnly"></param>
        public GXDLMSSpecialDaysTableDlg(GXDLMSSpecialDay item, bool readOnly)
        {
            // Preserve the font metrics used by the fixed-coordinate designer.
            Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F);
            InitializeComponent();
            SuspendLayout();
            var table = ModernPresentation.CompactSurface(this);
            ModernPresentation.Section(table, "Special day");
            ModernPresentation.CompactRow(table, IndexLbl.Text, IndexTb);
            ModernPresentation.CompactRow(table, DateLbl.Text, DateTb);
            ModernPresentation.CompactRow(table, DayIdLbl.Text, DayIdTb);
            IndexLbl.Visible = DateLbl.Visible = DayIdLbl.Visible = false;
            Controls.Add(table);
            ModernPresentation.DialogFooter(this, OkBtn, CancelBtn);
            ClientSize = new System.Drawing.Size(600 * DeviceDpi / 96, 270 * DeviceDpi / 96);
            ModernPresentation.Apply(this);
            ResumeLayout(true);
            Item = item;      
            if (readOnly)
            {
                IndexTb.ReadOnly = DateTb.ReadOnly = DayIdTb.ReadOnly = true;
            }
        }

        private void OkBtn_Click(object sender, EventArgs e)
        {
            try
            {
                // Removal is confirmation only; never normalize the selected model.
                if (!IndexTb.ReadOnly)
                {
                    ushort index = UInt16.Parse(IndexTb.Text);
                    GXDate date = new GXDate(DateTb.Text);
                    byte dayId = byte.Parse(DayIdTb.Text);
                    Item.Index = index;
                    Item.Date = date;
                    Item.DayId = dayId;
                }
                DialogResult = DialogResult.OK;
            }
            catch (Exception ex)
            {
                DialogResult = DialogResult.None;
                MessageBox.Show(this, ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void GXDLMSSpecialDaysTableDlg_Load(object sender, EventArgs e)
        {
            IndexTb.Text = Item.Index.ToString();
            // Display a default without changing the source until a valid OK.
            DateTb.Text = (Item.Date ?? new GXDate(DateTime.Now)).ToFormatString();
            DayIdTb.Text = Item.DayId.ToString();
        }
    }
}
