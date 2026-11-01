// Superintelligent Meter contributors, 2026-10-30. Shared native property layout. GPL-2.0-only.
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Forms;

namespace Gurux.DLMS.UI
{
    /// <summary>Current-DPI dimensions for property forms. Never scales fonts or replaces bound editors.</summary>
    public static class EditorLayoutPolicy
    {
        public const int SectionWidth = 800;
        public const int LabelColumnWidth = 144;
        public const int MinimumLabelColumnWidth = 100;
        public const int FieldWidth = 640;
        private static readonly ConditionalWeakTable<TableLayoutPanel, PropertyTable> Tables = new();
        private static readonly ConditionalWeakTable<Control, object> Sections = new();
        private static int S(Control c, int value) => (int)Math.Round(value * c.DeviceDpi / 96.0);

        private static void SetWidthLimit(Control control, int width)
        {
            // ComboBox deliberately strips MaximumSize.Height. Its surrounding
            // row must reserve PreferredHeight explicitly instead of centering a zero-height constraint.
            if (control is ComboBox)
            {
                if (control.MaximumSize.Width != width) control.MaximumSize = new Size(width, 0);
                return;
            }
            int height = control.MaximumSize.Height;
            if (control is TextBox text && !text.Multiline) height = text.PreferredHeight;
            else if (height == 0 && width > 0)
                // A width-only native maximum can collapse preferred heights or clear
                // intrinsic minima. Keep vertical growth effectively unbounded.
                height = S(control, 32767);
            var size = new Size(width, height);
            if (control.MaximumSize != size) control.MaximumSize = size;
        }

        /// <summary>Caps a compound property region without changing its internal columns.</summary>
        public static void ConstrainSection(Control region)
        {
            if (Sections.TryGetValue(region, out _)) return;
            Sections.Add(region, new object());
            void Fit() { if (!region.IsDisposed) SetWidthLimit(region, S(region, SectionWidth)); }
            region.DpiChangedAfterParent += (_, _) => Fit();
            region.ParentChanged += (_, _) => Fit();
            Fit();
        }

        /// <summary>Aligns caption/value rows; data surfaces remain flexible. Call once when building a section.</summary>
        public static void ConfigurePropertyTable(TableLayoutPanel table, bool boundSection = true)
        {
            if (!Tables.TryGetValue(table, out var state))
            {
                state = new PropertyTable(table, boundSection);
                Tables.Add(table, state);
            }
            state.BoundSection = boundSection;
            state.Fit();
        }

        /// <summary>Related nested sections keep their owner and share its adaptive caption column.</summary>
        public static void ShareLabelColumn(TableLayoutPanel table, TableLayoutPanel owner)
        {
            if (ReferenceEquals(table, owner)) return;
            if (!Tables.TryGetValue(table, out var state))
            {
                ConfigurePropertyTable(table);
                state = Tables.GetValue(table, _ => throw new InvalidOperationException());
            }
            state.ShareLabelColumn(owner);
        }

        private static bool Flexible(Control control)
        {
            // GXValueField contains dormant list/grid editors. Inspect its active presentation only.
            if (control is GXValueField value) return value.RequiresStructuredLayout;
            if (control is DataGridView || control is ListView || control is TreeView || control is TabControl ||
                control is RichTextBox || control is TextBox text && text.Multiline || control is GXObjectReferenceView) return true;
            if (control is ComboBox || control is TextBoxBase || control is ListBox || control is UpDownBase) return false;
            return control.Controls.Cast<Control>().Any(Flexible);
        }

        private sealed class PropertyTable
        {
            private readonly TableLayoutPanel table;
            private readonly Dictionary<Control, int> originalWidths = new();
            private readonly HashSet<GXValueField> fields = new();
            private bool arranging;
            private readonly Dictionary<RowStyle, MeasuredRow> measuredRows = new();
            private readonly Dictionary<RowStyle, float> nativeRowMinimums = new();
            private sealed class MeasuredRow { internal float LogicalBase; internal float Applied; internal int Dpi; }
            private TableLayoutPanel labelColumnOwner;
            internal bool BoundSection;

            internal void ShareLabelColumn(TableLayoutPanel owner)
            {
                if (ReferenceEquals(labelColumnOwner, owner)) return;
                if (labelColumnOwner != null)
                {
                    labelColumnOwner.Layout -= OwnerLayout;
                    labelColumnOwner.SizeChanged -= OwnerChanged;
                    labelColumnOwner.DpiChangedAfterParent -= OwnerChanged;
                }
                labelColumnOwner = owner;
                if (owner != null)
                {
                    owner.Layout += OwnerLayout;
                    owner.SizeChanged += OwnerChanged;
                    owner.DpiChangedAfterParent += OwnerChanged;
                }
                Fit();
            }

            private void OwnerLayout(object sender, LayoutEventArgs e) => Fit();
            private void OwnerChanged(object sender, EventArgs e) => Fit();
            internal PropertyTable(TableLayoutPanel table, bool boundSection)
            {
                this.table = table; BoundSection = boundSection;
                table.Layout += (_, _) => Fit();
                table.DpiChangedAfterParent += (_, _) => Fit();
                table.FontChanged += (_, _) => Fit();
                table.ParentChanged += (_, _) => Fit();
                table.Disposed += (_, _) => ShareLabelColumn(null);
            }

            private void Observe(Control control)
            {
                if (control is GXValueField value)
                {
                    if (fields.Add(value)) value.PresentationLayoutChanged += (_, _) => { Fit(); table.PerformLayout(); };
                    return;
                }
                foreach (Control child in control.Controls) Observe(child);
            }

            internal void Fit()
            {
                if (arranging || table.IsDisposed || table.ColumnStyles.Count < 2) return;
                arranging = true;
                try
                {
                    var controls = table.Controls.Cast<Control>().ToArray();
                    bool ValidCaption(Control c) => table.GetColumn(c) == 0 && table.GetColumnSpan(c) == 1 &&
                        table.GetRow(c) >= 0 && table.GetRow(c) < table.RowCount &&
                        table.GetControlFromPosition(1, table.GetRow(c)) is Control field && field is not Label;
                    bool hasLabels = controls.OfType<Label>().Any(ValidCaption);
                    bool CaptionGroup(Control c) => c is TableLayoutPanel && c.Controls.Count > 0 &&
                        c.Controls.Cast<Control>().All(child => child is Label || child is CheckBox);
                    var captions = controls.Where(c => ValidCaption(c) && (c is Label || hasLabels &&
                        (c is CheckBox || c is RadioButton || CaptionGroup(c)))).ToArray();
                    // A repeated checkbox/hour/tariff grid is a compound layout, not a property form.
                    if (captions.Length == 0 || table.ColumnCount > 3) return;
                    bool flexible = controls.Any(Flexible);
                    int cap = BoundSection && !flexible ? S(table, SectionWidth) : 0;
                    SetWidthLimit(table, cap);
                    var railOwner = labelColumnOwner is { IsDisposed: false } ? labelColumnOwner : table;
                    int rail = Math.Min(S(railOwner, LabelColumnWidth), Math.Max(S(railOwner, MinimumLabelColumnWidth), railOwner.ClientSize.Width / 3));
                    table.ColumnStyles[0].SizeType = SizeType.Absolute;
                    if (Math.Abs(table.ColumnStyles[0].Width - rail) > 1) table.ColumnStyles[0].Width = rail;
                    foreach (var caption in captions)
                    {
                        int row = table.GetRow(caption);
                        var cell = table.GetControlFromPosition(1, row);
                        Observe(cell);
                        if (!originalWidths.ContainsKey(cell)) originalWidths[cell] = (int)Math.Round(cell.MaximumSize.Width * 96.0 / Math.Max(96, cell.DeviceDpi));
                        bool data = Flexible(cell);
                        int width = data ? S(cell, originalWidths[cell]) : S(cell, FieldWidth);
                        SetWidthLimit(cell, width);
                        // Keep one field edge regardless of the old designer's horizontal cell margins.
                        if (cell.Margin.Left != 0 || cell.Margin.Right != 0) cell.Margin = new Padding(0, cell.Margin.Top, 0, cell.Margin.Bottom);
                        if (caption.AutoSize)
                        {
                            caption.MaximumSize = new Size(Math.Max(1, rail - caption.Margin.Horizontal), 0);
                            caption.MinimumSize = Size.Empty;
                        }
                        else if (caption is Label && row < table.RowStyles.Count && table.RowStyles[row].SizeType == SizeType.Absolute &&
                            cell.GetType().Name != "CompactActionRow")
                        {
                            int textHeight = TextRenderer.MeasureText(caption.Text, caption.Font,
                                new Size(Math.Max(1, rail - caption.Margin.Horizontal - caption.Padding.Horizontal), 0),
                                TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix).Height + caption.Margin.Vertical + caption.Padding.Vertical;
                            var style = table.RowStyles[row];
                            if (!measuredRows.TryGetValue(style, out var measured))
                            {
                                measured = new MeasuredRow { LogicalBase = style.Height * 96f / table.DeviceDpi,
                                    Applied = style.Height, Dpi = table.DeviceDpi };
                                measuredRows.Add(style, measured);
                            }
                            // Respect rows resized by a live structured field, but don't treat our
                            // own wrapped-caption height as a new permanent baseline.
                            float expected = measured.Applied * table.DeviceDpi / measured.Dpi;
                            if (Math.Abs(style.Height - expected) > 1)
                                measured.LogicalBase = style.Height * 96f / table.DeviceDpi;
                            float height = Math.Max(measured.LogicalBase * table.DeviceDpi / 96f, textHeight);
                            measured.Applied = height; measured.Dpi = table.DeviceDpi;
                            if (Math.Abs(style.Height - height) > 1) style.Height = height;
                        }
                        if (cell is ComboBox combo && row < table.RowStyles.Count)
                        {
                            var style = table.RowStyles[row];
                            if (!nativeRowMinimums.TryGetValue(style, out float baseline))
                            {
                                baseline = style.SizeType == SizeType.Absolute ? style.Height * 96f / table.DeviceDpi : 0;
                                nativeRowMinimums.Add(style, baseline);
                            }
                            int captionHeight = TextRenderer.MeasureText(caption.Text, caption.Font,
                                new Size(Math.Max(1, rail - caption.Margin.Horizontal - caption.Padding.Horizontal), 0),
                                TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix).Height + caption.Margin.Vertical + caption.Padding.Vertical;
                            int height = Math.Max((int)Math.Round(baseline * table.DeviceDpi / 96f),
                                Math.Max(captionHeight, combo.PreferredHeight + combo.Margin.Vertical));
                            style.SizeType = SizeType.Absolute;
                            if (Math.Abs(style.Height - height) > 1) style.Height = height;
                            if (combo.Dock == DockStyle.None) combo.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
                        }
                    }
                }
                finally { arranging = false; }
            }
        }
    }
}
