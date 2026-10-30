// superintelligentmeter contributors, 2026-10-29. Responsive native editor rows. GPL-2.0-only.
using System;
using System.Drawing;
using System.Windows.Forms;
using Gurux.DLMS.Enums;

namespace Gurux.DLMS.UI
{
    public static partial class ModernPresentation
    {
        private static void ConfigureCompactSurface(TableLayoutPanel table)
        {
            EditorLayoutPolicy.ConfigurePropertyTable(table);
        }

        // The existing value-field instance changes presentation when its bound data type
        // changes. Keep the surrounding row in sync without replacing the editor/binding.
        private static void ConfigureValueRow(TableLayoutPanel table, int row, Label caption, GXValueField field, int logicalHeight)
        {
            void FitValue()
            {
                if (table.IsDisposed || field.IsDisposed || row >= table.RowStyles.Count) return;
                int height = field.RequiresStructuredLayout ? Math.Max(logicalHeight, 160)
                    : field.Type is ValueFieldType.ListBox or ValueFieldType.CheckedListBox ? Math.Max(logicalHeight, 120) : logicalHeight;
                int pixels = Scale(table, height);
                if (caption.Width > 0)
                    pixels = Math.Max(pixels, TextRenderer.MeasureText(caption.Text, caption.Font,
                        new Size(Math.Max(1, caption.Width - caption.Padding.Horizontal), 0),
                        TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix).Height + caption.Margin.Vertical + caption.Padding.Vertical);
                if (field.MaximumSize.Height != 0) field.MaximumSize = new Size(field.MaximumSize.Width, 0);
                caption.TextAlign = height > 60 ? ContentAlignment.TopLeft : ContentAlignment.MiddleLeft;
                caption.Padding = height > 60 ? new Padding(0, Scale(table, 10), 0, 0) : Padding.Empty;
                if (Math.Abs(table.RowStyles[row].Height - pixels) > 1) table.RowStyles[row].Height = pixels;
            }
            field.PresentationLayoutChanged += (_, _) => FitValue();
            table.DpiChangedAfterParent += (_, _) => FitValue();
            FitValue();
        }

        private sealed class CompactActionRow : Panel
        {
            private readonly Control field;
            private readonly Control action;
            private readonly TableLayoutPanel table;
            private readonly int row;
            private readonly int logicalHeight;
            private bool arranging;
            private bool parentLayoutQueued;
            internal CompactActionRow(TableLayoutPanel table, int row, Control field, Control action, int logicalHeight)
            {
                this.table = table; this.row = row; this.field = field; this.action = action; this.logicalHeight = logicalHeight;
                Dock = DockStyle.Fill; Margin = Padding.Empty; TabStop = false;
                field.Dock = action.Dock = DockStyle.None;
                field.Margin = action.Margin = Padding.Empty;
                Controls.Add(field); Controls.Add(action);
                field.TabIndex = 0; action.TabIndex = 1;
                action.SizeChanged += (_, _) => PerformLayout();
            }
            protected override void OnLayout(LayoutEventArgs e)
            {
                base.OnLayout(e);
                if (arranging || field == null || action == null || table == null || row >= table.RowStyles.Count) return;
                arranging = true;
                try
                {
                    int inset = ModernPresentation.Scale(this, 4), gap = ModernPresentation.Scale(this, 12), baseHeight = ModernPresentation.Scale(this, logicalHeight);
                    int actionWidth = Math.Min(ClientSize.Width, Math.Max(action.Width, action.GetPreferredSize(Size.Empty).Width));
                    bool stacked = ClientSize.Width < actionWidth + gap + ModernPresentation.Scale(this, 180);
                    int actionHeight = Math.Max(ModernPresentation.Scale(this, 32), action.GetPreferredSize(new Size(actionWidth, 0)).Height);
                    int height = stacked ? baseHeight + actionHeight + inset : Math.Max(baseHeight, actionHeight + inset * 2);
                    if (Math.Abs(table.RowStyles[row].Height - height) > 1)
                    {
                        table.RowStyles[row].Height = height;
                        // The table may already be arranging this child with cached row
                        // bounds. Finish that pass before applying the new stacked height.
                        if (!parentLayoutQueued && table.IsHandleCreated)
                        {
                            parentLayoutQueued = true;
                            table.BeginInvoke((Action)(() =>
                            {
                                parentLayoutQueued = false;
                                if (!table.IsDisposed && !IsDisposed) table.PerformLayout();
                            }));
                        }
                    }
                    field.Bounds = new Rectangle(0, inset, Math.Max(1, stacked ? ClientSize.Width : ClientSize.Width - actionWidth - gap), baseHeight - inset * 2);
                    action.Bounds = new Rectangle(stacked ? 0 : ClientSize.Width - actionWidth, stacked ? baseHeight : inset, actionWidth, actionHeight);
                }
                finally { arranging = false; }
            }
        }
    }
}
