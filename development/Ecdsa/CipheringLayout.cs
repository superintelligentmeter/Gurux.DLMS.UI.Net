// superintelligentmeter, 2026-10-30: align nested key groups with the common scalar rail. GPL-2.0-only; original notices retained.
// superintelligentmeter, 2026-10-29: responsive native layout for the embedded ciphering editors. GPL-2.0-only; original Gurux notices remain in the owning forms.
using System;
using System.Drawing;
using System.Windows.Forms;

namespace Gurux.DLMS.UI.Ecdsa
{
    // Reuses each form's existing fields and event handlers. Native preferred sizes
    // and table columns avoid preserving stale anchor offsets after reparenting.
    internal static class CipheringLayout
    {
        private static int Scale(Control control, int value) =>
            (int)Math.Round(value * control.DeviceDpi / 96.0);

        internal static TableLayoutPanel Surface(Control parent)
        {
            if (parent is Panel panel && !(parent is TabPage))
            {
                panel.AutoSize = true;
                panel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            }
            if (parent is GroupBox group)
            {
                group.AutoSize = true;
                group.AutoSizeMode = AutoSizeMode.GrowAndShrink;
                // The parent and key groups share one scalar rail. Preserve the
                // group instances for security-mode visibility and enabled states.
                group.Padding = new Padding(0, Scale(group, 18), Scale(group, 8), Scale(group, 8));
            }
            var table = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 3,
                Margin = Padding.Empty,
                Padding = Padding.Empty,
                TabStop = false
            };
            table.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            // Fixed action column: every input ends at the same right edge, with or without an ASCII box or button.
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, Scale(parent, 112)));
            EditorLayoutPolicy.ConfigurePropertyTable(table);
            parent.Controls.Add(table);
            return table;
        }

        internal static void Row(TableLayoutPanel table, Control label, Control field, Control action = null)
        {
            int row = table.RowCount++;
            int gap = Scale(table, 4);
            table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            label.AutoSize = true;
            label.Dock = DockStyle.None;
            label.Anchor = AnchorStyles.Left;
            label.Margin = new Padding(0, gap, Scale(table, 12), gap);
            label.TabIndex = row * 3;
            table.Controls.Add(label, 0, row);
            field.Dock = DockStyle.Fill;
            field.Margin = new Padding(0, gap, 0, gap);
            field.TabIndex = row * 3 + 1;
            if (string.IsNullOrWhiteSpace(field.AccessibleName)) field.AccessibleName = label.Text.TrimEnd(':');
            table.Controls.Add(field, 1, row);
            if (action != null)
            {
                action.AutoSize = true;
                action.Dock = DockStyle.None;
                action.Anchor = AnchorStyles.Left;
                action.Margin = new Padding(Scale(table, 8), gap, 0, gap);
                action.TabIndex = row * 3 + 2;
                table.Controls.Add(action, 2, row);
            }
        }

        /// <summary>
        /// Keeps the action column of nested key groups (which sit inside a group box) ending where the
        /// owner's does, so combo boxes and text boxes share one right edge.
        /// </summary>
        internal static void AlignActionColumn(TableLayoutPanel owner, params TableLayoutPanel[] nested)
        {
            bool adjusting = false;
            void Adjust()
            {
                if (adjusting || owner.IsDisposed) return;
                adjusting = true;
                try
                {
                    int width = Scale(owner, 112);
                    if ((int)owner.ColumnStyles[2].Width != width) owner.ColumnStyles[2].Width = width;
                    foreach (var table in nested)
                    {
                        if (table.IsDisposed || table.Width <= 0 || owner.Width <= 0) continue;
                        int inset = Math.Max(0, owner.Width - table.Width);
                        int own = Math.Max(Scale(table, 84), width - inset);
                        if ((int)table.ColumnStyles[2].Width != own) table.ColumnStyles[2].Width = own;
                    }
                }
                finally { adjusting = false; }
            }
            owner.SizeChanged += (_, _) => Adjust();
            owner.DpiChangedAfterParent += (_, _) => Adjust();
            foreach (var table in nested) table.SizeChanged += (_, _) => Adjust();
            Adjust();
        }

        internal static void Span(TableLayoutPanel table, Control control)
        {
            int row = table.RowCount++;
            table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            control.Dock = DockStyle.Top;
            control.Margin = new Padding(0, Scale(table, 4), 0, Scale(table, 4));
            control.TabIndex = row * 3;
            table.Controls.Add(control, 0, row);
            table.SetColumnSpan(control, 3);
        }

        internal static FlowLayoutPanel Actions(params Button[] buttons)
        {
            var actions = new FlowLayoutPanel
            {
                AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink,
                WrapContents = true, Margin = Padding.Empty, Padding = Padding.Empty
            };
            foreach (var button in buttons)
            {
                button.AutoSize = true;
                button.AutoSizeMode = AutoSizeMode.GrowAndShrink;
                button.Dock = DockStyle.None;
                button.Anchor = AnchorStyles.Top | AnchorStyles.Left;
                button.Margin = new Padding(0, 0, Scale(button, 8), 0);
                button.Padding = new Padding(Scale(button, 8), Scale(button, 3), Scale(button, 8), Scale(button, 3));
                actions.Controls.Add(button);
            }
            return actions;
        }

        internal static void HostSuites(TabControl tabs, TabPage page, Panel host, Panel suite0, Panel suite1,
            TabPage template0, TabPage template1)
        {
            page.AutoScroll = true;
            page.Padding = new Padding(Scale(page, 10));
            host.AutoSize = true;
            host.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            host.Padding = Padding.Empty;
            foreach (var panel in new[] { suite0, suite1 })
            {
                panel.Dock = DockStyle.Top;
                panel.Margin = Padding.Empty;
                panel.Padding = Padding.Empty;
                host.Controls.Add(panel);
            }
            // These tabs only stored designer controls; the host application exposes
            // the Ciphering page via GetCiphetingTab. Keep the standalone form identical.
            tabs.TabPages.Remove(template0);
            tabs.TabPages.Remove(template1);
            template0.Dispose();
            template1.Dispose();
            ShowSuite(host, suite0, suite1, true);
        }

        internal static void ShowSuite(Panel host, Panel suite0, Panel suite1, bool useSuite0)
        {
            host.SuspendLayout();
            suite0.Visible = useSuite0;
            suite1.Visible = !useSuite0;
            host.ResumeLayout(true);
            host.Parent?.PerformLayout();
        }

        internal static void TrackScrollExtent(TabPage page, Control content)
        {
            // Docked AutoSize rows can grow after a scrollbar narrows their labels.
            // Advertise the measured content extent, never a previously scaled height.
            bool fitting = false;
            void Fit()
            {
                if (fitting || page.IsDisposed || content.IsDisposed) return;
                fitting = true;
                try
                {
                    var minimum = new Size(0, content.Height + page.Padding.Vertical);
                    if (page.AutoScrollMinSize != minimum) page.AutoScrollMinSize = minimum;
                }
                finally { fitting = false; }
            }
            content.SizeChanged += (_, _) => Fit();
            content.Layout += (_, _) => Fit();
            page.Layout += (_, _) => Fit();
            page.DpiChangedAfterParent += (_, _) => Fit();
            Fit();
        }
    }
}
