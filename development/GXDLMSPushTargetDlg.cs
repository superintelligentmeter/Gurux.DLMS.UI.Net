// superintelligentmeter contributors, 2026-10-30: align existing native property inputs through the shared width policy. GPL-2.0-only; original notices retained.
// Modified by Superintelligent Meter contributors, 2026-10-29: separate measured dialog actions from editor content. GPL-2.0-only; existing notices retained.
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

using Gurux.DLMS.Objects;
using Gurux.DLMS.Objects.Enums;
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
    public partial class GXDLMSPushTargetDlg : Form
    {
        public GXKeyValuePair<GXDLMSObject, GXDLMSCaptureObject> Target
        {
            get;
            private set;
        }

        public GXDLMSPushTargetDlg(
            GXKeyValuePair<GXDLMSObject, GXDLMSCaptureObject> target,
            GXDLMSObjectCollection objects)
        {
            Target = target;
            // Preserve the font metrics used by the fixed-coordinate designer.
            Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F);
            InitializeComponent();
            NativeDialogFooter.Apply(this, OkBtn, CancelBtn);
            NativePropertyDialog.Apply(this, (TargetLbl, TargetCb), (IndexLbl, IndexTB));
            foreach (GXDLMSObject it in objects)
            {
                TargetCb.Items.Add(it);
            }
            TargetCb.SelectedItem = target.Key;
            if (target.Value != null)
            {
                IndexTB.Text = Convert.ToString(target.Value.AttributeIndex);
            }
        }

        private void OkBtn_Click(object sender, EventArgs e)
        {
            try
            {
                if (TargetCb.SelectedItem == null)
                {
                    throw new Exception("Target is not selected.");
                }
                if (IndexTB.Text == "")
                {
                    throw new Exception("Invalid index.");
                }
                Target = new GXKeyValuePair<GXDLMSObject, GXDLMSCaptureObject>(TargetCb.SelectedItem as GXDLMSObject, new GXDLMSCaptureObject(int.Parse(IndexTB.Text), 0));
            }
            catch (Exception ex)
            {
                DialogResult = DialogResult.None;
                MessageBox.Show(this, ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
