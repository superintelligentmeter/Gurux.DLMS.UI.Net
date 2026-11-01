// superintelligentmeter contributors, 2026-10-30: bound property rows and align native Schedule actions with the entries list. GPL-2.0-only; original notices retained.
// Modified for the Intelmeter fork, 2026-10-16: .NET 10 migration, UI modernization and compatibility fixes. GPL-2.0-only; original notices retained.
//
// --------------------------------------------------------------------------
//  Gurux Ltd
//
//
//
// Filename:        $HeadURL: svn://mars/Projects/GuruxClub/GXDLMSDirector/Development/Views/GXDLMSScheduleView.cs $
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

using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using Gurux.DLMS.Objects;
using Gurux.DLMS;
using Gurux.DLMS.Enums;

namespace Gurux.DLMS.UI
{
    /// <summary>
    /// Online help:
    /// https://www.gurux.fi/Gurux.DLMS.Objects.GXDLMSSchedule
    /// </summary>
    [GXDLMSViewAttribute(typeof(Gurux.DLMS.Objects.GXDLMSSchedule))]
    partial class GXDLMSScheduleView : Form, IGXDLMSView
    {

        /// <summary>
        /// Constructor.
        /// </summary>
        public GXDLMSScheduleView()
        {
            // Preserve the font metrics used by the fixed-coordinate designer.
            Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F);
            InitializeComponent();
            SuspendLayout();
            var table = ModernPresentation.CompactSurface(this);
            ModernPresentation.Section(table, "Schedule");
            ModernPresentation.CompactRow(table, LogicalNameLbl.Text, LogicalNameTB);
            ModernPresentation.Section(table, ValueLbl.Text);
            int row = table.RowCount++;
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 300 * DeviceDpi / 96));
            EntriesView.Dock = DockStyle.Fill;
            EntriesView.Margin = Padding.Empty;
            EntriesView.AccessibleName = ValueLbl.Text;
            EntriesView.TabIndex = row * 2;
            table.Controls.Add(EntriesView, 0, row);
            table.SetColumnSpan(EntriesView, 3);
            int[] widths = { 64, 72, 132, 76, 112, 124, 180, 132, 112, 112 };
            for (int pos = 0; pos != widths.Length; ++pos)
                EntriesView.Columns[pos].Width = widths[pos] * DeviceDpi / 96;
            var actions = new ActionButtonPanel
            {
                Name = "ScheduleActions", Dock = DockStyle.Fill, AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink, WrapContents = true,
                FlowDirection = FlowDirection.RightToLeft, Margin = Padding.Empty,
                BackColor = table.BackColor
            };
            var buttons = new[] { gxButton1, AddObjectBtn, RemoveObjectBtn };
            for (int i = buttons.Length - 1; i >= 0; --i)
            {
                var button = buttons[i];
                ModernPresentation.CompactButton(button);
                button.AutoSize = false;
                button.Anchor = AnchorStyles.Top | AnchorStyles.Left;
                button.TabIndex = i;
                actions.Controls.Add(button);
            }
            ButtonLayoutPolicy.Apply(actions, buttons, minimumWidth: 128);
            row = table.RowCount++;
            table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            actions.TabIndex = row * 2;
            table.Controls.Add(actions, 0, row);
            table.SetColumnSpan(actions, 3);
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
            if (arg.Index != 2)
            {
                throw new IndexOutOfRangeException("index");
            }
            EntriesView.Items.Clear();
            GXDLMSSchedule target = (GXDLMSSchedule)Target;
            foreach (GXScheduleEntry item in target.Entries)
            {
                AddEntry(item);
            }
        }

        private void AddEntry(GXScheduleEntry item)
        {
            string ln;
            if (item.Script == null)
            {
                ln = "";
            }
            else
            {
                ln = item.Script.LogicalName;
            }
            ListViewItem li = EntriesView.Items.Add(item.Index.ToString());
            li.SubItems.AddRange(new string[] {
                    item.Enable.ToString(),
                    ln,
                    item.ScriptSelector.ToString() ,
                    item.SwitchTime.ToString() ,
                    item.ValidityWindow.ToString(),
                    item.ExecWeekdays.ToString() ,
                    item.ExecSpecDays.ToString() ,
                    item.BeginDate.ToString() ,
                    item.EndDate.ToString()});
            li.Tag = item;
        }

        public void OnAccessRightsChange(GXDLMSViewArguments arg)
        {
            if (arg.Index != 2)
            {
                throw new IndexOutOfRangeException("index");
            }
        }

        public void OnMethodAccessRightsChange(GXDLMSViewArguments arg)
        {
        }

        delegate void ShowScheduleDialogEventHandler(GXActionArgs arg);

        void OnShowDialog(GXActionArgs arg)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new ShowScheduleDialogEventHandler(OnShowDialog), arg).AsyncWaitHandle.WaitOne();
            }
            else
            {
                GXDLMSSchedule target = (GXDLMSSchedule)Target;
                if (arg.Index == 1)
                {
                    //Enable/Disable...
                    if (EntriesView.SelectedItems.Count == 1)
                    {
                        GXScheduleEntry it = (GXScheduleEntry)EntriesView.SelectedItems[0].Tag;
                        using GXDLMScheduleDlg dlg = new GXDLMScheduleDlg(it, target.Parent, 1);
                        if (dlg.ShowDialog(this.Parent) == DialogResult.OK)
                        {
                            if (it.Enable)
                            {
                                arg.Value = target.Enable(arg.Client, it);
                            }
                            else
                            {
                                arg.Value = target.Disable(arg.Client, it);
                            }
                            EntriesView.SelectedItems[0].SubItems[1].Text = it.Enable.ToString();
                        }
                        else
                        {
                            arg.Handled = true;
                        }
                    }
                    else
                    {
                        arg.Handled = true;
                    }
                }
                else if (arg.Index == 2)
                {
                    //Add
                    GXScheduleEntry it = new GXScheduleEntry();
                    using GXDLMScheduleDlg dlg = new GXDLMScheduleDlg(it, target.Parent, 2);
                    if (dlg.ShowDialog(this.Parent) == DialogResult.OK)
                    {
                        arg.Value = target.Insert(arg.Client, it);
                        AddEntry(it);
                    }
                    else
                    {
                        arg.Handled = true;
                    }
                }
                else if (arg.Index == 3)
                {
                    //Delete
                    if (EntriesView.SelectedItems.Count == 1)
                    {
                        GXScheduleEntry it = (GXScheduleEntry)EntriesView.SelectedItems[0].Tag;
                        using GXDLMScheduleDlg dlg = new GXDLMScheduleDlg(it, target.Parent, 3);
                        if (dlg.ShowDialog(this.Parent) == DialogResult.OK)
                        {
                            arg.Value = target.Delete(arg.Client, it);
                            EntriesView.SelectedItems[0].Remove();
                        }
                        else
                        {
                            arg.Handled = true;
                        }
                    }
                    else
                    {
                        arg.Handled = true;
                    }
                }
            }
        }

        public void PreAction(GXActionArgs arg)
        {
            if (arg.Action == ActionType.Action)
            {
                OnShowDialog(arg);
            }
        }

        public void PostAction(GXActionArgs arg)
        {
            if (arg.Exception == null)
            {
                GXHelpers.ShowMessageBox(this, Properties.Resources.ActionImplemented, "", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
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
