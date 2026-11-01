// superintelligentmeter contributors, 2026-10-30. Offline alignment regressions. GPL-2.0-only.
// SPDX-License-Identifier: GPL-2.0-only
using Gurux.DLMS.Objects;
using Gurux.DLMS.UI;

internal static partial class Program
{
    static readonly string[] ActionScheduleInputs = ["LogicalNameTB", "ScriptNameTB", "ScriptSelectorTB", "ScriptTypeTb"];

    static void CheckActionSchedule(Form editor, GXDLMSActionSchedule model, Dictionary<string, Control> originals, CaseResult result, int step)
    {
        string prefix = "ActionSchedule/" + result.State + "/step" + step;
        void Check(string name, bool passed, string detail = "") => result.BehaviorChecks.Add(new() { Name = prefix + "/" + name, Passed = passed, Detail = detail });
        Control Find(string name) => Descendants(editor).First(c => c.Name == name);
        Control Slot(Control control) => control.Parent?.GetType().Name is "ModernFieldFrame" or "NativeComboHost" ? control.Parent : control;
        Rectangle Relative(Control control) => new(editor.PointToClient(control.PointToScreen(Point.Empty)), control.Size);
        var fields = ActionScheduleInputs.Select(Find).ToArray();
        var slots = fields.Select(Slot).Select(Relative).ToArray();
        var labels = new[] { "LogicalNameLbl", "ScriptNameLbl", "ScriptSelectorLbl", "ScriptTypeLbl" }.Select(Find).ToArray();
        var captions = labels.Select(Relative).ToArray();
        Check("one-field-left-edge", slots.Max(r => r.Left) - slots.Min(r => r.Left) <= 2, string.Join("; ", ActionScheduleInputs.Zip(slots, (n, r) => n + "=" + r)));
        Check("one-field-right-edge", slots.Max(r => r.Right) - slots.Min(r => r.Right) <= 2);
        Check("one-caption-left-edge", captions.Max(r => r.Left) - captions.Min(r => r.Left) <= 2);
        var slotTabIndexes = fields.Select(Slot).Select(c => c.TabIndex).ToArray();
        Check("property-tab-order", slotTabIndexes.Zip(slotTabIndexes.Skip(1)).All(pair => pair.First < pair.Second), string.Join(",", slotTabIndexes));
        int height = (int)Math.Round(editor.DeviceDpi * 32d / 96);
        Check("standard-field-slot-heights", slots.All(r => Math.Abs(r.Height - height) <= 2), "Expected=" + height + "; heights=" + string.Join(",", slots.Select(r => r.Height)));
        Check("caption-field-separation", captions.Zip(slots).All(pair => pair.First.Right <= pair.Second.Left + 2));
        Check("captions-readable", labels.All(label => TextRenderer.MeasureText(label.Text, label.Font, new Size(label.Width, 32767), TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix).Height <= label.Height + 2));
        Check("retained-input-instances", fields.All(c => ReferenceEquals(originals[c.Name], c)));
        Check("attribute-bindings", ((GXValueField)Find("LogicalNameTB")).Index == 1 && ((GXValueField)Find("ScriptTypeTb")).Index == 3);
        var scripts = (ComboBox)Find("ScriptNameTB"); var selector = (TextBox)Find("ScriptSelectorTB"); var times = (ListView)Find("Time");
        Check("script-reference-and-selector-bound", ReferenceEquals(scripts.SelectedItem, model.Target) && selector.Text == model.ExecutedScriptSelector.ToString());
        Check("execution-list-bound", times.Items.Count == (model.ExecutionTime?.Length ?? 0) && times.Items.Cast<ListViewItem>().Select(i => i.Tag).SequenceEqual((model.ExecutionTime ?? []).Cast<object>()));
        Check("execution-list-usable", times.Width >= 240 * editor.DeviceDpi / 96 && times.Height >= 96 * editor.DeviceDpi / 96);
        Check("execution-list-flexible-width", times.Width > slots[0].Width);
        var root = (TableLayoutPanel)Find("ActionScheduleContent");
        var properties = Find("ActionScheduleProperties"); var footer = Find("TimeAddBtn").Parent!;
        Check("properties-list-footer-tab-order", properties.TabIndex < times.TabIndex && times.TabIndex < footer.TabIndex);
        Check("root-row-allocation-settled", Math.Abs(root.GetRowHeights().Sum() - root.ClientSize.Height) <= 2, "Rows=" + string.Join(",", root.GetRowHeights()) + "; client=" + root.ClientSize.Height);
        Check("footer-uses-root-bottom", Math.Abs(footer.Bottom - root.ClientSize.Height) <= 2, "FooterBottom=" + footer.Bottom + "; rootHeight=" + root.ClientSize.Height);
        Check("list-fills-space-before-footer", Math.Abs(times.Bottom - footer.Top) <= 2 && Math.Abs(times.Top - (properties.Bottom + properties.Margin.Bottom)) <= 2, "List=" + times.Bounds + "; footerTop=" + footer.Top + "; propertiesBottom=" + properties.Bottom);
        Check("no-outer-horizontal-scroll", !editor.HorizontalScroll.Visible);
        foreach (var field in fields) Check(field.Name + "/standard-font", field.Font.FontFamily.Name == ApplicationTypography.BodyFont.FontFamily.Name && Math.Abs(field.Font.SizeInPoints - ApplicationTypography.BodyFont.SizeInPoints) < .1);
        foreach (var combo in new[] { scripts }.Concat(Descendants(Find("ScriptTypeTb")).OfType<ComboBox>().Where(c => c.Visible)))
        {
            GetWindowRect(combo.Handle, out var native);
            Check(combo.Name + "/native-combo-height", Math.Abs(native.Bottom - native.Top - height) <= 2);
        }
        if (step == 2)
        {
            var initial = result.Captures.First(c => c.Variant == "default" && c.Width == 1100);
            foreach (var field in fields)
            {
                var before = initial.Controls.First(c => c.Name == field.Name); var now = Relative(field);
                Check(field.Name + "/restored-width-height", Math.Abs(before.Width - now.Width) <= 2 && Math.Abs(before.Height - now.Height) <= 2);
            }
        }
    }

    static void CheckActionScheduleReachability(CaseResult result)
    {
        foreach (int width in new[] { 1100, 620 })
        foreach (string name in ActionScheduleInputs.Concat(new[] { "Time", "TimeAddBtn", "TimeEditBtn", "TimeRemoveBtn" }))
        {
            var metrics = result.Captures.Where(c => c.Width == width && c.Status == "Captured").SelectMany(c => c.LayoutMetrics).Where(c => c.Name == name).ToArray();
            result.BehaviorChecks.Add(new() { Name = "ActionSchedule/" + result.State + "/" + width + "/" + name + "/reachable", Passed = metrics.Any(c => c.VisibleWidth >= c.Width - 4 && c.VisibleHeight >= c.Height - 4), Detail = "Visible in a captured top/bottom/reveal position; no actions invoked." });
        }
        result.FixtureNotes.Add("Focused alignment audit: original fields and bound models only; no settings, meter commands, modal Add/Edit actions, pointer input or global keys. Native DrawToBitmap and test-host activation limitations remain.");
    }
}
