// superintelligentmeter contributors, 2026-10-30: shared property widths with flexible Association data tabs. GPL-2.0-only; original notices retained.
// Modified 2026-10-30: constrain Association tabs to their available viewport. GPL-2.0-only.
// superintelligentmeter, 2026-10-29: preserve live conformance feedback and keep password actions visible at narrow widths. GPL-2.0-only; original notices retained.
// Modified for the Intelmeter fork, 2026-10-16: .NET 10 migration, UI modernization and compatibility fixes. GPL-2.0-only; original notices retained.
//
// --------------------------------------------------------------------------
//  Gurux Ltd
//
//
//
// Filename:        $HeadURL: svn://utopia/projects/GXDLMSDirector/Development/Views/GXDLMSAssociationLogicalNameView.cs $
//
// Version:         $Revision: 4781 $,
//                  $Date: 2012-03-19 10:23:38 +0200 (ma, 19 maalis 2012) $
//                  $Author: kurumi $
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
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Gurux.DLMS.Objects;
using Gurux.DLMS.Enums;
using System.Collections.Generic;
using Gurux.DLMS.Objects.Enums;

namespace Gurux.DLMS.UI
{
    /// <summary>
    /// Online help:
    /// https://www.gurux.fi/Gurux.DLMS.Objects.GXDLMSAssociationLogicalName
    /// </summary>
    [GXDLMSViewAttribute(typeof(GXDLMSAssociationLogicalName))]
    partial class GXDLMSAssociationLogicalNameView : Form, IGXDLMSView
    {

        class Country
        {
            public string Name = "";
            public byte JointIsoCtt;
            public byte CountryElement;
            public UInt16 CountryName;

            public override string ToString()
            {
                return Name;
            }
        }

        List<Country> Countries = new List<Country>();

        /// <summary>
        /// Constructor.
        /// </summary>
        public GXDLMSAssociationLogicalNameView()
        {
            // Preserve the font metrics used by the fixed-coordinate designer.
            Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F);
            InitializeComponent();
            string[] countries = Properties.Resources.Country.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string it in countries)
            {
                if (!it.StartsWith("#"))
                {
                    Country c = new Country();
                    string[] data = it.Split(new char[] { ';' });
                    c.Name = data[0];
                    string[] items = data[1].Split('.');
                    c.JointIsoCtt = byte.Parse(items[0]);
                    c.CountryElement = byte.Parse(items[1]);
                    c.CountryName = UInt16.Parse(items[2].Substring(3, items[2].Length - 4));
                    Countries.Add(c);
                    ApplicationRegistrationAuthorityCb.Items.Add(c);
                    AuthenticationRegistrationAuthorityCb.Items.Add(c);
                }
            }

            foreach (var it in Enum.GetValues(typeof(Authentication)))
            {
                AuthenticationMechanismIdCb.Items.Add(it);
            }
            if (System.ComponentModel.LicenseManager.UsageMode != System.ComponentModel.LicenseUsageMode.Designtime)
            {
                InitializePresentation();
            }
        }

        private static int ScaleFor(Control control, int value)
        {
            return (int)Math.Round(value * control.DeviceDpi / 96.0);
        }

        private static void DisposeOrphaned(Control[] controls)
        {
            foreach (Control control in controls)
            {
                if (control.Parent == null && !control.IsDisposed)
                {
                    control.Dispose();
                }
            }
        }

        private TableLayoutPanel CreateTabSurface(TabPage page, out Control[] oldControls, bool actionColumn = false)
        {
            oldControls = page.Controls.Cast<Control>().ToArray();
            page.Controls.Clear();
            page.Padding = new Padding(8);
            page.BackColor = Color.White;
            page.UseVisualStyleBackColor = false;
            page.AutoScroll = true;
            var table = new TableLayoutPanel
            {
                Name = "AssociationTabContent",
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = actionColumn ? 3 : 2,
                Padding = new Padding(0, 0, 8, 8),
                Margin = Padding.Empty,
                BackColor = Color.White
            };
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, ScaleFor(page, EditorLayoutPolicy.LabelColumnWidth)));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            if (actionColumn)
            {
                table.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            }
            page.Controls.Add(table);
            return table;
        }

        private static void AddFullWidthRow(TableLayoutPanel table, Control control)
        {
            int row = table.RowCount++;
            table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            control.Dock = DockStyle.Fill;
            control.Margin = Padding.Empty;
            table.Controls.Add(control, 0, row);
            table.SetColumnSpan(control, table.ColumnCount);
        }

        private void ConfigureTabHeaders(TabControl tabs)
        {
            tabs.DrawMode = TabDrawMode.OwnerDrawFixed;
            tabs.SizeMode = TabSizeMode.Fixed;
            tabs.ItemSize = new Size(ScaleFor(tabs, 86), ScaleFor(tabs, 24));
            tabs.BackColor = Color.White;
            tabs.Padding = new Point(2, 3);
            tabs.ShowToolTips = true;
            tabPage1.ToolTipText = "Objects";
            tabPage2.ToolTipText = "General";
            tabPage3.ToolTipText = tabPage3.Text;
            tabPage4.ToolTipText = tabPage4.Text;
            tabPage5.ToolTipText = tabPage5.Text;
            tabPage6.ToolTipText = "User List";
            tabPage3.Text = "xDLMS";
            tabPage4.Text = "App";
            tabPage5.Text = "Auth";
            tabPage6.Text = "Users";
            foreach (TabPage page in tabs.TabPages)
            {
                page.BackColor = Color.White;
                page.UseVisualStyleBackColor = false;
            }
        }

        private void BuildListPage(TabPage page, ListView list, params Button[] buttons)
        {
            Control[] oldControls = page.Controls.Cast<Control>().ToArray();
            page.Controls.Clear();
            page.Padding = new Padding(8);
            page.BackColor = Color.White;
            page.UseVisualStyleBackColor = false;
            page.AutoScroll = false;
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                Padding = Padding.Empty,
                Margin = Padding.Empty,
                BackColor = Color.White
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            list.Dock = DockStyle.Fill;
            list.Margin = new Padding(0, 0, 0, 8);
            list.BorderStyle = BorderStyle.FixedSingle;
            list.FullRowSelect = true;
            list.GridLines = true;
            if (list == ObjectsView)
            {
                ClassIdHeader.Width = 100;
                VersionHeader.Width = 72;
                LogicalNameHeader.Width = 140;
                AttributeAccesssHeader.Width = 150;
                MethodAccessHeader.Width = 150;
                list.Resize += (_, _) =>
                {
                    int available = Math.Max(400, list.ClientSize.Width - SystemInformation.VerticalScrollBarWidth - 4);
                    int remainder = Math.Max(170, available - ClassIdHeader.Width - VersionHeader.Width - LogicalNameHeader.Width);
                    AttributeAccesssHeader.Width = remainder / 2;
                    MethodAccessHeader.Width = remainder - AttributeAccesssHeader.Width;
                };
            }
            else
            {
                UserIdCh.Width = 60;
                UserNameCh.Width = Math.Max(180, list.ClientSize.Width - UserIdCh.Width - SystemInformation.VerticalScrollBarWidth - 4);
                list.Resize += (_, _) => UserNameCh.Width = Math.Max(180,
                    list.ClientSize.Width - UserIdCh.Width - SystemInformation.VerticalScrollBarWidth - 4);
            }
            layout.Controls.Add(list, 0, 0);

            var footer = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                Margin = Padding.Empty,
                Padding = Padding.Empty,
                BackColor = Color.White,
                AutoSize = true
            };
            footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            footer.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            footer.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            var left = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, WrapContents = false,
                Margin = Padding.Empty, Padding = Padding.Empty };
            var right = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Margin = Padding.Empty, Padding = Padding.Empty,
                FlowDirection = FlowDirection.LeftToRight, WrapContents = false
            };
            void SizeFooterButton(Button button)
            {
                ModernPresentation.CompactButton(button);
                button.AutoSize = false;
                button.Anchor = AnchorStyles.Top | AnchorStyles.Left;
                button.Size = new Size(ScaleFor(page, 100), ScaleFor(page, 32));
                button.MinimumSize = button.MaximumSize = button.Size;
                button.Margin = new Padding(0, 0, ScaleFor(page, 8), 0);
            }
            if (page == tabPage1)
            {
                SizeFooterButton(EditBtn);
                left.Controls.Add(EditBtn);
            }
            foreach (Button button in buttons)
            {
                SizeFooterButton(button);
                right.Controls.Add(button);
            }
            footer.Controls.Add(left, 0, 0);
            footer.Controls.Add(right, 1, 0);
            layout.Controls.Add(footer, 0, 1);
            page.Controls.Add(layout);
            DisposeOrphaned(oldControls);
        }

        private void InitializePresentation()
        {
            SuspendLayout();
            try
            {
                AutoScaleMode = AutoScaleMode.Dpi;
                AutoScroll = false;
                BackColor = Color.White;
                ForeColor = ModernPresentation.Ink;
                Padding = Padding.Empty;

                var root = new TableLayoutPanel
                {
                    Name = "AssociationContent",
                    Dock = DockStyle.Fill,
                    ColumnCount = 3,
                    RowCount = 0,
                    Padding = new Padding(12, 10, 12, 12),
                    Margin = Padding.Empty,
                    BackColor = Color.White
                };
                root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, ScaleFor(this, EditorLayoutPolicy.LabelColumnWidth)));
                root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
                // CompactRow keeps the inline version controls inside its value cell.
                // The spanning tabs retain all remaining workspace width.
                root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 0));
                ModernPresentation.Section(root, groupBox1.Text.Replace("  ", " "));
                var versionField = new FlowLayoutPanel
                {
                    AutoSize = true, WrapContents = false, FlowDirection = FlowDirection.LeftToRight,
                    Margin = Padding.Empty, Padding = Padding.Empty, Anchor = AnchorStyles.Left | AnchorStyles.Top
                };
                VersionLbl.AutoSize = true;
                VersionLbl.TextAlign = ContentAlignment.MiddleLeft;
                VersionLbl.Margin = new Padding(0, 9, 8, 0);
                VersionTb.BorderStyle = BorderStyle.FixedSingle;
                VersionTb.BackColor = Color.FromArgb(245, 247, 251);
                VersionTb.ForeColor = ModernPresentation.Ink;
                VersionTb.Width = ScaleFor(this, 90);
                VersionTb.Margin = new Padding(0, 4, 0, 4);
                versionField.Controls.Add(VersionLbl);
                versionField.Controls.Add(VersionTb);
                ModernPresentation.CompactRow(root, LogicalNameLbl.Text, LogicalNameTB, versionField, height: 40);
                tabControl1.Dock = DockStyle.Fill;
                tabControl1.Margin = new Padding(0, 6, 0, 0);
                ConfigureTabHeaders(tabControl1);
                int tabRow = root.RowCount++;
                root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
                root.Controls.Add(tabControl1, 0, tabRow);
                root.SetColumnSpan(tabControl1, 3);
                Controls.Add(root);

                BuildListPage(tabPage1, ObjectsView, AddObjectBtn, RemoveObjectBtn);
                BuildGeneralPage();
                BuildContextPage();
                BuildApplicationContextPage();
                BuildAuthenticationPage();
                BuildListPage(tabPage6, UsersList, AddUserBtn, RemoveUserBtn);
                EditorLayoutPolicy.ConfigurePropertyTable(root, boundSection: false);

                Controls.Remove(groupBox1);
                groupBox1.Dispose();
                ModernPresentation.Apply(this);
            }
            finally
            {
                ResumeLayout(true);
            }
        }

        private void BuildGeneralPage()
        {
            var table = CreateTabSurface(tabPage2, out Control[] oldControls, actionColumn: true);
            ModernPresentation.Section(table, groupBox2.Text.Replace("  ", " "));
            ModernPresentation.CompactRow(table, ClientSAPLbl.Text, ClientSAPTb);
            ModernPresentation.CompactRow(table, ServerSAPLbl.Text, ServerSAPTb);
            ModernPresentation.CompactRow(table, SecretLbl.Text, SecretTB, SecretAsciiCb);
            ModernPresentation.CompactRow(table, label1.Text, gxValueField1);
            ModernPresentation.Section(table, "Password actions");
            var passwordActions = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Margin = Padding.Empty,
                Padding = Padding.Empty, WrapContents = true
            };
            ModernPresentation.CompactButton(UpdatePwBtn);
            ModernPresentation.CompactButton(UpdateHighPwBtn);
            passwordActions.Controls.Add(UpdatePwBtn);
            passwordActions.Controls.Add(UpdateHighPwBtn);
            // Let wrapping increase the row height instead of clipping the second action.
            AddFullWidthRow(table, passwordActions);
            ModernPresentation.Section(table, "Security");
            ModernPresentation.CompactRow(table, SecuritySetupReferenceLbl.Text, SecuritySetupCb);
            EditorLayoutPolicy.ConfigurePropertyTable(table);
            groupBox2.Dispose();
            DisposeOrphaned(oldControls);
        }

        private void BuildContextPage()
        {
            var table = CreateTabSurface(tabPage3, out Control[] oldControls);
            ModernPresentation.Section(table, ConformanceLbl.Text.TrimEnd(':'));
            var checks = new TableLayoutPanel
            {
                Dock = DockStyle.Top, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 3, RowCount = 6, Margin = Padding.Empty, Padding = Padding.Empty,
                BackColor = Color.White
            };
            for (int i = 0; i < 3; ++i)
            {
                checks.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f / 3));
            }
            for (int i = 0; i < 6; ++i)
            {
                checks.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            }
            CheckBox[] flags =
            {
                ActionCB, GeneralProtectionCB, EventNotificationCB, GeneralBlockTransferCB,
                SelectiveAccessCB, Attribute0GetReferencingCB, GetCB, Attribute0SetReferencingCB,
                SetCB, PriorityManagementCB, AccessCB, GetBlockTransferCB, DataNotificationCB,
                SetBlockTransferCB, MultipleReferencesCB, ActionBlockTransferCB, DeltaValueEncodingCb
            };
            for (int i = 0; i < flags.Length; ++i)
            {
                flags[i].Margin = new Padding(0, 3, 12, 3);
                checks.Controls.Add(flags[i], i % 3, i / 3);
            }
            LNSettings.Controls.Clear();
            LNSettings.Dispose();
            AddFullWidthRow(table, checks);
            ModernPresentation.Section(table, "Context parameters");
            ModernPresentation.CompactRow(table, MaxReceivePDUSizeLbl.Text, MaxReceivePDUSizeTb);
            ModernPresentation.CompactRow(table, MaxSendPDUSizeLbl.Text, MaxSendPDUSizeTb);
            ModernPresentation.CompactRow(table, DLMSVersionNumberLbl.Text, DLMSVersionNumberTB);
            ModernPresentation.CompactRow(table, CypheringInfoLbl.Text, CypheringInfoTb);
            EditorLayoutPolicy.ConfigurePropertyTable(table);
            DisposeOrphaned(oldControls);
        }

        private void BuildApplicationContextPage()
        {
            var table = CreateTabSurface(tabPage4, out Control[] oldControls);
            ModernPresentation.Section(table, "Application context identifier");
            ModernPresentation.CompactRow(table, ApplicationRegistrationAuthorityLbl.Text, ApplicationRegistrationAuthorityCb);
            ModernPresentation.CompactRow(table, ApplicationJointISOCTTLbl.Text, ApplicationJointISOCTTTb);
            ModernPresentation.CompactRow(table, ApplicationCountryLbl.Text, ApplicationCountryTb);
            ModernPresentation.CompactRow(table, ApplicationCountryNameLbl.Text, ApplicationCountryNameTb);
            ModernPresentation.CompactRow(table, ApplicationIdentifiedOrganizationLbl.Text, ApplicationIdentifiedOrganizationTb);
            ModernPresentation.CompactRow(table, ApplicationDLMSUALbl.Text, ApplicationDLMSUATb);
            ModernPresentation.CompactRow(table, ApplicationContextLbl.Text, ApplicationContextTb);
            ModernPresentation.CompactRow(table, ApplicationContextIDLbl.Text, ApplicationContextIDCb);
            EditorLayoutPolicy.ConfigurePropertyTable(table);
            DisposeOrphaned(oldControls);
        }

        private void BuildAuthenticationPage()
        {
            var table = CreateTabSurface(tabPage5, out Control[] oldControls);
            ModernPresentation.Section(table, "Authentication mechanism identifier");
            ModernPresentation.CompactRow(table, AuthenticationRegistrationAuthorityLbl.Text, AuthenticationRegistrationAuthorityCb);
            ModernPresentation.CompactRow(table, AuthenticationJointISOCTTLbl.Text, AuthenticationJointISOCTTTb);
            ModernPresentation.CompactRow(table, AuthenticationCountryLbl.Text, AuthenticationCountryTb);
            ModernPresentation.CompactRow(table, AuthenticationCountryNameLbl.Text, AuthenticationCountryNameTb);
            ModernPresentation.CompactRow(table, AuthenticationIdentifiedorganizationLbl.Text, AuthenticationIdentifiedorganizationTb);
            ModernPresentation.CompactRow(table, AuthenticationDLMSUALbl.Text, AuthenticationDLMSUATb);
            ModernPresentation.CompactRow(table, AuthenticationMechanismNameLbl.Text, AuthenticationMechanismNameTb);
            ModernPresentation.CompactRow(table, AuthenticationMechanismIdLbl.Text, AuthenticationMechanismIdCb);
            EditorLayoutPolicy.ConfigurePropertyTable(table);
            DisposeOrphaned(oldControls);
        }

        #region IGXDLMSView Members

        GXDLMSObject _target;
        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public GXDLMSObject Target
        {
            get
            {
                return _target;
            }
            set
            {
                _target = value;
                if (value != null)
                {
                    VersionTb.Text = value.Version.ToString();
                }
                else
                {
                    VersionTb.Text = "";
                }
            }
        }

        Country FindCountry(byte jointIsoCtt, byte countryElement, UInt16 countryName)
        {
            foreach (var it in Countries)
            {
                if (it.JointIsoCtt == jointIsoCtt && it.CountryElement == countryElement && it.CountryName == countryName)
                {
                    return it;
                }
            }
            return null;
        }

        private bool Freeze;

        private void ShowConformance(Conformance c)
        {
            Freeze = true;
            GeneralProtectionCB.Checked = (c & Conformance.GeneralProtection) != 0;
            GeneralBlockTransferCB.Checked = (c & Conformance.GeneralBlockTransfer) != 0;
            Attribute0SetReferencingCB.Checked = (c & Conformance.Attribute0SupportedWithSet) != 0;
            PriorityManagementCB.Checked = (c & Conformance.PriorityMgmtSupported) != 0;
            Attribute0GetReferencingCB.Checked = (c & Conformance.Attribute0SupportedWithGet) != 0;
            GetBlockTransferCB.Checked = (c & Conformance.BlockTransferWithGetOrRead) != 0;
            SetBlockTransferCB.Checked = (c & Conformance.BlockTransferWithSetOrWrite) != 0;
            ActionBlockTransferCB.Checked = (c & Conformance.BlockTransferWithAction) != 0;
            MultipleReferencesCB.Checked = (c & Conformance.MultipleReferences) != 0;
            DataNotificationCB.Checked = (c & Conformance.DataNotification) != 0;
            AccessCB.Checked = (c & Conformance.Access) != 0;
            GetCB.Checked = (c & Conformance.Get) != 0;
            SetCB.Checked = (c & Conformance.Set) != 0;
            SelectiveAccessCB.Checked = (c & Conformance.SelectiveAccess) != 0;
            EventNotificationCB.Checked = (c & Conformance.EventNotification) != 0;
            ActionCB.Checked = (c & Conformance.Action) != 0;
            DeltaValueEncodingCb.Checked = (c & Conformance.DeltaValueEncoding) != 0;
            Freeze = false;
        }

        public void OnValueChanged(GXDLMSViewArguments arg)
        {
            GXDLMSAssociationLogicalName target = Target as GXDLMSAssociationLogicalName;
            //object list.
            if (arg.Index == 2)
            {
                GXDLMSObjectCollection items = target.ObjectList;
                ObjectsView.Items.Clear();
                if (items != null)
                {
                    foreach (GXDLMSObject it in items)
                    {
                        ListViewItem li = ObjectsView.Items.Add(it.ObjectType.ToString());
                        li.SubItems.Add(it.Version.ToString()); //Version
                        li.SubItems.Add(it.LogicalName);
                        li.Tag = it;
                        //access_rights: access_right
                        if (it is IGXDLMSBase)
                        {
                            string str = null;
                            //Show attribute access.
                            int cnt = (it as IGXDLMSBase).GetAttributeCount();
                            for (int pos = 1; pos != cnt + 1; ++pos)
                            {
                                if (str != null)
                                {
                                    str += ", ";
                                }
                                if (target.Version < 3)
                                {
                                    AccessMode mode = it.GetAccess(pos);
                                    str += pos.ToString() + " = " + mode;
                                }
                                else
                                {
                                    AccessMode3 mode = it.GetAccess3(pos);
                                    str += pos.ToString() + " = " + mode;
                                }
                            }
                            li.SubItems.Add(str);
                            //Show method access.
                            str = null;
                            cnt = (it as IGXDLMSBase).GetMethodCount();
                            for (int pos = 1; pos != cnt + 1; ++pos)
                            {
                                if (str != null)
                                {
                                    str += ", ";
                                }
                                if (target.Version < 3)
                                {
                                    MethodAccessMode mode = it.GetMethodAccess(pos);
                                    str += pos.ToString() + " = " + mode;
                                }
                                else
                                {
                                    MethodAccessMode3 mode = it.GetMethodAccess3(pos);
                                    str += pos.ToString() + " = " + mode;
                                }
                            }
                            li.SubItems.Add(str);
                        }
                    }
                }
            }
            //Associated partners ID.
            else if (arg.Index == 3)
            {
                ClientSAPTb.Text = Convert.ToString(target.ClientSAP);
                ServerSAPTb.Text = Convert.ToString(target.ServerSAP);
            }
            else if (arg.Index == 4)
            {
                try
                {
                    this.ApplicationContextIDCb.SelectedIndexChanged -= new System.EventHandler(this.ApplicationContextIDCb_SelectedIndexChanged);
                    ApplicationContextIDCb.Items.Clear();
                    if (target.ApplicationContextName.ContextId == ApplicationContextName.LogicalName ||
                        target.ApplicationContextName.ContextId == ApplicationContextName.LogicalNameWithCiphering)
                    {
                        ApplicationContextIDCb.Items.AddRange(new object[] {
                        ApplicationContextName.LogicalName, ApplicationContextName.LogicalNameWithCiphering});
                    }
                    else
                    {
                        ApplicationContextIDCb.Items.AddRange(new object[] {
                        ApplicationContextName.ShortName, ApplicationContextName.ShortNameWithCiphering});
                    }
                    // Application context name.
                    ApplicationJointISOCTTTb.Text = Convert.ToString(target.ApplicationContextName.JointIsoCtt);
                    ApplicationCountryTb.Text = Convert.ToString(target.ApplicationContextName.Country);
                    ApplicationCountryNameTb.Text = Convert.ToString(target.ApplicationContextName.CountryName);
                    ApplicationIdentifiedOrganizationTb.Text = Convert.ToString(target.ApplicationContextName.IdentifiedOrganization);
                    ApplicationDLMSUATb.Text = Convert.ToString(target.ApplicationContextName.DlmsUA);
                    ApplicationContextTb.Text = Convert.ToString(target.ApplicationContextName.ApplicationContext);
                    ApplicationContextIDCb.SelectedItem = target.ApplicationContextName.ContextId;
                    ApplicationRegistrationAuthorityCb.SelectedItem = FindCountry(target.ApplicationContextName.JointIsoCtt, target.ApplicationContextName.Country, target.ApplicationContextName.CountryName);
                }
                finally
                {
                    ApplicationContextIDCb.SelectedIndexChanged += new System.EventHandler(this.ApplicationContextIDCb_SelectedIndexChanged);
                }
            }
            else if (arg.Index == 5)
            {
                // xDLMS_context_info
                ShowConformance(target.XDLMSContextInfo.Conformance);
                MaxReceivePDUSizeTb.Text = target.XDLMSContextInfo.MaxReceivePduSize.ToString();
                MaxSendPDUSizeTb.Text = target.XDLMSContextInfo.MaxSendPduSize.ToString();
                DLMSVersionNumberTB.Text = target.XDLMSContextInfo.DlmsVersionNumber.ToString();
                CypheringInfoTb.Text = GXDLMSTranslator.ToHex(target.XDLMSContextInfo.CypheringInfo);
            }
            else if (arg.Index == 6)
            {
                Freeze = true;
                // authentication_mechanism_name
                AuthenticationJointISOCTTTb.Text = Convert.ToString(target.AuthenticationMechanismName.JointIsoCtt);
                AuthenticationCountryTb.Text = Convert.ToString(target.AuthenticationMechanismName.Country);
                AuthenticationCountryNameTb.Text = Convert.ToString(target.AuthenticationMechanismName.CountryName);
                AuthenticationIdentifiedorganizationTb.Text = Convert.ToString(target.AuthenticationMechanismName.IdentifiedOrganization);
                AuthenticationDLMSUATb.Text = Convert.ToString(target.AuthenticationMechanismName.DlmsUA);
                AuthenticationMechanismNameTb.Text = Convert.ToString(target.AuthenticationMechanismName.AuthenticationMechanismName);
                AuthenticationMechanismIdCb.Text = Convert.ToString(target.AuthenticationMechanismName.MechanismId);
                AuthenticationRegistrationAuthorityCb.SelectedItem = FindCountry(target.AuthenticationMechanismName.JointIsoCtt, target.AuthenticationMechanismName.Country, target.AuthenticationMechanismName.CountryName);
                Freeze = false;
            }
            else if (arg.Index == 7)
            {
                //Secret.
                if (GXHelpers.IsAscii(target.Secret))
                {
                    SecretAsciiCb.Checked = true;
                    SecretTB.Text = ASCIIEncoding.ASCII.GetString(target.Secret);
                }
                else
                {
                    SecretTB.Text = GXDLMSTranslator.ToHex(target.Secret);
                }
            }
            else if (arg.Index == 9)
            {
                SecuritySetupCb.Items.Clear();
                //security_setup_reference
                if (target.Parent != null)
                {
                    SecuritySetupCb.Items.Add("");
                    foreach (GXDLMSSecuritySetup it in target.Parent.GetObjects(ObjectType.SecuritySetup))
                    {
                        SecuritySetupCb.Items.Add(it);
                        if (target.SecuritySetupReference == it.LogicalName)
                        {
                            SecuritySetupCb.SelectedIndexChanged -= new System.EventHandler(this.SecuritySetupCb_SelectedIndexChanged);
                            SecuritySetupCb.SelectedItem = it;
                            SecuritySetupCb.SelectedIndexChanged += new System.EventHandler(this.SecuritySetupCb_SelectedIndexChanged);
                        }
                    }
                }
                else
                {
                    GXDLMSSecuritySetup it = new GXDLMSSecuritySetup(target.SecuritySetupReference);
                    SecuritySetupCb.Items.Add(it);
                    SecuritySetupCb.SelectedIndexChanged -= new System.EventHandler(this.SecuritySetupCb_SelectedIndexChanged);
                    SecuritySetupCb.SelectedItem = it;
                    SecuritySetupCb.SelectedIndexChanged += new System.EventHandler(this.SecuritySetupCb_SelectedIndexChanged);
                }
            }
            //User list.
            else if (arg.Index == 10)
            {
                List<KeyValuePair<byte, string>> items = target.UserList;
                UsersList.Items.Clear();
                if (items != null)
                {
                    foreach (KeyValuePair<byte, string> it in items)
                    {
                        ListViewItem li = UsersList.Items.Add(it.Key.ToString());
                        li.SubItems.Add(it.Value);
                        li.Tag = it.Key;
                    }
                }
            }
            else if (arg.Index == 11) //Current user
            {
                foreach (ListViewItem it in UsersList.Items)
                {
                    if ((byte)it.Tag == target.CurrentUser.Key)
                    {
                        it.Selected = true;
                    }
                    else if (it.Selected)
                    {
                        it.Selected = false;
                    }
                }
            }
        }

        public void OnAccessRightsChange(GXDLMSViewArguments arg)
        {
            bool writable = arg.Connected && arg.Client.CanWrite(Target, arg.Index);
            if (arg.Index == 2)
            {
            }
            else if (arg.Index == 3)
            {
                ClientSAPTb.ReadOnly = ServerSAPTb.ReadOnly = !writable;
            }
            else if (arg.Index == 4)
            {
                // Application context name.
                ApplicationJointISOCTTTb.ReadOnly = ApplicationCountryTb.ReadOnly =
                ApplicationCountryNameTb.ReadOnly = ApplicationIdentifiedOrganizationTb.ReadOnly =
                ApplicationDLMSUATb.ReadOnly = ApplicationContextTb.ReadOnly = true;
                ApplicationContextIDCb.Enabled = writable;
            }
            else if (arg.Index == 5)
            {
                // xDLMS_context_info
                MaxReceivePDUSizeTb.ReadOnly = MaxSendPDUSizeTb.ReadOnly =
                    DLMSVersionNumberTB.ReadOnly = CypheringInfoTb.ReadOnly = !writable;

                GeneralProtectionCB.Enabled = GeneralBlockTransferCB.Enabled = Attribute0SetReferencingCB.Enabled =
                    PriorityManagementCB.Enabled = Attribute0GetReferencingCB.Enabled = GetBlockTransferCB.Enabled =
                SetBlockTransferCB.Enabled = ActionBlockTransferCB.Enabled = MultipleReferencesCB.Enabled =
                DataNotificationCB.Enabled = AccessCB.Enabled = GetCB.Enabled = SetCB.Enabled =
                SelectiveAccessCB.Enabled = EventNotificationCB.Enabled = ActionCB.Enabled = DeltaValueEncodingCb.Enabled = writable;
            }
            else if (arg.Index == 6)
            {
                // authentication_mechanism_name
                AuthenticationJointISOCTTTb.ReadOnly = AuthenticationCountryTb.ReadOnly = AuthenticationCountryNameTb.ReadOnly =
                    AuthenticationIdentifiedorganizationTb.ReadOnly = AuthenticationDLMSUATb.ReadOnly =
                    AuthenticationMechanismNameTb.ReadOnly = !writable;
                AuthenticationMechanismIdCb.Enabled = writable;
            }
            else if (arg.Index == 9)
            {
                SecuritySetupCb.Enabled = writable;
            }
        }

        public void OnMethodAccessRightsChange(GXDLMSViewArguments arg)
        {
            if (arg.Index == 2)
            {
                UpdatePwBtn.Enabled = arg.Client.CanInvoke(Target, 2);
            }
        }

        delegate void ShowDialogEventHandler(GXDLMSObject it, GXActionArgs arg);

        void OnShowDialog(GXDLMSObject it, GXActionArgs arg)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new ShowDialogEventHandler(OnShowDialog), it, arg).AsyncWaitHandle.WaitOne();
            }
            else
            {
                ListViewItem li = null;
                bool remove = arg.Index == 4 && it == null;
                if (remove)
                {
                    if (ObjectsView.SelectedItems.Count == 1)
                    {
                        li = ObjectsView.SelectedItems[0];
                        it = (GXDLMSObject)li.Tag;
                    }
                    else
                    {
                        arg.Handled = true;
                        return;
                    }
                }
                if (arg.Index == -1)
                {
                    if (ObjectsView.SelectedItems.Count == 1)
                    {
                        li = ObjectsView.SelectedItems[0];
                        it = (GXDLMSObject)li.Tag;
                    }
                    else
                    {
                        arg.Handled = true;
                        return;
                    }
                }
                GXDLMSAssociationLogicalName target = Target as GXDLMSAssociationLogicalName;
                GXDLMSAssociationViewDlg dlg = new GXDLMSAssociationViewDlg(target, it, true, remove);
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    if (remove || arg.Index == -1)
                    {
                        arg.Tag = dlg.GetTarget();
                        arg.Value = target.RemoveObject(arg.Client, it);
                        li.Remove();
                    }
                    else
                    {
                        it = dlg.GetTarget();
                        li = ObjectsView.Items.Add(it.ObjectType.ToString());
                        li.SubItems.Add(it.Version.ToString());
                        li.SubItems.Add(it.LogicalName);
                        li.SubItems.Add("");
                        li.SubItems.Add("");
                        li.Tag = it;
                        target.ObjectList.Add(it);
                        arg.Value = target.AddObject(arg.Client, it);
                    }
                }
                else
                {
                    arg.Handled = true;
                }
            }
        }

        delegate void UpdatePasswordDialogEventHandler(GXActionArgs arg);

        void OnUpdatePassword(GXActionArgs arg)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new UpdatePasswordDialogEventHandler(OnUpdatePassword), arg).AsyncWaitHandle.WaitOne();
            }
            else
            {
                //Update pw.
                GXTextDlg dlg;
                if (arg.Action == ActionType.Write)
                {
                    dlg = new GXTextDlg("Update Low level password.", "Password", "");
                }
                else
                {
                    dlg = new GXTextDlg("Update High level password.", "Password", "");
                }
                arg.Handled = dlg.ShowDialog(Parent) != DialogResult.OK;
                if (!arg.Handled)
                {
                    byte[] value;
                    if (!dlg.GetValue().StartsWith("0x"))
                    {
                        value = ASCIIEncoding.ASCII.GetBytes(dlg.GetValue());
                    }
                    else
                    {
                        value = GXDLMSTranslator.HexToBytes(dlg.GetValue());
                    }
                    GXDLMSAssociationLogicalName target = Target as GXDLMSAssociationLogicalName;
                    if (arg.Action == ActionType.Write)
                    {
                        target.Secret = value;
                    }
                    else
                    {
                        GXByteBuffer bb = new GXByteBuffer();
                        bb.SetUInt8((byte)DataType.OctetString);
                        bb.SetUInt8((byte)value.Length);
                        bb.Set(value);
                        arg.Value = bb.Array();
                    }
                }
            }
        }

        delegate void ShowUserDialogEventHandler(bool addUser, GXActionArgs arg);

        void OnShowDialog(bool addUser, GXActionArgs arg)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new ShowUserDialogEventHandler(OnShowDialog), addUser, arg).AsyncWaitHandle.WaitOne();
            }
            else
            {
                GXDLMSAssociationLogicalName target = Target as GXDLMSAssociationLogicalName;
                if (addUser)
                {
                    GXUserDlg dlg = new GXUserDlg((byte)target.UserList.Count, "", false);
                    if (dlg.ShowDialog(this) == DialogResult.OK)
                    {
                        ListViewItem li = UsersList.Items.Add(dlg.UserId.ToString());
                        li.SubItems.Add(dlg.UserName);
                        arg.Value = target.AddUser(arg.Client, dlg.UserId, dlg.UserName);
                    }
                    else
                    {
                        arg.Handled = true;
                    }
                }
                else
                {
                    if (UsersList.SelectedItems.Count == 1)
                    {
                        ListViewItem li = UsersList.SelectedItems[0];
                        GXUserDlg dlg = new GXUserDlg(byte.Parse(li.SubItems[0].Text), li.SubItems[1].Text, true);
                        if (dlg.ShowDialog(this) == DialogResult.OK)
                        {
                            arg.Value = target.RemoveUser(arg.Client, dlg.UserId, dlg.UserName);
                            li.Remove();
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
            //Edit object in association view.
            if (arg.Action == ActionType. Action && arg.Index == -1)
            {
                if (arg.Tag == null)
                {
                    OnShowDialog(null, arg);
                }
                else
                {
                    GXDLMSAssociationLogicalName target = Target as GXDLMSAssociationLogicalName;
                    arg.Value = target.AddObject(arg.Client, arg.Tag as GXDLMSObject);
                    arg.Tag = null;
                }
            }
            //Add object to association view.
            else if (arg.Index == 3)
            {
                GXDLMSObject it = new GXDLMSData();
                OnShowDialog(it, arg);
            }
            else if (arg.Index == 4)
            {
                // Remove object from association view.
                OnShowDialog(null, arg);
            }
            //Add user to user list.
            else if (arg.Index == 5)
            {
                OnShowDialog(true, arg);
            }
            else if (arg.Index == 6)
            {
                // Remove user from user list.
                OnShowDialog(false, arg);
            }
            if ((arg.Action == ActionType.Write && arg.Index == 7) ||
                arg.Action == ActionType.Action && arg.Index == 2)
            {
                OnUpdatePassword(arg);
            }
            else if (arg.Action == ActionType.Write && arg.Index == 2)
            {
                //Skip write invoke.
                arg.Handled = true;
            }
        }

        public void PostAction(GXActionArgs arg)
        {
            //Edit object in association view.
            if (arg.Action == ActionType.Action && arg.Index == -1)
            {
                if (arg.Tag == null)
                {
                    arg.Action = ActionType.Read;
                    arg.Index = 2;
                }
            }
            else if (arg.Action == ActionType.Action && (arg.Index == 4 || arg.Index == 4))
            {
                arg.Action = ActionType.Read;
                arg.Index = 2;
            }
            else if (arg.Action == ActionType.Action && (arg.Index == 5 || arg.Index == 6))
            {
                arg.Action = ActionType.Read;
                arg.Index = 10;
            }
            else
            {
                arg.Action = ActionType.None;
            }
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

        private void SecretAsciiCb_CheckedChanged(object sender, EventArgs e)
        {
            try
            {
                if (SecretAsciiCb.Checked)
                {
                    byte[] data = GXDLMSTranslator.HexToBytes(SecretTB.Text);
                    if (!GXHelpers.IsAscii(data))
                    {
                        SecretAsciiCb.CheckedChanged -= SecretAsciiCb_CheckedChanged;
                        SecretAsciiCb.Checked = !SecretAsciiCb.Checked;
                        SecretAsciiCb.CheckedChanged += SecretAsciiCb_CheckedChanged;
                        throw new ArgumentOutOfRangeException(Properties.Resources.InvalidASCII);
                    }
                    SecretTB.Text = ASCIIEncoding.ASCII.GetString(data);
                }
                else
                {
                    SecretTB.Text = GXDLMSTranslator.ToHex(ASCIIEncoding.ASCII.GetBytes(SecretTB.Text));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message);
            }
        }

        private void SecuritySetupCb_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                GXDLMSAssociationLogicalName target = Target as GXDLMSAssociationLogicalName;
                if (SecuritySetupCb.SelectedItem is GXDLMSSecuritySetup)
                {
                    target.SecuritySetupReference = ((GXDLMSSecuritySetup)SecuritySetupCb.SelectedItem).LogicalName;
                }
                else
                {
                    target.SecuritySetupReference = null;
                }
                errorProvider1.SetError(SecuritySetupCb, Properties.Resources.ValueChangedTxt);
                Target.UpdateDirty(9, SecuritySetupCb.SelectedItem);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message);
            }
        }

        private void ApplicationContextIDCb_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                GXDLMSAssociationLogicalName target = Target as GXDLMSAssociationLogicalName;
                target.ApplicationContextName.ContextId = (ApplicationContextName)ApplicationContextIDCb.SelectedItem;
                errorProvider1.SetError(ApplicationContextIDCb, Properties.Resources.ValueChangedTxt);
                Target.UpdateDirty(4, ApplicationContextIDCb.SelectedItem);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message);
            }
        }

        private void MaxReceivePDUSizeTb_Leave(object sender, EventArgs e)
        {
            try
            {
                if (MaxReceivePDUSizeTb.Text != "")
                {
                    GXDLMSAssociationLogicalName target = Target as GXDLMSAssociationLogicalName;
                    UInt16 v;
                    if (UInt16.TryParse(MaxReceivePDUSizeTb.Text, out v) && target.XDLMSContextInfo.MaxReceivePduSize != v)
                    {
                        target.XDLMSContextInfo.MaxReceivePduSize = v;
                        Target.UpdateDirty(5, target.XDLMSContextInfo);
                        errorProvider1.SetError(MaxReceivePDUSizeTb, Properties.Resources.ValueChangedTxt);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void MaxSendPDUSizeTb_Leave(object sender, EventArgs e)
        {
            try
            {
                if (MaxSendPDUSizeTb.Text != "")
                {
                    GXDLMSAssociationLogicalName target = Target as GXDLMSAssociationLogicalName;
                    UInt16 v;
                    if (UInt16.TryParse(MaxSendPDUSizeTb.Text, out v) && target.XDLMSContextInfo.MaxSendPduSize != v)
                    {
                        target.XDLMSContextInfo.MaxSendPduSize = v;
                        Target.UpdateDirty(5, target.XDLMSContextInfo);
                        errorProvider1.SetError(MaxSendPDUSizeTb, Properties.Resources.ValueChangedTxt);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// User has change client SAP.
        /// </summary>
        private void ClientSAPTb_Leave(object sender, EventArgs e)
        {
            try
            {
                if (ClientSAPTb.Text != "")
                {
                    GXDLMSAssociationLogicalName target = Target as GXDLMSAssociationLogicalName;
                    byte v;
                    if (byte.TryParse(ClientSAPTb.Text, out v) && target.ClientSAP != v)
                    {
                        if (v > 0x7F)
                        {
                            throw new ArgumentOutOfRangeException("Client SAP must be between 0 to 127");
                        }
                        target.ClientSAP = v;
                        Target.UpdateDirty(3, target.ClientSAP);
                        errorProvider1.SetError(ClientSAPTb, Properties.Resources.ValueChangedTxt);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// User has change server SAP.
        /// </summary>
        private void ServerSAPTb_Leave(object sender, EventArgs e)
        {
            try
            {
                if (ServerSAPTb.Text != "")
                {
                    GXDLMSAssociationLogicalName target = Target as GXDLMSAssociationLogicalName;
                    UInt16 v;
                    if (UInt16.TryParse(ServerSAPTb.Text, out v) && target.ServerSAP != v)
                    {
                        if (v > 0x3FFF)
                        {
                            throw new ArgumentOutOfRangeException("Server SAP must be between 0 to 16383");
                        }
                        target.ServerSAP = v;
                        Target.UpdateDirty(3, target.ServerSAP);
                        errorProvider1.SetError(ServerSAPTb, Properties.Resources.ValueChangedTxt);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void AuthenticationMechanismIdCb_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!Freeze)
            {
                try
                {
                    GXDLMSAssociationLogicalName target = Target as GXDLMSAssociationLogicalName;
                    target.AuthenticationMechanismName.MechanismId = (Authentication)AuthenticationMechanismIdCb.SelectedItem;
                    errorProvider1.SetError(AuthenticationMechanismIdCb, Properties.Resources.ValueChangedTxt);
                    Target.UpdateDirty(6, AuthenticationMechanismIdCb.SelectedItem);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, ex.Message);
                }
            }
        }

        private void OnConformanceChange(object sender, EventArgs e)
        {
            if (!Freeze)
            {
                try
                {
                    Conformance c = Conformance.None;
                    if (GeneralProtectionCB.Checked)
                    {
                        c |= Conformance.GeneralProtection;
                    }
                    if (GeneralBlockTransferCB.Checked)
                    {
                        c |= Conformance.GeneralBlockTransfer;
                    }
                    if (Attribute0SetReferencingCB.Checked)
                    {
                        c |= Conformance.Attribute0SupportedWithSet;
                    }
                    if (PriorityManagementCB.Checked)
                    {
                        c |= Conformance.PriorityMgmtSupported;
                    }
                    if (Attribute0GetReferencingCB.Checked)
                    {
                        c |= Conformance.Attribute0SupportedWithGet;
                    }
                    if (GetBlockTransferCB.Checked)
                    {
                        c |= Conformance.BlockTransferWithGetOrRead;
                    }
                    if (SetBlockTransferCB.Checked)
                    {
                        c |= Conformance.BlockTransferWithSetOrWrite;
                    }
                    if (ActionBlockTransferCB.Checked)
                    {
                        c |= Conformance.BlockTransferWithAction;
                    }
                    if (MultipleReferencesCB.Checked)
                    {
                        c |= Conformance.MultipleReferences;
                    }
                    if (DataNotificationCB.Checked)
                    {
                        c |= Conformance.DataNotification;
                    }
                    if (AccessCB.Checked)
                    {
                        c |= Conformance.Access;
                    }
                    if (GetCB.Checked)
                    {
                        c |= Conformance.Get;
                    }
                    if (SetCB.Checked)
                    {
                        c |= Conformance.Set;
                    }
                    if (SelectiveAccessCB.Checked)
                    {
                        c |= Conformance.SelectiveAccess;
                    }
                    if (EventNotificationCB.Checked)
                    {
                        c |= Conformance.EventNotification;
                    }
                    if (ActionCB.Checked)
                    {
                        c |= Conformance.Action;
                    }
                    if (DeltaValueEncodingCb.Checked)
                    {
                        c |= Conformance.DeltaValueEncoding;
                    }
                    GXDLMSAssociationLogicalName target = Target as GXDLMSAssociationLogicalName;
                    target.XDLMSContextInfo.Conformance = c;
                    // The legacy group was replaced by the responsive conformance grid.
                    errorProvider1.SetError(sender as Control ?? GeneralProtectionCB, Properties.Resources.ValueChangedTxt);
                    Target.UpdateDirty(5, target.XDLMSContextInfo);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, ex.Message);
                }
            }
        }
    }

    /// <summary>
    /// Draws the Association Logical Name editor tabs in the same rounded style as
    /// the Director workspace, while keeping WinForms tab selection and keyboard
    /// behavior. DPI updates change native metrics in place to avoid recreating the
    /// tab HWND and cascading a second layout pass through every page.
    /// </summary>
    internal sealed class AssociationTabControl : TabControl
    {
        private int hovered = -1;
        private readonly bool compact;

        internal AssociationTabControl() : this(false) { }

        internal AssociationTabControl(bool compact)
        {
            this.compact = compact;
            SetStyle(ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.AllPaintingInWmPaint | ControlStyles.ResizeRedraw, !SystemInformation.HighContrast);
            DrawMode = SystemInformation.HighContrast ? TabDrawMode.Normal : TabDrawMode.OwnerDrawFixed;
            SizeMode = TabSizeMode.Fixed;
            Padding = new Point(2, 3);
            ItemSize = new Size(86, 24);
        }

        private void UpdateMetrics()
        {
            int width = compact
                ? Math.Min((int)Math.Round(220 * DeviceDpi / 96f),
                    Math.Max((int)Math.Round(80 * DeviceDpi / 96f),
                        TabPages.Cast<TabPage>().Select(page => TextRenderer.MeasureText(page.Text, Font).Width)
                            .DefaultIfEmpty(0).Max() + (int)Math.Round(24 * DeviceDpi / 96f)))
                : (int)Math.Round(86 * DeviceDpi / 96f);
            int height = (int)Math.Round(24 * DeviceDpi / 96f);
            int horizontalPadding = (int)Math.Round(2 * DeviceDpi / 96f);
            int verticalPadding = (int)Math.Round((compact ? 2 : 3) * DeviceDpi / 96f);
            if (!IsHandleCreated)
            {
                Padding = new Point(horizontalPadding, verticalPadding);
                ItemSize = new Size(width, height);
            }
            else
            {
                SendMessage(Handle, 0x1329, IntPtr.Zero, (IntPtr)((height << 16) | (width & 0xFFFF))); // TCM_SETITEMSIZE
                SendMessage(Handle, 0x132B, IntPtr.Zero, (IntPtr)((verticalPadding << 16) | (horizontalPadding & 0xFFFF))); // TCM_SETPADDING
                base.OnResize(EventArgs.Empty);
            }
            Invalidate();
        }

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr window, int message, IntPtr wParam, IntPtr lParam);

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            UpdateMetrics();
        }

        protected override void OnDpiChangedAfterParent(EventArgs e)
        {
            base.OnDpiChangedAfterParent(e);
            UpdateMetrics();
        }

        protected override void OnSystemColorsChanged(EventArgs e)
        {
            base.OnSystemColorsChanged(e);
            bool draw = !SystemInformation.HighContrast;
            SetStyle(ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.AllPaintingInWmPaint | ControlStyles.ResizeRedraw, draw);
            DrawMode = draw ? TabDrawMode.OwnerDrawFixed : TabDrawMode.Normal;
            UpdateStyles();
            UpdateMetrics();
        }

        protected override void OnSelectedIndexChanged(EventArgs e) { base.OnSelectedIndexChanged(e); Invalidate(); }
        protected override void OnGotFocus(EventArgs e) { base.OnGotFocus(e); Invalidate(); }
        protected override void OnLostFocus(EventArgs e) { base.OnLostFocus(e); Invalidate(); }
        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            int next = -1;
            for (int i = 0; i < TabCount; ++i)
                if (GetTabRect(i).Contains(e.Location)) { next = i; break; }
            if (next != hovered) { hovered = next; Invalidate(); }
        }
        protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); hovered = -1; Invalidate(); }

        private GraphicsPath CreateTabOutline(Rectangle rect, int join)
        {
            float left = rect.Left;
            float right = rect.Right - 1;
            float top = rect.Top;
            float radius = Math.Min((compact ? 6 : 4) * DeviceDpi / 96f, Math.Min((right - left) / 2, (join - top) / 2));
            var path = new GraphicsPath();
            path.AddLine(left, join, left, top + radius);
            path.AddArc(left, top, radius * 2, radius * 2, 180, 90);
            path.AddLine(left + radius, top, right - radius, top);
            path.AddArc(right - radius * 2, top, radius * 2, radius * 2, 270, 90);
            path.AddLine(right, top + radius, right, join);
            return path;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            if (SystemInformation.HighContrast) { base.OnPaint(e); return; }
            Color surface = Color.White;
            Color chrome = compact ? surface : Color.FromArgb(243, 243, 243);
            Color line = Color.FromArgb(214, 214, 214);
            Color accent = Color.FromArgb(0, 122, 204);
            Color ink = Color.FromArgb(27, 39, 59);
            e.Graphics.Clear(surface);
            if (TabCount == 0) return;
            Rectangle display = DisplayRectangle;
            int join = Math.Max(GetTabRect(0).Bottom, display.Top) - 1;
            using (var pen = new Pen(line)) e.Graphics.DrawLine(pen, 0, join, Width, join);
            for (int i = 0; i < TabCount; ++i)
            {
                Rectangle bounds = GetTabRect(i);
                bounds.Y = 2;
                bounds.Height = Math.Max(1, join - bounds.Top + 1);
                if (bounds.Width < 3 || bounds.Height < 3) continue;
                bool selected = i == SelectedIndex;
                using var outline = CreateTabOutline(bounds, join);
                var smoothing = e.Graphics.SmoothingMode;
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (var brush = new SolidBrush(i == hovered && !selected ? Color.FromArgb(226, 234, 243) : selected ? surface : chrome))
                    e.Graphics.FillPath(brush, outline);
                using (var pen = new Pen(selected ? accent : line)) e.Graphics.DrawPath(pen, outline);
                if (selected)
                {
                    using var surfacePen = new Pen(surface);
                    e.Graphics.DrawLine(surfacePen, bounds.Left + 1, join, bounds.Right - 2, join);
                }
                e.Graphics.SmoothingMode = smoothing;
                Rectangle text = Rectangle.Inflate(bounds, -(int)Math.Round(3 * DeviceDpi / 96f), -2);
                TextRenderer.DrawText(e.Graphics, TabPages[i].Text, Font, text, ink,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
                    TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);
                if (selected && Focused && ShowFocusCues)
                    ControlPaint.DrawFocusRectangle(e.Graphics, Rectangle.Inflate(bounds, -3, -3));
            }
        }
    }
}
