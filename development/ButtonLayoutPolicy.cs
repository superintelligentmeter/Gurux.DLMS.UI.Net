// superintelligentmeter contributors, 2026-10-30. Shared native action-group geometry. GPL-2.0-only.
// SPDX-License-Identifier: GPL-2.0-only
using System;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Forms;
using System.Windows.Forms.Layout;

namespace Gurux.DLMS.UI
{
    /// <summary>Native flow wrapping with stable reading order and optional right-aligned rows.</summary>
    public sealed class ActionButtonPanel : FlowLayoutPanel
    {
        private readonly LayoutEngine actionLayout = new ActionLayoutEngine();
        private int nativeLayout;
        private bool arranging;
        internal bool AlignRowsRight;
        public override LayoutEngine LayoutEngine => nativeLayout > 0 ? base.LayoutEngine : actionLayout;

        public override Size GetPreferredSize(Size proposedSize)
        {
            ++nativeLayout;
            try { return base.GetPreferredSize(proposedSize); }
            finally { --nativeLayout; }
        }

        private bool Arrange(LayoutEventArgs args)
        {
            if (arranging) return false;
            arranging = true;
            try
            {
                bool parentNeedsLayout;
                ++nativeLayout;
                try { parentNeedsLayout = base.LayoutEngine.Layout(this, args); }
                finally { --nativeLayout; }
                if (!AlignRowsRight || !Visible || ClientSize.Width < 1) return parentNeedsLayout;
                // Native LTR flow keeps wrapped rows in reading/tab order. Align
                // each completed row rather than reversing the entire sequence.
                SuspendLayout();
                try
                {
                    foreach (var row in Controls.OfType<Button>().Where(button => button.Visible).GroupBy(button => button.Top))
                    {
                        int shift = Math.Max(0, ClientSize.Width - Padding.Right - row.Max(button => button.Right));
                        if (shift == 0) continue;
                        foreach (var button in row) button.Left += shift;
                    }
                }
                finally { ResumeLayout(false); }
                return parentNeedsLayout;
            }
            finally { arranging = false; }
        }

        private sealed class ActionLayoutEngine : LayoutEngine
        {
            public override bool Layout(object container, LayoutEventArgs args) => ((ActionButtonPanel)container).Arrange(args);
        }
    }

    /// <summary>Sizes existing buttons without changing typography, colors, handlers or method bindings.</summary>
    public static class ButtonLayoutPolicy
    {
        public const int ActionHeight = 32;
        public const int Gap = 8;
        private static readonly ConditionalWeakTable<ActionButtonPanel, Group> Groups = new();
        private static readonly ConditionalWeakTable<Button, InlineButton> InlineButtons = new();

        /// <summary>Keep an inline field action text-safe while its owner controls placement and margins.</summary>
        public static void ApplyToButton(Button button, int minimumWidth = 100, int height = ActionHeight)
        {
            if (InlineButtons.TryGetValue(button, out _)) return;
            var state = new InlineButton(button, minimumWidth, height);
            InlineButtons.Add(button, state);
            state.Fit();
        }

        /// <summary>Keep related inline actions on one measured width without moving their controls.</summary>
        public static void MatchInlineWidths(params Button[] buttons)
        {
            var states = buttons.Distinct().Select(button =>
            {
                ApplyToButton(button);
                return InlineButtons.GetValue(button, _ => throw new InvalidOperationException());
            }).ToArray();
            foreach (var state in states) state.MatchWidth(states);
        }

        private sealed class InlineButton
        {
            private readonly Button button;
            private readonly int minimum, height;
            private bool fitting, pending;
            private InlineButton[] widthPeers = Array.Empty<InlineButton>();
            internal InlineButton(Button button, int minimum, int height)
            {
                this.button = button; this.minimum = minimum; this.height = height;
                button.AutoSize = false;
                button.FontChanged += Changed; button.TextChanged += Changed;
                button.PaddingChanged += Changed; button.ParentChanged += Changed;
                button.DpiChangedAfterParent += Changed; button.HandleCreated += Changed;
            }
            internal void MatchWidth(InlineButton[] peers)
            {
                widthPeers = peers;
                foreach (var peer in peers.Where(peer => peer != this))
                {
                    peer.button.FontChanged += Changed;
                    peer.button.TextChanged += Changed;
                    peer.button.PaddingChanged += Changed;
                    peer.button.ParentChanged += Changed;
                    peer.button.DpiChangedAfterParent += Changed;
                    peer.button.HandleCreated += Changed;
                }
                Fit();
            }
            private void Changed(object sender, EventArgs args)
            {
                Fit();
                if (pending || fitting || button.IsDisposed || !button.IsHandleCreated) return;
                pending = true;
                button.BeginInvoke((Action)(() =>
                {
                    try { if (!button.IsDisposed) Fit(); }
                    finally { pending = false; }
                }));
            }
            internal void Fit()
            {
                if (fitting || button.IsDisposed ||
                    (button.TopLevelControl as Form)?.WindowState == FormWindowState.Minimized) return;
                fitting = true;
                try
                {
                    var size = Measure(button, button.DeviceDpi, minimum, height);
                    if (widthPeers.Length > 0)
                        size.Width = widthPeers.Where(peer => !peer.button.IsDisposed)
                            .Select(peer => Measure(peer.button, peer.button.DeviceDpi, peer.minimum, peer.height).Width)
                            .DefaultIfEmpty(size.Width).Max();
                    if (button.Size == size && button.MinimumSize == size && button.MaximumSize == size) return;
                    button.MinimumSize = button.MaximumSize = Size.Empty;
                    button.Size = button.MinimumSize = button.MaximumSize = size;
                }
                finally { fitting = false; }
            }
        }

        /// <summary>Reading order is explicit; native flow layout supplies wrapping and tab ownership.</summary>
        public static void Apply(ActionButtonPanel group, Button[] buttonsInReadingOrder,
            bool alignRight = true, int minimumWidth = 100, int height = ActionHeight,
            int horizontalInset = 0, int topInset = 4, int bottomInset = 4, bool equalWidths = true, bool wrap = true)
        {
            if (group == null) throw new ArgumentNullException(nameof(group));
            if (buttonsInReadingOrder == null) throw new ArgumentNullException(nameof(buttonsInReadingOrder));
            if (Groups.TryGetValue(group, out _)) return;
            var state = new Group(group, buttonsInReadingOrder.Distinct().ToArray(), alignRight,
                minimumWidth, height, horizontalInset, topInset, bottomInset, equalWidths, wrap);
            Groups.Add(group, state);
            state.Fit();
        }

        /// <summary>Current-DPI text-safe size for an existing native button.</summary>
        public static Size GetPreferredSize(Button button, int minimumWidth = 100, int height = ActionHeight) =>
            Measure(button, button.DeviceDpi, minimumWidth, height);

        private static Size Measure(Button button, int dpi, int minimumWidth, int height)
        {
            int S(int value) => (int)Math.Round(value * dpi / 96d);
            const TextFormatFlags flags = TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix;
            // Native Button layout can use the screen DC while painting uses
            // the control DC. Reserve both extents on mixed-DPI displays so
            // its internal caption layout cannot wrap into a clipped line.
            Size text = TextRenderer.MeasureText(button.Text, button.Font, Size.Empty, flags);
            if (button.IsHandleCreated)
            {
                using var graphics = button.CreateGraphics();
                Size painted = TextRenderer.MeasureText(graphics, button.Text, button.Font, Size.Empty, flags);
                text = new Size(Math.Max(text.Width, painted.Width), Math.Max(text.Height, painted.Height));
            }
            int image = button.Image == null ? 0 : button.Image.Width + S(Gap);
            int width = Math.Max(S(minimumWidth), text.Width + image + button.Padding.Horizontal +
                button.FlatAppearance.BorderSize * 2 + S(8));
            int logical = (int)(Math.Ceiling(width * 96d / Math.Max(1, dpi) / 4d) * 4);
            // Reserve native border/internal text insets vertically too. Font.Height
            // alone can be smaller than the rendered line on mixed-DPI displays.
            int contentHeight = Math.Max(text.Height, button.Font.Height);
            return new Size(S(logical), Math.Max(S(height), contentHeight + button.Padding.Vertical +
                button.FlatAppearance.BorderSize * 2 + S(8)));
        }

        private sealed class Group
        {
            private readonly ActionButtonPanel panel;
            private readonly Button[] buttons;
            private readonly bool right, equal;
            private readonly int minimumWidth, height, horizontal, top, bottom;
            private bool fitting, pending;

            internal Group(ActionButtonPanel panel, Button[] buttons, bool right, int minimumWidth,
                int height, int horizontal, int top, int bottom, bool equal, bool wrap)
            {
                this.panel = panel; this.buttons = buttons; this.right = right; this.equal = equal;
                this.minimumWidth = minimumWidth; this.height = height;
                this.horizontal = horizontal; this.top = top; this.bottom = bottom;
                panel.SuspendLayout();
                try
                {
                    panel.AutoSize = true; panel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
                    panel.WrapContents = wrap;
                    panel.FlowDirection = FlowDirection.LeftToRight;
                    panel.AlignRowsRight = right;
                    panel.Margin = Padding.Empty; panel.MinimumSize = Size.Empty;
                    var ordered = buttons;
                    for (int i = 0; i < ordered.Length; ++i)
                    {
                        var button = ordered[i];
                        if (button.Parent != panel) panel.Controls.Add(button);
                        panel.Controls.SetChildIndex(button, i);
                    }
                    for (int i = 0; i < buttons.Length; ++i)
                    {
                        var button = buttons[i];
                        button.Dock = DockStyle.None; button.Anchor = AnchorStyles.Top | AnchorStyles.Left;
                        button.AutoSize = false; button.TabIndex = i;
                        button.FontChanged += Changed; button.TextChanged += Changed;
                        button.PaddingChanged += Changed; button.VisibleChanged += Changed; button.HandleCreated += Changed;
                        button.DpiChangedAfterParent += Changed;
                        // WinForms rescales a button's size on a monitor change independently of its
                        // minimum and maximum size; restore the measured size whenever it drifts.
                        button.SizeChanged += Changed;
                    }
                    panel.ParentChanged += Changed; panel.FontChanged += Changed; panel.VisibleChanged += Changed;
                    panel.DpiChangedAfterParent += Changed; panel.HandleCreated += Changed;
                }
                finally { panel.ResumeLayout(true); }
            }

            private int S(int value) => (int)Math.Round(value * panel.DeviceDpi / 96d);
            private void Changed(object sender, EventArgs args)
            {
                Fit();
                // Fonts and native children finish their DPI transaction after
                // the parent's notification. One bounded pass uses those results.
                if (pending || fitting || panel.IsDisposed || !panel.IsHandleCreated) return;
                pending = true;
                panel.BeginInvoke((Action)(() =>
                {
                    try { if (!panel.IsDisposed) Fit(); }
                    finally { pending = false; }
                }));
            }

            internal void Fit()
            {
                if (fitting || panel.IsDisposed || buttons.Any(b => b.IsDisposed) ||
                    (panel.TopLevelControl as Form)?.WindowState == FormWindowState.Minimized) return;
                fitting = true;
                panel.SuspendLayout();
                try
                {
                    var sizes = buttons.Select(button => Measure(button, panel.DeviceDpi, minimumWidth, height)).ToArray();
                    int[] widths = sizes.Select(size => size.Width).ToArray();
                    int common = widths.DefaultIfEmpty(S(minimumWidth)).Max();
                    int buttonHeight = sizes.Select(size => size.Height).DefaultIfEmpty(S(height)).Max();
                    // Four explicit vertical margin pixels give wrapped rows an
                    // eight-pixel gap, with the same first/last inset as one row.
                    int vertical = S(Math.Min(4, Math.Min(top, bottom)));
                    panel.Padding = new Padding(S(horizontal), S(top) - vertical,
                        S(horizontal), S(bottom) - vertical);
                    for (int i = 0; i < buttons.Length; ++i)
                    {
                        var button = buttons[i];
                        var desired = new Size(equal ? common : widths[i], buttonHeight);
                        if (button.Size != desired || button.MinimumSize != desired || button.MaximumSize != desired)
                        {
                            button.MinimumSize = button.MaximumSize = Size.Empty;
                            button.Size = button.MinimumSize = button.MaximumSize = desired;
                        }
                        button.Margin = new Padding(0, vertical, i + 1 < buttons.Length ? S(Gap) : 0, vertical);
                    }
                }
                finally { panel.ResumeLayout(true); fitting = false; }
            }
        }
    }
}
