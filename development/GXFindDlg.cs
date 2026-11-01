// Modified for the Intelmeter fork, 2026-10-16: .NET 10 migration, UI modernization and compatibility fixes. GPL-2.0-only; original notices retained.
//
// --------------------------------------------------------------------------
//  Gurux Ltd
//
//
//
//
// Version:         $Revision: 9442 $,
//                  $Date: 2017-05-23 15:21:03 +0300 (ti, 23 touko 2017) $
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
using System.Reflection;
using System.Windows.Forms;

namespace Gurux.DLMS.UI
{
    /// <summary>
    /// Search dialog for OBIS code.
    /// </summary>
    partial class GXFindDlg : Form
    {
        private GXFindParameters parameters;
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="p">Find settings.</param>
        public GXFindDlg(GXFindParameters p)
        {
            parameters = p;
            // Preserve the font metrics used by the fixed-coordinate designer.
            Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F);
            InitializeComponent();
            if ((p.Hide & SearchDialogHidden.Obis) != 0)
            {
                LogicalNameRb.Visible = false;
                AnyTextRb.Visible = false;
                AnyTextRb.Checked = true;
            }
            if ((p.Hide & SearchDialogHidden.Text) != 0)
            {
                AnyTextRb.Visible = false;
                LogicalNameRb.Visible = false;
                LogicalNameRb.Checked = true;
            }
            if (!string.IsNullOrEmpty(parameters.Text))
            {
                FindTb.Text = p.Text;
                AnyTextRb.Checked = true;
            }
            else
            {
                FindTb.Text = p.ObisCode;
            }
            ApplyLayout(p.Hide == SearchDialogHidden.None);
        }

        private void ApplyLayout(bool showModes)
        {
            SuspendLayout();
            var table = ModernPresentation.CompactSurface(this);
            ModernPresentation.CompactRow(table, label1.Text, FindTb);
            label1.Visible = false;
            // Keep both original radio buttons in one group, including hidden modes.
            var modes = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.TopDown,
                WrapContents = false, TabIndex = 1 };
            modes.Controls.Add(LogicalNameRb);
            modes.Controls.Add(AnyTextRb);
            LogicalNameRb.TabIndex = 0;
            AnyTextRb.TabIndex = 1;
            if (showModes)
            {
                ModernPresentation.CompactRow(table, "Search by:", modes, height: 60);
                // Line the caption up with the first option instead of centring it between both.
                if (table.GetControlFromPosition(0, table.RowCount - 1) is Label modeCaption)
                {
                    modeCaption.TextAlign = System.Drawing.ContentAlignment.TopLeft;
                    modeCaption.Padding = new Padding(0, (int)(6 * DeviceDpi / 96f), 0, 0);
                }
            }
            else
            {
                modes.Visible = false;
                Controls.Add(modes);
            }
            table.TabIndex = 0;
            Controls.Add(table);
            var footer = ModernPresentation.DialogFooter(this, FindBtn, CancelBtn);
            footer.TabIndex = 1;
            FindBtn.TabIndex = 0;
            CancelBtn.TabIndex = 1;
            ClientSize = new System.Drawing.Size((int)(600 * DeviceDpi / 96f),
                (int)((showModes ? 172 : 124) * DeviceDpi / 96f));
            ModernPresentation.Apply(this);
            // Size to the content so nothing scrolls and no empty band is left above the buttons.
            int contentWidth = ClientSize.Width - Padding.Horizontal;
            int needed = Padding.Vertical + table.GetPreferredSize(new System.Drawing.Size(contentWidth, 0)).Height +
                footer.GetPreferredSize(System.Drawing.Size.Empty).Height + (int)(8 * DeviceDpi / 96f);
            ClientSize = new System.Drawing.Size(ClientSize.Width, needed);
            ActiveControl = FindTb;
            ResumeLayout(true);
        }

        #region Assembly Attribute Accessors

        public string AssemblyTitle
        {
            get
            {
                object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyTitleAttribute), false);
                if (attributes.Length > 0)
                {
                    AssemblyTitleAttribute titleAttribute = (AssemblyTitleAttribute)attributes[0];
                    if (titleAttribute.Title != "")
                    {
                        return titleAttribute.Title;
                    }
                }
                return System.IO.Path.GetFileNameWithoutExtension(Assembly.GetExecutingAssembly().Location);
            }
        }

        public string AssemblyVersion
        {
            get
            {
                return Assembly.GetExecutingAssembly().GetName().Version.ToString();
            }
        }

        public string AssemblyDescription
        {
            get
            {
                object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyDescriptionAttribute), false);
                if (attributes.Length == 0)
                {
                    return "";
                }
                return ((AssemblyDescriptionAttribute)attributes[0]).Description;
            }
        }

        public string AssemblyProduct
        {
            get
            {
                object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyProductAttribute), false);
                if (attributes.Length == 0)
                {
                    return "";
                }
                return ((AssemblyProductAttribute)attributes[0]).Product;
            }
        }

        public string AssemblyCopyright
        {
            get
            {
                object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyCopyrightAttribute), false);
                if (attributes.Length == 0)
                {
                    return "";
                }
                return ((AssemblyCopyrightAttribute)attributes[0]).Copyright;
            }
        }

        public string AssemblyCompany
        {
            get
            {
                object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyCompanyAttribute), false);
                if (attributes.Length == 0)
                {
                    return "";
                }
                return ((AssemblyCompanyAttribute)attributes[0]).Company;
            }
        }
        #endregion

        private void FindBtn_Click(object sender, EventArgs e)
        {
            try
            {
                if (LogicalNameRb.Checked)
                {
                    //If OBIS code is given as hex.
                    if (FindTb.Text.IndexOf('.') == -1)
                    {
                        byte[] tmp = GXDLMSTranslator.HexToBytes(FindTb.Text);
                        if (tmp.Length != 6)
                        {
                            throw new Exception("Invalid OBIS code.");
                        }
                    }
                    else if (FindTb.Text.Split(new char[] { '.' }).Length != 6)
                    {
                        throw new Exception("Invalid OBIS code.");
                    }
                    parameters.ObisCode = FindTb.Text.Trim();
                    parameters.Text = null;
                }
                else
                {
                    string text = FindTb.Text.Trim();
                    if (string.IsNullOrEmpty(text))
                    {
                        throw new Exception("Invalid text to search for.");
                    }
                    parameters.ObisCode = null;
                    parameters.Text = text;
                }
            }
            catch (Exception Ex)
            {
                MessageBox.Show(this, Ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                DialogResult = DialogResult.None;
            }
        }

        private void LogicalNameRb_CheckedChanged(object sender, EventArgs e)
        {
            this.Text = "Find by Logical Name";
        }

        private void AnyTextRb_CheckedChanged(object sender, EventArgs e)
        {
            this.Text = "Find";
        }
    }
}
