// superintelligentmeter contributors, 2026-10-30. Consistent native read-range input heights. GPL-2.0-only.
// SPDX-License-Identifier: GPL-2.0-only
using System;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Gurux.DLMS.UI
{
    public static partial class ModernPresentation
    {
        private static readonly ConditionalWeakTable<Control, NativeInputHeight> NativeInputHeights = new();

        // Opt in only the original Profile Generic read-range controls. No editors,
        // bindings, native arrows or event handlers are replaced.
        private static void UseStandardInputHeight(Control field)
        {
            if (field is NumericUpDown)
                NativeInputHeights.GetValue(field, editor => new NativeInputHeight(editor)).Fit();
        }

        private sealed class NativeInputHeight
        {
            private const uint NoSize = 0x0001, NoMove = 0x0002, NoZOrder = 0x0004, NoActivate = 0x0010;
            private readonly Control editor;
            private readonly TextBox numericText;
            private readonly HeightWindow outerWindow;
            private readonly HeightWindow textWindow;
            private bool fitting;

            internal NativeInputHeight(Control editor)
            {
                this.editor = editor;
                if (editor is NumericUpDown number)
                {
                    numericText = number.Controls.OfType<TextBox>().FirstOrDefault();
                    outerWindow = new HeightWindow(this, false);
                    if (numericText != null)
                    {
                        textWindow = new HeightWindow(this, true);
                        numericText.HandleCreated += TextHandleCreated;
                        numericText.HandleDestroyed += TextHandleDestroyed;
                        numericText.BackColorChanged += InputColorChanged;
                        numericText.ReadOnlyChanged += InputColorChanged;
                    }
                    number.Paint += PaintNumericPadding;
                    number.EnabledChanged += InputColorChanged;
                }
                editor.HandleCreated += HandleCreated;
                editor.HandleDestroyed += HandleDestroyed;
                editor.FontChanged += MetricsChanged;
                editor.DpiChangedAfterParent += MetricsChanged;
                editor.Disposed += Disposed;
                AttachWindows();
            }

            private int DesiredHeight => Math.Max(Scale(editor, 32), ((NumericUpDown)editor).PreferredHeight);
            private int TextHeight => Math.Min(Math.Max(1, editor.ClientSize.Height - 4), numericText.PreferredHeight);
            private int TextTop => Math.Max(2, (editor.ClientSize.Height - TextHeight) / 2);

            private void AttachWindows()
            {
                if (outerWindow != null && editor.IsHandleCreated && outerWindow.Handle == IntPtr.Zero)
                    outerWindow.AssignHandle(editor.Handle);
                if (textWindow != null && numericText.IsHandleCreated && textWindow.Handle == IntPtr.Zero)
                    textWindow.AssignHandle(numericText.Handle);
            }

            internal void Fit()
            {
                if (fitting || editor.IsDisposed) return;
                fitting = true;
                try
                {
                    AttachWindows();
                    if (!editor.IsHandleCreated) return;
                    if (editor.Height != DesiredHeight)
                        InputWindowPosition(editor.Handle, IntPtr.Zero, 0, 0, editor.Width, DesiredHeight,
                            NoMove | NoZOrder | NoActivate);
                    if (numericText != null && numericText.IsHandleCreated &&
                        (numericText.Height != TextHeight || numericText.Top != TextTop))
                        InputWindowPosition(numericText.Handle, IntPtr.Zero, numericText.Left, TextTop,
                            numericText.Width, TextHeight, NoZOrder | NoActivate);
                    editor.Invalidate();
                }
                finally { fitting = false; }
            }

            private void HandleCreated(object sender, EventArgs e) { AttachWindows(); Fit(); }
            private void HandleDestroyed(object sender, EventArgs e) { outerWindow?.ReleaseHandle(); }
            private void TextHandleCreated(object sender, EventArgs e) { AttachWindows(); Fit(); }
            private void TextHandleDestroyed(object sender, EventArgs e) { textWindow?.ReleaseHandle(); }
            private void MetricsChanged(object sender, EventArgs e) => Fit();
            private void InputColorChanged(object sender, EventArgs e) => editor.Invalidate();

            private void PaintNumericPadding(object sender, PaintEventArgs e)
            {
                if (numericText == null || numericText.IsDisposed) return;
                Color color = numericText.BackColor;
                var background = TypeDescriptor.GetProperties(editor)[nameof(Control.BackColor)];
                if (!editor.Enabled && !(background?.ShouldSerializeValue(editor) ?? false)) color = SystemColors.Control;
                // UpDownBase paints its own border and child controls. Fill only the
                // newly exposed interior padding; leave that native drawing intact.
                int left = Math.Max(1, numericText.Left - 1);
                int width = Math.Max(0, Math.Min(editor.ClientSize.Width - 1, numericText.Right + 1) - left);
                using var brush = new SolidBrush(color);
                if (numericText.Top > 1) e.Graphics.FillRectangle(brush, left, 1, width, numericText.Top - 1);
                int bottom = editor.ClientSize.Height - 1;
                if (numericText.Bottom < bottom) e.Graphics.FillRectangle(brush, left, numericText.Bottom, width, bottom - numericText.Bottom);
            }

            private void Disposed(object sender, EventArgs e)
            {
                outerWindow?.ReleaseHandle();
                textWindow?.ReleaseHandle();
                editor.HandleCreated -= HandleCreated;
                editor.HandleDestroyed -= HandleDestroyed;
                editor.FontChanged -= MetricsChanged;
                editor.DpiChangedAfterParent -= MetricsChanged;
                editor.Disposed -= Disposed;
                if (editor is NumericUpDown number)
                {
                    number.Paint -= PaintNumericPadding;
                    number.EnabledChanged -= InputColorChanged;
                    if (numericText != null)
                    {
                        numericText.HandleCreated -= TextHandleCreated;
                        numericText.HandleDestroyed -= TextHandleDestroyed;
                        numericText.BackColorChanged -= InputColorChanged;
                        numericText.ReadOnlyChanged -= InputColorChanged;
                    }
                }
            }

            private sealed class HeightWindow : NativeWindow
            {
                private readonly NativeInputHeight owner;
                private readonly bool text;
                internal HeightWindow(NativeInputHeight owner, bool text) { this.owner = owner; this.text = text; }
                protected override void WndProc(ref Message m)
                {
                    if (m.Msg == 0x0046 && m.LParam != IntPtr.Zero && !owner.editor.IsDisposed)
                    {
                        var position = Marshal.PtrToStructure<InputPosition>(m.LParam);
                        if (text && (position.Flags & (NoSize | NoMove)) != (NoSize | NoMove))
                        {
                            if ((position.Flags & NoMove) != 0) position.X = owner.numericText.Left;
                            position.Y = owner.TextTop;
                            position.Flags &= ~NoMove;
                            if ((position.Flags & NoSize) == 0) position.Height = owner.TextHeight;
                            Marshal.StructureToPtr(position, m.LParam, false);
                        }
                        else if (!text && (position.Flags & NoSize) == 0)
                        {
                            // UpDownBase strips minimum heights and resets its size on
                            // child/text changes. Constrain that native resize once, rather
                            // than reacting with a repeating SizeChanged/BeginInvoke loop.
                            position.Height = owner.DesiredHeight;
                            Marshal.StructureToPtr(position, m.LParam, false);
                        }
                    }
                    base.WndProc(ref m);
                }
            }

            [StructLayout(LayoutKind.Sequential)]
            private struct InputPosition
            {
                internal IntPtr Handle, InsertAfter;
                internal int X, Y, Width, Height;
                internal uint Flags;
            }
            [DllImport("user32.dll", EntryPoint = "SetWindowPos")]
            [return: MarshalAs(UnmanagedType.Bool)]
            private static extern bool InputWindowPosition(IntPtr handle, IntPtr insertAfter,
                int x, int y, int width, int height, uint flags);
        }
    }
}
