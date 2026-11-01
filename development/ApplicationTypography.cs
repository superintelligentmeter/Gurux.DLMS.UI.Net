// Modified for the Intelmeter fork, 2026-10-16: .NET 10 migration, UI modernization and compatibility fixes. GPL-2.0-only; original notices retained.
// Superintelligent Meter contributors, 2026-11-01: a closing window is hidden and not rescaled for DPI. GPL-2.0-only.
// Superintelligent Meter contributors, 2026-10-31: the body size follows each window's monitor DPI. GPL-2.0-only.
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Gurux.DLMS.UI
{
    // Apply point-based typography once; WinForms owns subsequent DPI scaling.
    public static class ApplicationTypography
    {
        public const float BodyFontPoints = 9f;
        public static readonly Font BodyFont = new Font("Segoe UI", BodyFontPoints, FontStyle.Regular, GraphicsUnit.Point);
        private static readonly ConditionalWeakTable<Control, object> Styled = new();
        private static readonly Dictionary<string, Font> Fonts = new();
        private static bool installed;
        private static float systemDpi;

        public static void Install()
        {
            if (installed) return;
            installed = true;
            Application.Idle += (_, _) =>
            {
                foreach (Form form in Application.OpenForms.Cast<Form>().ToArray()) Apply(form);
            };
        }

        /// <summary>The body size, in points, for a control at its current monitor DPI.</summary>
        /// <remarks>
        /// Point sizes are realized at the system DPI (the primary display's DPI at sign-in).
        /// With per-monitor DPI, WinForms shows a window on a monitor with another DPI by
        /// scaling its fonts by DeviceDpi / system DPI, so the body size follows that scale.
        /// A fixed 9 pt floor re-enlarged every dialog that was styled after it opened on a
        /// lower-DPI monitor (125% text on a 100% monitor next to a 125% primary display).
        /// </remarks>
        public static float BodyPointsFor(Control control)
        {
            if (control == null) return BodyFontPoints;
            // Child windows share their top-level window's DPI. A child's cached DeviceDpi
            // can lag behind when Windows places the window on another monitor while it opens.
            Control top = control.TopLevelControl;
            int dpi = top != null && top.IsHandleCreated ? top.DeviceDpi : control.DeviceDpi;
            return BodyFontPoints * dpi / SystemDpi;
        }

        private static float SystemDpi
        {
            get
            {
                if (systemDpi <= 0)
                {
                    try { systemDpi = GetDpiForSystem(); }
                    catch (EntryPointNotFoundException) { }
                    if (systemDpi <= 0)
                    {
                        using var screen = Graphics.FromHwnd(IntPtr.Zero);
                        systemDpi = screen.DpiY;
                    }
                }
                return systemDpi;
            }
        }

        private static bool IsMonospace(Font font) =>
            font.Name.IndexOf("Consol", StringComparison.OrdinalIgnoreCase) >= 0
            || font.Name.IndexOf("Courier", StringComparison.OrdinalIgnoreCase) >= 0;

        private static Font Target(Control control, Font source)
        {
            bool mono = IsMonospace(source);
            string family = mono ? source.Name : "Segoe UI";
            float body = BodyPointsFor(control);
            float size;
            if (source.Name != "Segoe UI") size = Math.Max(body, source.SizeInPoints);
            // A tool strip's default menu font can keep the DPI of the monitor its window was
            // created on when Windows places the window on another monitor while opening it.
            // Menus, tool bars and status bars use the body size of the monitor they are on.
            else if (control is ToolStrip && Math.Abs(source.SizeInPoints - body) <= body / 4) size = body;
            // A new control can inherit an already DPI-scaled Segoe UI font.
            // Preserve that size rather than replacing it with the startup size.
            else size = source.SizeInPoints;
            size = (float)Math.Round(size, 2);
            string key = family + "|" + size + "|" + source.Style;
            if (!Fonts.TryGetValue(key, out Font font))
                Fonts.Add(key, font = new Font(family, size, source.Style, GraphicsUnit.Point));
            return font;
        }

        public static void Apply(Control root)
        {
            if (root.IsDisposed) return;
            if (Styled.TryGetValue(root, out _))
            {
                // A form styled before its window existed (the Idle pass can run while a dialog is
                // still being constructed) is watched once its window is live.
                if (root is Form && !WatchedWindows.TryGetValue(root, out _) && root.IsHandleCreated) WatchToolStrips(root);
                return;
            }
            // A window that is already showing has its monitor DPI. A control on it without a
            // handle (for example on a tab page not opened yet) can still report the DPI it was
            // constructed with; style that control once WinForms has created it on the monitor.
            Control top = root.TopLevelControl;
            bool live = top != null && top.IsHandleCreated;
            var pending = new List<(Control Control, Font Font)>();
            var waiting = new List<Control>();
            void Capture(Control control)
            {
                if (Styled.TryGetValue(control, out _)) return;
                Styled.Add(control, new object());
                if (live && !control.IsHandleCreated && control.DeviceDpi != top.DeviceDpi && control is not ToolStrip) waiting.Add(control);
                else pending.Add((control, Target(control, control.Font)));
                foreach (Control child in control.Controls) Capture(child);
                if (control.ContextMenuStrip != null) Capture(control.ContextMenuStrip);
            }
            Capture(root);
            foreach (var entry in pending)
            {
                entry.Control.Font = entry.Font;
                Attach(entry.Control);
            }
            foreach (Control control in waiting)
            {
                Attach(control);
                void Created(object sender, EventArgs args)
                {
                    control.HandleCreated -= Created;
                    if (!control.IsDisposed) control.Font = Target(control, control.Font);
                }
                control.HandleCreated += Created;
                if (control.IsHandleCreated) Created(control, EventArgs.Empty);
            }
            if (live && root is Form) WatchToolStrips(root);
        }

        private static readonly ConditionalWeakTable<Control, object> WatchedWindows = new();

        /// <summary>
        /// When Windows places a window on another monitor while it opens, WinForms can leave its
        /// tool strips (menus, tool bars, status bars) on the window's original legacy font,
        /// rescaled for the new monitor. After the first typography pass of a showing window and
        /// after each of its monitor changes, give such strips the body font of their monitor.
        /// </summary>
        private static void WatchToolStrips(Control window)
        {
            if (!WatchedWindows.TryGetValue(window, out _))
            {
                WatchedWindows.Add(window, new object());
                if (window is Form form)
                {
                    // Disposing an owned window removes its owner, and when the window is on a monitor
                    // whose DPI differs from the one it was created for, Windows reports the system DPI
                    // and then the monitor DPI again (WM_DPICHANGED twice). WinForms rescales every
                    // control each time, and the window is still on screen: about a second of a
                    // half-laid-out dialog (the DLMS Translator), seconds for Device Properties.
                    // A window that is closing and will be disposed is hidden first, its layout work is
                    // paused and its DPI scaling is skipped. A reused instance (shown again) resumes.
                    bool closed = false;
                    var paused = new List<Control>();
                    form.FormClosed += (_, _) =>
                    {
                        closed = true;
                        if (form.Modal || form.IsDisposed || !form.IsHandleCreated) return;
                        form.Visible = false;
                        PauseLayout(form, paused);
                    };
                    form.VisibleChanged += (_, _) =>
                    {
                        if (!form.Visible || form.IsDisposed) return;
                        closed = false;
                        if (paused.Count == 0) return;
                        foreach (Control c in paused) if (!c.IsDisposed) c.ResumeLayout(false);
                        paused.Clear();
                        form.PerformLayout();
                    };
                    // Moving a window to a monitor with another DPI rescales every control while the old
                    // window is still painted: lines and half-sized controls flash for seconds (about 8 s
                    // and thousands of repaints for the DLMS Translator). Windows reports the change to the
                    // top-level window first (WM_DPICHANGED_BEFOREPARENT, before any control is rescaled), so
                    // freeze drawing and layout then, and lay out and repaint once when the rescaling settles.
                    var settle = new System.Windows.Forms.Timer { Interval = 150 };
                    var dpiPaused = new List<Control>();
                    bool frozen = false;
                    void Freeze()
                    {
                        if (closed || form.IsDisposed || !form.IsHandleCreated || !form.Visible) return;
                        // Only owned windows and dialogs (the Tools windows): the main window and the object
                        // views it hosts keep WinForms' own DPI handling.
                        if (form.Owner == null && !form.Modal) return;
                        if (!frozen)
                        {
                            frozen = true;
                            LockWindowUpdate(form.Handle);
                            PauseLayout(form, dpiPaused);
                        }
                        settle.Stop();
                        settle.Start();
                    }
                    void Release()
                    {
                        settle.Stop();
                        if (!frozen) return;
                        frozen = false;
                        LockWindowUpdate(IntPtr.Zero);
                        // Resume inner containers first and lay each out: skipping the layout leaves stale
                        // sizes (and scroll bars that are not needed) in controls that were rescaled while paused.
                        for (int i = dpiPaused.Count - 1; i >= 0; --i)
                            if (!dpiPaused[i].IsDisposed) dpiPaused[i].ResumeLayout(true);
                        dpiPaused.Clear();
                        if (form.IsDisposed || !form.IsHandleCreated) return;
                        form.PerformLayout();
                        form.Invalidate(true);
                        form.Update();
                    }
                    settle.Tick += (_, _) => Release();
                    Freezers.Add(form, Freeze);
                    var watcher = new DpiWatcher(Freeze);
                    if (form.IsHandleCreated) watcher.AssignHandle(form.Handle);
                    form.HandleCreated += (_, _) => watcher.AssignHandle(form.Handle);
                    form.HandleDestroyed += (_, _) => watcher.ReleaseHandle();
                    form.Disposed += (_, _) =>
                    {
                        if (frozen) { frozen = false; LockWindowUpdate(IntPtr.Zero); }
                        settle.Dispose();
                    };
                    form.DpiChanged += (_, e) =>
                    {
                        if (closed) { e.Cancel = true; return; }
                        Freeze();
                        PostToolStripCheck(window);
                    };
                }
            }
            PostToolStripCheck(window);
        }

        private sealed class DpiWatcher : NativeWindow
        {
            private readonly Action began;
            internal DpiWatcher(Action began) { this.began = began; }
            protected override void WndProc(ref Message m)
            {
                // WM_DPICHANGED_BEFOREPARENT (0x02E2) reaches the window before its controls are rescaled.
                if (m.Msg == 0x02E2 || m.Msg == 0x02E0) began();
                base.WndProc(ref m);
            }
        }

        [DllImport("user32.dll")]
        private static extern bool LockWindowUpdate(IntPtr hWndLock);

        private static void PauseLayout(Control parent, List<Control> paused)
        {
            parent.SuspendLayout();
            paused.Add(parent);
            foreach (Control child in parent.Controls)
                if (child.Controls.Count > 0) PauseLayout(child, paused);
        }

        private static void PostToolStripCheck(Control window)
        {
            if (window.IsDisposed || !window.IsHandleCreated) return;
            window.BeginInvoke((Action)(() =>
            {
                void Check(Control parent)
                {
                    foreach (Control child in parent.Controls)
                    {
                        if (child is ToolStrip strip && !strip.IsDisposed && strip.Font.Name != "Segoe UI" && !IsMonospace(strip.Font))
                            strip.Font = Target(strip, strip.Font);
                        Check(child);
                    }
                }
                if (!window.IsDisposed) Check(window);
            }));
        }

        // Windows reports a monitor change to the child windows before the top-level window, and each
        // control then rescales and lays itself out while the window is still painted. A control sees
        // that first, so it asks its window to freeze drawing and layout until the rescaling settles.
        private static readonly ConditionalWeakTable<Control, Action> Freezers = new();

        private static void Attach(Control control)
        {
            control.DpiChangedBeforeParent += (_, _) =>
            {
                Form owner = control.FindForm();
                if (owner != null && Freezers.TryGetValue(owner, out Action freeze)) freeze();
            };
            control.ControlAdded += (_, e) => Apply(e.Control);
            control.ContextMenuStripChanged += (_, _) =>
            {
                if (control.ContextMenuStrip != null) Apply(control.ContextMenuStrip);
            };
            if (control is ToolStrip strip)
            {
                strip.ItemAdded += (_, _) => StyleItems(strip);
                // When Windows places a window on another monitor while it opens, WinForms can
                // rescale a tool strip from the window's original legacy font after this pass.
                // Give it the body font of its monitor again.
                strip.FontChanged += (_, _) =>
                {
                    if (!strip.IsDisposed && strip.Font.Name != "Segoe UI" && !IsMonospace(strip.Font))
                        strip.Font = Target(strip, strip.Font);
                };
                StyleItems(strip);
            }
            if (control is DataGridView grid)
            {
                grid.FontChanged += (_, _) => UpdateGrid(grid);
                grid.ColumnAdded += (_, _) => UpdateGrid(grid);
                UpdateGrid(grid);
            }
        }

        private static void StyleItems(ToolStrip strip)
        {
            foreach (ToolStripItem item in strip.Items)
            {
                item.ResetFont();
                if (item is ToolStripDropDownItem drop)
                {
                    // Auto-generated dropdowns inherit the owner item font.
                    // Explicit item fonts would scale independently on opening.
                    if (!Styled.TryGetValue(drop.DropDown, out _))
                    {
                        Styled.Add(drop.DropDown, new object());
                        drop.DropDown.ItemAdded += (_, _) => StyleItems(drop.DropDown);
                    }
                    StyleItems(drop.DropDown);
                }
            }
        }

        private static void UpdateGrid(DataGridView grid)
        {
            if (grid.IsDisposed) return;
            grid.DefaultCellStyle.Font = grid.Font;
            grid.ColumnHeadersDefaultCellStyle.Font = grid.Font;
            grid.RowHeadersDefaultCellStyle.Font = grid.Font;
            grid.ColumnHeadersDefaultCellStyle.WrapMode = DataGridViewTriState.False;
            foreach (DataGridViewColumn column in grid.Columns)
            {
                int width = TextRenderer.MeasureText(column.HeaderText, grid.Font).Width
                    + (int)Math.Ceiling(24 * grid.DeviceDpi / 96f);
                column.MinimumWidth = Math.Max(2, width);
            }
        }

        [DllImport("user32.dll")]
        private static extern uint GetDpiForSystem();
    }
}
