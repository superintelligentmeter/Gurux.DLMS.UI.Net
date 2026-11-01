// superintelligentmeter contributors, 2026-10-30. Bounded button monitor gate. GPL-2.0-only.
// SPDX-License-Identifier: GPL-2.0-only
using Gurux.DLMS.UI;

internal static partial class Program
{
    static void ReviewButtonRoundtrip(Form shell, Form editor, CaseResult result, string output)
    {
        void Check(string name, bool pass, string detail = "") => result.BehaviorChecks.Add(new() { Name = "Buttons/roundtrip/" + name, Passed = pass, Detail = detail });
        var screens = Screen.AllScreens;
        var origin = screens[0].WorkingArea;
        void SetSize(Size size)
        {
            shell.ClientSize = size; Pump(); shell.ClientSize = size;
            editor.Bounds = shell.ClientRectangle; shell.PerformLayout(); editor.PerformLayout();
            for (int n = 0; n < 5; n++) Pump();
        }
        string Geometry() => string.Join(";", ButtonAuditMetrics.Measure(editor).Where(b => b.Visible).OrderBy(b => b.Name).Select(b =>
            $"{b.Name}:{b.OuterX},{b.OuterY},{b.Width},{b.Height}:{b.FontFamily},{b.FontPoints:0.00},{b.FontStyle}:{b.ForeColor},{b.BackColor}:{b.ActionIndex},{b.Action}"));
        shell.Location = new(origin.X + 24, origin.Y + 24); for (int n = 0; n < 6; n++) Pump();
        SetSize(new(1100, 760)); Capture(editor, result, output, new(1100,760), "gate-origin");
        string initial = Geometry();
        var before = ButtonAuditMetrics.Measure(editor).Where(b => b.Visible).ToDictionary(b => b.Name);
        foreach (int index in Enumerable.Range(0, screens.Length).Concat(new[] { 0 }))
        {
            var area = screens[index].WorkingArea;
            shell.Location = new(area.X + 24, area.Y + 24); for (int n = 0; n < 6; n++) Pump();
            SetSize(new(1100, 760));
            string label = "gate-monitor" + index + "-dpi" + editor.DeviceDpi;
            Check(label + "/actual-monitor", Screen.FromControl(shell).DeviceName == screens[index].DeviceName);
            Capture(editor, result, output, new(1100,760), label + "-wide");
            if (editor.GetType().Name == "GXDLMSActivityCalendarView")
            {
                SetSize(new(320,560));
                Capture(editor, result, output, new(320,560), label + "-wrapped");
                var members = ButtonAuditMetrics.Measure(editor).Where(b => b.Visible && new[] { "SeasonProfileActiveAddBtn", "SeasonProfileActiveEditBtn", "SeasonProfileActiveRemoveBtn" }.Contains(b.Name)).ToArray();
                Check(label + "/calendar-wrap-exercised", members.Select(b => b.OuterY).Distinct().Count() > 1);
                var visual = members.OrderBy(b => b.OuterY).ThenBy(b => b.OuterX).Select(b => b.Name).ToArray();
                Check(label + "/wrapped-reading-order", visual.SequenceEqual(new[] { "SeasonProfileActiveAddBtn", "SeasonProfileActiveEditBtn", "SeasonProfileActiveRemoveBtn" }), string.Join(",", visual));
                Check(label + "/wrapped-tab-order", members.OrderBy(b => b.TabIndex).Select(b => b.Name).SequenceEqual(visual));
            }
            SetSize(new(1100,760));
            shell.WindowState = FormWindowState.Minimized; for (int n = 0; n < 3; n++) Pump();
            Check(label + "/minimized", shell.WindowState == FormWindowState.Minimized);
            shell.WindowState = FormWindowState.Normal; SetSize(new(1100,760));
            Capture(editor, result, output, new(1100,760), label + "-minimize-restored");
            shell.WindowState = FormWindowState.Maximized; for (int n = 0; n < 4; n++) Pump();
            Check(label + "/maximized", shell.WindowState == FormWindowState.Maximized);
            shell.WindowState = FormWindowState.Normal; SetSize(new(1100,760));
            Capture(editor, result, output, new(1100,760), label + "-maximize-restored");
            Check(label + "/original-controls-bindings", ButtonAuditMetrics.OriginalsRetained(editor));
        }
        shell.Location = new(origin.X + 24, origin.Y + 24); for (int n = 0; n < 6; n++) Pump();
        SetSize(new(1100,760)); Capture(editor, result, output, new(1100,760), "gate-return");
        var after = ButtonAuditMetrics.Measure(editor).Where(b => b.Visible).ToDictionary(b => b.Name);
        bool restored = before.Count == after.Count && before.All(pair => after.TryGetValue(pair.Key, out var b) &&
            Math.Abs(pair.Value.OuterX-b.OuterX)<=2 && Math.Abs(pair.Value.OuterY-b.OuterY)<=2 && Math.Abs(pair.Value.Width-b.Width)<=2 && Math.Abs(pair.Value.Height-b.Height)<=2 &&
            pair.Value.FontFamily==b.FontFamily && Math.Abs(pair.Value.FontPoints-b.FontPoints)<.1 && pair.Value.FontStyle==b.FontStyle &&
            pair.Value.ForeColor==b.ForeColor && pair.Value.BackColor==b.BackColor && pair.Value.ActionIndex==b.ActionIndex && pair.Value.Action==b.Action);
        Check("return-geometry-font-colors-bindings", restored, "Before=" + initial + "; after=" + Geometry());
        result.FixtureNotes.Add("Single bounded physical-monitor button gate used normal/minimize/maximize and returned to origin. Calendar forced native footer wrapping at320physical pixels;360pixels was insufficient to force wrapping at96DPI in the earlier raw probe. No action button, transport, settings operation or global keyboard/pointer input invoked; existing test-host focus limitation applies.");
    }
}
