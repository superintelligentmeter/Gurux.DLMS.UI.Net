// Modified for the superintelligentmeter fork, 2026-10-16: .NET 10 migration, UI modernization and compatibility fixes. GPL-2.0-only; original notices retained.
//
// --------------------------------------------------------------------------
//  Gurux Ltd
//
//
//
// Filename:        $HeadURL$
//
// Version:         $Revision$,
//                  $Date$
//                  $Author$
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
// More information of Gurux products: https://www.gurux.org
//
// This code is licensed under the GNU General Public License v2.
// Full text may be retrieved at http://www.gnu.org/licenses/gpl-2.0.txt
//---------------------------------------------------------------------------

using Gurux.DLMS.Enums;
using Gurux.DLMS.Objects;
using System;
using System.Windows.Forms;

namespace Gurux.DLMS.UI
{
    public partial class GXDLMSModemConfigurationDlg : Form
    {
        public GXDLMSModemInitialisation Data
        {
            get;
            private set;
        }

        public GXDLMSModemConfigurationDlg(GXDLMSModemInitialisation item)
        {
            // Preserve the font metrics used by the fixed-coordinate designer.
            Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F);
            InitializeComponent();
            Data = item;
            RequestTb.Text = item.Request;
            ResponseTb.Text = item.Response;
            DelayTb.Text = item.Delay.ToString();
            if (System.ComponentModel.LicenseManager.UsageMode != System.ComponentModel.LicenseUsageMode.Designtime)
            {
                InitializePresentation();
            }
        }

        private void InitializePresentation()
        {
            SuspendLayout();
            try
            {
                var table = ModernPresentation.CompactSurface(this);
                Controls.Add(table);
                ModernPresentation.CompactRow(table, RequestLbl.Text, RequestTb);
                ModernPresentation.CompactRow(table, ResponseLbl.Text, ResponseTb);
                ModernPresentation.CompactRow(table, DelayLbl.Text, DelayTb);
                RequestLbl.Visible = ResponseLbl.Visible = DelayLbl.Visible = false;
                table.TabIndex = 0;
                var footer = ModernPresentation.DialogFooter(this, OkBtn, CancelBtn);
                footer.TabIndex = 1;
                OkBtn.TabIndex = 0;
                CancelBtn.TabIndex = 1;
                ClientSize = new System.Drawing.Size((int)(600 * DeviceDpi / 96f), (int)(250 * DeviceDpi / 96f));
                ModernPresentation.Apply(this);
                ActiveControl = RequestTb;
            }
            finally
            {
                ResumeLayout(true);
            }
        }
        private void OkBtn_Click(object sender, EventArgs e)
        {
            try
            {
                if (string.IsNullOrEmpty(RequestTb.Text))
                {
                    throw new Exception("Request is not selected.");
                }
                if (string.IsNullOrEmpty(ResponseTb.Text))
                {
                    throw new Exception("Response is not selected.");
                }
                ushort delay = UInt16.Parse(DelayTb.Text);
                Data.Request = RequestTb.Text;
                Data.Response = ResponseTb.Text;
                Data.Delay = delay;
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                DialogResult = DialogResult.None;
            }
        }
    }
}
