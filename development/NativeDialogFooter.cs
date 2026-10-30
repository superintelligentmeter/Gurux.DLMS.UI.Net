// Superintelligent Meter contributors, 2026-10-29. Native dialog action layout. GPL-2.0-only.
using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace Gurux.DLMS.UI
{
    // Preserve each editor and handler while reserving a separate, measured action row.
    internal static class NativeDialogFooter
    {
        private static int S(Control control, int value) => (int)Math.Round(value * control.DeviceDpi / 96d);

        internal static void Apply(Form form, Button accept, Button cancel)
        {
            form.SuspendLayout();
            try
            {
                var children = form.Controls.Cast<Control>().Where(c => c != accept && c != cancel).ToArray();
                int contentHeight = Math.Max(1, Math.Min(accept.Top, cancel.Top) - S(form, 6));
                int contentBottom = children.Length == 0 ? 0 : children.Max(c => c.Bottom);
                contentHeight = Math.Max(contentHeight, contentBottom + S(form, 6));
                var body = new Panel { Name = "NativeDialogBody", Dock = DockStyle.Fill, AutoScroll = true, Size = new Size(form.ClientSize.Width, contentHeight) };
                foreach (Control child in children) body.Controls.Add(child);
                var footer = Actions(form, accept, cancel);
                form.Controls.Clear();
                form.Controls.Add(body);
                form.Controls.Add(footer);
                form.ClientSize = new Size(form.ClientSize.Width, contentHeight + footer.GetPreferredSize(new Size(form.ClientSize.Width, 0)).Height);
                form.MinimumSize = new Size(Math.Max(form.MinimumSize.Width, S(form, 300)), Math.Max(form.MinimumSize.Height, S(form, 160)));
                // Reparenting an IButtonControl clears its form's keyboard mapping.
                form.AcceptButton = accept;
                form.CancelButton = cancel;
            }
            finally { form.ResumeLayout(true); }
        }

        internal static FlowLayoutPanel Actions(Form form, Button accept, Button cancel)
        {
            var footer = new FlowLayoutPanel
            {
                Name = "NativeDialogActions", Dock = DockStyle.Bottom, AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink, FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false, Padding = new Padding(S(form, 12), S(form, 8), S(form, 12), S(form, 10))
            };
            foreach (Button button in new[] { cancel, accept })
            {
                button.Dock = DockStyle.None; button.Anchor = AnchorStyles.None;
                button.AutoSize = true; button.AutoSizeMode = AutoSizeMode.GrowAndShrink;
                button.MinimumSize = new Size(S(form, 80), S(form, 28));
                button.Margin = new Padding(S(form, 8), 0, 0, 0);
                footer.Controls.Add(button);
            }
            return footer;
        }

        internal static void FunctionEditor(Form form, Label nameLabel, Control name, Label enabledLabel, Control enabled,
            Label targetLabel, Control target, Button add, ListView objects, Button remove, Button accept, Button cancel)
        {
            form.SuspendLayout();
            try
            {
                var header = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 3, RowCount = 3, Padding = new Padding(S(form, 12)) };
                header.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
                header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
                header.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
                int row = 0;
                foreach (var pair in new[] { (nameLabel, name), (enabledLabel, enabled), (targetLabel, target) })
                {
                    header.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                    pair.Item1.AutoSize = true; pair.Item1.Anchor = AnchorStyles.Left; pair.Item1.Dock = DockStyle.None;
                    pair.Item1.Margin = new Padding(0, S(form, 6), S(form, 12), S(form, 6));
                    pair.Item2.Dock = DockStyle.None; pair.Item2.Anchor = AnchorStyles.Left | AnchorStyles.Right;
                    pair.Item2.Margin = new Padding(0, S(form, 5), S(form, 8), S(form, 5));
                    header.Controls.Add(pair.Item1, 0, row); header.Controls.Add(pair.Item2, 1, row);
                    if (row != 2) header.SetColumnSpan(pair.Item2, 2);
                    row++;
                }
                add.AutoSize = true; add.AutoSizeMode = AutoSizeMode.GrowAndShrink;
                add.MinimumSize = new Size(S(form, 80), S(form, 28)); add.Anchor = AnchorStyles.None; add.Dock = DockStyle.None;
                header.Controls.Add(add, 2, 2);
                objects.SizeChanged += (_, _) =>
                {
                    if (objects.Columns.Count == 3)
                    {
                        objects.Columns[0].Width = S(objects, 130);
                        objects.Columns[1].Width = S(objects, 140);
                        objects.Columns[2].Width = Math.Max(S(objects, 90), objects.ClientSize.Width - objects.Columns[0].Width - objects.Columns[1].Width - S(objects, 6));
                    }
                };
                objects.Dock = DockStyle.Fill; objects.Margin = new Padding(S(form, 12), 0, S(form, 12), 0);
                var actions = Actions(form, accept, cancel);
                actions.Dock = DockStyle.Fill;
                var bottom = new TableLayoutPanel { Dock = DockStyle.Bottom, AutoSize = true, ColumnCount = 2 };
                bottom.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); bottom.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
                remove.AutoSize = true; remove.AutoSizeMode = AutoSizeMode.GrowAndShrink; remove.Anchor = AnchorStyles.Left;
                remove.MinimumSize = new Size(S(form, 80), S(form, 28)); remove.Margin = new Padding(S(form, 12), S(form, 8), 0, S(form, 10));
                bottom.Controls.Add(remove, 0, 0); bottom.Controls.Add(actions, 1, 0);
                var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3 };
                root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
                root.RowStyles.Add(new RowStyle(SizeType.AutoSize)); root.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                root.Controls.Add(header, 0, 0); root.Controls.Add(objects, 0, 1); root.Controls.Add(bottom, 0, 2);
                form.Controls.Clear(); form.Controls.Add(root);
                header.TabIndex = 0; name.TabIndex = 0; enabled.TabIndex = 1; target.TabIndex = 2; add.TabIndex = 3;
                objects.TabIndex = 1; bottom.TabIndex = 2; remove.TabIndex = 0; actions.TabIndex = 1;
                accept.TabIndex = 0; cancel.TabIndex = 1;
                form.MinimumSize = new Size(S(form, 420), S(form, 300));
                form.ClientSize = new Size(Math.Max(form.ClientSize.Width, S(form, 460)), Math.Max(form.ClientSize.Height, S(form, 360)));
                form.AcceptButton = accept; form.CancelButton = cancel;
            }
            finally { form.ResumeLayout(true); }
        }
        internal static void ScriptEditor(Form form, MenuStrip menu, Label idLabel, TextBox id, Label listLabel,
            ListView list, Button add, Button edit, Button remove, Button accept, Button cancel)
        {
            form.SuspendLayout();
            try
            {
                var identity = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, Padding = new Padding(S(form, 12), S(form, 6), S(form, 12), 0) };
                idLabel.AutoSize = true; idLabel.Anchor = AnchorStyles.Left; idLabel.Margin = new Padding(0, S(form, 5), S(form, 12), S(form, 5));
                id.Anchor = AnchorStyles.None; id.Margin = new Padding(0, S(form, 3), 0, S(form, 3)); id.Width = S(form, 100);
                identity.Controls.Add(idLabel); identity.Controls.Add(id);
                listLabel.AutoSize = true; listLabel.Margin = new Padding(S(form, 12), S(form, 6), 0, S(form, 6));
                // Keep headers and the full logical name readable at each monitor DPI.
                list.SizeChanged += (_, _) =>
                {
                    if (list.Columns.Count == 5)
                    {
                        int[] widths = { 52, 115, 125, 50 };
                        for (int column = 0; column != widths.Length; ++column)
                            list.Columns[column].Width = S(list, widths[column]);
                        list.Columns[4].Width = Math.Max(S(list, 80), list.ClientSize.Width -
                            list.Columns.Cast<ColumnHeader>().Take(4).Sum(column => column.Width) - S(list, 6));
                    }
                };
                list.Dock = DockStyle.Fill; list.Margin = new Padding(S(form, 12), 0, S(form, 12), 0);
                var tools = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, FlowDirection = FlowDirection.RightToLeft, WrapContents = false, Padding = new Padding(S(form, 12), S(form, 8), S(form, 12), 0) };
                foreach (Button button in new[] { remove, edit, add })
                {
                    button.AutoSize = true; button.AutoSizeMode = AutoSizeMode.GrowAndShrink;
                    button.MinimumSize = new Size(S(form, 80), S(form, 28)); button.Anchor = AnchorStyles.None; button.Dock = DockStyle.None;
                    button.Margin = new Padding(S(form, 8), 0, 0, 0); tools.Controls.Add(button);
                }
                var actions = Actions(form, accept, cancel); actions.Dock = DockStyle.Fill;
                var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 6 };
                root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
                for (int row = 0; row != 6; ++row) root.RowStyles.Add(new RowStyle(row == 3 ? SizeType.Percent : SizeType.AutoSize, row == 3 ? 100 : 0));
                menu.Dock = DockStyle.Fill; menu.Margin = Padding.Empty;
                root.Controls.Add(menu, 0, 0); root.Controls.Add(identity, 0, 1); root.Controls.Add(listLabel, 0, 2);
                root.Controls.Add(list, 0, 3); root.Controls.Add(tools, 0, 4); root.Controls.Add(actions, 0, 5);
                form.Controls.Clear(); form.Controls.Add(root);
                menu.TabIndex = 0; identity.TabIndex = 1; id.TabIndex = 0; list.TabIndex = 2; tools.TabIndex = 3; actions.TabIndex = 4;
                add.TabIndex = 0; edit.TabIndex = 1; remove.TabIndex = 2; accept.TabIndex = 0; cancel.TabIndex = 1;
                form.MinimumSize = new Size(S(form, 400), S(form, 320));
                form.ClientSize = new Size(Math.Max(form.ClientSize.Width, S(form, 480)), Math.Max(form.ClientSize.Height, S(form, 360)));
                form.AcceptButton = accept; form.CancelButton = cancel;
            }
            finally { form.ResumeLayout(true); }
        }
    }
}
