// Superintelligent Meter contributors, 2026-10-30. Focused native caption/edge regressions. GPL-2.0-only.
using Gurux.DLMS.UI;

internal static partial class Program
{
    static void CheckCaptionAlignment(Form editor, CaseResult result, CaptureResult capture)
    {
        string type = editor.GetType().Name;
        if (!new[] { "GXDLMSAssociationLogicalNameView", "GXDLMSClockView", "GXDLMSImageTransferView", "GXDLMSActivityCalendarView", "GXDLMSAutoConnectView" }.Contains(type)) return;
        void Check(string name, bool pass, string detail = "") => result.BehaviorChecks.Add(new() { Name = "CaptionAlignment/" + capture.Width + "/" + capture.Variant + "/" + name, Passed = pass, Detail = detail });
        Control Find(string name) => Descendants(editor).Single(c => c.Name == name);
        Rectangle Outer(Control control) => new(control.Parent!.PointToScreen(control.Location), control.Size);
        void Caption(string name, string? exact = null)
        {
            var b = (Button)Find(name);
            if (!b.Visible) return;
            var m = capture.Buttons.Single(m => m.Name == name);
            int required = Math.Max(m.AmbientSingleWidth, m.NativeAdapterUnboundedWidth) + b.Padding.Horizontal + m.BorderSize * 2 + (int)Math.Round(8 * b.DeviceDpi / 96d);
            Check(name + "/caption-room", b.Width >= required, $"Width={b.Width}; native={m.NativeAdapterUnboundedWidth}; controlDC={m.TextWidth}; required={required}; font={m.FontPoints}; DPI={m.Dpi}");
            int requiredHeight = Math.Max(m.NativeAdapterBoundedHeight, m.TextHeight) + b.Padding.Vertical + m.BorderSize * 2 + (int)Math.Round(8 * b.DeviceDpi / 96d);
            Check(name + "/caption-height", b.Height >= requiredHeight, $"Height={b.Height}; line={m.NativeAdapterBoundedHeight}; controlDC={m.TextHeight}; padding={b.Padding.Vertical}; required={requiredHeight}; DPI={m.Dpi}");
            if (exact != null) Check(name + "/complete-caption", b.Text == exact, b.Text);
        }
        if (type == "GXDLMSAssociationLogicalNameView")
        {
            foreach (string name in new[] { "UpdatePwBtn", "UpdateHighPwBtn" })
            {
                Caption(name);
                var b = (Button)Find(name);
                if (b.Visible) Check(name + "/long-caption-minimum", b.Width >= Math.Round(200 * b.DeviceDpi / 96d));
            }
        }
        else if (type == "GXDLMSClockView")
        {
            var time = Outer(Find("TimeTB")); var zone = Outer(Find("TimeZoneTB"));
            var current = Outer(Find("UpdateTimeBtn")); var currentZone = Outer(Find("CurrentTimeZoneBtn"));
            Check("paired-input-edges", time.Left == zone.Left && time.Right == zone.Right && time.Height == zone.Height, $"Time={time}; zone={zone}");
            Check("paired-action-edges", current.Left == currentZone.Left && current.Right == currentZone.Right && current.Height == currentZone.Height, $"Time={current}; zone={currentZone}");
            foreach (string name in new[] { "UpdateTimeBtn", "CurrentTimeZoneBtn", "AdjustToQuarterBtn", "AdjustToMeasuringPeriodBtn", "AdjustToMinuteBtn", "AdjustToPresetTimeBtn", "PresetAdjustingTimeBtn", "ShiftTimeBtn" }) Caption(name);
            Check("preset-full-name", Find("PresetAdjustingTimeBtn").Text == "Preset Adjusting Time");
            var grid = (TableLayoutPanel)Find("PresetAdjustingTimeBtn").Parent!;
            var buttons = grid.Controls.OfType<Button>().Where(b => b.Visible).ToArray();
            Check("grid-equal-widths", buttons.Select(b => b.Width).Distinct().Count() <= 2 && buttons.Max(b => b.Width) - buttons.Min(b => b.Width) <= 1);
        }
        else if (type == "GXDLMSImageTransferView")
        {
            Caption("ActivateImageBtn", "Activate Image"); Caption("VerifyImageBtn", "Verify Image"); Caption("UpdateImageBtn", "Update Image");
            int dataRight = Outer(Find("ImagesView")).Right;
            foreach (string name in new[] { "ImageTransferredBlocksStatusTB", "DescriptionList", "UpdateImageBtn" })
                Check(name + "/shared-data-right-edge", Math.Abs(Outer(Find(name)).Right-dataRight)<=1,
                    $"Images={Outer(Find("ImagesView"))}; control={Outer(Find(name))}");
            foreach (string name in new[] { "ActivateImageBtn", "VerifyImageBtn", "UpdateImageBtn" })
                Check(name + "/consistent-native-caption-renderer", ((Button)Find(name)).UseCompatibleTextRendering);
        }
        else if (type == "GXDLMSActivityCalendarView")
        {
            foreach (var b in Descendants(editor).OfType<Button>().Where(b => b.Visible && b.Parent is ActionButtonPanel && b.Parent.Parent is Panel panel && panel.Dock == DockStyle.Bottom))
            {
                var rect = b.RectangleToScreen(b.ClientRectangle);
                bool contained = true;
                for (Control? parent = b.Parent; parent != null && parent != editor; parent = parent.Parent)
                {
                    contained &= parent.RectangleToScreen(parent.ClientRectangle).Contains(rect);
                    if (parent is TabPage) break;
                }
                Check(b.Name + "/footer-full-height", contained, $"Button={b.Bounds}; footer={b.Parent!.Bounds}; panel={b.Parent!.Parent!.ClientRectangle}");
            }
        }
        else if (type == "GXDLMSAutoConnectView")
        {
            var windows = Outer(Find("CallingWindowLV")); var destinations = Outer(Find("DestinationLv"));
            Check("list-edges", windows.Left == destinations.Left && windows.Right == destinations.Right, $"Calling={windows}; destinations={destinations}");
            foreach (var pair in new[] { ("TimeRemoveBtn", windows), ("DestinationRemoveBtn", destinations), ("ConnectBtn", windows) })
                Check(pair.Item1 + "/right-edge", Outer(Find(pair.Item1)).Right == pair.Item2.Right, $"Button={Outer(Find(pair.Item1))}; list={pair.Item2}");
            Check("calling-caption-above-list", Outer(Find("CallingWindowLbl")).Bottom <= windows.Top);
        }
    }
}
