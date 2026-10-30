// superintelligentmeter contributors, 2026-10-30: native dropdown layout without redundant frames. GPL-2.0-only; original notices retained.
// Modified for the Intelmeter fork, 2026-10-16: .NET 10 migration, UI modernization and compatibility fixes. GPL-2.0-only; original notices retained.
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Gurux.DLMS.Enums;

namespace Gurux.DLMS.UI
{
    public static partial class ModernPresentation
    {
        // The remaining objects in the owner's configuration, in navigation order.
        // Existing bound controls, containers and event handlers remain the same instances.
        private static readonly HashSet<string> RemainingModels = new HashSet<string>((
            "GXDLMSAssociationShortName GXDLMSSapAssignment GXDLMSImageTransfer GXDLMSIECLocalPortSetup GXDLMSIECOpticalPortSetup " +
            "GXDLMSRegisterMonitor GXDLMSActionSchedule GXDLMSHdlcSetup GXDLMSIecTwistedPairSetup " +
            "GXDLMSMBusSlavePortSetup GXDLMSUtilityTables GXDLMSModemConfiguration GXDLMSAutoAnswer " +
            "GXDLMSAutoConnect GXDLMSDataProtection GXDLMSPushSetup GXDLMSTcpUdpSetup GXDLMSIp4Setup " +
            "GXDLMSMacAddressSetup GXDLMSPppSetup GXDLMSGprsSetup GXDLMSGSMDiagnostic GXDLMSIp6Setup " +
            "GXDLMSSFSKPhyMacSetUp GXDLMSSFSKActiveInitiator GXDLMSSFSKMacSynchronizationTimeouts " +
            "GXDLMSSFSKMacCounters GXDLMSCompactData GXDLMSSecuritySetup GXDLMSParameterMonitor " +
            "GXDLMSArbitrator GXDLMSDisconnectControl GXDLMSLimiter GXDLMSMBusClient GXDLMSWirelessModeQchannel " +
            "GXDLMSMBusMasterPortSetup GXDLMSMBusDiagnostic GXDLMSLlcSscsSetup GXDLMSPrimeNbOfdmPlcPhysicalLayerCounters " +
            "GXDLMSPrimeNbOfdmPlcMacSetup GXDLMSPrimeNbOfdmPlcMacFunctionalParameters GXDLMSPrimeNbOfdmPlcMacCounters " +
            "GXDLMSPrimeNbOfdmPlcMacNetworkAdministrationData GXDLMSPrimeNbOfdmPlcApplicationsIdentification " +
            "GXDLMSG3PlcMacLayerCounters GXDLMSG3PlcMacSetup GXDLMSG3Plc6LoWPan GXDLMSNtpSetup GXDLMSAccount " +
            "GXDLMSCredit GXDLMSCharge GXDLMSTokenGateway GXDLMSIEC6205541Attributes GXDLMSFunctionControl " +
            "GXDLMSCommunicationPortProtection GXDLMSLteMonitoring GXDLMSTariffPlan").Split(' '));

        private static bool ReflowRemainingObject(Form form)
        {
            if (form is not IGXDLMSView || !form.GetType().GetCustomAttributes(typeof(GXDLMSViewAttribute), true)
                .Cast<GXDLMSViewAttribute>().Any(a => RemainingModels.Contains(a.DLMSType.Name))) return false;
            // Constructor-built approved layouts already use the shared rows.
            if (Descendants(form).Any(c => c is TableLayoutPanel))
            {
                form.AutoScrollMinSize = Size.Empty;
                foreach (var surface in form.Controls.OfType<TableLayoutPanel>()) surface.MinimumSize = Size.Empty;
                return false;
            }
            form.SuspendLayout();
            try
            {
                form.AutoScaleMode = AutoScaleMode.Dpi;
                form.Font = BodyFont;
                form.BackColor = Color.White;
                if (form is GXDLMSG3Plc6LoWPanView)
                {
                    // Routing flags must not change the label column of scalar rows.
                    var flags = new[] { "DisableDefaultRoutingCB", "DefaultCoordRouteEnabledCb" }
                        .Select(name => Descendants(form).First(c => c.Name == name)).ToArray();
                    int top = flags[0].Parent.Controls.Cast<Control>().Max(c => c.Bottom) + Scale(form, 24);
                    for (int i = 0; i < flags.Length; ++i)
                        flags[i].Location = new Point(0, top + Scale(form, i * 40));
                }
                if (form.GetType().Name == "GXDLMSIp6SetupView")
                {
                    var add = Descendants(form).First(c => c.Name == "AddAddressBtn");
                    var remove = Descendants(form).First(c => c.Name == "RemoveAddressBtb");
                    add.Location = new Point(0, remove.Top);
                    remove.Left = Scale(form, 108);
                }
                if (form.GetType().Name == "GXDLMSAutoConnectView")
                {
                    var connect = Descendants(form).First(c => c.Name == "ConnectBtn");
                    var remove = Descendants(form).First(c => c.Name == "TimeRemoveBtn");
                    connect.Location = new Point(remove.Right + Scale(form, 8), remove.Top);
                }
                if (form.GetType().Name == "GXDLMSIecTwistedPairSetupView")
                {
                    // The designer stacks these actions beside each list. Give the
                    // shared layout one ordered footer row for each independent list.
                    foreach (string prefix in new[] { "PrimaryAddresses", "Tabis" })
                    {
                        var actions = new[] { "AddBtn", "EditBtn", "RemoveBtn" }
                            .Select(suffix => Descendants(form).First(c => c.Name == prefix + suffix)).ToArray();
                        int top = actions.Max(c => c.Top);
                        for (int i = 0; i < actions.Length; ++i)
                            actions[i].Location = new Point(Scale(form, i * 108), top);
                    }
                }
                if (form.GetType().Name == "GXDLMSNtpSetupView")
                {
                    var keys = Descendants(form).First(c => c.Name == "KeysView");
                    var label = Descendants(form).First(c => c.Name == "KeysLbl");
                    label.Parent.Controls.Add(keys);
                    keys.Location = new Point(Scale(form, 116), label.Top);
                    foreach (var button in label.Parent.Controls.OfType<Button>())
                        button.Top = label.Top + Scale(form, 160);
                }
                if (form.GetType().Name == "GXDLMSSecuritySetupView")
                {
                    var policy = Descendants(form).First(c => c.Name == "SecurityPolicyTB");
                    var legacyPolicy = Descendants(form).First(c => c.Name == "SecurityCb");
                    var parent = policy.Parent;
                    var alternatives = new EditorAlternativePanel { Bounds = policy.Bounds, BackColor = Color.White };
                    parent.Controls.Add(alternatives);
                    alternatives.Controls.Add(legacyPolicy);
                    alternatives.Controls.Add(policy);
                    legacyPolicy.Dock = DockStyle.Top;
                    policy.Dock = DockStyle.Fill;
                    policy.BackColor = Color.White;
                }
                var allButtons = Descendants(form).OfType<Button>().ToArray();
                int buttonWidth = Math.Max(Scale(form, 100), allButtons.Select(b =>
                    TextRenderer.MeasureText(b.Text, BodyFont).Width + Scale(form, 24)).DefaultIfEmpty(0).Max());
                foreach (Button button in allButtons)
                {
                    CompactButton(button);
                    StyleButton(button, false);
                    button.Font = BodyFont;
                    button.AutoSize = false;
                    button.Size = new Size(buttonWidth, Scale(form, 32));
                    button.MinimumSize = button.MaximumSize = button.Size;
                }
                ArrangeObjectContainer(form);
                if (form.GetType().Name == "GXDLMSPushSetupView")
                {
                    var push = Descendants(form).First(c => c.Name == "PushBtn");
                    var logicalName = Descendants(form).First(c => c.Name == "LogicalNameTB");
                    var rows = (TableLayoutPanel)logicalName.Parent;
                    var position = rows.GetPositionFromControl(logicalName);
                    var oldFooter = push.Parent;
                    var header = new TableLayoutPanel
                    {
                        Dock = DockStyle.Fill, Margin = Padding.Empty, BackColor = Color.White,
                        ColumnCount = 2, RowCount = 1
                    };
                    header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
                    header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, push.Width + Scale(form, 8)));
                    header.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
                    header.Controls.Add(logicalName, 0, 0);
                    push.Dock = DockStyle.Fill;
                    header.Controls.Add(push, 1, 0);
                    rows.Controls.Add(header, position.Column, position.Row);
                    // The action no longer needs a footer below the expanding tabs.
                    if (oldFooter.Parent is TableLayoutPanel footerRows)
                    {
                        int footerRow = footerRows.GetRow(oldFooter);
                        footerRows.Controls.Remove(oldFooter);
                        footerRows.RowStyles[footerRow].SizeType = SizeType.Absolute;
                        footerRows.RowStyles[footerRow].Height = 0;
                        oldFooter.Dispose();
                    }
                }
            }
            finally { form.ResumeLayout(true); }
            return true;
        }

        private readonly struct EditorExtent
        {
            internal EditorExtent(int height, bool stretch) { Height = height; Stretch = stretch; }
            internal int Height { get; }
            internal bool Stretch { get; }
        }

        // Version-dependent editors occupy one cell; their existing visibility handlers choose which to show.
        private sealed class EditorAlternativePanel : Panel { }

        private static EditorExtent ArrangeObjectContainer(Control parent)
        {
            var children = parent.Controls.Cast<Control>().OrderBy(c => c.Top).ThenBy(c => c.Left).ToArray();
            var bounds = children.ToDictionary(c => c, c => c.Bounds);
            foreach (var field in children.OfType<GXValueField>())
            {
                if (field.Type is ValueFieldType.TextBox or ValueFieldType.CompoBox)
                {
                    var rectangle = bounds[field];
                    rectangle.Height = Scale(parent, 20);
                    bounds[field] = rectangle;
                }
            }
            // Reparenting must not split a native radio group into independent groups.
            var radios = children.OfType<RadioButton>().ToArray();
            foreach (var radio in radios)
                radio.CheckedChanged += (_, _) =>
                {
                    if (radio.Checked)
                        foreach (var sibling in radios)
                            if (sibling != radio) sibling.Checked = false;
                };
            var extents = new Dictionary<Control, EditorExtent>();
            foreach (Control child in children)
            {
                child.Font = BodyFont;
                if (child is EditorAlternativePanel)
                    extents[child] = new EditorExtent(Scale(parent, 140), false);
                else if (child is TabControl tabs)
                {
                    int minimum = 120;
                    foreach (TabPage page in tabs.TabPages)
                    {
                        page.UseVisualStyleBackColor = false;
                        page.BackColor = Color.White;
                        ArrangeObjectContainer(page);
                    }
                    extents[child] = new EditorExtent(Scale(parent, minimum + 32), true);
                }
                else if (child is GroupBox || child.GetType() == typeof(Panel))
                    extents[child] = ArrangeObjectContainer(child);
                else if (child is ListView list)
                {
                    list.BackColor = Color.White;
                    list.ForeColor = Ink;
                    list.BorderStyle = BorderStyle.FixedSingle;
                    void FitColumns()
                    {
                        if (list.Columns.Count == 0 || list.ClientSize.Width < 1) return;
                        int[] minimums = list.Columns.Cast<ColumnHeader>().Select(c => Math.Max(Scale(list, 90),
                            TextRenderer.MeasureText(c.Text, list.Font).Width + Scale(list, 18))).ToArray();
                        int extra = Math.Max(0, list.ClientSize.Width - SystemInformation.VerticalScrollBarWidth - 4 - minimums.Sum());
                        for (int i = 0; i < minimums.Length; ++i) list.Columns[i].Width = minimums[i] + extra / minimums.Length;
                    }
                    list.SizeChanged += (_, _) => FitColumns();
                    list.FontChanged += (_, _) => FitColumns();
                    extents[child] = new EditorExtent(Scale(parent, 150), true);
                }
                else if (child is DataGridView grid)
                {
                    grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
                    grid.ColumnHeadersDefaultCellStyle.WrapMode = DataGridViewTriState.True;
                    extents[child] = new EditorExtent(Scale(parent, 150), true);
                }
                else if (child is TreeView || child is ListBox || child is CheckedListBox)
                    extents[child] = new EditorExtent(Scale(parent, 120), true);
                else if (child is GXValueField value)
                {
                    value.ApplyModernStyle(compact: true);
                    bool tall = value.Type is ValueFieldType.ListBox or ValueFieldType.CheckedListBox or ValueFieldType.Xml;
                    if (!tall) value.MaximumSize = new Size(0, Scale(parent, 32));
                    extents[child] = new EditorExtent(Scale(parent, tall ? 100 : 40), tall);
                }
                else if (child is TextBox text && text.Multiline)
                {
                    text.BackColor = Color.White;
                    extents[child] = new EditorExtent(Scale(parent, 100), true);
                }
                else extents[child] = new EditorExtent(Scale(parent, 40), false);
            }

            int labelWidth = Math.Min(Scale(parent, 220), Math.Max(Scale(parent, 144),
                children.OfType<Label>().Select(l => TextRenderer.MeasureText(l.Text, BodyFont).Width + Scale(parent, 12))
                .DefaultIfEmpty(0).Max()));
            var table = new TableLayoutPanel { Name = "ObjectEditorRows", Dock = DockStyle.Fill,
                ColumnCount = 2, RowCount = 0, BackColor = Color.White, Margin = Padding.Empty, Padding = Padding.Empty };
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, labelWidth));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            var pending = new HashSet<Control>(children);
            int minimumHeight = 0;
            bool stretches = false;
            foreach (Control first in children)
            {
                if (!pending.Remove(first)) continue;
                var row = new List<Control> { first };
                if (first is not GroupBox && (first is not Panel || first is EditorAlternativePanel) && first is not TabControl)
                {
                    foreach (Control candidate in children)
                        if (pending.Contains(candidate) && candidate is not GroupBox && (candidate is not Panel || candidate is EditorAlternativePanel) &&
                            (candidate is Button) == (first is Button) &&
                            (Math.Abs(bounds[candidate].Top - bounds[first].Top) <= Scale(parent, 9) ||
                             (((candidate is Label && extents[first].Stretch) || (first is Label && extents[candidate].Stretch)) &&
                              bounds[candidate].Top < bounds[first].Bottom && bounds[first].Top < bounds[candidate].Bottom)))
                        { row.Add(candidate); pending.Remove(candidate); }
                }
                row = row.OrderBy(c => bounds[c].Left).ToList();
                // Legacy side-by-side pairs use different label widths in every row.
                // Stack them onto the shared label/field grid, including narrow windows.
                if (row.Count >= 4 && row.Count % 2 == 0 &&
                    Enumerable.Range(0, row.Count / 2).All(i => row[i * 2] is Label &&
                        row[i * 2 + 1] is not Label && row[i * 2 + 1] is not Button))
                {
                    for (int i = 0; i < row.Count; i += 2)
                    {
                        int pairRow = table.RowCount++;
                        int pairHeight = Math.Max(extents[row[i]].Height, extents[row[i + 1]].Height);
                        bool pairStretch = extents[row[i + 1]].Stretch;
                        table.RowStyles.Add(new RowStyle(pairStretch ? SizeType.Percent : SizeType.Absolute, pairStretch ? 100 : pairHeight));
                        Place(row[i], table, 0, pairRow);
                        Place(row[i + 1], table, 1, pairRow);
                        minimumHeight += pairHeight;
                        stretches |= pairStretch;
                    }
                    continue;
                }
                int index = table.RowCount++;
                bool stretch = row.Any(c => extents[c].Stretch);
                int height = row.Max(c => extents[c].Height);
                table.RowStyles.Add(new RowStyle(stretch ? SizeType.Percent : SizeType.Absolute, stretch ? 100 : height));
                minimumHeight += height;
                stretches |= stretch;
                if (row.All(c => c is Button))
                {
                    var actions = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true,
                        WrapContents = true, FlowDirection = FlowDirection.RightToLeft, Margin = Padding.Empty,
                        BackColor = Color.White };
                    foreach (Control button in row.AsEnumerable().Reverse())
                    { button.Dock = DockStyle.None; button.Anchor = AnchorStyles.Top | AnchorStyles.Left;
                        button.Margin = new Padding(0, Scale(parent, 4), Scale(parent, 8), Scale(parent, 4)); actions.Controls.Add(button); }
                    table.RowStyles[index].SizeType = SizeType.AutoSize;
                    table.Controls.Add(actions, 0, index); table.SetColumnSpan(actions, 2);
                }
                else if (row.Count == 3 && row.Any(c => c.Name == "RepeaterStatusCB") && row.Any(c => c.Name == "RepeaterTB"))
                {
                    var caption = new TableLayoutPanel
                    {
                        Dock = DockStyle.Fill, Margin = Padding.Empty, BackColor = Color.White,
                        ColumnCount = 2, RowCount = 1
                    };
                    caption.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
                    caption.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, Scale(parent, 32)));
                    caption.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
                    Place(row.First(c => c is Label), caption, 0, 0);
                    Place(row.First(c => c.Name == "RepeaterStatusCB"), caption, 1, 0);
                    table.Controls.Add(caption, 0, index);
                    Place(row.First(c => c.Name == "RepeaterTB"), table, 1, index);
                }
                else if (row.Count == 2 && (row[0] is Label || row[0] is RadioButton))
                {
                    Place(row[0], table, 0, index);
                    Place(row[1], table, 1, index);
                }
                else if (row.Count == 1 && parent.FindForm() is GXDLMSG3Plc6LoWPanView && row[0] is CheckBox)
                    Place(row[0], table, 1, index);
                else if (row.Count == 1)
                { var cell = Place(row[0], table, 0, index); table.SetColumnSpan(cell, 2); }
                else
                {
                    var line = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = row.Count,
                        RowCount = 1, Margin = Padding.Empty, BackColor = Color.White };
                    line.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
                    for (int i = 0; i < row.Count; ++i)
                    {
                        bool fixedWidth = row[i] is Label || row[i] is Button || row[i] is CheckBox || row[i] is RadioButton;
                        int width = row[i] is Button ? row[i].Width + Scale(parent, 8) : TextRenderer.MeasureText(row[i].Text, BodyFont).Width + Scale(parent, row[i] is CheckBox or RadioButton ? 44 : 24);
                        line.ColumnStyles.Add(new ColumnStyle(fixedWidth ? SizeType.Absolute : SizeType.Percent, fixedWidth ? width : 100));
                        Place(row[i], line, i, 0);
                    }
                    EditorLayoutPolicy.ConfigurePropertyTable(line, boundSection: false);
                    table.Controls.Add(line, 0, index); table.SetColumnSpan(line, 2);
                }
            }
            parent.BackColor = Color.White;
            parent.Padding = parent is GroupBox ? new Padding(Scale(parent, 10), Scale(parent, 8), Scale(parent, 10), Scale(parent, 8))
                : new Padding(Scale(parent, parent is Form ? 12 : 6));
            parent.Controls.Add(table);
            // WinForms gives surplus space to the final row even when it is Absolute.
            // A filler row keeps the last input the same height as every other input.
            if (!stretches)
            {
                table.RowCount++;
                table.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            }
            minimumHeight += parent.Padding.Vertical;
            if (parent is GroupBox) minimumHeight += parent.Font.Height + Scale(parent, 12);
            double logicalMinimum = minimumHeight * 96d / parent.DeviceDpi;
            double logicalAbsoluteRows = table.RowStyles.Cast<RowStyle>().Where(r => r.SizeType == SizeType.Absolute).Sum(r => r.Height) * 96d / table.DeviceDpi;
            bool fittingSurface = false;
            void FitSurface()
            {
                if (fittingSurface || table.IsDisposed || parent.IsDisposed) return;
                fittingSurface = true;
                try
                {
                    int absoluteRows = (int)Math.Round(table.RowStyles.Cast<RowStyle>().Where(r => r.SizeType == SizeType.Absolute).Sum(r => r.Height));
                    int required = (int)Math.Round((logicalMinimum - logicalAbsoluteRows) * parent.DeviceDpi / 96d) + absoluteRows;
                    if (parent is Form || parent is TabPage)
                    {
                        int available = Math.Max(1, parent.ClientSize.Height - parent.Padding.Vertical);
                        table.Height = stretches ? Math.Max(available, required - parent.Padding.Vertical) : required - parent.Padding.Vertical;
                    }
                    else if (parent.Parent is TableLayoutPanel outer)
                    {
                        int row = outer.GetRow(parent);
                        if (row >= 0 && row < outer.RowStyles.Count && outer.RowStyles[row].SizeType == SizeType.Absolute)
                        {
                            int height = required + Scale(parent, 8);
                            if (Math.Abs(outer.RowStyles[row].Height - height) > 1) outer.RowStyles[row].Height = height;
                        }
                    }
                }
                finally { fittingSurface = false; }
            }
            if (parent is Form || parent is TabPage)
            {
                ((ScrollableControl)parent).AutoScroll = true;
                table.Dock = DockStyle.Top;
            }
            table.Layout += (_, _) => FitSurface();
            parent.SizeChanged += (_, _) => FitSurface();
            parent.ParentChanged += (_, _) => FitSurface();
            FitSurface();
            EditorLayoutPolicy.ConfigurePropertyTable(table);
            foreach (var adaptive in table.Controls.OfType<GXValueField>())
            {
                int row = table.GetRow(adaptive);
                if (table.GetColumn(adaptive) == 1 && row >= 0 && row < table.RowStyles.Count &&
                    table.RowStyles[row].SizeType == SizeType.Absolute && table.GetControlFromPosition(0, row) is Label caption)
                    ConfigureValueRow(table, row, caption, adaptive,
                        (int)Math.Round(table.RowStyles[row].Height * 96d / table.DeviceDpi));
            }
            parent.PerformLayout();
            return new EditorExtent(minimumHeight + Scale(parent, 8), stretches);
        }

        private static Control Place(Control control, TableLayoutPanel table, int column, int row)
        {
            control.Dock = DockStyle.Fill;
            control.Margin = new Padding(0, Scale(table, 4), Scale(table, 8), Scale(table, 4));
            if (control is Label label) { label.AutoSize = false; label.TextAlign = ContentAlignment.MiddleLeft; label.ForeColor = Muted; }
            if (control is TextBox text && !text.Multiline)
            {
                text.BorderStyle = BorderStyle.None;
                text.BackColor = Color.FromArgb(245, 247, 251);
                var frame = new ModernFieldFrame(text, text.BackColor)
                {
                    Dock = DockStyle.Fill, Margin = control.Margin,
                    MaximumSize = new Size(0, Scale(table, 32)),
                    Padding = new Padding(Scale(table, 8), Scale(table, 7), Scale(table, 8), Scale(table, 5))
                };
                text.Dock = DockStyle.Top;
                text.Margin = Padding.Empty;
                control = frame;
            }
            else if (control is ComboBox combo)
            {
                combo.FlatStyle = FlatStyle.Standard;
                combo.BackColor = Color.FromArgb(245, 247, 251);
                var comboMargin = control.Margin;
                var frame = new NativeComboHost(combo)
                {
                    Dock = DockStyle.Fill, Margin = comboMargin,
                    MaximumSize = new Size(0, Scale(table, 32))
                };
                combo.Margin = Padding.Empty;
                control = frame;
            }
            table.Controls.Add(control, column, row);
            return control;
        }
    }
}
