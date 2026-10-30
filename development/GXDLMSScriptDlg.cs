// Modified by Superintelligent Meter contributors, 2026-10-29: measured script actions and read-only/validation correctness. GPL-2.0-only; existing notices retained.
// Modified for the Intelmeter fork, 2026-10-16: .NET 10 migration, UI modernization and compatibility fixes. GPL-2.0-only; original notices retained.
using Gurux.DLMS.Enums;
using Gurux.DLMS.Objects;
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

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Gurux.DLMS.UI
{
    public partial class GXDLMSScriptDlg : Form
    {
        GXDLMSScript target;
        readonly GXDLMSScript original;
        readonly bool readOnly;
        /// <summary>
        /// Available targets.
        /// </summary>
        GXDLMSObjectCollection targets;

        private static object CopyParameter(object value)
        {
            if (value is byte[] bytes) return bytes.Clone();
            if (value is object[] values) return values.Select(CopyParameter).ToArray();
            if (value is Array array) return array.Clone();
            return value;
        }

        private void ShowAction(GXDLMSScriptAction a, ListViewItem li)
        {
            if (li == null)
            {
                li = Actions.Items.Add(a.Type.ToString());
            }
            else
            {
                li.SubItems.Clear();
                li.Text = a.Type.ToString();
            }
            if (a.Target == null)
            {
                li.SubItems.Add(ObjectType.None.ToString());
                li.SubItems.Add("");
                li.SubItems.Add("");
                li.SubItems.Add("");
            }
            else
            {
                li.SubItems.Add(a.Target.ObjectType.ToString());
                li.SubItems.Add(a.Target.LogicalName);
                li.SubItems.Add(a.Index.ToString());
                if (a.Parameter is byte[])
                {
                    li.SubItems.Add(GXDLMSTranslator.ToHex(a.Parameter as byte[]));
                }
                else
                {
                    li.SubItems.Add(Convert.ToString(a.Parameter));
                }
            }
            li.Tag = a;
        }
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="value"></param>
        public GXDLMSScriptDlg(GXDLMSScript value, GXDLMSObjectCollection objects, bool readOnly)
        {
            // Preserve the font metrics used by the fixed-coordinate designer.
            Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F);
            InitializeComponent();
            NativeDialogFooter.ScriptEditor(this, menuStrip1, IdLbl, IdTb, ActionsLbl, Actions, AddBtn, EditBtn, RemoveBtn, OkBtn, CancelBtn);
            this.readOnly = readOnly;
            IdTb.ReadOnly = readOnly;
            AddBtn.Enabled = EditBtn.Enabled = RemoveBtn.Enabled = !readOnly;
            editToolStripMenuItem.Enabled = editToolStripMenuItem1.Enabled = !readOnly;
            original = value;
            target = new GXDLMSScript { Id = value.Id };
            foreach (var action in value.Actions)
            {
                target.Actions.Add(new GXDLMSScriptAction
                {
                    Type = action.Type,
                    Target = action.Target,
                    Index = action.Index,
                    ParameterDataType = action.ParameterDataType,
                    Parameter = CopyParameter(action.Parameter)
                });
            }
            targets = objects;
            removeToolStripMenuItem.Enabled = addToolStripMenuItem.Enabled =
                deleteToolStripMenuItem.Enabled = addToolStripMenuItem2.Enabled = !readOnly;
            if (target.Id != 0)
            {
                IdTb.Text = target.Id.ToString();
            }
            foreach (GXDLMSScriptAction a in target.Actions)
            {
                ShowAction(a, null);
            }
        }

        /// <summary>
        /// Add new script.
        /// </summary>
        private void AddScript(object sender, EventArgs e)
        {
            if (readOnly) return;
            try
            {
                GXDLMSScriptAction a = new GXDLMSScriptAction();
                GXDLMSScriptActionDlg dlg = new GXDLMSScriptActionDlg(a, targets);
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    ShowAction(a, null);
                    target.Actions.Add(a);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        /// <summary>
        /// Edit script.
        /// </summary>
        private void EditScript(object sender, EventArgs e)
        {
            if (readOnly) return;
            try
            {
                if (Actions.SelectedItems.Count == 1)
                {
                    GXDLMSScriptAction a = Actions.SelectedItems[0].Tag as GXDLMSScriptAction;
                    GXDLMSScriptActionDlg dlg = new GXDLMSScriptActionDlg(a, targets);
                    if (dlg.ShowDialog(this) == DialogResult.OK)
                    {
                        ShowAction(a, Actions.SelectedItems[0]);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Remove selected script.
        /// </summary>
        private void RemoveScript(object sender, EventArgs e)
        {
            if (readOnly) return;
            while (Actions.SelectedItems.Count != 0)
            {
                ListViewItem li = Actions.SelectedItems[0];
                target.Actions.Remove((GXDLMSScriptAction) li.Tag);
                Actions.Items.Remove(li);
            }
        }

        private void OkBtn_Click(object sender, EventArgs e)
        {
            try
            {
                if (!readOnly)
                {
                    int identifier = int.Parse(IdTb.Text);
                    original.Id = identifier;
                    original.Actions.Clear();
                    original.Actions.AddRange(target.Actions);
                }
            }
            catch (Exception ex)
            {
                DialogResult = DialogResult.None;
                MessageBox.Show(this, ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
