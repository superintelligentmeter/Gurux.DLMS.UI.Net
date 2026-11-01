// Modified for the superintelligentmeter fork, 2026-10-16: .NET 10 migration, UI modernization and compatibility fixes. GPL-2.0-only; original notices retained.
//
// --------------------------------------------------------------------------
//  Gurux Ltd
//
//
//
// Filename:        $HeadURL: svn://mars/Projects/GuruxClub/GXDLMSDirector/Development/Views/GXDLMSHdlcSetupView.cs $
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
using Gurux.DLMS;
using Gurux.DLMS.Objects;
using Gurux.DLMS.Enums;

namespace Gurux.DLMS.UI
{
    /// <summary>
    /// Online help:
    /// https://www.gurux.fi/Gurux.DLMS.Objects.GXDLMSHdlcSetup
    /// </summary>
    [GXDLMSViewAttribute(typeof(GXDLMSHdlcSetup))]
    partial class GXDLMSHdlcSetupView : Form, IGXDLMSView
    {

        /// <summary>
        /// Constructor.
        /// </summary>
        public GXDLMSHdlcSetupView()
        {
            // Preserve the font metrics used by the fixed-coordinate designer.
            Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F);
            InitializeComponent();
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
                ModernPresentation.Section(table, groupBox1.Text);
                ModernPresentation.CompactRow(table, LogicalNameLbl.Text, LogicalNameTB);
                ModernPresentation.CompactRow(table, SpeedLbl.Text, SpeedTB);
                ModernPresentation.CompactRow(table, InternalTimeOutLbl.Text, InternalTimeOutLTB);
                ModernPresentation.CompactRow(table, InactivityTimeoutLbl.Text, InactivityTimeoutTB);
                ModernPresentation.CompactRow(table, DeviceAddressLbl.Text, DeviceAddressTB);
                ModernPresentation.Section(table, groupBox2.Text);
                ModernPresentation.CompactRow(table, TransmitWindowSizeLbl.Text, TransmitWindowSizeTB);
                ModernPresentation.CompactRow(table, TransmitMaxLengthLbl.Text, TransmitMaxLengthTB);
                ModernPresentation.Section(table, groupBox3.Text);
                ModernPresentation.CompactRow(table, ReceiveWindowSizeLbl.Text, ReceiveWindowSizeTB);
                ModernPresentation.CompactRow(table, ReceiveMaxLengthLbl.Text, ReceiveMaxLengthTB);
                Controls.Remove(groupBox1);
                groupBox1.Dispose();
                ModernPresentation.Apply(this);
            }
            finally
            {
                ResumeLayout(true);
            }
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

        public void OnAccessRightsChange(GXDLMSViewArguments arg)
        {
            throw new NotImplementedException();
        }

        public void OnMethodAccessRightsChange(GXDLMSViewArguments arg)
        {
        }

        public void PreAction(GXActionArgs arg)
        {
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
                    errorProvider1.SetError(SpeedTB, Dirty ? Properties.Resources.ValueChangedTxt : string.Empty);
                    break;
                case 3:
                    errorProvider1.SetError(TransmitWindowSizeTB, Dirty ? Properties.Resources.ValueChangedTxt : string.Empty);
                    break;
                case 4:
                    errorProvider1.SetError(ReceiveWindowSizeTB, Dirty ? Properties.Resources.ValueChangedTxt : string.Empty);
                    break;
                case 5:
                    errorProvider1.SetError(TransmitMaxLengthTB, Dirty ? Properties.Resources.ValueChangedTxt : string.Empty);
                    break;
                case 6:
                    errorProvider1.SetError(ReceiveMaxLengthTB, Dirty ? Properties.Resources.ValueChangedTxt : string.Empty);
                    break;
                case 7:
                    errorProvider1.SetError(InternalTimeOutLTB, Dirty ? Properties.Resources.ValueChangedTxt : string.Empty);
                    break;
                case 8:
                    errorProvider1.SetError(InactivityTimeoutTB, Dirty ? Properties.Resources.ValueChangedTxt : string.Empty);
                    break;
                case 9:
                    errorProvider1.SetError(DeviceAddressTB, Dirty ? Properties.Resources.ValueChangedTxt : string.Empty);
                    break;
                default:
                    errorProvider1.Clear();
                    break;
            }
        }

        #endregion




    }
}
