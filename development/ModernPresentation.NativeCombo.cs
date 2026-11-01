// superintelligentmeter contributors, 2026-10-30. Native dropdowns sized to standard input rows. GPL-2.0-only.
// SPDX-License-Identifier: GPL-2.0-only
using System;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Gurux.DLMS.UI
{
    public static partial class ModernPresentation
    {
        private static readonly ConditionalWeakTable<ComboBox, NativeComboSizing> NativeComboSizes = new();

        // Keep the real Windows combo frame, arrow, list, accessibility and input
        // behavior. Only its closed selection field adopts the standard input height.
        internal static void LayoutNativeCombo(ComboBox combo, Rectangle bounds) =>
            NativeComboSizes.GetValue(combo, editor => new NativeComboSizing(editor)).Layout(bounds);

        private sealed class NativeComboSizing
        {
            private const int GetItemHeight = 0x0154;
            private const int SetItemHeight = 0x0153;
            private readonly ComboBox editor;
            private bool arranging;
            private bool layoutPending;

            internal NativeComboSizing(ComboBox editor)
            {
                this.editor = editor;
                editor.HandleCreated += (_, _) => QueueLayout();
                editor.HandleDestroyed += (_, _) => layoutPending = false;
                editor.FontChanged += (_, _) => QueueLayout();
                editor.DpiChangedAfterParent += (_, _) => QueueLayout();
                editor.DropDownStyleChanged += (_, _) => QueueLayout();
                editor.SizeChanged += (_, _) => { if (!arranging) QueueLayout(); };
            }

            private void QueueLayout()
            {
                if (layoutPending || editor.IsDisposed || !editor.IsHandleCreated) return;
                layoutPending = true;
                editor.BeginInvoke((Action)(() =>
                {
                    layoutPending = false;
                    if (!editor.IsDisposed) editor.Parent?.PerformLayout();
                }));
            }

            internal void Layout(Rectangle bounds)
            {
                if (arranging || editor.IsDisposed) return;
                arranging = true;
                try
                {
                    editor.Dock = DockStyle.None;
                    if (editor.DropDownStyle == ComboBoxStyle.Simple)
                    {
                        editor.Bounds = bounds;
                        return;
                    }
                    int nativeHeight = editor.PreferredHeight;
                    int height = Math.Max(nativeHeight, Math.Min(Scale(editor, 32), bounds.Height));
                    int top = bounds.Top + Math.Max(0, (bounds.Height - height) / 2);
                    int width = Math.Max(0, bounds.Width);
                    // Avoid shrinking and growing an already-correct native window
                    // on every parent layout pass (including focus/selection changes).
                    if (editor.IsHandleCreated && editor.Left == bounds.Left && editor.Top == top &&
                        editor.Width == width && editor.Height == height) return;
                    // WinForms bounds changes restore PreferredHeight. Set geometry
                    // first, then adjust the native selection height LAST.
                    editor.SetBounds(bounds.Left, top, width, nativeHeight);
                    if (!editor.IsHandleCreated) return;
                    int selectionHeight = (int)ComboMessage(editor.Handle, GetItemHeight, new IntPtr(-1), IntPtr.Zero);
                    int listHeight = (int)ComboMessage(editor.Handle, GetItemHeight, IntPtr.Zero, IntPtr.Zero);
                    if (selectionHeight < 0 || !ComboWindowRect(editor.Handle, out var rectangle)) return;
                    int chrome = Math.Max(0, rectangle.Bottom - rectangle.Top - selectionHeight);
                    int desired = Math.Max(listHeight, height - chrome);
                    if (desired > 0 && desired != selectionHeight)
                        ComboMessage(editor.Handle, SetItemHeight, new IntPtr(-1), new IntPtr(desired));
                }
                finally { arranging = false; }
            }

            [StructLayout(LayoutKind.Sequential)]
            private struct NativeRectangle { internal int Left, Top, Right, Bottom; }

            [DllImport("user32.dll", EntryPoint = "SendMessageW")]
            private static extern IntPtr ComboMessage(IntPtr handle, int message, IntPtr wParam, IntPtr lParam);
            [DllImport("user32.dll", EntryPoint = "GetWindowRect")]
            [return: MarshalAs(UnmanagedType.Bool)]
            private static extern bool ComboWindowRect(IntPtr handle, out NativeRectangle rectangle);
        }

        private sealed class NativeComboHost : Panel
        {
            private readonly ComboBox editor;
            private bool arranging;

            internal NativeComboHost(ComboBox editor)
            {
                this.editor = editor;
                Name = editor.Name + "Host";
                TabStop = false;
                TabIndex = editor.TabIndex;
                editor.Dock = DockStyle.None;
                editor.Anchor = AnchorStyles.Top | AnchorStyles.Left;
                editor.Margin = Padding.Empty;
                editor.TabIndex = 0;
                Controls.Add(editor);
                editor.FontChanged += (_, _) => PerformLayout();
            }

            protected override void OnLayout(LayoutEventArgs e)
            {
                base.OnLayout(e);
                if (editor == null || arranging) return;
                arranging = true;
                try
                {
                    BackColor = Parent?.BackColor ?? Color.White;
                    LayoutNativeCombo(editor, ClientRectangle);
                }
                finally { arranging = false; }
            }

            protected override void OnParentChanged(EventArgs e) { base.OnParentChanged(e); PerformLayout(); }
            protected override void OnDpiChangedAfterParent(EventArgs e) { base.OnDpiChangedAfterParent(e); PerformLayout(); }
            public override Size GetPreferredSize(Size proposedSize) => editor == null ? base.GetPreferredSize(proposedSize) : editor.GetPreferredSize(proposedSize);
        }
    }
}
