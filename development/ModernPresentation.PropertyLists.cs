// superintelligentmeter contributors, 2026-10-30. Shared aligned scalar sections with an expanding object list. GPL-2.0-only.
// SPDX-License-Identifier: GPL-2.0-only
using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace Gurux.DLMS.UI
{
    public static partial class ModernPresentation
    {
        private static void ReflowActionSchedule(Form form) => ReflowPropertyList(form,
            "ActionSchedule", "Time", null, new[] { "TimeAddBtn", "TimeEditBtn", "TimeRemoveBtn" },
            new[] { "groupBox1" }, new[] { "LogicalNameLbl", "LogicalNameTB" },
            new[] { "groupBox2" }, new[] { "ScriptNameLbl", "ScriptNameTB" },
            new[] { "ScriptSelectorLbl", "ScriptSelectorTB" }, new[] { "ScriptTypeLbl", "ScriptTypeTb" });

        private static void ReflowParameterMonitor(Form form) => ReflowPropertyList(form,
            "ParameterMonitor", "ParametersLV", "ParametersLbl", new[] { "AddBtn", "RemoveBtn" },
            new[] { "groupBox1" }, new[] { "LogicalNameLbl", "LogicalNameTB" },
            new[] { "ChangedParameterGB" }, new[] { "TargetLbl", "TargetTb" },
            new[] { "IndexLbl", "IndexTb" }, new[] { "ValueLbl", "ValueTb" },
            new[] { "CaptureTimeLbl", "CaptureTimeTb" });

        private static void ReflowPropertyList(Form form, string surfaceName, string listName,
            string listCaptionName, string[] buttonNames, params string[][] rows)
        {
            var oldGroup = Find(form, "groupBox1");
            var controls = Descendants(oldGroup).Where(c => !string.IsNullOrEmpty(c.Name))
                .GroupBy(c => c.Name).ToDictionary(items => items.Key, items => items.First());
            controls[oldGroup.Name] = oldGroup;
            form.SuspendLayout();
            try
            {
                form.AutoScaleMode = AutoScaleMode.Dpi;
                form.Font = BodyFont;
                form.BackColor = Color.White;
                form.Padding = new Padding(Scale(form, 16));
                form.AutoScroll = true;
                form.AutoScrollMinSize = Size.Empty;
                int listRow = listCaptionName == null ? 1 : 2;
                var root = new TableLayoutPanel
                {
                    Name = surfaceName + "Content", Dock = DockStyle.Top, ColumnCount = 1, RowCount = listRow + 2,
                    BackColor = Color.White, Margin = Padding.Empty, Padding = Padding.Empty
                };
                root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
                root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                if (listCaptionName != null) root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
                root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                form.Controls.Add(root);
                // Related scalar sections share one grid. A semantic section heading
                // must not introduce another horizontal inset for its property rows.
                var properties = new TableLayoutPanel
                {
                    Name = surfaceName + "Properties", Dock = DockStyle.Top, AutoSize = true,
                    AutoSizeMode = AutoSizeMode.GrowAndShrink, ColumnCount = 2, RowCount = 0,
                    BackColor = Color.White, Margin = new Padding(0, 0, 0, Scale(form, 8)), TabIndex = 0
                };
                properties.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, Scale(form, EditorLayoutPolicy.LabelColumnWidth)));
                properties.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
                root.Controls.Add(properties, 0, 0);
                void Heading(string text)
                {
                    int row = properties.RowCount++;
                    properties.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                    var heading = new Label { Text = text, AutoSize = true, Font = BodyFont, ForeColor = Ink,
                        Margin = new Padding(0, row == 0 ? 0 : Scale(form, 12), 0, Scale(form, 8)) };
                    properties.Controls.Add(heading, 0, row);
                    properties.SetColumnSpan(heading, 2);
                }
                void Property(string captionName, string fieldName)
                {
                    int row = properties.RowCount++;
                    properties.RowStyles.Add(new RowStyle(SizeType.Absolute, Scale(form, 40)));
                    Control caption = controls[captionName], field = controls[fieldName];
                    caption.Font = field.Font = BodyFont;
                    caption.TabIndex = row * 2;
                    int tabIndex = row * 2 + 1;
                    field.TabIndex = tabIndex;
                    field.AccessibleName = caption.Text;
                    if (field is GXValueField value) value.ApplyModernStyle(compact: true);
                    Place(caption, properties, 0, row);
                    var cell = Place(field, properties, 1, row);
                    cell.TabIndex = tabIndex;
                }
                foreach (var row in rows)
                {
                    if (row.Length == 1) Heading(controls[row[0]].Text);
                    else Property(row[0], row[1]);
                }
                EditorLayoutPolicy.ConfigurePropertyTable(properties);
                if (listCaptionName != null)
                {
                    var caption = (Label)controls[listCaptionName];
                    caption.Font = BodyFont;
                    caption.ForeColor = Muted;
                    caption.AutoSize = true;
                    caption.Dock = DockStyle.Fill;
                    caption.TextAlign = ContentAlignment.TopLeft;
                    caption.Margin = new Padding(0, 0, 0, Scale(form, 8));
                    caption.TabIndex = 1;
                    root.Controls.Add(caption, 0, 1);
                }
                var list = (ListView)controls[listName];
                list.Font = BodyFont;
                list.BackColor = Color.White;
                list.ForeColor = Ink;
                list.BorderStyle = BorderStyle.FixedSingle;
                list.Dock = DockStyle.Fill;
                list.Margin = Padding.Empty;
                list.TabIndex = listRow;
                root.Controls.Add(list, 0, listRow);
                void FitListColumns()
                {
                    if (list.Columns.Count == 0) return;
                    int available = Math.Max(1, list.ClientSize.Width - SystemInformation.VerticalScrollBarWidth - Scale(list, 4));
                    // A reference/name uses the free width; short index columns keep
                    // their content-sized allocation instead of taking half the list.
                    int trailing = 0;
                    for (int column = 1; column < list.Columns.Count; ++column)
                    {
                        int width = Math.Max(Scale(list, 90), TextRenderer.MeasureText(list.Columns[column].Text, list.Font).Width + Scale(list, 18));
                        list.Columns[column].Width = width;
                        trailing += width;
                    }
                    list.Columns[0].Width = Math.Max(Scale(list, 120), available - trailing);
                }
                list.SizeChanged += (_, _) => FitListColumns();
                list.DpiChangedAfterParent += (_, _) => FitListColumns();
                var actions = new ActionButtonPanel
                {
                    Dock = DockStyle.Fill, AutoSize = true, FlowDirection = FlowDirection.RightToLeft,
                    WrapContents = true, BackColor = Color.White, Margin = Padding.Empty,
                    Padding = new Padding(0, Scale(form, 8), 0, 0), TabIndex = listRow + 1
                };
                foreach (string name in buttonNames.Reverse())
                {
                    var button = (Button)controls[name];
                    CompactButton(button);
                    StyleButton(button, false);
                    button.Font = BodyFont;
                    button.AutoSize = false;
                    button.Size = button.MinimumSize = button.MaximumSize = new Size(Scale(form, 100), Scale(form, 32));
                    button.Margin = Padding.Empty;
                    button.TabIndex = Array.IndexOf(buttonNames, name);
                    actions.Controls.Add(button);
                }
                ButtonLayoutPolicy.Apply(actions, buttonNames.Select(name => (Button)controls[name]).ToArray());
                root.Controls.Add(actions, 0, listRow + 1);
                form.Controls.Remove(oldGroup);
                oldGroup.Dispose();
                bool fitting = false;
                bool layoutPending = false;
                void QueueLayout()
                {
                    if (layoutPending || root.IsDisposed || !root.IsHandleCreated) return;
                    layoutPending = true;
                    root.BeginInvoke((Action)(() =>
                    {
                        layoutPending = false;
                        if (!root.IsDisposed) root.PerformLayout();
                    }));
                }
                void FitHeight()
                {
                    if (fitting || root.IsDisposed || (form.TopLevelControl as Form)?.WindowState == FormWindowState.Minimized) return;
                    fitting = true;
                    try
                    {
                        int width = Math.Max(1, root.ClientSize.Width);
                        int header = properties.GetPreferredSize(new Size(width, 0)).Height + properties.Margin.Vertical;
                        int footer = actions.GetPreferredSize(new Size(width, 0)).Height;
                        int captionHeight = listCaptionName == null ? 0 :
                            controls[listCaptionName].GetPreferredSize(new Size(width, 0)).Height + controls[listCaptionName].Margin.Vertical;
                        int height = Math.Max(form.ClientSize.Height - form.Padding.Vertical, header + captionHeight + Scale(form, 150) + footer);
                        if (root.Height != height)
                        {
                            root.Height = height;
                            // Percent rows can retain their previous allocation when
                            // this runs inside the parent layout transaction.
                            QueueLayout();
                        }
                    }
                    finally { fitting = false; }
                }
                form.SizeChanged += (_, _) => FitHeight();
                root.SizeChanged += (_, _) => FitHeight();
                properties.SizeChanged += (_, _) => FitHeight();
                actions.SizeChanged += (_, _) => FitHeight();
                root.DpiChangedAfterParent += (_, _) => { FitHeight(); QueueLayout(); };
                root.HandleCreated += (_, _) => QueueLayout();
                FitHeight();
                FitListColumns();
            }
            finally { form.ResumeLayout(true); }
        }

    }
}
