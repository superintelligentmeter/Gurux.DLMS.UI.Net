// Superintelligent Meter contributors, 2026-10-30. Aligned Short Name properties. GPL-2.0-only.
// SPDX-License-Identifier: GPL-2.0-only
using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace Gurux.DLMS.UI
{
    public static partial class ModernPresentation
    {
        private static void AlignShortNameGeneral(Form form)
        {
            Control Find(string name) => Descendants(form).Single(control => control.Name == name);
            var page = (TabPage)Find("tabPage2");
            var logicalName = (GXValueField)Find("LogicalNameTB");
            var secret = (TextBox)Find("SecretTB");
            var secretFrame = secret.Parent;
            var security = (ComboBox)Find("SecuritySetupCb");
            var securityHost = security.Parent;
            var ascii = (CheckBox)Find("SecretAsciiCb");
            var update = (Button)Find("UpdateTimeBtn");
            var secretLabel = (Label)Find("SecretLbl");
            var securityLabel = (Label)Find("SecuritySetupReferenceLbl");
            var oldContents = page.Controls.Cast<Control>().ToArray();
            var header = (TableLayoutPanel)logicalName.Parent;
            int headerRow = header.GetRow(logicalName), headerColumn = header.GetColumn(logicalName);
            int headerSpan = header.GetColumnSpan(logicalName);
            var logicalCell = new Panel { Name = "ShortNameLogicalNameCell", Dock = DockStyle.Fill,
                Margin = logicalName.Margin, TabIndex = logicalName.TabIndex, TabStop = false };
            header.Controls.Remove(logicalName);
            header.Controls.Add(logicalCell, headerColumn, headerRow);
            header.SetColumnSpan(logicalCell, headerSpan);
            logicalCell.Controls.Add(logicalName);
            logicalName.Dock = DockStyle.Fill; logicalName.Margin = Padding.Empty; logicalName.TabIndex = 0;

            page.SuspendLayout();
            var rows = new TableLayoutPanel { Name = "ShortNameGeneralRows", ColumnCount = 2, RowCount = 2,
                BackColor = page.BackColor, Margin = Padding.Empty, Padding = Padding.Empty, TabIndex = 0 };
            rows.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, Scale(page, 144)));
            rows.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            rows.RowStyles.Add(new RowStyle(SizeType.Absolute, Scale(page, 40)));
            rows.RowStyles.Add(new RowStyle(SizeType.Absolute, Scale(page, 40)));
            try
            {
                foreach (var control in new Control[] { secretFrame, securityHost, ascii, update, secretLabel, securityLabel })
                    control.Parent.Controls.Remove(control);
                // Dispose only emptied presentation containers. Keep the native combo
                // host because its layout handlers still belong to this ComboBox.
                foreach (var control in oldContents) control.Dispose();
                page.Padding = Padding.Empty; page.AutoScroll = true; page.AutoScrollMinSize = Size.Empty;
                page.Controls.Add(rows);
                ButtonLayoutPolicy.ApplyToButton(update);
                var actions = new SecretActions(ascii, update) { Name = "ShortNameSecretActions", TabStop = false };
                secretFrame.MaximumSize = Size.Empty;
                var secretRow = new CompactActionRow(rows, 0, secretFrame, actions, 40) { TabIndex = 1 };
                foreach (var label in new[] { secretLabel, securityLabel })
                {
                    label.AutoSize = false; label.Dock = DockStyle.Fill; label.ForeColor = Muted;
                    label.Margin = new Padding(0, 0, Scale(page, 12), 0);
                }
                secretLabel.TextAlign = ContentAlignment.TopLeft;
                securityLabel.TextAlign = ContentAlignment.MiddleLeft;
                securityHost.Dock = DockStyle.Fill; securityHost.TabIndex = 3;
                securityHost.MaximumSize = Size.Empty;
                rows.Controls.Add(secretLabel, 0, 0); rows.Controls.Add(secretRow, 1, 0);
                rows.Controls.Add(securityLabel, 0, 1); rows.Controls.Add(securityHost, 1, 1);
                secretLabel.TabIndex = 0; securityLabel.TabIndex = 2;

                bool fitting = false, pending = false;
                void Fit()
                {
                    if (fitting || rows.IsDisposed || !page.Visible || !rows.IsHandleCreated || !logicalCell.IsHandleCreated ||
                        (form.TopLevelControl as Form)?.WindowState == FormWindowState.Minimized) return;
                    fitting = true;
                    try
                    {
                        int actionWidth = actions.GetPreferredSize(Size.Empty).Width;
                        var actionMaximum = new Size(actionWidth, 0);
                        if (actions.MaximumSize != actionMaximum) actions.MaximumSize = actionMaximum;
                        int InViewX(Control control)
                        {
                            int x = 0;
                            for (var current = control; current != null && current != form; current = current.Parent) x += current.Left;
                            return x;
                        }
                        int left = InViewX(logicalCell) - InViewX(rows);
                        if (left < 0) return;
                        int available = Math.Max(1, page.ClientSize.Width - left - Scale(page, 4));
                        int width = Math.Max(1, Math.Min(logicalCell.ClientSize.Width, available));
                        // One visible input rail, including the native tab-frame inset.
                        var maximum = new Size(width, logicalName.MaximumSize.Height);
                        if (logicalName.MaximumSize != maximum) logicalName.MaximumSize = maximum;
                        int totalWidth = Math.Max(1, left + width);
                        if (rows.Width != totalWidth) rows.Width = totalWidth;
                        if (rows.ColumnStyles[0].Width != left) rows.ColumnStyles[0].Width = left;
                        secretLabel.Padding = new Padding(0, Scale(rows, 12), 0, 0);
                        var margin = new Padding(0, Scale(rows, 4), 0, Scale(rows, 4));
                        if (securityHost.Margin != margin) securityHost.Margin = margin;
                        int scalarRow = Scale(rows, 40);
                        if (rows.RowStyles[1].Height != scalarRow) rows.RowStyles[1].Height = scalarRow;
                        rows.PerformLayout(); secretRow.PerformLayout();
                        int height = (int)Math.Ceiling(rows.RowStyles.Cast<RowStyle>().Sum(style => style.Height));
                        if (rows.Height != height) rows.Height = height;
                    }
                    finally { fitting = false; }
                }
                void Queue()
                {
                    if (pending || fitting || rows.IsDisposed || !form.IsHandleCreated) return;
                    pending = true;
                    form.BeginInvoke((Action)(() => { pending = false; if (!rows.IsDisposed) Fit(); }));
                }
                rows.HandleCreated += (_, _) => Queue(); rows.Layout += (_, _) => Queue();
                page.SizeChanged += (_, _) => Queue(); page.Layout += (_, _) => Queue();
                page.VisibleChanged += (_, _) => Queue();
                page.DpiChangedAfterParent += (_, _) => Queue();
                header.Layout += (_, _) => Queue(); logicalCell.SizeChanged += (_, _) => Queue();
                logicalName.SizeChanged += (_, _) => Queue(); logicalName.FontChanged += (_, _) => Queue();
                actions.SizeChanged += (_, _) => Queue(); update.SizeChanged += (_, _) => Queue();
                ascii.FontChanged += (_, _) => Queue(); update.FontChanged += (_, _) => Queue();
                Queue();
            }
            finally { page.ResumeLayout(true); }
        }

        private sealed class SecretActions : Panel
        {
            private readonly CheckBox ascii;
            private readonly Button update;
            private bool arranging;
            internal SecretActions(CheckBox ascii, Button update)
            {
                this.ascii = ascii; this.update = update;
                Margin = Padding.Empty; Padding = Padding.Empty;
                Controls.Add(ascii); Controls.Add(update);
                ascii.Dock = update.Dock = DockStyle.None;
                ascii.Anchor = update.Anchor = AnchorStyles.Top | AnchorStyles.Left;
                ascii.Margin = update.Margin = Padding.Empty;
                ascii.TabIndex = 0; update.TabIndex = 1;
                ascii.FontChanged += (_, _) => Parent?.PerformLayout();
                ascii.DpiChangedAfterParent += (_, _) => Parent?.PerformLayout();
                update.SizeChanged += (_, _) => Parent?.PerformLayout();
            }
            public override Size GetPreferredSize(Size proposedSize)
            {
                Size check = ascii.GetPreferredSize(Size.Empty);
                int gap = ModernPresentation.Scale(this, 8), width = check.Width + gap + update.Width;
                return proposedSize.Width > 0 && proposedSize.Width < width
                    ? new Size(Math.Max(check.Width, update.Width), check.Height + gap + update.Height)
                    : new Size(width, Math.Max(check.Height, update.Height));
            }
            protected override void OnLayout(LayoutEventArgs e)
            {
                base.OnLayout(e);
                if (arranging || ascii == null || update == null) return;
                arranging = true;
                try
                {
                    Size check = ascii.GetPreferredSize(Size.Empty);
                    int gap = ModernPresentation.Scale(this, 8);
                    bool stacked = ClientSize.Width < check.Width + gap + update.Width;
                    ascii.Bounds = new Rectangle(0, stacked ? 0 : Math.Max(0, (ClientSize.Height - check.Height) / 2), check.Width, check.Height);
                    update.Location = new Point(stacked ? 0 : check.Width + gap,
                        stacked ? check.Height + gap : Math.Max(0, (ClientSize.Height - update.Height) / 2));
                }
                finally { arranging = false; }
            }
        }
    }
}
