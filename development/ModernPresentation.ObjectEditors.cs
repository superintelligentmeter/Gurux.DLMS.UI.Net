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
            "GXDLMSRegisterMonitor GXDLMSHdlcSetup GXDLMSIecTwistedPairSetup " +
            "GXDLMSMBusSlavePortSetup GXDLMSUtilityTables GXDLMSModemConfiguration GXDLMSAutoAnswer " +
            "GXDLMSAutoConnect GXDLMSDataProtection GXDLMSPushSetup GXDLMSTcpUdpSetup GXDLMSIp4Setup " +
            "GXDLMSMacAddressSetup GXDLMSPppSetup GXDLMSGprsSetup GXDLMSGSMDiagnostic GXDLMSIp6Setup " +
            "GXDLMSSFSKPhyMacSetUp GXDLMSSFSKActiveInitiator GXDLMSSFSKMacSynchronizationTimeouts " +
            "GXDLMSSFSKMacCounters GXDLMSCompactData GXDLMSSecuritySetup " +
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
                    var windows = Descendants(form).First(c => c.Name == "CallingWindowLV");
                    // The remote Connect method is distinct from editing the
                    // calling-window rows. Keep it above their contextual footer.
                    connect.Location = new Point(0, windows.Top - Scale(form, 4));
                    // Put the caption above a full-width list, on the same
                    // rails as Destinations and both contextual action rows.
                    var caption = Descendants(form).First(c => c.Name == "CallingWindowLbl");
                    caption.Location = new Point(0, windows.Top);
                    windows.Location = new Point(0, windows.Top + Scale(form, 24));
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
                if (form is GXDLMSCompactDataView || form is GXDLMSPrimeNbOfdmPlcMacNetworkAdministrationDataView)
                {
                    var tabs = Descendants(form).OfType<TabControl>().First();
                    var names = form is GXDLMSCompactDataView ? new[] { "ResetBtn", "CaptureBtn" } : new[] { "ResetBtn" };
                    for (int i = 0; i < names.Length; ++i)
                    {
                        var button = Descendants(form).First(c => c.Name == names[i]);
                        button.Location = new Point(Scale(form, i * 108), tabs.Top - Scale(form, 4));
                    }
                }
                if (form is GXDLMSAssociationShortNameView)
                {
                    // Object actions belong to the Objects tab, on the same
                    // content edge as its list rather than outside the tab frame.
                    var objects = Descendants(form).First(c => c.Name == "ObjectsView");
                    var actions = new[] { "ObjectAddBtn", "ObjectEditBtn", "ObjectRemoveBtn" }
                        .Select(name => (Button)Descendants(form).First(c => c.Name == name)).ToArray();
                    for (int i = 0; i < actions.Length; ++i)
                    {
                        objects.Parent.Controls.Add(actions[i]);
                        actions[i].Anchor = AnchorStyles.Top | AnchorStyles.Left;
                        actions[i].Location = new Point(Scale(form, i * 108), objects.Bottom + Scale(form, 8));
                    }
                }
                if (form is GXDLMSChargeView)
                {
                    var list = Descendants(form).First(c => c.Name == "PassiveChargePerUnits");
                    // Preserve separate local list editing and remote method rows.
                    foreach (var names in new[] { new[] { "AddBtn", "EditBtn", "RemoveBtn" },
                        new[] { "gxButton4", "gxButton2" } })
                    {
                        int row = names[0] == "AddBtn" ? 0 : 1;
                        for (int i = 0; i < names.Length; ++i)
                        {
                            var action = Descendants(form).First(c => c.Name == names[i]);
                            action.Location = new Point(Scale(form, i * 108), list.Bottom + Scale(form, 8 + row * 40));
                        }
                    }
                }
                if (form is GXDLMSImageTransferView)
                {
                    foreach (var name in new[] { "ActivateImageBtn", "VerifyImageBtn", "UpdateImageBtn" })
                    {
                        var button = (Button)Descendants(form).First(c => c.Name == name);
                        button.Text = button.Text.TrimEnd('.');
                        // Use one GDI+ layout/paint path for these captions. The flat
                        // GDI adapter can clip ascenders/descenders after DPI scaling
                        // even when the outer button passes the text-size checks.
                        button.UseCompatibleTextRendering = true;
                    }
                }
                // These groups contain presentation-only sections of the same property
                // form. A nested table adds another label inset; reuse one property
                // grid while keeping every bound editor and event handler intact.
                PromotePropertySections(form);
                var allButtons = Descendants(form).OfType<Button>().ToArray();
                foreach (Button button in allButtons)
                {
                    CompactButton(button);
                    StyleButton(button, false);
                    button.Font = BodyFont;
                    button.AutoSize = false;
                    button.Size = ButtonLayoutPolicy.GetPreferredSize(button);
                    button.MinimumSize = button.MaximumSize = button.Size;
                }
                ArrangeObjectContainer(form);
                if (form is GXDLMSAssociationShortNameView) AlignShortNameGeneral(form);
                RepairReportedLayouts(form);
                if (form is GXDLMSIp6SetupView)
                {
                    var tabs = (TabControl)Descendants(form).First(c => c.Name == "AddressTab");
                    var add = Descendants(form).First(c => c.Name == "AddAddressBtn");
                    var footer = (FlowLayoutPanel)add.Parent;
                    var rows = (TableLayoutPanel)footer.Parent;
                    int oldRow = rows.GetRow(footer);
                    rows.Controls.Remove(footer);
                    rows.RowStyles[oldRow].SizeType = SizeType.Absolute;
                    rows.RowStyles[oldRow].Height = 0;
                    var footerRows = new Dictionary<TableLayoutPanel, int>();
                    void ShowAddressFooter()
                    {
                        if (rows.IsDisposed || tabs.SelectedTab == null) return;
                        var list = Descendants(tabs.SelectedTab).OfType<ListView>().First();
                        var body = (TableLayoutPanel)list.Parent;
                        if (footer.Parent is TableLayoutPanel previous && footerRows.TryGetValue(previous, out int previousRow))
                        {
                            previous.Controls.Remove(footer);
                            previous.RowStyles[previousRow].SizeType = SizeType.Absolute;
                            previous.RowStyles[previousRow].Height = 0;
                        }
                        if (!footerRows.TryGetValue(body, out int row))
                        {
                            row = body.RowCount++;
                            body.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                            footerRows.Add(body, row);
                        }
                        body.RowStyles[row].SizeType = SizeType.AutoSize;
                        list.Margin = new Padding(list.Margin.Left, list.Margin.Top, list.Margin.Right, 0);
                        footer.Margin = new Padding(0, 0, list.Margin.Right, 0);
                        body.Controls.Add(footer, 0, row); body.SetColumnSpan(footer, body.ColumnCount);
                        body.PerformLayout();
                    }
                    tabs.SelectedIndexChanged += (_, _) => ShowAddressFooter();
                    ShowAddressFooter();
                }
            }
            finally { form.ResumeLayout(true); }
            return true;
        }

        private sealed class EditorSectionCaption : Label { }

        private static void PromotePropertySections(Form form)
        {
            string[] names = form.GetType().Name switch
            {
                "GXDLMSGprsSetupView" => new[] { "groupBox2", "groupBox3" },
                "GXDLMSGSMDiagnosticView" => new[] { "CellInfoCb" },
                "GXDLMSIEC6205541AttributesView" => new[] { "groupBox3" },
                "GXDLMSLimiterView" => new[] { "MonitoredGB", "groupBox4", "groupBox2", "groupBox3" },
                "GXDLMSPushSetupView" => new[] { "GXSendDestinationAndMethodGB" },
                "GXDLMSRegisterMonitorView" => new[] { "MonitoredValueGB" },
                _ => Array.Empty<string>()
            };
            foreach (string name in names)
            {
                var group = Descendants(form).OfType<GroupBox>().Single(c => c.Name == name);
                var parent = group.Parent;
                var origin = group.Location;
                var children = group.Controls.Cast<Control>().ToArray();
                var locations = children.ToDictionary(c => c, c => c.Location);
                var heading = new EditorSectionCaption
                {
                    Name = group.Name + "Heading", Text = group.Text,
                    Location = origin, Size = new Size(group.Width, Scale(form, 32)),
                    Font = BodyFont, ForeColor = Ink, TabStop = false
                };
                parent.SuspendLayout();
                try
                {
                    foreach (var child in children)
                    {
                        parent.Controls.Add(child);
                        child.Location = new Point(origin.X + locations[child].X, origin.Y + locations[child].Y);
                    }
                    parent.Controls.Add(heading);
                    parent.Controls.Remove(group);
                    group.Dispose();
                }
                finally { parent.ResumeLayout(false); }
            }
            if (form is GXDLMSGSMDiagnosticView)
                // The original caption was copied from a listening-window editor.
                Descendants(form).Single(c => c.Name == "AdjacentCellsLbl").Text = "Adjacent cells:";
        }

        private static void AlignTallCaption(Control caption, Control editor)
        {
            // Scalar fields retain centered captions; data surfaces start at the
            // top of their row, including lists and multiline native text editors.
            if (caption is Label label && (editor is ListView or DataGridView or TreeView or ListBox or
                RichTextBox or EditorAlternativePanel || editor is TextBox text && text.Multiline))
            {
                label.TextAlign = ContentAlignment.TopLeft;
                label.Padding = new Padding(0, Scale(label, 4), 0, 0);
            }
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
                    // Keep the image catalog compact; block status and the transfer
                    // log use the remaining space. Longer catalogs scroll natively.
                    bool compactImages = parent.FindForm() is GXDLMSImageTransferView && list.Name == "ImagesView";
                    // IEC Twisted Pair needs room for both independent action rows.
                    int listHeight = compactImages ? 80 : parent.FindForm() is GXDLMSIecTwistedPairSetupView &&
                        list.Name is "PrimaryAddressesList" or "TabisList" ? 96 : 150;
                    extents[child] = new EditorExtent(Scale(parent, listHeight), !compactImages);
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
            int minimumHeight = 0, allocatedAbsoluteHeight = 0;
            bool stretches = false;
            var actionRows = new List<FlowLayoutPanel>();
            foreach (Control first in children)
            {
                if (!pending.Remove(first)) continue;
                var row = new List<Control> { first };
                if (first is not EditorSectionCaption && first is not GroupBox && (first is not Panel || first is EditorAlternativePanel) && first is not TabControl)
                {
                    foreach (Control candidate in children)
                        if (pending.Contains(candidate) && candidate is not EditorSectionCaption && candidate is not GroupBox && (candidate is not Panel || candidate is EditorAlternativePanel) &&
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
                        AlignTallCaption(row[i], row[i + 1]);
                        minimumHeight += pairHeight;
                        if (!pairStretch) allocatedAbsoluteHeight += pairHeight;
                        stretches |= pairStretch;
                    }
                    continue;
                }
                int index = table.RowCount++;
                bool stretch = row.Any(c => extents[c].Stretch);
                int height = row.Max(c => extents[c].Height);
                table.RowStyles.Add(new RowStyle(stretch ? SizeType.Percent : SizeType.Absolute, stretch ? 100 : height));
                minimumHeight += height;
                if (!stretch && !row.All(c => c is Button)) allocatedAbsoluteHeight += height;
                stretches |= stretch;
                if (row.All(c => c is Button))
                {
                    var actions = new ActionButtonPanel { Dock = DockStyle.Fill, AutoSize = true,
                        WrapContents = true, FlowDirection = FlowDirection.RightToLeft, Margin = Padding.Empty,
                        BackColor = Color.White };
                    ButtonLayoutPolicy.Apply(actions, row.Cast<Button>().ToArray());
                    // Match the data cell's explicit right inset, including on
                    // tab pages. Peer gaps live inside the action group only.
                    actions.Margin = new Padding(0, 0, Scale(parent, 8), 0);
                    actionRows.Add(actions);
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
                    AlignTallCaption(row[0], row[1]);
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
            // Capture the allocated baseline, not rows already resized by child parenting.
            // Otherwise a nested group can grow while the outer table subtracts its new
            // height from the old minimum, leaving the footer clipped.
            double logicalAbsoluteRows = allocatedAbsoluteHeight * 96d / table.DeviceDpi;
            bool fittingSurface = false;
            void FitSurface()
            {
                if (fittingSurface || table.IsDisposed || parent.IsDisposed) return;
                fittingSurface = true;
                try
                {
                    int absoluteRows = (int)Math.Round(table.RowStyles.Cast<RowStyle>().Where(r => r.SizeType == SizeType.Absolute).Sum(r => r.Height));
                    int required = (int)Math.Round((logicalMinimum - logicalAbsoluteRows) * parent.DeviceDpi / 96d) + absoluteRows;
                    foreach (var actions in actionRows)
                    {
                        int width = Math.Max(1, table.ClientSize.Width - actions.Margin.Horizontal);
                        required += Math.Max(0, actions.GetPreferredSize(new Size(width, 0)).Height - Scale(parent, 40));
                    }
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
            foreach (var actions in actionRows)
            {
                if (parent.FindForm() is GXDLMSFunctionControlView)
                {
                    // Function actions sit in the middle of the group at any window width.
                    actions.Dock = DockStyle.None;
                    actions.Anchor = AnchorStyles.Top | AnchorStyles.Left;
                    actions.AutoSize = true;
                    if (actions is ActionButtonPanel actionPanel) actionPanel.AlignRowsRight = false;
                    bool centering = false;
                    void CenterActions()
                    {
                        if (centering || actions.IsDisposed || table.IsDisposed) return;
                        // Centre the buttons themselves: the panel can keep a stale width after a DPI change.
                        int content = actions.Controls.Cast<Control>().Where(c => c.Visible)
                            .Select(c => c.Right + c.Margin.Right).DefaultIfEmpty(0).Max();
                        int left = Math.Max(0, (table.ClientSize.Width - table.Padding.Horizontal - content) / 2);
                        var margin = new Padding(left, actions.Margin.Top, 0, 0);
                        if (actions.Margin == margin) return;
                        centering = true;
                        try { actions.Margin = margin; } finally { centering = false; }
                    }
                    table.Layout += (_, _) => CenterActions();
                    table.DpiChangedAfterParent += (_, _) => CenterActions();
                    actions.SizeChanged += (_, _) => CenterActions();
                    actions.Layout += (_, _) => CenterActions();
                    CenterActions();
                    continue;
                }
                int row = table.GetRow(actions);
                var peer = table.Controls.Cast<Control>().Where(c => table.GetRow(c) < row &&
                    (c is ListView || c is DataGridView || c is TreeView || c is TabControl))
                    .OrderByDescending(c => table.GetRow(c)).FirstOrDefault();

                if (peer is null && parent.FindForm() is not GXDLMSCompactDataView &&
                    parent.FindForm() is not GXDLMSPrimeNbOfdmPlcMacNetworkAdministrationDataView)
                {
                    var field = table.Controls.Cast<Control>().Where(c => table.GetRow(c) < row &&
                        table.GetColumn(c) == 1 && (c is GXValueField || c is ModernFieldFrame || c is NativeComboHost || c is ComboBox))
                        .OrderByDescending(c => table.GetRow(c)).FirstOrDefault();
                    if (field != null)
                    {
                        bool aligning = false;
                        void AlignScalarActions()
                        {
                            if (aligning || actions.IsDisposed || field.IsDisposed || table.IsDisposed) return;
                            var margin = new Padding(0, 0, Math.Max(0, table.ClientSize.Width-field.Right), 0);
                            if (actions.Margin == margin) return;
                            aligning = true;
                            try { actions.Margin = margin; } finally { aligning = false; }
                        }
                        table.Layout += (_, _) => AlignScalarActions();
                        table.DpiChangedAfterParent += (_, _) => AlignScalarActions();
                        field.SizeChanged += (_, _) => AlignScalarActions();
                        AlignScalarActions();
                    }
                }
                if (peer is not null)
                {
                    actions.Margin = new Padding(0, 0, peer.Margin.Right, 0);

                    // The action group's four-pixel top inset is the complete
                    // list-to-button gap; do not retain another data-cell inset.
                    int peerRow = table.GetRow(peer);
                    bool followsData = table.Controls.Cast<Control>().Where(c => table.GetRow(c) > peerRow && table.GetRow(c) < row)
                        .All(c => c is FlowLayoutPanel);
                    if (followsData)
                        peer.Margin = new Padding(peer.Margin.Left, peer.Margin.Top, peer.Margin.Right, 0);
                }
            }
            foreach (var adaptive in table.Controls.OfType<GXValueField>())
            {
                int row = table.GetRow(adaptive);
                if (table.GetColumn(adaptive) == 1 && row >= 0 && row < table.RowStyles.Count &&
                    table.RowStyles[row].SizeType == SizeType.Absolute && table.GetControlFromPosition(0, row) is Label caption)
                    ConfigureValueRow(table, row, caption, adaptive,
                        (int)Math.Round(table.RowStyles[row].Height * 96d / table.DeviceDpi));
            }
            SetPropertyTabOrder(table);
            parent.PerformLayout();
            return new EditorExtent(minimumHeight + Scale(parent, 8), stretches);
        }

        private static void SetPropertyTabOrder(TableLayoutPanel table)
        {
            // Promoted sections previously had independent tab indices. Follow
            // the final reading order, including layout-only native editor hosts.
            foreach (Control cell in table.Controls)
            {
                cell.TabIndex = table.GetRow(cell) * table.ColumnCount + table.GetColumn(cell);
                if (cell is TableLayoutPanel inline) SetPropertyTabOrder(inline);
            }
        }

        private static Control Place(Control control, TableLayoutPanel table, int column, int row)
        {
            control.Dock = DockStyle.Fill;
            control.Margin = new Padding(0, Scale(table, 4), Scale(table, 8), Scale(table, 4));
            if (control is Label label) { label.AutoSize = false; label.TextAlign = ContentAlignment.MiddleLeft; label.ForeColor = label is EditorSectionCaption ? Ink : Muted; }
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
            if (control is Button button)
            {
                ButtonLayoutPolicy.ApplyToButton(button);
                void FitActionColumn()
                {
                    if (table.IsDisposed || column >= table.ColumnStyles.Count) return;
                    var style = table.ColumnStyles[column];
                    if (style.SizeType != SizeType.Absolute) return;
                    int width = button.Width + button.Margin.Horizontal;
                    if (Math.Abs(style.Width - width) > 1) style.Width = width;
                }
                button.SizeChanged += (_, _) => FitActionColumn();
                FitActionColumn();
            }
            return control;
        }

        private static void RepairReportedLayouts(Form form)
        {
            Control Named(string name) => Descendants(form).Single(c => c.Name == name);
            if (form is GXDLMSImageTransferView)
            {
                // Full-span rows do not pass through the captioned-field policy.
                // Remove the leftover designer inset so the log shares the data edge.
                var log = Named("DescriptionList");
                log.Margin = new Padding(log.Margin.Left, log.Margin.Top, 0, log.Margin.Bottom);
            }
            void Inline(Control field, Button action, TableLayoutPanel table, int row, int logicalHeight = 40)
            {
                field.Parent.Controls.Remove(field);
                action.Parent.Controls.Remove(action);
                ButtonLayoutPolicy.ApplyToButton(action);
                var line = new CompactActionRow(table, row, field, action, logicalHeight)
                { MaximumSize = new Size(Scale(table, 640), 0), TabIndex = field.TabIndex };
                table.Controls.Add(line, 1, row);
                table.SetColumnSpan(line, table.ColumnCount - 1);
            }
            if (form is GXDLMSPushSetupView)
            {
                var field = (GXValueField)Named("LogicalNameTB");
                var table = (TableLayoutPanel)field.Parent;
                var push = (Button)Named("PushBtn");
                var old = push.Parent;
                int obsolete = table.GetRow(old);
                Inline(field, push, table, table.GetRow(field));
                if (old.Controls.Count == 0 && obsolete >= 0)
                {
                    table.Controls.Remove(old); old.Dispose();
                    table.RowStyles[obsolete].SizeType = SizeType.Absolute;
                    table.RowStyles[obsolete].Height = 0;
                }
            }
            if (form is GXDLMSSecuritySetupView)
            {
                var suite = (GXValueField)Named("SuiteTB");
                var table = (TableLayoutPanel)suite.Parent;
                var update = (Button)Named("UpdateKeyBtn");
                var old = update.Parent;
                int obsolete = table.GetRow(old);
                Inline(suite, update, table, table.GetRow(suite));
                if (old.Controls.Count == 0 && obsolete >= 0)
                {
                    table.Controls.Remove(old); old.Dispose();
                    table.RowStyles[obsolete].SizeType = SizeType.Absolute;
                    table.RowStyles[obsolete].Height = 0;
                }
                var policy = Named("SecurityPolicyTB").Parent;
                var activate = (Button)Named("ActivateSecurityBtn");
                var oldActivation = activate.Parent;
                int activationRow = table.GetRow(oldActivation);
                Inline(policy, activate, table, table.GetRow(policy), 140);
                if (oldActivation.Controls.Count == 0 && activationRow >= 0)
                {
                    table.Controls.Remove(oldActivation); oldActivation.Dispose();
                    table.RowStyles[activationRow].SizeType = SizeType.Absolute;
                    table.RowStyles[activationRow].Height = 0;
                }
                ButtonLayoutPolicy.MatchInlineWidths(update, activate);
                var text = (TextBox)Named("SystemTitleDescriptionTb");
                int row = table.GetRow(text);
                table.Controls.Remove(text);
                text.BorderStyle = BorderStyle.None;
                text.BackColor = Color.FromArgb(245, 247, 251);
                text.ScrollBars = ScrollBars.Vertical;
                var frame = new ModernFieldFrame(text, text.BackColor)
                { Name = "SystemTitleDescriptionField", Dock = DockStyle.Fill,
                  Margin = new Padding(0, Scale(table, 4), 0, Scale(table, 4)),
                  MaximumSize = new Size(Scale(table, 640), 0),
                  Padding = new Padding(Scale(table, 8)) };
                text.Dock = DockStyle.Fill; text.Margin = Padding.Empty;
                table.Controls.Add(frame, 1, row);
                table.RowStyles[row].SizeType = SizeType.Absolute;
                void FitDescription()
                {
                    int height = text.Font.Height * 5 + frame.Padding.Vertical + frame.Margin.Vertical;
                    if (table.RowStyles[row].Height != height) table.RowStyles[row].Height = height;
                }
                text.FontChanged += (_, _) => FitDescription();
                frame.DpiChangedAfterParent += (_, _) => FitDescription();
                FitDescription();
                table.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); table.RowCount++;
            }

            if (form is GXDLMSPrimeNbOfdmPlcMacNetworkAdministrationDataView)
            {
                var footer = (ActionButtonPanel)Named("ResetBtn").Parent;
                var rows = (TableLayoutPanel)footer.Parent;
                var tabs = (TabControl)Named("Tabs");
                bool fitting = false, pending = false;
                void QueueLayout()
                {
                    if (pending || rows.IsDisposed || !rows.IsHandleCreated) return;
                    pending = true;
                    rows.BeginInvoke((Action)(() =>
                    {
                        pending = false;
                        if (!rows.IsDisposed) rows.PerformLayout();
                    }));
                }
                void FitReset()
                {
                    if (fitting || rows.IsDisposed || tabs.SelectedTab == null) return;
                    var list = Descendants(tabs.SelectedTab).OfType<ListView>().First();
                    int right = list.Right;
                    Control owner = list.Parent;
                    while (owner != null && owner != rows) { right += owner.Left; owner = owner.Parent; }
                    if (owner != rows) return;
                    var margin = new Padding(0, 0, Math.Max(0, rows.ClientSize.Width-right), 0);
                    if (footer.Margin == margin) return;
                    fitting = true;
                    try { footer.Margin = margin; QueueLayout(); } finally { fitting = false; }
                }
                rows.Layout += (_, _) => FitReset();
                rows.HandleCreated += (_, _) => QueueLayout();
                tabs.SelectedIndexChanged += (_, _) => FitReset();
                foreach (var list in Descendants(tabs).OfType<ListView>())
                {
                    list.SizeChanged += (_, _) => FitReset();
                    list.DpiChangedAfterParent += (_, _) => FitReset();
                }
                FitReset();
            }
            if (form is GXDLMSCompactDataView)
            {
                var upper = (ActionButtonPanel)Named("CaptureBtn").Parent;
                var rows = (TableLayoutPanel)upper.Parent;
                var tabs = Descendants(form).OfType<TabControl>().First();
                bool fitting = false;
                void Fit()
                {
                    if (fitting || upper.IsDisposed || tabs.SelectedTab == null) return;
                    fitting = true;
                    try
                    {
                        var description = Named("TemplateDescriptionTb").Parent;
                        int descriptionRow = rows.GetRow(description);
                        var maximum = new Size(description.MaximumSize.Width, Scale(rows, 32));
                        if (description.MaximumSize != maximum) description.MaximumSize = maximum;
                        if (descriptionRow >= 0 && rows.RowStyles[descriptionRow].Height != Scale(rows, 40))
                            rows.RowStyles[descriptionRow].Height = Scale(rows, 40);
                        // Use the tab viewport, including its native frame and content
                        // insets. Hidden child bounds can be stale after a DPI move.
                        var list = Named("CaptureObjectsLv");
                        var page = tabs.TabPages.Cast<TabPage>().Single(p => Descendants(p).Contains(list));
                        // WinForms TabControl has a four-device-pixel native frame.
                        // Derive the remaining inset from the current content margins,
                        // rather than its cached DisplayRectangle after a DPI move.
                        int inset = tabs.Margin.Right + page.Padding.Right + list.Margin.Right + 4;
                        var remove = Named("ColumnRemoveBtn");
                        if (remove.Visible)
                        {
                            int right = remove.Right;
                            Control owner = remove.Parent;
                            while (owner != null && owner != rows) { right += owner.Left; owner = owner.Parent; }
                            if (owner == rows) inset = Math.Max(0, rows.ClientSize.Width - right);
                        }
                        // Reset and Capture always start where the fields start, whatever the window width,
                        // instead of moving between the right edge and the left after a re-layout.
                        if (upper.Dock != DockStyle.None) upper.Dock = DockStyle.None;
                        upper.Anchor = AnchorStyles.Top | AnchorStyles.Left;
                        upper.AutoSize = true;
                        var widths = rows.GetColumnWidths();
                        var margin = new Padding(widths.Length > 0 ? widths[0] : 0, 0, 0, 0);
                        if (upper.Margin != margin) upper.Margin = margin;
                    }
                    finally { fitting = false; }
                }
                rows.Layout += (_, _) => Fit(); tabs.SelectedIndexChanged += (_, _) => Fit();
                tabs.DpiChangedAfterParent += (_, _) => Fit(); Fit();
            }
        }
    }
}
