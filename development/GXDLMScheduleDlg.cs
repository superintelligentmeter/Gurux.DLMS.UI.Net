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
using Gurux.DLMS.Objects.Enums;
using System;
using System.Windows.Forms;

namespace Gurux.DLMS.UI
{
    public partial class GXDLMScheduleDlg : Form
    {
        readonly GXScheduleEntry entry;
        readonly int mode;
        public GXDLMScheduleDlg(
            GXScheduleEntry e,
            GXDLMSObjectCollection objects, int index)
        {
            entry = e;
            mode = index;
            // Preserve the font metrics used by the fixed-coordinate designer.
            Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F);
            InitializeComponent();
            ApplyLayout();
            foreach (GXDLMSObject it in objects.GetObjects(ObjectType.ScriptTable))
            {
                ScriptTb.Items.Add(it);
            }
            ScriptTb.SelectedItem = entry.Script;
            IndexTB.Text = entry.Index.ToString();
            EnabledCb.Checked = entry.Enable;
            SelectorTb.Text = entry.ScriptSelector.ToString();
            TimeTb.Text = (entry.SwitchTime ?? new GXTime()).ToFormatString();
            MondayCb.Checked = (entry.ExecWeekdays & Weekdays.Monday) != 0;
            TuesdayCb.Checked = (entry.ExecWeekdays & Weekdays.Tuesday) != 0;
            WednesdayCb.Checked = (entry.ExecWeekdays & Weekdays.Wednesday) != 0;
            ThursdayCb.Checked = (entry.ExecWeekdays & Weekdays.Thursday) != 0;
            FridayCb.Checked = (entry.ExecWeekdays & Weekdays.Friday) != 0;
            SaturdayCb.Checked = (entry.ExecWeekdays & Weekdays.Saturday) != 0;
            SundayCb.Checked = (entry.ExecWeekdays & Weekdays.Sunday) != 0;
            ValidityWindowTb.Text = entry.ValidityWindow.ToString();
            ExecSpecDaysTb.Text = entry.ExecSpecDays;
            BeginTb.Text = (entry.BeginDate ?? new GXDate(-1, 1, 1)).ToFormatString();
            EndTb.Text = (entry.EndDate ?? new GXDate(-1, 1, 1)).ToFormatString();
            //Enable/Disable
            if (index == 1)
            {
                this.Text = "Enable/Disable schedule entry";
                ScriptTb.Enabled = false;
                IndexTB.ReadOnly = true;
                EnabledCb.Enabled = true;
                SelectorTb.ReadOnly = true;
                TimeTb.ReadOnly = true;
                MondayCb.Enabled = false;
                TuesdayCb.Enabled = false;
                WednesdayCb.Enabled = false;
                ThursdayCb.Enabled = false;
                FridayCb.Enabled = false;
                SaturdayCb.Enabled = false;
                SundayCb.Enabled = false;
                ValidityWindowTb.ReadOnly = true;
                ExecSpecDaysTb.ReadOnly = true;
                BeginTb.ReadOnly = true;
                EndTb.ReadOnly = true;
            }
            else if (index == 2)
            {
                this.Text = "Add schedule entry";
            }
            //Remove
            else if (index == 3)
            {
                this.Text = "Remove schedule entry";
                ScriptTb.Enabled = false;
                IndexTB.ReadOnly = true;
                EnabledCb.Enabled = false;
                SelectorTb.ReadOnly = true;
                TimeTb.ReadOnly = true;
                MondayCb.Enabled = false;
                TuesdayCb.Enabled = false;
                WednesdayCb.Enabled = false;
                ThursdayCb.Enabled = false;
                FridayCb.Enabled = false;
                SaturdayCb.Enabled = false;
                SundayCb.Enabled = false;
                ValidityWindowTb.ReadOnly = true;
                ExecSpecDaysTb.ReadOnly = true;
                BeginTb.ReadOnly = true;
                EndTb.ReadOnly = true;
            }
        }


        private void ApplyLayout()
        {
            SuspendLayout();
            var table = ModernPresentation.CompactSurface(this);
            ModernPresentation.Section(table, "Schedule entry");
            ModernPresentation.CompactRow(table, IndexLbl.Text, IndexTB);
            ModernPresentation.CompactRow(table, "", EnabledCb);
            ModernPresentation.CompactRow(table, ScriptLbl.Text, ScriptTb);
            ModernPresentation.CompactRow(table, SelectorLbl.Text, SelectorTb);
            ModernPresentation.Section(table, "Execution");
            ModernPresentation.CompactRow(table, TimeLbl.Text, TimeTb);
            ModernPresentation.CompactRow(table, ValidityWindowLbl.Text, ValidityWindowTb);
            var weekdays = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = true };
            foreach (var day in new[] { MondayCb, TuesdayCb, WednesdayCb, ThursdayCb, FridayCb, SaturdayCb, SundayCb })
            {
                day.Margin = new Padding(4, 6, 12, 6);
                weekdays.Controls.Add(day);
            }
            ExecWeekdaysGb.Controls.Add(weekdays);
            ExecWeekdaysGb.Dock = DockStyle.Fill;
            int row = table.RowCount++;
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 100 * DeviceDpi / 96));
            table.Controls.Add(ExecWeekdaysGb, 0, row);
            table.SetColumnSpan(ExecWeekdaysGb, 3);
            ExecWeekdaysGb.TabIndex = row * 2;
            ModernPresentation.CompactRow(table, ExecSpecDaysLbl.Text, ExecSpecDaysTb);
            ModernPresentation.CompactRow(table, BeginLbl.Text, BeginTb);
            ModernPresentation.CompactRow(table, EndLbl.Text, EndTb);
            foreach (var label in new[] { IndexLbl, ScriptLbl, SelectorLbl, TimeLbl, ValidityWindowLbl, ExecSpecDaysLbl, BeginLbl, EndLbl })
                label.Visible = false;
            // Scroll the fields independently so confirmation remains reachable at narrow widths.
            var body = new Panel
            {
                Name = "ScheduleBody", Dock = DockStyle.Fill, AutoScroll = true,
                Padding = Padding, AutoScrollMinSize = AutoScrollMinSize, TabIndex = 0
            };
            AutoScroll = false;
            AutoScrollMinSize = System.Drawing.Size.Empty;
            Padding = Padding.Empty;
            body.Controls.Add(table);
            Controls.Add(body);
            table.TabIndex = 0;
            var footer = ModernPresentation.DialogFooter(this, OkBtn, CancelBtn);
            footer.TabIndex = 1;
            OkBtn.TabIndex = 0;
            CancelBtn.TabIndex = 1;
            ClientSize = new System.Drawing.Size(640 * DeviceDpi / 96, 710 * DeviceDpi / 96);
            ModernPresentation.Apply(this);
            ResumeLayout(true);
        }

        private void OkBtn_Click(object sender, EventArgs e)
        {
            try
            {
                if (ScriptTb.SelectedItem == null)
                {
                    throw new Exception("Target is not selected.");
                }
                // Validate the complete input before publishing any caller-owned state.
                var candidate = new GXScheduleEntry();
                candidate.Index = Convert.ToUInt16(IndexTB.Text);
                if (candidate.Index == 0)
                {
                    throw new Exception("Invalid index. Index is one based.");
                }
                candidate.Enable = EnabledCb.Checked;
                candidate.Script = (GXDLMSScriptTable)ScriptTb.SelectedItem;
                candidate.ScriptSelector = Convert.ToUInt16(SelectorTb.Text);
                candidate.SwitchTime = new GXTime(TimeTb.Text);
                candidate.ExecWeekdays = Weekdays.None;
                if (MondayCb.Checked)
                {
                    candidate.ExecWeekdays |= Weekdays.Monday;
                }
                if (TuesdayCb.Checked)
                {
                    candidate.ExecWeekdays |= Weekdays.Tuesday;
                }
                if (WednesdayCb.Checked)
                {
                    candidate.ExecWeekdays |= Weekdays.Wednesday;
                }
                if (ThursdayCb.Checked)
                {
                    candidate.ExecWeekdays |= Weekdays.Thursday;
                }
                if (FridayCb.Checked)
                {
                    candidate.ExecWeekdays |= Weekdays.Friday;
                }
                if (SaturdayCb.Checked)
                {
                    candidate.ExecWeekdays |= Weekdays.Saturday;
                }
                if (SundayCb.Checked)
                {
                    candidate.ExecWeekdays |= Weekdays.Sunday;
                }
                candidate.ValidityWindow = Convert.ToUInt16(ValidityWindowTb.Text);
                candidate.ExecSpecDays = ExecSpecDaysTb.Text;
                candidate.BeginDate = new GXDate(BeginTb.Text);
                candidate.EndDate = new GXDate(EndTb.Text);
                if (mode == 1)
                {
                    entry.Enable = candidate.Enable;
                }
                else if (mode == 2)
                {
                    // An unchanged formatted date/time must retain its full skip metadata.
                    if (entry.SwitchTime != null && TimeTb.Text == entry.SwitchTime.ToFormatString())
                        candidate.SwitchTime = entry.SwitchTime;
                    if (entry.BeginDate != null && BeginTb.Text == entry.BeginDate.ToFormatString())
                        candidate.BeginDate = entry.BeginDate;
                    if (entry.EndDate != null && EndTb.Text == entry.EndDate.ToFormatString())
                        candidate.EndDate = entry.EndDate;
                    entry.Index = candidate.Index;
                    entry.Enable = candidate.Enable;
                    entry.Script = candidate.Script;
                    entry.ScriptSelector = candidate.ScriptSelector;
                    entry.SwitchTime = candidate.SwitchTime;
                    entry.ExecWeekdays = candidate.ExecWeekdays;
                    entry.ValidityWindow = candidate.ValidityWindow;
                    entry.ExecSpecDays = candidate.ExecSpecDays;
                    entry.BeginDate = candidate.BeginDate;
                    entry.EndDate = candidate.EndDate;
                }
                // Mode 3 is confirmation only. The original validation still applies.

            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                DialogResult = DialogResult.None;
            }
        }
    }
}
