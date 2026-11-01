// superintelligentmeter contributors, 2026-10-30: bound property rows while allowing data lists to use the workspace. GPL-2.0-only; original notices retained.
// Modified for the superintelligentmeter fork, 2026-10-16: .NET 10 migration, UI modernization and compatibility fixes. GPL-2.0-only; original notices retained.
//
// --------------------------------------------------------------------------
//  Gurux Ltd
//
//
//
// Filename:        $HeadURL: svn://mars/Projects/GuruxClub/GXDLMSDirector/Development/Views/GXDLMSSpecialDaysTableView.cs $
//
// Version:         $Revision: 8933 $,
//                  $Date: 2016-11-23 09:20:07 +0200 (ke, 23 marras 2016) $
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

using System.Windows.Forms;
using Gurux.DLMS.Objects;
using Gurux.DLMS.Enums;

namespace Gurux.DLMS.UI
{
    /// <summary>
    /// Online help:
    /// https://www.gurux.fi/Gurux.DLMS.Objects.GXDLMSSpecialDaysTable
    /// </summary>
    [GXDLMSViewAttribute(typeof(GXDLMSSpecialDaysTable))]
    partial class GXDLMSSpecialDaysTableView : Form, IGXDLMSView
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        public GXDLMSSpecialDaysTableView()
        {
            // Preserve the font metrics used by the fixed-coordinate designer.
            Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F);
            InitializeComponent();
            SuspendLayout();
            var table = ModernPresentation.CompactSurface(this);
            ModernPresentation.Section(table, "Special days table");
            ModernPresentation.CompactRow(table, LogicalNameLbl.Text, LogicalNameTB);
            ModernPresentation.Section(table, "Entries");
            int row = table.RowCount++;
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 280 * DeviceDpi / 96));
            SpecialDay.Dock = DockStyle.Fill;
            SpecialDay.AccessibleName = "Special day entries";
            SpecialDay.TabIndex = 4;
            table.Controls.Add(SpecialDay, 0, row);
            table.SetColumnSpan(SpecialDay, 3);
            IndexHeader.Width = 90 * DeviceDpi / 96;
            SpecialDayDateHeader.Width = 240 * DeviceDpi / 96;
            DayIdHeader.Width = 90 * DeviceDpi / 96;
            var actions = new ActionButtonPanel { AutoSize = true, Dock = DockStyle.Fill };
            ModernPresentation.CompactButton(DayAddBtn);
            ModernPresentation.CompactButton(DayRemoveBtn);
            actions.Controls.Add(DayAddBtn);
            actions.Controls.Add(DayRemoveBtn);
            SpecialDay.Margin = Padding.Empty;
            ButtonLayoutPolicy.Apply(actions, new[] { DayAddBtn, DayRemoveBtn });
            row = table.RowCount++;
            table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            table.Controls.Add(actions, 0, row);
            table.SetColumnSpan(actions, table.ColumnCount);
            Controls.Add(table);
            Controls.Remove(groupBox1);
            groupBox1.Dispose();
            EditorLayoutPolicy.ConfigurePropertyTable(table, boundSection: false);
            ModernPresentation.Apply(this);
            ResumeLayout(true);
        }
        #region IGXDLMSView Members

        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public GXDLMSObject Target
        {
            get;
            set;
        }

        public void OnValueChanged(GXDLMSViewArguments arg)
        {
            if (arg.Index == 2)
            {
                GXDLMSSpecialDaysTable target = Target as GXDLMSSpecialDaysTable;
                GXDLMSSpecialDay[] items = target.Entries;
                SpecialDay.Items.Clear();
                if (items != null)
                {
                    foreach (GXDLMSSpecialDay it in items)
                    {
                        ListViewItem li = SpecialDay.Items.Add(it.Index.ToString());
                        li.SubItems.Add(it.Date == null ? string.Empty : it.Date.ToString());
                        li.SubItems.Add(it.DayId.ToString());
                        li.Tag = it;
                    }
                }
            }
        }

        public void OnAccessRightsChange(GXDLMSViewArguments arg)
        {
        }

        public void OnMethodAccessRightsChange(GXDLMSViewArguments arg)
        {
        }

        delegate void SpecialDaysDlgEventHandler(GXActionArgs arg, GXDLMSSpecialDaysTableDlg dlg, GXDLMSSpecialDay it);

        void OnSpecialDaysDlg(GXActionArgs arg, GXDLMSSpecialDaysTableDlg dlg, GXDLMSSpecialDay it)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new SpecialDaysDlgEventHandler(OnSpecialDaysDlg), arg, dlg, it).AsyncWaitHandle.WaitOne();
            }
            else
            {
                bool remove = false;
                ListViewItem li = null;
                if (it == null)
                {
                    remove = true;
                    GXDLMSSpecialDaysTable target = Target as GXDLMSSpecialDaysTable;
                    if (SpecialDay.SelectedItems.Count != 1)
                    {
                        arg.Handled = true;
                        return;
                    }
                    else
                    {
                         li = SpecialDay.SelectedItems[0];
                        dlg.Item = it = (GXDLMSSpecialDay)li.Tag;                        
                    }
                }
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    GXDLMSSpecialDaysTable target = Target as GXDLMSSpecialDaysTable;
                    if (!remove)
                    {
                        li = SpecialDay.Items.Add(it.Index.ToString());
                        li.SubItems.Add(it.Date == null ? string.Empty : it.Date.ToString());
                        li.SubItems.Add(it.DayId.ToString());
                        li.Tag = it;
                        arg.Value = target.Insert(arg.Client, it);
                    }
                    else
                    {
                        arg.Value = target.Delete(arg.Client, it);
                        li.Remove();
                    }
                }
                else
                {
                    arg.Handled = true;
                }
            }
        }

        public void PreAction(GXActionArgs arg)
        {
            //Entries are handled using actions.
            if (arg.Index == 1)
            {
                GXDLMSSpecialDay it = new GXDLMSSpecialDay();
                using GXDLMSSpecialDaysTableDlg dlg = new GXDLMSSpecialDaysTableDlg(it, false);
                OnSpecialDaysDlg(arg, dlg, it);
            }
            else if (arg.Index == 2)
            {
                using GXDLMSSpecialDaysTableDlg dlg = new GXDLMSSpecialDaysTableDlg(null, true);
                OnSpecialDaysDlg(arg, dlg, null);               
            }
        }

        public void PostAction(GXActionArgs arg)
        {
            arg.Action = ActionType.None;
        }

        public System.Windows.Forms.ErrorProvider ErrorProvider
        {
            get
            {
                return errorProvider1;
            }
        }

        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public string Description
        {
            get
            {
                return null;
            }
            set
            {
            }
        }

        public void OnDirtyChange(int index, bool Dirty)
        {

        }

        #endregion               
    }
}
