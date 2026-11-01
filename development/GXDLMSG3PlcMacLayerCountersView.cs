// superintelligentmeter, 2026-10-29: clear per-attribute change indicators on clean notifications. GPL-2.0-only; original notices retained.
// Modified for the superintelligentmeter fork, 2026-10-16: .NET 10 migration, UI modernization and compatibility fixes. GPL-2.0-only; original notices retained.
//
// --------------------------------------------------------------------------
//  Gurux Ltd
//
//
//
// Filename:        $HeadURL: svn://mars/Projects/GuruxClub/GXDLMSDirector/Development/Views/GXDLMSG3PlcMacLayerCounters.cs $
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
    /// https://www.gurux.fi/Gurux.DLMS.Objects.GXDLMSG3PlcMacLayerCounters
    /// </summary>
    [GXDLMSViewAttribute(typeof(GXDLMSG3PlcMacLayerCounters))]
    partial class GXDLMSG3PlcMacLayerCountersView : Form, IGXDLMSView
    {

        /// <summary>
        /// Constructor.
        /// </summary>
        public GXDLMSG3PlcMacLayerCountersView()
        {
            // Preserve the font metrics used by the fixed-coordinate designer.
            Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F);
            InitializeComponent();
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
            throw new IndexOutOfRangeException("index");
        }

        public void PreAction(GXActionArgs arg)
        {
            arg.Value = (sbyte)0;
            DialogResult ret;
            if (arg.Index == 1)
            {
                //Reset.
                ret = GXHelpers.ShowMessageBox(this, Properties.Resources.RegisterResetWarning, "", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
                arg.Handled = ret != DialogResult.Yes;
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
                return null;
            }
        }

        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public string Description
        {
            get
            {
                return DescriptionTB.Text;
            }
            set
            {
                DescriptionTB.Text = value;
            }
        }

        public void OnDirtyChange(int index, bool Dirty)
        {
            switch (index)
            {
                case 2:
                    errorProvider1.SetError(TxDataPacketCountTB, Dirty ? Properties.Resources.ValueChangedTxt : null);
                    break;
                case 3:
                    errorProvider1.SetError(RxDataPacketCountTB, Dirty ? Properties.Resources.ValueChangedTxt : null);
                    break;
                case 4:
                    errorProvider1.SetError(TxCmdPacketCountTB, Dirty ? Properties.Resources.ValueChangedTxt : null);
                    break;

                case 5:
                    errorProvider1.SetError(RxCmdPacketCountTB, Dirty ? Properties.Resources.ValueChangedTxt : null);
                    break;
                case 6:
                    errorProvider1.SetError(CSMAFailCountTB, Dirty ? Properties.Resources.ValueChangedTxt : null);
                    break;
                case 7:
                    errorProvider1.SetError(CSMANoAckCountTB, Dirty ? Properties.Resources.ValueChangedTxt : null);
                    break;
                case 8:
                    errorProvider1.SetError(BadCrcCountTB, Dirty ? Properties.Resources.ValueChangedTxt : null);
                    break;
                case 9:
                    errorProvider1.SetError(TxDataBroadcastCountTB, Dirty ? Properties.Resources.ValueChangedTxt : null);
                    break;
                case 10:
                    errorProvider1.SetError(RxDataBroadcastCountTB, Dirty ? Properties.Resources.ValueChangedTxt : null);
                    break;
                default:
                    errorProvider1.Clear();
                    break;
            }
        }

        public void OnAccessRightsChange(GXDLMSViewArguments arg)
        {
            throw new IndexOutOfRangeException("index");
        }

        public void OnMethodAccessRightsChange(GXDLMSViewArguments arg)
        {
        }
        #endregion

        private void ValueTB_KeyPress(object sender, KeyPressEventArgs e)
        {

        }

        private void ValueTB_KeyUp(object sender, KeyEventArgs e)
        {

        }
    }
}
