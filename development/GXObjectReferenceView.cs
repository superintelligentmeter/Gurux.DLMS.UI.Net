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
            BorderStyle = BorderStyle.None, AutoSize = false, TabStop = true
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
            int height = TextHeight(identity, innerWidth) + Pixels(12);
            if (hasReference) height += Pixels(4) + TextHeight(details, innerWidth);
            return new Size(width, Math.Max(Pixels(32), height));
        }

        protected override void OnLayout(LayoutEventArgs e)
        {
            base.OnLayout(e);
            if (identity == null || arranging) return;
            arranging = true;
            try
            {
                int width = Math.Max(1, ClientSize.Width - Pixels(16));
                int height = TextHeight(identity, width);
                identity.Bounds = new Rectangle(Pixels(8), Pixels(6), width, height);
                details.Bounds = new Rectangle(Pixels(8), identity.Bottom + Pixels(4), width, TextHeight(details, width));
                int nextHeight = GetPreferredSize(new Size(ClientSize.Width, 0)).Height;
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
