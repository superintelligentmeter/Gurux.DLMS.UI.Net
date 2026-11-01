// superintelligentmeter contributors, 2026-10-30. Absolute rail and retained-binding regressions. GPL-2.0-only.
// SPDX-License-Identifier: GPL-2.0-only
using Gurux.DLMS.Objects;
using Gurux.DLMS.UI;

internal static partial class Program
{
    sealed record RailGroup(string Name, string Heading, Control Scope, Control[] Children, TabPage? Tab);
    sealed class RailSnapshot
    {
        public Dictionary<Control, (string Name, int? Index)> Original = new();
        public List<RailGroup> Groups = new();
        public Dictionary<Control, Control[]> Peers = new();
    }
    static readonly Dictionary<Form, RailSnapshot> RailSnapshots = new();
    static string[] PromotedGroups(string name) => name switch
    {
        "GXDLMSGprsSetupView" => ["groupBox2", "groupBox3"],
        "GXDLMSGSMDiagnosticView" => ["CellInfoCb"],
        "GXDLMSIEC6205541AttributesView" => ["groupBox3"],
        "GXDLMSLimiterView" => ["MonitoredGB", "groupBox4", "groupBox2", "groupBox3"],
        "GXDLMSPushSetupView" => ["GXSendDestinationAndMethodGB"],
        "GXDLMSRegisterMonitorView" => ["MonitoredValueGB"],
        _ => []
    };
    static IEnumerable<Control> Ancestors(Control control)
    { for (Control? parent = control.Parent; parent != null; parent = parent.Parent) yield return parent; }
    static bool OuterEditor(Control c) => !Ancestors(c).Any(p => p is GXValueField or NumericUpDown) && c is GXValueField or TextBoxBase or ComboBox or NumericUpDown or DateTimePicker;
    static void RememberInputRails(Form editor)
    {
        string[] groups = PromotedGroups(editor.GetType().Name);
        if (groups.Length == 0) return;
        var snapshot = new RailSnapshot();
        foreach (var c in Descendants(editor).Where(c => !string.IsNullOrEmpty(c.Name) && c is not GroupBox && c is not Panel && !Ancestors(c).Any(p => p is GXValueField)))
            snapshot.Original[c] = (c.Name, c is GXValueField field ? field.Index : null);
        foreach (string name in groups)
        {
            var group = Descendants(editor).OfType<GroupBox>().Single(c => c.Name == name);
            snapshot.Groups.Add(new(name, group.Text, group.Parent!, Descendants(group).Where(OuterEditor).ToArray(), Ancestors(group).OfType<TabPage>().FirstOrDefault()));
            snapshot.Peers.TryAdd(group.Parent!, Descendants(group.Parent!).Where(OuterEditor).ToArray());
        }
        RailSnapshots[editor] = snapshot;
    }
    static (TableLayoutPanel Table, Control Cell, Label Caption)? PropertyCell(Control field)
    {
        for (Control? cell = field; cell?.Parent != null; cell = cell.Parent)
            if (cell.Parent is TableLayoutPanel table && table.GetColumn(cell) == 1 && table.GetControlFromPosition(0, table.GetRow(cell)) is Label caption && table.GetColumnSpan(caption) == 1)
                return (table, cell, caption);
        return null;
    }
    static Control RailSlot(Control field) => field.Parent?.GetType().Name is "ModernFieldFrame" or "NativeComboHost" ? field.Parent : field;
    static void CheckInputRails(Form editor, CaseResult result, string variant)
    {
        string prefix = editor.GetType().Name + "/" + variant;
        void Check(string name, bool passed, string detail = "") => result.BehaviorChecks.Add(new() { Name = prefix + "/" + name, Passed = passed, Detail = detail });
        Rectangle Bounds(Control c) => new(editor.PointToClient(c.PointToScreen(Point.Empty)), c.Size);
        if (RailSnapshots.TryGetValue(editor, out var snapshot))
        {
            var all = Descendants(editor).ToHashSet();
            Check("original-controls-and-indices-retained", snapshot.Original.All(p => all.Contains(p.Key) && !p.Key.IsDisposed && p.Key.Name == p.Value.Name && (p.Value.Index == null || ((GXValueField)p.Key).Index == p.Value.Index)));
            Check("named-controls-not-duplicated", snapshot.Original.All(p => all.Count(c => c.Name == p.Value.Name) == 1));
            foreach (var scope in snapshot.Peers)
            {
                var fields = scope.Value.Where(c => c.Visible && LayoutMetrics.Role(c) == "Scalar").ToArray();
                if (fields.Length < 2) continue;
                var positions = fields.Select(c => (Control: c, Slot: Bounds(RailSlot(c)), Cell: PropertyCell(c))).ToArray();
                string scopeName = string.IsNullOrEmpty(scope.Key.Name) ? scope.Key.GetType().Name : scope.Key.Name;
                Check(scopeName + "/explicit-peer-set-present", positions.All(p => p.Cell != null), string.Join(",", fields.Select(c => c.Name)));
                Check(scopeName + "/absolute-field-left-edge", positions.Max(p => p.Slot.Left) - positions.Min(p => p.Slot.Left) <= 2, string.Join(";", positions.Select(p => p.Control.Name + "=" + p.Slot.Left)));
                var cells = positions.Where(p => p.Cell != null).Select(p => p.Cell!.Value).ToArray();
                if (cells.Length == fields.Length)
                {
                    var captions = cells.Select(c => Bounds(c.Caption).Left).ToArray();
                    Check(scopeName + "/absolute-caption-left-edge", captions.Max() - captions.Min() <= 2, string.Join(",", captions));
                    var rights = cells.Select(c => c.Table.Controls.Cast<Control>().Where(x => c.Table.GetRow(x) == c.Table.GetRow(c.Cell) && c.Table.GetColumn(x) >= 1).Max(x => Bounds(x).Right)).ToArray();
                    Check(scopeName + "/plain-or-compound-right-edge", rights.Max() - rights.Min() <= 2, string.Join(",", rights));
                }
            }
            foreach (var group in snapshot.Groups)
            {
                var heading = Descendants(editor).OfType<Label>().SingleOrDefault(c => c.Name == group.Name + "Heading");
                Check(group.Name + "/heading-retained", heading != null && heading.Text == group.Heading);
                if (heading?.Visible == true && heading.Parent is TableLayoutPanel table)
                {
                    int row = table.GetRow(heading);
                    var children = group.Children.Where(c => c.Visible).ToArray();
                    Check(group.Name + "/heading-full-row-before-fields", table.GetColumnSpan(heading) == table.ColumnCount && children.All(c => Bounds(heading).Bottom <= Bounds(RailSlot(c)).Top + 2), "HeadingRow=" + row);
                    Check(group.Name + "/heading-not-grouped-with-editor", !table.Controls.Cast<Control>().Any(c => c != heading && table.GetRow(c) == row));
                }
                Check(group.Name + "/same-tab-scope", group.Children.All(c => Ancestors(c).OfType<TabPage>().FirstOrDefault() == group.Tab));
            }
        }
        foreach (var label in Descendants(editor).OfType<Label>().Where(c => c.Visible && c.Parent is TableLayoutPanel).ToArray())
        {
            var table = (TableLayoutPanel)label.Parent!;
            if (table.GetColumn(label) != 0 || table.GetColumnSpan(label) != 1) continue;
            var cell = table.GetControlFromPosition(1, table.GetRow(label));
            if (cell == null || cell.Height < 64 * editor.DeviceDpi / 96) continue;
            bool Tall(Control c) => c.Visible && !Ancestors(c).Any(p => p is GXValueField) && (c is ListView or DataGridView or TreeView or ListBox or RichTextBox || c.GetType().Name == "EditorAlternativePanel" || c is TextBox text && text.Multiline);
            if (!new[] { cell }.Concat(Descendants(cell)).Any(Tall)) continue;
            string name = string.IsNullOrEmpty(label.Name) ? label.Text : label.Name;
            Check("tall-caption/" + name, label.TextAlign == ContentAlignment.TopLeft && Bounds(label).Top + label.Padding.Top <= Bounds(cell).Top + 8 * editor.DeviceDpi / 96, "TextAlign=" + label.TextAlign + "; captionTop=" + Bounds(label).Top + "; fieldTop=" + Bounds(cell).Top);
        }
    }
    static void CheckRailInteractions(Form editor, GXDLMSObject model, CaseResult result)
    {
        string type = editor.GetType().Name;
        void Check(string name, bool passed, string detail = "") => result.BehaviorChecks.Add(new() { Name = type + "/" + name, Passed = passed, Detail = detail });
        string[] wanted = type == "GXDLMSIEC6205541AttributesView" ? ["LogicalNameTB", "IssuerIDTb", "DecoderReferenceNumberTb", "PanCheckDigitTb", "CommodityTb"] : type == "GXDLMSRegisterMonitorView" ? ["MonitoredValueCb", "AttributeIndexTB", "ShowAllTargetsCb"] : [];
        if (wanted.Length == 0) return;
        // Empty legacy Index uses Leave validation, which AutoValidate.Disable cannot suppress.
        // Preserve blank geometry and Show-all coverage; actual navigation uses the valid bound state.
        if (model is not GXDLMSRegisterMonitor blankMonitor || blankMonitor.MonitoredValue.ObjectType != Gurux.DLMS.Enums.ObjectType.None)
        {
            var originals = Descendants(editor).Where(c => wanted.Contains(c.Name)).ToArray();
            var order = new List<string>(); Control? previous = null;
            for (int i = 0; i < 200 && editor.SelectNextControl(previous, true, true, true, false); i++)
            {
                Control? selected = Descendants(editor).FirstOrDefault(c => c.Focused);
                if (selected == null) { selected = editor.ActiveControl; while (selected is ContainerControl container && container.ActiveControl != null) selected = container.ActiveControl; }
                if (selected == null || selected == previous) break;
                previous = selected;
                var owner = new[] { selected }.Concat(Ancestors(selected)).FirstOrDefault(c => wanted.Contains(c.Name));
                if (owner != null && !order.Contains(owner.Name)) order.Add(owner.Name);
            }
            var eligible = wanted.Where(n => originals.Any(c => c.Name == n && c.Visible && c.Enabled && (c.TabStop || Descendants(c).Any(x => x.Visible && x.Enabled && x.TabStop)))).ToArray();
            Check("actual-forward-tab-order", eligible.Length >= 2 && eligible.SequenceEqual(order), "Expected=" + string.Join(",", eligible) + "; observed=" + string.Join(",", order));
        }
        else result.FixtureNotes.Add("Blank RegisterMonitor navigation excluded: existing AttributeIndexTB_Leave parses empty text and opens a modal error. Representative state covers actual forward navigation.");
        if (model is GXDLMSRegisterMonitor monitor)
        {
            var toggle = Descendants(editor).OfType<CheckBox>().Single(c => c.Name == "ShowAllTargetsCb");
            var targets = Descendants(editor).OfType<ComboBox>().Single(c => c.Name == "MonitoredValueCb");
            string identity = monitor.MonitoredValue.ObjectType + "/" + monitor.MonitoredValue.LogicalName + "/" + monitor.MonitoredValue.AttributeIndex;
            toggle.Checked = false; Pump(); var filtered = targets.Items.Cast<object>().ToArray();
            toggle.Checked = true; Pump(); var expanded = targets.Items.Cast<object>().ToArray();
            Check("show-all-event-expands-targets", expanded.Length > filtered.Length && expanded.Contains(monitor.Parent.OfType<GXDLMSScriptTable>().First()));
            toggle.Checked = false; Pump();
            Check("show-all-event-restores-targets-and-binding", targets.Items.Cast<object>().SequenceEqual(filtered) && identity == monitor.MonitoredValue.ObjectType + "/" + monitor.MonitoredValue.LogicalName + "/" + monitor.MonitoredValue.AttributeIndex);
        }
    }
}
