// superintelligentmeter contributors, 2026-10-29. Bounded native dialog layouts. GPL-2.0-only.
using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace Gurux.DLMS.UI
{
    // Retain the designer's controls and handlers; let rows and footers measure
    // themselves instead of stretching old absolute coordinates after font changes.
    internal static class NativeDialogLayout
    {
        private static int S(Control c, int value) => (int)Math.Round(value * c.DeviceDpi / 96d);
        private static Control Find(Control c, string name) => c.Controls.Find(name, true).Single();
        internal static void Apply(Form form)
        {
            form.SuspendLayout();
            try
            {
                var controls = form.Controls.Cast<Control>().SelectMany(Flatten).Where(c => !string.IsNullOrEmpty(c.Name)).GroupBy(c => c.Name).ToDictionary(g => g.Key, g => g.First());
                Control C(string name) => controls[name];
                var rows = Rows(form);
                switch (form.GetType().Name)
                {
                    case "GXDLMSKeyTableDlg":
                        Row(rows, C("IDLbl"), C("IdTb")); Row(rows, C("KeyLbl"), C("KeyTb")); break;
                    case "GXDLMSRoutingTableDlg":
                        foreach (string prefix in new[] { "Destination", "NextHop", "RouteCost", "HopCount", "WeakLinkCount", "ValidTime" }) Row(rows, C(prefix + "Lbl"), C(prefix + "Tb"));
                        break;
                    case "GXDLMSNeighbourTableDlg":
                        Row(rows, C("AddressLbl"), C("AddressTb"));
                        Row(rows, null, controls.Values.OfType<CheckBox>().Single());
                        foreach (string prefix in new[] { "ToneMap", "Modulation", "TxGain", "TxGainResolution", "TransmitterGain", "LinkQualityIndicator", "PhaseDifference", "TMRValidTime", "ValidTime" })
                            Row(rows, C(prefix + "Lbl"), C(prefix + (prefix == "Modulation" || prefix == "TxGainResolution" ? "Cb" : "Tb")));
                        break;
                    case "GXRegisterMonitorThresholdDlg":
                        Row(rows, C("ValueLbl"), C("ValueTb")); Row(rows, C("ParameterTypeLbl"), C("ValueTypeTb")); break;
                    case "GXRegisterMonitorActionsDlg":
                        foreach (string prefix in new[] { "Up", "Down" })
                        {
                            var group = (GroupBox)C(prefix + "NameCb").Parent;
                            var nested = Rows(group);
                            Row(nested, C(prefix + "NameLbl"), C(prefix + "NameCb"));
                            Row(nested, C(prefix + "SelectorLbl"), C(prefix + "SelectorTb"));
                            group.Controls.Clear(); group.Controls.Add(nested);
                            group.AutoSize = true; group.AutoSizeMode = AutoSizeMode.GrowAndShrink;
                            group.Dock = DockStyle.Fill; group.Padding = new Padding(S(group, 10));
                            Span(rows, group);
                        }
                        break;
                    default: throw new ArgumentException("Unsupported dialog layout", nameof(form));
                }
                Span(rows, Footer(form, (Button)C("OkBtn"), (Button)C("CancelBtn")));
                form.Controls.Clear();
                form.Padding = new Padding(S(form, 12));
                form.AutoScaleMode = AutoScaleMode.Dpi;
                form.AutoScaleDimensions = new SizeF(form.DeviceDpi, form.DeviceDpi);
                form.AutoSize = true; form.AutoSizeMode = AutoSizeMode.GrowAndShrink;
                rows.MinimumSize = new Size(S(form, 440), 0);
                form.MinimumSize = new Size(S(form, 480), 0);
                form.ClientSize = new Size(S(form, 464), S(form, 120));
                form.Controls.Add(rows);
                form.AcceptButton = (IButtonControl)C("OkBtn");
                form.CancelButton = (IButtonControl)C("CancelBtn");
            }
            finally { form.ResumeLayout(true); }
        }
        private static System.Collections.Generic.IEnumerable<Control> Flatten(Control c)
        {
            yield return c;
            foreach (Control child in c.Controls) foreach (var nested in Flatten(child)) yield return nested;
        }
        private static TableLayoutPanel Rows(Control parent)
        {
            var rows = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, ColumnCount = 2, Margin = Padding.Empty, Padding = Padding.Empty };
            rows.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize)); rows.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            EditorLayoutPolicy.ConfigurePropertyTable(rows);
            return rows;
        }
        private static void Row(TableLayoutPanel rows, Control caption, Control editor)
        {
            int row = rows.RowCount++;
            rows.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            if (caption != null)
            {
                caption.AutoSize = true; caption.Dock = DockStyle.None; caption.Anchor = AnchorStyles.Left;
                caption.Margin = new Padding(0, S(rows, 8), S(rows, 16), S(rows, 8));
                caption.TabIndex = row * 2;
                rows.Controls.Add(caption, 0, row);
            }
            editor.Dock = DockStyle.None; editor.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            editor.Margin = new Padding(0, S(rows, 6), 0, S(rows, 6));
            editor.MinimumSize = new Size(S(rows, 200), 0);
            editor.TabIndex = row * 2 + 1;
            if (caption != null) editor.AccessibleName = caption.Text.TrimEnd(':');
            rows.Controls.Add(editor, 1, row);
        }
        private static void Span(TableLayoutPanel rows, Control content)
        {
            int row = rows.RowCount++; rows.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            content.TabIndex = row * 2;
            rows.Controls.Add(content, 0, row); rows.SetColumnSpan(content, 2);
        }
        private static FlowLayoutPanel Footer(Control parent, params Button[] buttons)
        {
            var footer = new FlowLayoutPanel { AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, WrapContents = true, Margin = new Padding(0, S(parent, 10), 0, 0) };
            foreach (var button in buttons.Reverse())
            {
                button.AutoSize = true; button.AutoSizeMode = AutoSizeMode.GrowAndShrink;
                button.Dock = DockStyle.None; button.Anchor = AnchorStyles.None;
                button.MinimumSize = new Size(S(parent, 80), S(parent, 30));
                button.Margin = new Padding(S(parent, 8), 0, 0, 0);
                button.TabIndex = Array.IndexOf(buttons, button);
                footer.Controls.Add(button);
            }
            return footer;
        }
        internal static void CertificateGeneration(Form form)
        {
            form.SuspendLayout();
            try
            {
                var kind = (GroupBox)Find(form, "groupBox3");
                var generate = (GroupBox)Find(form, "groupBox2");
                var request = (GroupBox)Find(form, "groupBox1");
                var choices = kind.Controls.Cast<Control>().OrderBy(c => c.TabIndex).ToArray();
                var types = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Top, WrapContents = true };
                foreach (Control choice in choices) { choice.Dock = DockStyle.None; choice.AutoSize = true; choice.Margin = new Padding(S(form, 4), S(form, 4), S(form, 12), S(form, 4)); types.Controls.Add(choice); }
                kind.Controls.Clear(); kind.Controls.Add(types);
                kind.AutoSize = true; kind.AutoSizeMode = AutoSizeMode.GrowAndShrink; kind.Dock = DockStyle.Fill;
                var fields = Rows(generate);
                Row(fields, Find(generate, "label1"), Find(generate, "SystemTitleTb"));
                Span(fields, Footer(generate, (Button)Find(generate, "CSRBtn")));
                generate.Controls.Clear(); generate.Controls.Add(fields);
                generate.AutoSize = true; generate.AutoSizeMode = AutoSizeMode.GrowAndShrink; generate.Dock = DockStyle.Fill;
                var text = (TextBox)Find(request, "Pkcs10Tb"); text.Dock = DockStyle.Fill;
                request.Dock = DockStyle.Fill; request.MinimumSize = new Size(0, S(form, 120));
                var accept = form.AcceptButton; var cancel = form.CancelButton;
                var footer = Footer(form, (Button)Find(form, "LoadBtn"), (Button)Find(form, "SaveBtn"), (Button)Find(form, "OKBtn"), (Button)Find(form, "CancelBtn"));
                var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 4 };
                root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
                root.RowStyles.Add(new RowStyle(SizeType.AutoSize)); root.RowStyles.Add(new RowStyle(SizeType.AutoSize)); root.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                root.Controls.Add(kind, 0, 0); root.Controls.Add(generate, 0, 1); root.Controls.Add(request, 0, 2); root.Controls.Add(footer, 0, 3);
                form.Controls.Clear(); form.Padding = new Padding(S(form, 12)); form.Controls.Add(root);
                form.MinimumSize = new Size(S(form, 460), S(form, 440));
                form.AcceptButton = accept; form.CancelButton = cancel;
            }
            finally { form.ResumeLayout(true); }
        }
    }
}
