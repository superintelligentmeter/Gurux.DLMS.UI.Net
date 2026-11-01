// superintelligentmeter contributors, 2026-10-30. Offline button audit evidence. GPL-2.0-only.
// SPDX-License-Identifier: GPL-2.0-only
#nullable enable
using System.Drawing;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows.Forms;

internal static class ButtonAuditMetrics
{
    static readonly Dictionary<Form, Dictionary<Button, (string Index, string Action)>> Originals = new();
    static IEnumerable<Control> Children(Control p) { foreach (Control c in p.Controls) { yield return c; foreach (var n in Children(c)) yield return n; } }
    static string Path(Control c) => (c.Parent == null ? "" : Path(c.Parent) + "/") + (string.IsNullOrEmpty(c.Name) ? c.GetType().Name + "[" + (c.Parent?.Controls.GetChildIndex(c) ?? 0) + "]" : c.Name);
    static string Property(object? o, string name) { try { return o?.GetType().GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(o)?.ToString() ?? ""; } catch { return ""; } }
    internal static void Remember(Form form) => Originals[form] = Children(form).OfType<Button>().ToDictionary(b => b, b => (Property(b, "Index"), Property(b, "Action")));
    internal static bool OriginalsRetained(Form form)
    {
        if (!Originals.TryGetValue(form, out var before)) return false;
        var current = Children(form).ToHashSet();
        return before.All(p => !p.Key.IsDisposed && current.Contains(p.Key) && p.Value == (Property(p.Key, "Index"), Property(p.Key, "Action")));
    }
    internal static List<ButtonAuditCheck> FooterChecks(Form form)
    {
        var all = Children(form).ToArray();
        var roots = new HashSet<Control>(all.Where(c => c.Name is "SettingsActions" or "NativeDialogActions" or "NativeDialogFooter" or "HostActions" or "DialogActions"));
        foreach (var confirmation in new[] { form.AcceptButton as Button, form.CancelButton as Button }.Where(b => b != null))
        {
            if (confirmation!.Parent != null) roots.Add(confirmation.Parent);
            for (Control? p = confirmation.Parent; p != null && p != form; p = p.Parent)
                if (p.Dock == DockStyle.Bottom) roots.Add(p);
        }
        var checks = new List<ButtonAuditCheck>();
        var metrics = Measure(form);
        var footerButtons = roots.SelectMany(r => Children(r).OfType<Button>().Concat(r is Button b ? new[] { b } : Array.Empty<Button>())).Distinct();
        foreach (var button in footerButtons.Where(b => b.Visible))
        {
            var metric = metrics.Single(m => m.ControlIdentity == RuntimeHelpers.GetHashCode(button) && m.Path == Path(button));
            if (metric.Exclusion.Length != 0) continue;
            checks.Add(new() { Name = "pinned-footer/" + metric.Name + "/all-ancestor-visible",
                Passed = metric.VisibleWidth + 1 >= metric.ClientWidth && metric.VisibleHeight + 1 >= metric.ClientHeight,
                Detail = $"Visible={metric.VisibleWidth}x{metric.VisibleHeight}; client={metric.ClientWidth}x{metric.ClientHeight}; parent={metric.ParentPath}; scrollAncestor={metric.HasScrollableAncestor} (never a waiver for a fixed footer)." });
        }
        foreach (var group in all.OfType<FlowLayoutPanel>().Where(g => g.Visible && g.Parent is TableLayoutPanel))
        {
            var owner = (TableLayoutPanel)group.Parent!; int column = owner.GetColumn(group);
            if (owner.GetColumnSpan(group) != 1 || column < 0 || column >= owner.ColumnStyles.Count || owner.ColumnStyles[column].SizeType != SizeType.AutoSize) continue;
            var peers = group.Controls.OfType<Button>().Where(b => b.Visible).ToArray();
            if (peers.Length < 2) continue;
            checks.Add(new() { Name = "autosize-column/" + Path(group) + "/single-row",
                Passed = peers.Select(b => b.Top).Distinct().Count() == 1,
                Detail = string.Join(";", peers.Select(b => b.Name + ":" + b.Bounds)) });
        }
        return checks;
    }
    internal static List<ButtonAuditMetric> Measure(Form form)
    {
        var result = new List<ButtonAuditMetric>();
        foreach (var b in Children(form).OfType<Button>().Where(b => !b.IsDisposed))
        {
            // Location belongs to the parent's client coordinate system. Using
            // b.PointToScreen(Empty) here would add the native client border inset.
            Point outer = form.PointToClient(b.Parent!.PointToScreen(b.Location));
            Point parent = ReferenceEquals(b.Parent, form) ? Point.Empty : form.PointToClient(b.Parent.Parent!.PointToScreen(b.Parent.Location));
            var visible = b.RectangleToScreen(b.ClientRectangle);
            bool scroll = false;
            for (Control? p = b.Parent; p != null; p = p.Parent)
            {
                visible.Intersect(p.RectangleToScreen(p.ClientRectangle));
                scroll |= p is ScrollableControl sc && (sc.VerticalScroll.Visible || sc.HorizontalScroll.Visible);
            }
            int textWidth = 0, textHeight = 0; float hdcDpi = 0; var ambientSingle = TextRenderer.MeasureText(b.Text, b.Font, new Size(int.MaxValue, int.MaxValue), TextFormatFlags.SingleLine); var adapterUnbounded = TextRenderer.MeasureText(b.Text, b.Font, new Size(int.MaxValue, int.MaxValue), TextFormatFlags.WordBreak | TextFormatFlags.TextBoxControl); var adapterBounded = TextRenderer.MeasureText(b.Text, b.Font, new Size(Math.Max(1, b.ClientSize.Width-b.Padding.Horizontal-10), int.MaxValue), TextFormatFlags.WordBreak | TextFormatFlags.TextBoxControl);
            if (b.Visible && b.IsHandleCreated)
            {
                using var graphics = b.CreateGraphics(); hdcDpi = graphics.DpiX;
                var measured = TextRenderer.MeasureText(graphics, b.Text, b.Font, new Size(int.MaxValue, int.MaxValue), TextFormatFlags.SingleLine);
                textWidth = measured.Width; textHeight = measured.Height;
            }
            int border = b.FlatStyle == FlatStyle.Flat ? b.FlatAppearance.BorderSize : 1;
            var preferred = b.GetPreferredSize(Size.Empty);
            var native = new NativeRect(); bool hasNative = b.IsHandleCreated && GetWindowRect(b.Handle, out native);
            Point nativeOrigin = hasNative ? form.PointToClient(new(native.Left, native.Top)) : Point.Empty;
            string exclusion = "";
            if (b.Text.Trim() is "..." or "…") exclusion = "browse/ellipsis";
            else if (b.Image != null && string.IsNullOrWhiteSpace(b.Text)) exclusion = "icon-only";
            else for (Control? p = b.Parent; p != null; p = p.Parent)
            {
                if (p is ToolStrip) { exclusion = "toolstrip-host"; break; }
                if (p.GetType().Name == "GXValueField") { exclusion = "value-field-internal"; break; }
            }
            var owner = b.Parent as TableLayoutPanel ?? b.Parent.Parent as TableLayoutPanel;
            var cell = ReferenceEquals(owner, b.Parent) ? (Control)b : b.Parent;
            int row = owner?.GetRow(cell) ?? -1;
            object? target = null;
            try { target = b.GetType().GetProperty("Target")?.GetValue(b); } catch { }
            result.Add(new()
            {
                Name = b.Name, Path = Path(b), Type = b.GetType().Name, ParentPath = Path(b.Parent), ParentType = b.Parent.GetType().Name,
                Text = b.Text, Visible = b.Visible, Enabled = b.Enabled, TabIndex = b.TabIndex, TabStop = b.TabStop,
                OuterX = outer.X, OuterY = outer.Y, Width = b.Width, Height = b.Height, ClientWidth = b.ClientSize.Width, ClientHeight = b.ClientSize.Height,
                NativeExists = hasNative, NativeX = nativeOrigin.X, NativeY = nativeOrigin.Y, NativeWidth = hasNative ? native.Right - native.Left : 0, NativeHeight = hasNative ? native.Bottom - native.Top : 0,
                ParentX = parent.X, ParentY = parent.Y, ParentWidth = b.Parent.Width, ParentHeight = b.Parent.Height,
                VisibleWidth = b.Visible ? Math.Max(0, visible.Width) : 0, VisibleHeight = b.Visible ? Math.Max(0, visible.Height) : 0, HasScrollableAncestor = scroll,
                Dpi = b.DeviceDpi, HdcDpi = hdcDpi, FontFamily = b.Font.FontFamily.Name, FontPoints = b.Font.SizeInPoints, FontStyle = b.Font.Style.ToString(),
                ForeColor = b.ForeColor.ToArgb(), BackColor = b.BackColor.ToArgb(), PaddingLeft = b.Padding.Left, PaddingTop = b.Padding.Top, PaddingRight = b.Padding.Right, PaddingBottom = b.Padding.Bottom,
                MarginLeft = b.Margin.Left, MarginTop = b.Margin.Top, MarginRight = b.Margin.Right, MarginBottom = b.Margin.Bottom,
                AutoSize = b.AutoSize, AutoEllipsis = b.AutoEllipsis, Anchor = b.Anchor.ToString(), Dock = b.Dock.ToString(), FlatStyle = b.FlatStyle.ToString(), BorderSize = border,
                UseCompatibleTextRendering = b.UseCompatibleTextRendering, UseMnemonic = b.UseMnemonic, TextImageRelation = b.TextImageRelation.ToString(), TextAlign = b.TextAlign.ToString(), ImageAlign = b.ImageAlign.ToString(), HasImage = b.Image != null,
                AmbientSingleWidth = ambientSingle.Width, NativeAdapterUnboundedWidth = adapterUnbounded.Width, NativeAdapterBoundedHeight = adapterBounded.Height, TextWidth = textWidth, TextHeight = textHeight, TextRequiredWidth = textWidth + b.Padding.Horizontal + 2 * border,
                TextWithReserveWidth = textWidth + b.Padding.Horizontal + 2 * border + (int)Math.Ceiling(8 * b.DeviceDpi / 96d), PreferredWidth = preferred.Width, PreferredHeight = preferred.Height,
                ActionIndex = Property(b, "Index"), Action = Property(b, "Action"), TargetType = target?.GetType().FullName ?? "", TargetLogicalName = Property(target, "LogicalName"),
                ControlIdentity = RuntimeHelpers.GetHashCode(b), OriginalInstance = Originals.TryGetValue(form, out var originals) && originals.ContainsKey(b),
                TablePath = owner == null ? "" : Path(owner), TableRow = row, TableRowHeight = row >= 0 && owner != null && row < owner.GetRowHeights().Length ? owner.GetRowHeights()[row] : 0,
                PeerButtons = b.Parent.Controls.OfType<Button>().Select(p => p.Name).ToArray(), FlowDirection = b.Parent is FlowLayoutPanel flow ? flow.FlowDirection.ToString() : "", WrapContents = b.Parent is FlowLayoutPanel wrap && wrap.WrapContents,
                Exclusion = exclusion, AcceptButton = ReferenceEquals(form.AcceptButton, b), CancelButton = ReferenceEquals(form.CancelButton, b)
            });
        }
        return result;
    }
    [StructLayout(LayoutKind.Sequential)] struct NativeRect { public int Left, Top, Right, Bottom; }
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)] static extern bool GetWindowRect(IntPtr window, out NativeRect rect);
}

internal sealed class ButtonAuditCheck
{
    public string Name { get; set; } = ""; public bool Passed { get; set; } public string Detail { get; set; } = "";
}

internal sealed class ButtonAuditMetric
{
    public string Name { get; set; } = ""; public string Path { get; set; } = ""; public string Type { get; set; } = ""; public string Text { get; set; } = "";
    public string ParentPath { get; set; } = ""; public string ParentType { get; set; } = ""; public bool Visible { get; set; } public bool Enabled { get; set; } public bool TabStop { get; set; } public int TabIndex { get; set; }
    public int OuterX { get; set; } public int OuterY { get; set; } public int Width { get; set; } public int Height { get; set; } public int ClientWidth { get; set; } public int ClientHeight { get; set; }
    public bool NativeExists { get; set; } public int NativeX { get; set; } public int NativeY { get; set; } public int NativeWidth { get; set; } public int NativeHeight { get; set; }
    public int ParentX { get; set; } public int ParentY { get; set; } public int ParentWidth { get; set; } public int ParentHeight { get; set; } public int VisibleWidth { get; set; } public int VisibleHeight { get; set; } public bool HasScrollableAncestor { get; set; }
    public int Dpi { get; set; } public float HdcDpi { get; set; } public string FontFamily { get; set; } = ""; public float FontPoints { get; set; } public string FontStyle { get; set; } = ""; public int ForeColor { get; set; } public int BackColor { get; set; }
    public int PaddingLeft { get; set; } public int PaddingTop { get; set; } public int PaddingRight { get; set; } public int PaddingBottom { get; set; } public int MarginLeft { get; set; } public int MarginTop { get; set; } public int MarginRight { get; set; } public int MarginBottom { get; set; }
    public bool AutoSize { get; set; } public bool AutoEllipsis { get; set; } public string Anchor { get; set; } = ""; public string Dock { get; set; } = ""; public string FlatStyle { get; set; } = ""; public int BorderSize { get; set; }
    public bool UseCompatibleTextRendering { get; set; } public bool UseMnemonic { get; set; } public string TextImageRelation { get; set; } = ""; public string TextAlign { get; set; } = ""; public string ImageAlign { get; set; } = ""; public bool HasImage { get; set; }
    public int AmbientSingleWidth { get; set; } public int NativeAdapterUnboundedWidth { get; set; } public int NativeAdapterBoundedHeight { get; set; } public int TextWidth { get; set; } public int TextHeight { get; set; } public int TextRequiredWidth { get; set; } public int TextWithReserveWidth { get; set; } public int PreferredWidth { get; set; } public int PreferredHeight { get; set; }
    public string ActionIndex { get; set; } = ""; public string Action { get; set; } = ""; public string TargetType { get; set; } = ""; public string TargetLogicalName { get; set; } = ""; public int ControlIdentity { get; set; } public bool OriginalInstance { get; set; }
    public string TablePath { get; set; } = ""; public int TableRow { get; set; } public int TableRowHeight { get; set; } public string[] PeerButtons { get; set; } = []; public string FlowDirection { get; set; } = ""; public bool WrapContents { get; set; }
    public string Exclusion { get; set; } = ""; public bool AcceptButton { get; set; } public bool CancelButton { get; set; }
}
