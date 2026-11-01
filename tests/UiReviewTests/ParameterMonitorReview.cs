// superintelligentmeter contributors, 2026-10-30. Offline Parameter Monitor alignment. GPL-2.0-only.
// SPDX-License-Identifier: GPL-2.0-only
using Gurux.DLMS.Objects;
using Gurux.DLMS.UI;

internal static partial class Program
{
    static readonly string[] ParameterMonitorInputs = ["LogicalNameTB", "TargetTb", "IndexTb", "ValueTb", "CaptureTimeTb"];

    static void CheckParameterMonitor(Form editor, GXDLMSParameterMonitor model, Dictionary<string, Control> originals, CaseResult result, int step)
    {
        string prefix = "ParameterMonitor/" + result.State + "/step" + step;
        void Check(string name, bool passed, string detail = "") => result.BehaviorChecks.Add(new() { Name = prefix + "/" + name, Passed = passed, Detail = detail });
        Control Find(string name) => Descendants(editor).First(c => c.Name == name);
        Control Slot(Control control) => control.Parent?.GetType().Name is "ModernFieldFrame" or "NativeComboHost" ? control.Parent : control;
        Rectangle Relative(Control control) => new(editor.PointToClient(control.PointToScreen(Point.Empty)), control.Size);
        var fields = ParameterMonitorInputs.Select(Find).ToArray(); var slots = fields.Select(Slot).Select(Relative).ToArray();
        var labels = new[] { "LogicalNameLbl", "TargetLbl", "IndexLbl", "ValueLbl", "CaptureTimeLbl" }.Select(Find).ToArray();
        var captions = labels.Select(Relative).ToArray();
        Check("one-field-left-edge", slots.Max(r => r.Left) - slots.Min(r => r.Left) <= 2, string.Join("; ", ParameterMonitorInputs.Zip(slots, (n, r) => n + "=" + r)));
        Check("one-field-right-edge", slots.Max(r => r.Right) - slots.Min(r => r.Right) <= 2);
        Check("one-caption-left-edge", captions.Max(r => r.Left) - captions.Min(r => r.Left) <= 2);
        var tabOrder = fields.Select(Slot).OrderBy(c => Relative(c).Top).Select(c => c.TabIndex).ToArray();
        Check("visual-property-tab-order", tabOrder.Zip(tabOrder.Skip(1)).All(pair => pair.First < pair.Second), string.Join(",", tabOrder));
        int height = (int)Math.Round(editor.DeviceDpi * 32d / 96);
        Check("standard-field-slot-heights", slots.All(r => Math.Abs(r.Height - height) <= 2), "Expected=" + height + "; heights=" + string.Join(",", slots.Select(r => r.Height)));
        Check("caption-field-separation", captions.Zip(slots).All(pair => pair.First.Right <= pair.Second.Left + 2));
        Check("captions-readable", labels.All(label => TextRenderer.MeasureText(label.Text, label.Font, new Size(label.Width, 32767), TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix).Height <= label.Height + 2));
        Check("retained-input-instances", fields.All(c => ReferenceEquals(originals[c.Name], c)));
        Check("attribute-bindings", ((GXValueField)Find("LogicalNameTB")).Index == 1 && ((GXValueField)Find("CaptureTimeTb")).Index == 3);
        var changed = model.ChangedParameter; bool populated = changed?.Target != null;
        Check("changed-parameter-bound", Find("TargetTb").Text == (populated ? changed!.Target.ToString() : "") && Find("IndexTb").Text == (populated ? changed!.AttributeIndex.ToString() : "") && Find("ValueTb").Text == (populated ? Convert.ToString(changed!.Value) : ""));
        Check("changed-fields-readonly", new[] { "TargetTb", "IndexTb", "ValueTb" }.All(n => ((TextBox)Find(n)).ReadOnly));
        var list = (ListView)Find("ParametersLV"); var caption = Find("ParametersLbl"); var root = (TableLayoutPanel)Find("ParameterMonitorContent");
        var properties = Find("ParameterMonitorProperties"); var footer = Find("AddBtn").Parent!;
        Check("parameter-list-bound", list.Items.Count == model.Parameters.Count && list.Items.Cast<ListViewItem>().Select(i => i.Tag).SequenceEqual(model.Parameters.Cast<object>()) && list.Items.Cast<ListViewItem>().All(i => i.SubItems[1].Text == ((GXDLMSTarget)i.Tag!).AttributeIndex.ToString()));
        Check("list-caption-above-list", Relative(caption).Bottom <= Relative(list).Top + 2 && Math.Abs(Relative(caption).Left - Relative(list).Left) <= 6, "Caption=" + Relative(caption) + "; list=" + Relative(list));
        Check("parameter-list-usable", list.Width >= 240 * editor.DeviceDpi / 96 && list.Height >= 96 * editor.DeviceDpi / 96);
        Check("parameter-list-flexible-width", list.Width > slots[0].Width);
        Check("fixed-index-column", list.Columns.Count == 2 && Math.Abs(list.Columns[1].Width - 90 * editor.DeviceDpi / 96) <= 2);
        Check("properties-list-footer-tab-order", properties.TabIndex < list.TabIndex && list.TabIndex < footer.TabIndex);
        Check("root-row-allocation-settled", Math.Abs(root.GetRowHeights().Sum() - root.ClientSize.Height) <= 2, "Rows=" + string.Join(",", root.GetRowHeights()) + "; client=" + root.ClientSize.Height);
        Check("footer-uses-root-bottom", Math.Abs(footer.Bottom - root.ClientSize.Height) <= 2);
        Check("list-fills-space-before-footer", Math.Abs(list.Bottom - footer.Top) <= 2);
        Check("no-outer-horizontal-scroll", !editor.HorizontalScroll.Visible);
        foreach (var field in fields) Check(field.Name + "/standard-font", field.Font.FontFamily.Name == ApplicationTypography.BodyFont.FontFamily.Name && Math.Abs(field.Font.SizeInPoints - ApplicationTypography.BodyFont.SizeInPoints) < .1);
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

    static void CheckParameterMonitorReachability(CaseResult result)
    {
        foreach (int width in new[] { 1100, 620 })
        foreach (string name in ParameterMonitorInputs.Concat(new[] { "ParametersLbl", "ParametersLV", "AddBtn", "RemoveBtn" }))
        {
            var metrics = result.Captures.Where(c => c.Width == width && c.Status == "Captured").SelectMany(c => c.LayoutMetrics).Where(c => c.Name == name).ToArray();
            result.BehaviorChecks.Add(new() { Name = "ParameterMonitor/" + result.State + "/" + width + "/" + name + "/reachable", Passed = metrics.Any(c => c.VisibleWidth >= c.Width - 4 && c.VisibleHeight >= c.Height - 4), Detail = "Visible in a captured top/bottom/reveal position; no GXButton actions invoked." });
        }
        result.FixtureNotes.Add("Focused property/list alignment only. Long scalar strings remain in native read-only textboxes; no meter, settings or modal PreAction path is invoked. Test-host activation is not excluded.");
    }
}
