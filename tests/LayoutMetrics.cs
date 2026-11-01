// superintelligentmeter contributors, 2026-10-30. Offline width/appearance evidence. GPL-2.0-only.
#nullable enable
using System.Drawing;
using System.Windows.Forms;
using System.Reflection;
using Gurux.DLMS.UI;
using Gurux.DLMS.Enums;

internal static class LayoutMetrics
{
    static readonly PropertyInfo? StructuredProperty = typeof(GXValueField).GetProperty("RequiresStructuredLayout", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
    internal static IEnumerable<Control> Children(Control parent)
    {
        foreach (Control child in parent.Controls) { yield return child; foreach (var nested in Children(child)) yield return nested; }
    }
    internal static string Path(Control c) => (c.Parent == null ? "" : Path(c.Parent) + "/") + (string.IsNullOrEmpty(c.Name) ? c.GetType().Name + "[" + (c.Parent?.Controls.GetChildIndex(c) ?? 0) + "]" : c.Name);
    internal static string Role(Control c)
    {
        for (Control? p = c.Parent; p != null; p = p.Parent)
            if (p is GXValueField || p.GetType().Name == "GXObjectReferenceView") return "Internal";
        if (c is GXValueField field)
        {
            if (StructuredProperty?.GetValue(field) is true) return "Structured";
            if (field.Type.ToString() is "Xml" or "ListBox" or "CheckedListBox") return "Structured";
            try { if (field.Index > 0 && field.Target?.GetDataType(field.Index) is DataType.Array or DataType.Structure) return "Structured"; } catch { }
            return "Scalar";
        }
        if (c.GetType().Name == "GXObjectReferenceView") return "Reference";
        if (c is ListView or DataGridView or TreeView or ListBox or RichTextBox) return "FlexibleData";
        if (c is TextBox box && box.Multiline) return "Structured";
        if (c is TextBoxBase or ComboBox or NumericUpDown or DateTimePicker) return "Scalar";
        if (c is Label) return "Caption";
        if (c is CheckBox or RadioButton) return "Choice";
        if (c is ButtonBase) return "Action";
        if (c is TableLayoutPanel) return "Region";
        return "Container";
    }
    internal static List<LayoutMetric> Measure(Form form)
    {
        var all = Children(form).Where(c => c.Visible).ToArray();
        var result = new List<LayoutMetric>();
        foreach (Control c in all)
        {
            var origin = form.PointToClient(c.PointToScreen(Point.Empty));
            Rectangle visible = c.RectangleToScreen(c.ClientRectangle);
            bool scrolling = false;
            for (Control? p = c.Parent; p != null; p = p.Parent)
            {
                visible.Intersect(p.RectangleToScreen(p.ClientRectangle));
                scrolling |= p is ScrollableControl sc && (sc.VerticalScroll.Visible || sc.HorizontalScroll.Visible);
            }
            string dataType = "";
            if (c is GXValueField field) try { if (field.Index > 0) dataType = field.Target?.GetDataType(field.Index).ToString() ?? ""; } catch { }
            bool captionClip = c is Label label && !label.AutoEllipsis && TextRenderer.MeasureText(label.Text, label.Font,
                new Size(Math.Max(1, label.ClientSize.Width), 32767), TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix).Height > label.ClientSize.Height + 2;
            var metric = new LayoutMetric
            {
                Name = c.Name, Path = Path(c), ParentPath = c.Parent == null ? "" : Path(c.Parent), Type = c.GetType().Name,
                Role = Role(c), DataType = dataType, Text = c.Text, X = origin.X, Y = origin.Y, Width = c.Width, Height = c.Height, ClientWidth = c.ClientSize.Width, ClientHeight = c.ClientSize.Height,
                Left = c.Left, Top = c.Top, ParentWidth = c.Parent?.ClientSize.Width ?? 0, ParentHeight = c.Parent?.ClientSize.Height ?? 0,
                Dpi = c.DeviceDpi, MaximumWidth = c.MaximumSize.Width, MinimumWidth = c.MinimumSize.Width,
                FontFamily = c.Font.FontFamily.Name, FontPoints = c.Font.SizeInPoints, FontStyle = c.Font.Style.ToString(),
                ForeColor = c.ForeColor.ToArgb(), BackColor = c.BackColor.ToArgb(), FontHeight = c.Font.Height,
                VisibleWidth = Math.Max(0, visible.Width), VisibleHeight = Math.Max(0, visible.Height), HasScrollableAncestor = scrolling,
                CaptionClip = captionClip, Enabled = c.Enabled, Dock = c.Dock.ToString(), Anchor = c.Anchor.ToString(),
                Multiline = c is TextBox tb && tb.Multiline, WordWrap = c is TextBox tb2 && tb2.WordWrap,
                ScrollBars = c is TextBox tb3 ? tb3.ScrollBars.ToString() : "", AttributeIndex = c is GXValueField vf ? vf.Index : 0
            };
            metric.MarginLeft = c.Margin.Left; metric.MarginRight = c.Margin.Right; metric.PaddingLeft = c.Padding.Left; metric.PaddingRight = c.Padding.Right;
            if (c.Parent is TableLayoutPanel owner) { metric.TableColumn = owner.GetColumn(c); metric.TableRow = owner.GetRow(c); metric.TableColumnSpan = owner.GetColumnSpan(c); }
            if (c is TableLayoutPanel table)
            {
                metric.ColumnWidths = table.GetColumnWidths();
                metric.ColumnStyles = table.ColumnStyles.Cast<ColumnStyle>().Select(s => s.SizeType + ":" + s.Width).ToArray();
                metric.RowHeights = table.GetRowHeights();
                metric.RowStyles = table.RowStyles.Cast<RowStyle>().Select(s => s.SizeType + ":" + s.Height).ToArray();
                metric.HasFlexibleContent = Children(c).Any(x => x.Visible && Role(x) is "Structured" or "FlexibleData" or "Reference");
            }
            if (c is ComboBox combo) { metric.NativePreferredHeight = combo.PreferredHeight; metric.NativeStyle = combo.DropDownStyle + "/" + combo.FlatStyle; }
            result.Add(metric);
        }
        return result;
    }
}
internal sealed class LayoutMetric
{
    public string Name { get; set; } = ""; public string Path { get; set; } = ""; public string ParentPath { get; set; } = "";
    public string Type { get; set; } = ""; public string Role { get; set; } = ""; public string DataType { get; set; } = ""; public string Text { get; set; } = "";
    public int X { get; set; } public int Y { get; set; } public int Width { get; set; } public int Height { get; set; } public int Left { get; set; } public int Top { get; set; }
    public int ClientWidth { get; set; } public int ClientHeight { get; set; }
    public int ParentWidth { get; set; } public int ParentHeight { get; set; } public int Dpi { get; set; } public int MaximumWidth { get; set; } public int MinimumWidth { get; set; }
    public string FontFamily { get; set; } = ""; public float FontPoints { get; set; } public string FontStyle { get; set; } = ""; public int ForeColor { get; set; } public int BackColor { get; set; } public int FontHeight { get; set; }
    public int VisibleWidth { get; set; } public int VisibleHeight { get; set; } public bool HasScrollableAncestor { get; set; } public bool CaptionClip { get; set; } public bool Enabled { get; set; }
    public string Dock { get; set; } = ""; public string Anchor { get; set; } = ""; public bool Multiline { get; set; } public bool WordWrap { get; set; } public string ScrollBars { get; set; } = ""; public int AttributeIndex { get; set; }
    public int[] ColumnWidths { get; set; } = []; public string[] ColumnStyles { get; set; } = []; public bool HasFlexibleContent { get; set; } public int NativePreferredHeight { get; set; } public string NativeStyle { get; set; } = "";
    public int[] RowHeights { get; set; } = []; public string[] RowStyles { get; set; } = [];
    public int MarginLeft { get; set; } public int MarginRight { get; set; } public int PaddingLeft { get; set; } public int PaddingRight { get; set; } public int TableColumn { get; set; } = -1; public int TableRow { get; set; } = -1; public int TableColumnSpan { get; set; } = 1;
}
