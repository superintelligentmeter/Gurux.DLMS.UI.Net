// superintelligentmeter, 2026-10-29: retain the supplied attribute index when the target selector is read-only. GPL-2.0-only; original notices retained.
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
using System;
using System.Windows.Forms;

namespace Gurux.DLMS.UI
{
    public partial class GXDLMSTargetObjectDlg : Form
    {
        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public GXDLMSObject Target
        {
            get;
            set;
        }

        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public byte Index
        {
            get;
            set;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="caption">Window caption.</param>
        /// <param name="target">Selected objects.</param>
        /// <param name="objects">List of COSEM objects.</param>
        public GXDLMSTargetObjectDlg(string caption, GXDLMSObject target, GXDLMSObjectCollection objects)
        {
            Target = target;
            // Preserve the font metrics used by the fixed-coordinate designer.
            Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F);
            InitializeComponent();
            Text = caption;
            if (objects == null)
            {
                TargetCb.Enabled = false;
                Target = target;
            }
            else
            {
                foreach (GXDLMSObject it in objects)
                {
                    TargetCb.Items.Add(it);
                    if (it == target)
                    {
                        TargetCb.SelectedItem = target;
                    }
                }
            }
            IndexLbl.Visible = IndexTB.Visible = false;
            ApplyLayout(false);
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="caption">Window caption.</param>
        /// <param name="target">Selected objects.</param>
        /// <param name="index">Attribute index.</param>
        /// <param name="objects">List of COSEM objects.</param>
        public GXDLMSTargetObjectDlg(string caption, GXDLMSObject target, int index, GXDLMSObjectCollection objects)
        {
            Target = target;
            Index = checked((byte)index);
            // Preserve the font metrics used by the fixed-coordinate designer.
            Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F);
            InitializeComponent();
            Text = caption;
            if (objects == null)
            {
                TargetCb.Enabled = false;
                Target = target;
                IndexTB.Enabled = false;
            }
            else
            {
                foreach (GXDLMSObject it in objects)
                {
                    TargetCb.Items.Add(it);
                    if (it == target)
                    {
                        TargetCb.SelectedItem = target;
                    }
                }
                IndexTB.Text = index.ToString();
            }
            ApplyLayout(true);
        }

        private void ApplyLayout(bool showIndex)
        {
            SuspendLayout();
            var table = ModernPresentation.CompactSurface(this);
            ModernPresentation.CompactRow(table, TargetLbl.Text, TargetCb);
            TargetLbl.Visible = false;
            if (showIndex)
            {
                ModernPresentation.CompactRow(table, IndexLbl.Text, IndexTB);
            }
            IndexLbl.Visible = false;
            table.TabIndex = 0;
            Controls.Add(table);
            var footer = ModernPresentation.DialogFooter(this, OkBtn, CancelBtn);
            footer.TabIndex = 1;
            OkBtn.TabIndex = 0;
            CancelBtn.TabIndex = 1;
            ClientSize = new System.Drawing.Size((int)(600 * DeviceDpi / 96f),
                (int)((showIndex ? 200 : 160) * DeviceDpi / 96f));
            ModernPresentation.Apply(this);
            ActiveControl = TargetCb.Enabled ? (Control)TargetCb : OkBtn;
            ResumeLayout(true);
        }

        private void GXDLMSTargetObjectDlg_Load(object sender, EventArgs e)
        {
            if(!TargetCb.Enabled)
            {
                TargetCb.Items.Add(Target);
                TargetCb.SelectedIndex = 0;
                IndexTB.Text = Index.ToString();
            }
        }


        private void OkBtn_Click(object sender, EventArgs e)
        {
            try
            {
                GXDLMSObject selectedTarget = Target;
                byte selectedIndex = Index;
                if (TargetCb.Enabled)
                {
                    if (TargetCb.SelectedItem == null)
                    {
                        throw new Exception("Target is not selected.");
                    }
                    selectedTarget = TargetCb.SelectedItem as GXDLMSObject;
                }
                if (IndexTB.Visible)
                {
                    if (IndexTB.Text == "")
                    {
                        throw new Exception("Invalid index.");
                    }
                    selectedIndex = byte.Parse(IndexTB.Text);
                    if (selectedIndex == 0)
                    {
                        throw new Exception("Invalid index.");
                    }
                }
                Target = selectedTarget;
                Index = selectedIndex;
            }
            catch (Exception ex)
            {
                DialogResult = DialogResult.None;
                MessageBox.Show(this, ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

    }
}
