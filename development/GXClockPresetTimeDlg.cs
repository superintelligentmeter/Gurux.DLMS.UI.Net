// Modified for the superintelligentmeter fork, 2026-10-16: .NET 10 migration, UI modernization and compatibility fixes. GPL-2.0-only; original notices retained.
//
// --------------------------------------------------------------------------
//  Gurux Ltd
//
//
//
// Filename:        $HeadURL:  $
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

using System;
using System.Windows.Forms;

namespace Gurux.DLMS.UI
{
    public partial class GXClockPresetTimeDlg : Form
    {
        /// <summary>
        /// Preset time.
        /// </summary>
        internal GXDateTime PresetTime
        {
            get;
            private set;
        }

        /// <summary>
        /// Interval start.
        /// </summary>
        internal GXDateTime IntervalStart
        {
            get;
            private set;
        }

        /// <summary>
        /// Interval end.
        /// </summary>
        internal GXDateTime IntervalEnd
        {
            get;
            private set;
        }

        public GXClockPresetTimeDlg()
        {
            // Preserve the font metrics used by the fixed-coordinate designer.
            Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F);
            InitializeComponent();
            SuspendLayout();
            var table = ModernPresentation.CompactSurface(this);
            ModernPresentation.Section(table, "Preset clock time");
            ModernPresentation.CompactRow(table, "Preset time", PresetTimeTb, NowPresetBtn);
            ModernPresentation.CompactRow(table, "Interval start", IntervalStartTb, NowStartBtn);
            ModernPresentation.CompactRow(table, "Interval end", IntervalEndTb, NowEndBtn);
            PresetTimeLbl.Visible = IntervalStartLbl.Visible = IntervalEndLbl.Visible = false;
            Controls.Add(table);
            ModernPresentation.DialogFooter(this, OkBtn, CancelBtn);
            ClientSize = new System.Drawing.Size((int)(600 * DeviceDpi / 96f), (int)(260 * DeviceDpi / 96f));
            ModernPresentation.Apply(this);
            ResumeLayout(true);
        }

        /// <summary>
        /// Accept changes.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OkBtn_Click(object sender, EventArgs e)
        {
            try
            {
                var preset = new GXDateTime(PresetTimeTb.Text);
                var start = new GXDateTime(IntervalStartTb.Text);
                var end = new GXDateTime(IntervalEndTb.Text);
                PresetTime = preset;
                IntervalStart = start;
                IntervalEnd = end;
            }
            catch (Exception ex)
            {
                DialogResult = DialogResult.None;
                MessageBox.Show(this, ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void NowPresetBtn_Click(object sender, EventArgs e)
        {
            PresetTimeTb.Text = DateTime.Now.ToString();
        }

        private void NowStartBtn_Click(object sender, EventArgs e)
        {
            IntervalStartTb.Text = DateTime.Now.ToString();
        }

        private void NowEndBtn_Click(object sender, EventArgs e)
        {
            IntervalEndTb.Text = DateTime.Now.ToString();
        }
    }
}
