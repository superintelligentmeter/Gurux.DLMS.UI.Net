// superintelligentmeter contributors, 2026-10-30. Read-only object references. GPL-2.0-only.
// SPDX-License-Identifier: GPL-2.0-only
using System;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using Gurux.DLMS.Objects;

namespace Gurux.DLMS.UI
{
    // A reference has a useful identity, not an arbitrary structured-value editor.
    // Native text boxes preserve selection, copy and keyboard access without editing
    // the model. The surrounding layout grows to the wrapped text's measured height.
    internal sealed class GXObjectReferenceView : Panel
    {
        private readonly TextBox identity;
        private readonly TextBox details;
        private bool hasReference;
        private bool arranging;
        private bool settlePending;
        private int preferredHeight;
        internal event EventHandler ContentSizeChanged;

        public GXObjectReferenceView()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            TabStop = false;
            identity = CreateText("ReferenceIdentity", "Sort object");
            details = CreateText("ReferenceDetails", "Sort object attributes");
            identity.TabIndex = 0;
            details.TabIndex = 1;
            Controls.Add(identity);
            Controls.Add(details);
            foreach (var text in new[] { identity, details })
            {
                text.Enter += (_, _) => Invalidate();
                text.Leave += (_, _) => Invalidate();
                text.FontChanged += (_, _) => PerformLayout();
                text.KeyDown += (_, e) =>
                {
                    if (e.Control && e.KeyCode == Keys.A) { text.SelectAll(); e.SuppressKeyPress = true; }
                };
            }
            UpdateReference(null);
        }

        private static TextBox CreateText(string name, string accessibleName) => new TextBox
        {
            Name = name, AccessibleName = accessibleName, ReadOnly = true,
            Multiline = true, WordWrap = true, ScrollBars = ScrollBars.None,
            BorderStyle = BorderStyle.None, AutoSize = false, TabStop = true,
            // Width belongs to the viewport. DefaultLayout must not advertise
            // stale pre-resize textbox widths as a horizontal scroll extent.
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };

        internal void UpdateReference(GXDLMSProfileGeneric profile)
        {
            var reference = profile?.SortObject;
            hasReference = reference != null;
            string title = "No sort object";
            string attributes = string.Empty;
            if (hasReference)
            {
                title = reference.ObjectType + " · " + reference.LogicalName;
                string description = (reference.Description ?? string.Empty).Replace('\r', ' ').Replace('\n', ' ').Trim();
                if (description.Length != 0 && description != reference.ObjectType.ToString() && description != reference.LogicalName)
                    title += Environment.NewLine + description;
                attributes = "Attribute: " + profile.SortAttributeIndex.ToString(CultureInfo.CurrentCulture)
                    + " · Data index: " + profile.SortDataIndex.ToString(CultureInfo.CurrentCulture);
            }
            if (identity.Text != title) identity.Text = title;
            if (details.Text != attributes) details.Text = attributes;
            details.Visible = hasReference;
            ApplyColors();
            PerformLayout();
        }

        private int Pixels(int value) => (int)Math.Ceiling(value * DeviceDpi / 96d);

        private int TextHeight(TextBox text, int width) => Math.Max(text.Font.Height,
            TextRenderer.MeasureText(text.Text, text.Font, new Size(Math.Max(1, width - Pixels(4)), int.MaxValue),
                TextFormatFlags.TextBoxControl | TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding).Height) + Pixels(2);

        public override Size GetPreferredSize(Size proposedSize)
        {
            if (identity == null) return base.GetPreferredSize(proposedSize);
            int width = proposedSize.Width > 0 ? proposedSize.Width : Math.Max(1, ClientSize.Width);
            int innerWidth = Math.Max(1, width - Pixels(16));
            int height = TextHeight(identity, innerWidth) + Pixels(6) * 2;
            if (hasReference) height += Pixels(4) + TextHeight(details, innerWidth);
            return new Size(width, Math.Max(Pixels(32), height));
        }

        protected override void OnLayout(LayoutEventArgs e)
        {
            if (identity == null) { base.OnLayout(e); return; }
            if (arranging || IsDisposed) return;
            arranging = true;
            try
            {
                // Minimized hosts have no usable viewport. Preserve the last
                // measured extent until the native restore supplies real bounds.
                if (Width <= Pixels(16) || Height <= 0 ||
                    (TopLevelControl as Form)?.WindowState == FormWindowState.Minimized)
                {
                    base.OnLayout(e);
                    return;
                }
                int previousClientWidth = ClientSize.Width;
                // Decide from the full viewport, not a scrollbar retained from
                // the previous reference. Stage new bounds before native layout
                // scans them, so long-to-short refresh can remove that scrollbar.
                bool needsVertical = AutoScroll && GetPreferredSize(new Size(Math.Max(1, Width), 0)).Height > Height;
                int scrollbarWidth = Math.Max(0, Width - ClientSize.Width);
                if (scrollbarWidth == 0) scrollbarWidth = SystemInformation.VerticalScrollBarWidth;
                int width = Math.Max(1, Width - (needsVertical ? scrollbarWidth : 0) - Pixels(16));
                int identityHeight = TextHeight(identity, width);
                int detailsHeight = TextHeight(details, width);
                int nextHeight = Math.Max(Pixels(32), Pixels(6) * 2 + identityHeight +
                    (hasReference ? Pixels(4) + detailsHeight : 0));
                var offset = AutoScroll ? AutoScrollPosition : Point.Empty;
                identity.Bounds = new Rectangle(Pixels(8) + offset.X, Pixels(6) + offset.Y, width, identityHeight);
                details.Bounds = new Rectangle(Pixels(8) + offset.X, identity.Bottom + Pixels(4), width, detailsHeight);
                var extent = AutoScroll ? new Size(0, nextHeight) : Size.Empty;
                if (AutoScrollMinSize != extent) AutoScrollMinSize = extent;

                // Native anchoring may resize children once when a bar appears or
                // disappears. A single coalesced pass uses the resulting viewport.
                base.OnLayout(e);
                if ((previousClientWidth != ClientSize.Width || identity.Width != width || details.Width != width) &&
                    !settlePending && IsHandleCreated)
                {
                    settlePending = true;
                    BeginInvoke((Action)(() =>
                    {
                        // Keep the guard set through the settle pass: native
                        // minimum textbox bounds must not post an endless retry.
                        try { if (!IsDisposed) PerformLayout(); }
                        finally { settlePending = false; }
                    }));
                }
                if (preferredHeight != nextHeight)
                {
                    preferredHeight = nextHeight;
                    ContentSizeChanged?.Invoke(this, EventArgs.Empty);
                }
            }
            finally { arranging = false; }
        }

        protected override void OnFontChanged(EventArgs e) { base.OnFontChanged(e); PerformLayout(); }
        protected override void OnDpiChangedAfterParent(EventArgs e) { base.OnDpiChangedAfterParent(e); PerformLayout(); }
        protected override void OnSystemColorsChanged(EventArgs e) { base.OnSystemColorsChanged(e); ApplyColors(); }

        private void ApplyColors()
        {
            if (identity == null) return;
            BackColor = SystemInformation.HighContrast ? SystemColors.Window : Color.FromArgb(245, 247, 251);
            identity.BackColor = details.BackColor = BackColor;
            identity.ForeColor = SystemInformation.HighContrast ? SystemColors.WindowText : hasReference ? ModernPresentation.Ink : ModernPresentation.Muted;
            details.ForeColor = SystemInformation.HighContrast ? SystemColors.WindowText : ModernPresentation.Muted;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            using var pen = new Pen(SystemInformation.HighContrast ? SystemColors.WindowText : ContainsFocus ? ModernPresentation.Accent : ModernPresentation.Line);
            e.Graphics.DrawRectangle(pen, 0, 0, Math.Max(0, Width - 1), Math.Max(0, Height - 1));
        }
    }
}
