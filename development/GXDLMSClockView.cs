// superintelligentmeter, 2026-10-29: preserve per-attribute write access when timezone and daylight-saving values refresh. GPL-2.0-only; original notices retained.
// Modified for the Intelmeter fork, 2026-10-16: .NET 10 migration, UI modernization and compatibility fixes. GPL-2.0-only; original notices retained.
//
// --------------------------------------------------------------------------
//  Gurux Ltd
//
//
//
// Filename:        $HeadURL: svn://mars/Projects/GuruxClub/GXDLMSDirector/Development/Views/GXDLMSClockView.cs $
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
using System.Windows.Forms;
using Gurux.DLMS.Objects;
using Gurux.DLMS.Enums;

namespace Gurux.DLMS.UI
{
    /// <summary>
    /// Online help:
    /// https://www.gurux.fi/Gurux.DLMS.Objects.GXDLMSClock
    /// </summary>
    [GXDLMSViewAttribute(typeof(GXDLMSClock))]
    partial class GXDLMSClockView : Form, IGXDLMSView
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        public GXDLMSClockView()
        {
            // Preserve the font metrics used by the fixed-coordinate designer.
            Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F);
            InitializeComponent();
            this.EnabledCB.Checked = false;
        }

        #region IGXDLMSView Members

        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public GXDLMSObject Target
        {
            get;
            set;
        }

        private bool timeZoneWritable;
        private bool daylightBeginWritable;
        private bool daylightEndWritable;
        private bool daylightDeviationWritable;

        private void ApplyDaylightSavingAccess()
        {
            BeginTB.ReadOnly = !EnabledCB.Checked || !daylightBeginWritable;
            EndTB.ReadOnly = !EnabledCB.Checked || !daylightEndWritable;
            DeviationTB.ReadOnly = !EnabledCB.Checked || !daylightDeviationWritable;
        }

        private void UpdateDaylightSavingAccess(GXDLMSViewArguments arg)
        {
            daylightBeginWritable = arg.Connected && arg.Client.CanWrite(Target, 5);
            daylightEndWritable = arg.Connected && arg.Client.CanWrite(Target, 6);
            daylightDeviationWritable = arg.Connected && arg.Client.CanWrite(Target, 7);
            ApplyDaylightSavingAccess();
        }

        private void UpdateTimeZone(GXDLMSClock target, bool writable)
        {
            bool useTimeZone = target.TimeZone != short.MinValue;
            if (!useTimeZone)
            {
                TimeZoneTB.Value = "";
            }
            //If time zone is not used.
            TimeZoneCb.CheckedChanged -= new System.EventHandler(TimeZoneCb_CheckedChanged);
            TimeZoneCb.Checked = useTimeZone;
            TimeZoneCb.CheckedChanged += new System.EventHandler(TimeZoneCb_CheckedChanged);
            timeZoneWritable = writable;
            TimeZoneTB.ReadOnly = !writable || !useTimeZone;
            CurrentTimeZoneBtn.Enabled = writable && useTimeZone;
        }

        public void OnValueChanged(GXDLMSViewArguments arg)
        {
            GXDLMSClock target = Target as GXDLMSClock;
            if (arg.Index == 2)
            {
                //This is handled before.
            }
            else if (arg.Index == 3)
            {
                UpdateTimeZone(target, arg.Connected && arg.Client.CanWrite(Target, 3));
            }
            else if (arg.Index == 5)
            {
                BeginTB.Value = arg.Value;
            }
            else if (arg.Index == 6)
            {
                EndTB.Value = arg.Value;
            }
            else if (arg.Index == 7)
            {
                DeviationTB.Value = arg.Value;
            }
            else if (arg.Index == 8)
            {
                EnabledCB.CheckedChanged -= new System.EventHandler(EnabledCB_CheckedChanged);
                this.EnabledCB.Checked = target.Enabled;
                UpdateDaylightSavingAccess(arg);
                EnabledCB.CheckedChanged += new System.EventHandler(EnabledCB_CheckedChanged);
            }
            else
            {
                throw new IndexOutOfRangeException("index");
            }
        }

        public void OnAccessRightsChange(GXDLMSViewArguments arg)
        {
            bool enabled = arg.Connected && arg.Client.CanWrite(Target, arg.Index);
            if (arg.Index == 2)
            {
                TimeTB.ReadOnly = !enabled;
                UpdateTimeBtn.Enabled = enabled;
            }
            else if (arg.Index == 3)
            {
                GXDLMSClock target = Target as GXDLMSClock;
                TimeZoneCb.Enabled = enabled;
                CurrentTimeZoneBtn.Enabled = enabled;
                UpdateTimeZone(target, enabled);
            }
            else if (arg.Index == 5)
            {
                BeginTB.ReadOnly = !enabled;
            }
            else if (arg.Index == 6)
            {
                EndTB.ReadOnly = !enabled;
            }
            else if (arg.Index == 7)
            {
                DeviationTB.ReadOnly = !enabled;
            }
            else if (arg.Index == 8)
            {
                EnabledCB.Enabled = enabled;
            }
            else
            {
                throw new IndexOutOfRangeException("index");
            }
            UpdateDaylightSavingAccess(arg);
        }
        public void OnMethodAccessRightsChange(GXDLMSViewArguments arg)
        {
        }

        delegate void ShowDlgEventHandler(GXActionArgs arg);

        void OnShowDlg(GXActionArgs arg)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new ShowDlgEventHandler(OnShowDlg), arg).AsyncWaitHandle.WaitOne();
            }
            else
            {
                if (arg.Index == 5)
                {
                    GXClockPresetTimeDlg dlg = new GXClockPresetTimeDlg();
                    if (dlg.ShowDialog(this) == DialogResult.OK)
                    {
                        GXByteBuffer bb = new GXByteBuffer();
                        bb.SetUInt8((byte) DataType.Structure);
                        bb.SetUInt8(4);
                        //Add data type and length.
                        bb.SetUInt8((byte)DataType.OctetString);
                        bb.SetUInt8(12);
                        bb.Set(GXDLMSTranslator.HexToBytes(dlg.PresetTime.ToHex(false, GXDlmsUi.UseMeterTimeZone)));
                        //Add data type and length.
                        bb.SetUInt8((byte)DataType.OctetString);
                        bb.SetUInt8(12);
                        bb.Set(GXDLMSTranslator.HexToBytes(dlg.IntervalStart.ToHex(false, GXDlmsUi.UseMeterTimeZone)));
                        //Add data type and length.
                        bb.SetUInt8((byte)DataType.OctetString);
                        bb.SetUInt8(12);
                        bb.Set(GXDLMSTranslator.HexToBytes(dlg.IntervalEnd.ToHex(false, GXDlmsUi.UseMeterTimeZone)));
                        arg.Value = bb.Array();
                    }
                    else
                    {
                        arg.Handled = true;
                    }
                }
                else if (arg.Index == 6)
                {
                    GXTextDlg dlg = new GXTextDlg("Shift Time", "Shift Time (s):", "", typeof(Int16));
                    if (dlg.ShowDialog(this) == DialogResult.OK)
                    {
                        arg.Value = Int16.Parse(dlg.GetValue());
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

        public void PreAction(GXActionArgs arg)
        {
            DialogResult ret;
            if (arg.Action == ActionType.Write)
            {
                if (arg.Index == 2)
                {
                    //Update current time
                    ret = GXHelpers.ShowMessageBox(this, Properties.Resources.TimeSetWarning, "", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
                    if (ret == DialogResult.Yes)
                    {
                        (Target as GXDLMSClock).Time = DateTime.Now;
                    }
                    arg.Handled = ret != DialogResult.Yes;
                }
                else if (arg.Index == 3)
                {
                    //Update current time zone.
                    ret = GXHelpers.ShowMessageBox(this, Properties.Resources.TimeZoneSetWarning, "", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
                    if (ret == DialogResult.Yes)
                    {
                        if (arg.Client.UseUtc2NormalTime)
                        {
                            (Target as GXDLMSClock).TimeZone = (int)TimeZoneInfo.Local.BaseUtcOffset.TotalMinutes;
                        }
                        else
                        {
                            (Target as GXDLMSClock).TimeZone = -(int)TimeZoneInfo.Local.BaseUtcOffset.TotalMinutes;
                        }
                        Target.UpdateDirty(3, (Target as GXDLMSClock).TimeZone);
                    }
                    arg.Handled = ret != DialogResult.Yes;
                }
            }
            else if (arg.Action == ActionType.Action)
            {
                if (arg.Index == 5 || arg.Index == 6)
                {
                    OnShowDlg(arg);
                }
                else
                {
                    arg.Value = (sbyte)0;
                }
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
            switch (index)
            {
                case 2:
                    errorProvider1.SetError(TimeTB, Dirty ? Properties.Resources.ValueChangedTxt : string.Empty);
                    break;
                case 3:
                    errorProvider1.SetError(TimeZoneTB, Dirty ? Properties.Resources.ValueChangedTxt : string.Empty);
                    break;
                case 4:
                    errorProvider1.SetError(StatusTB, Dirty ? Properties.Resources.ValueChangedTxt : string.Empty);
                    break;
                case 5:
                    errorProvider1.SetError(BeginTB, Dirty ? Properties.Resources.ValueChangedTxt : string.Empty);
                    break;
                case 6:
                    errorProvider1.SetError(EndTB, Dirty ? Properties.Resources.ValueChangedTxt : string.Empty);
                    break;
                case 7:
                    errorProvider1.SetError(DeviationTB, Dirty ? Properties.Resources.ValueChangedTxt : string.Empty);
                    break;
                case 8:
                    errorProvider1.SetError(EnabledCB, Dirty ? Properties.Resources.ValueChangedTxt : string.Empty);
                    break;
                case 9:
                    ErrorProvider.SetIconAlignment(ClockBaseTB, ErrorIconAlignment.TopRight);
                    errorProvider1.SetError(ClockBaseTB, Dirty ? Properties.Resources.ValueChangedTxt : string.Empty);
                    break;
                default:
                    errorProvider1.Clear();
                    break;
            }
        }

        #endregion

        /// <summary>
        /// User has change summer time enabled.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EnabledCB_CheckedChanged(object sender, EventArgs e)
        {
            ApplyDaylightSavingAccess();
            bool check = EnabledCB.Checked;
            (Target as GXDLMSClock).Enabled = check;
            Target.UpdateDirty(8, check);
            errorProvider1.SetError(EnabledCB, Properties.Resources.ValueChangedTxt);
        }

        private void TimeZoneCb_CheckedChanged(object sender, EventArgs e)
        {
            TimeZoneTB.ReadOnly = !timeZoneWritable || !TimeZoneCb.Checked;
            CurrentTimeZoneBtn.Enabled = timeZoneWritable && TimeZoneCb.Checked;
            if (!TimeZoneCb.Checked)
            {
                (Target as GXDLMSClock).TimeZone = short.MinValue;
            }
            else
            {
                (Target as GXDLMSClock).TimeZone = -(int)TimeZoneInfo.Local.BaseUtcOffset.TotalMinutes;
                TimeZoneTB.Value = (Target as GXDLMSClock).TimeZone.ToString();
            }
            Target.UpdateDirty(3, (Target as GXDLMSClock).TimeZone);
        }
    }
}
