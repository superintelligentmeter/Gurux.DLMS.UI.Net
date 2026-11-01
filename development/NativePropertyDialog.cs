// superintelligentmeter contributors, 2026-10-30: shared widths for native property-dialog rows. GPL-2.0-only.
using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace Gurux.DLMS.UI
{
    // Explicit row definitions preserve the original controls, bindings and native
    // editor behavior. Document/list dialogs and compound input groups stay separate.
    internal static class NativePropertyDialog
    {
        private static int S(Control control, int value) =>
            (int)Math.Round(value * control.DeviceDpi / 96d);

        internal static void Apply(Form form, params (Control Caption, Control Editor)[] definitions)
        {
            var body = form.Controls.OfType<Panel>().Single(c => c.Name == "NativeDialogBody");
            form.SuspendLayout();
            body.SuspendLayout();
            try
            {
                body.Padding = new Padding(S(form, 12));
                body.TabIndex = 0;
                var table = Rows(body, definitions);
                table.TabIndex = 0;
                var footer = form.Controls.Find("NativeDialogActions", false).Single();
                footer.TabIndex = 1;
                int width = Math.Max(form.ClientSize.Width, S(form, 480));
                form.ClientSize = new Size(width, form.ClientSize.Height);
                table.Width = Math.Max(1, width - body.Padding.Horizontal);
                int contentHeight = table.GetPreferredSize(new Size(table.Width, 0)).Height + body.Padding.Vertical;
                int footerHeight = footer.GetPreferredSize(new Size(width, 0)).Height;
                form.ClientSize = new Size(width, Math.Max(form.ClientSize.Height, contentHeight + footerHeight));
            }
            finally
            {
                body.ResumeLayout(true);
                form.ResumeLayout(true);
            }
        }

        internal static void Group(GroupBox group, params (Control Caption, Control Editor)[] definitions)
        {
            group.SuspendLayout();
            try
            {
                // Keep the semantic group and its Visible/Enabled ownership, while
                // giving nested scalar rows the same left rail as their outer rows.
                group.Padding = new Padding(0, S(group, 10), S(group, 10), S(group, 10));
                Rows(group, definitions);
                group.AutoSize = true;
                group.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            }
            finally { group.ResumeLayout(true); }
        }

        internal static TableLayoutPanel Rows(Control parent, params (Control Caption, Control Editor)[] definitions)
        {
            var table = new TableLayoutPanel
            {
                Name = "NativePropertyRows", Dock = DockStyle.Top, AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink, ColumnCount = 2,
                Margin = Padding.Empty, Padding = Padding.Empty, BackColor = parent.BackColor
            };
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, S(parent, EditorLayoutPolicy.LabelColumnWidth)));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            foreach (var (caption, editor) in definitions)
            {
                int row = table.RowCount++;
                table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                if (caption != null)
                {
                    caption.AutoSize = true;
                    caption.Dock = DockStyle.None;
                    caption.Anchor = AnchorStyles.Left;
                    caption.Margin = new Padding(0, S(parent, 7), S(parent, 12), S(parent, 7));
                    caption.TabIndex = row * 2;
                    table.Controls.Add(caption, 0, row);
                    if (string.IsNullOrWhiteSpace(editor.AccessibleName))
                        editor.AccessibleName = caption.Text.TrimEnd(':');
                }
                editor.Dock = DockStyle.None;
                editor.Anchor = AnchorStyles.Left | AnchorStyles.Right;
                editor.Margin = new Padding(0, S(parent, 6), 0, S(parent, 6));
                editor.TabIndex = row * 2 + 1;
                table.Controls.Add(editor, caption == null ? 0 : 1, row);
                if (caption == null) table.SetColumnSpan(editor, 2);
            }
            parent.Controls.Add(table);
            EditorLayoutPolicy.ConfigurePropertyTable(table);
            foreach (var group in definitions.Select(pair => pair.Editor).OfType<GroupBox>())
                foreach (var nested in group.Controls.OfType<TableLayoutPanel>().Where(c => c.Name == "NativePropertyRows"))
                    EditorLayoutPolicy.ShareLabelColumn(nested, table);
            return table;
        }

        internal static Control Alongside(Control field, params Control[] actions)
        {
            var row = new TableLayoutPanel
            {
                AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = actions.Length + 1, RowCount = 1, Margin = Padding.Empty,
                Padding = Padding.Empty, BackColor = field.Parent.BackColor
            };
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            row.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            field.Dock = DockStyle.None;
            field.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            field.Margin = Padding.Empty;
            field.TabIndex = 0;
            row.Controls.Add(field, 0, 0);
            for (int i = 0; i < actions.Length; ++i)
            {
                var action = actions[i];
                row.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
                action.Dock = DockStyle.None;
                action.Anchor = AnchorStyles.Left;
                action.AutoSize = true;
                action.Margin = new Padding(S(field, 8), 0, 0, 0);
                action.TabIndex = i + 1;
                row.Controls.Add(action, i + 1, 0);
            }
            return row;
        }
    }
}
