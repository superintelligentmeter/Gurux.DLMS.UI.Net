// Superintelligent Meter contributors, 2026-10-31. Reported layout regressions. GPL-2.0-only.
using Gurux.DLMS.UI;
internal static partial class Program
{
    static void CheckReportedLayouts(Form form, CaseResult result, CaptureResult capture)
    {
        Control Find(string name) => Descendants(form).Single(c => c.Name == name);
        Rectangle Bounds(Control c) => new(c.Parent!.PointToScreen(c.Location), c.Size);
        int S(int n) => (int)Math.Round(n * form.DeviceDpi / 96d);
        void Check(string name, bool pass, string detail = "") => result.BehaviorChecks.Add(new() { Name = "ReportedLayout/" + capture.Variant + "/" + name, Passed = pass, Detail = detail });
        if (form is GXDLMSActivityCalendarView or GXDLMSModemConfigurationView)
            foreach (var b in Descendants(form).OfType<Button>().Where(b => b.Visible))
                Check(b.Name + "/shared-style", b.FlatStyle == (SystemInformation.HighContrast ? FlatStyle.System : FlatStyle.Flat) && !b.UseVisualStyleBackColor);
        void Inline(string fieldName, string actionName)
        {
            var f = Find(fieldName); var b = Find(actionName); if (!f.Visible || !b.Visible) return;
            var field = Bounds(f); var action = Bounds(b);
            Check(actionName + "/gap", action.Left >= field.Right + S(12) || action.Top >= field.Bottom + S(4), $"Field={field}; action={action}");
            Check(actionName + "/aligned", Math.Abs(field.Top-action.Top)<=1 || field.Left==action.Left);
        }
        if (form is GXDLMSPushSetupView) Inline("LogicalNameTB", "PushBtn");
        (string action, string field) edge = form.GetType().Name switch
        {
            "GXDLMSG3PlcMacLayerCountersView" => ("ResetBtn", "RxDataBroadcastCountTB"),
            "GXDLMSPrimeNbOfdmPlcMacCountersView" => ("ResetBtn", "CSMABusyCounterTb"),
            "GXDLMSPrimeNbOfdmPlcPhysicalLayerCountersView" => ("ResetBtn", "RXDropCountTb"),
            "GXDLMSLlcSscsSetupView" => ("ResetBtn", "BaseAddressTb"),
            "GXDLMSAccountView" => ("ResetBtn", "MaxProvisionPeriodTb"),
            "GXDLMSCreditView" => ("InvokeCreditBtn", "PeriodTb"),
            _ => ("", "")
        };
        if (edge.action.Length>0 && Find(edge.action).Visible)
            Check("scalar-method-right-edge", Bounds(Find(edge.action)).Right==Bounds(Find(edge.field)).Right,
                $"Action={Bounds(Find(edge.action))}; field={Bounds(Find(edge.field))}");
        if (form.GetType().Name == "GXDLMSPrimeNbOfdmPlcMacNetworkAdministrationDataView")
        {
            var tabs=(TabControl)Find("Tabs");
            var list=Descendants(tabs.SelectedTab!).OfType<ListView>().First();
            Check("data-method-right-edge", Math.Abs(Bounds(Find("ResetBtn")).Right-Bounds(list).Right)<=1);
        }

        if (form.GetType().Name == "GXDLMSDisconnectControlView")
        {
            foreach (string name in new[] { "DisconnectBtn", "ReConnectBtn" })
            {
                var button = (GXButton)Find(name); var bounds = Bounds(button);
                bool contained = true;
                for (Control? parent=button.Parent; parent!=null && parent!=form; parent=parent.Parent)
                    contained &= parent.RectangleToScreen(parent.ClientRectangle).Contains(bounds);
                Check(name+"/full-ancestor-containment", contained, $"Button={bounds}");
                Check(name+"/original-method", button.Action==ActionType.Action && button.Index==(name=="DisconnectBtn"?1:2));
            }
            foreach (var table in form.Controls.OfType<TableLayoutPanel>())
                Check("outer-rows-fit", table.GetRowHeights().All(height=>height>=0), string.Join(",",table.GetRowHeights()));
        }
        if (form is GXDLMSIp6SetupView)
        {
            var tabs = (TabControl)Find("AddressTab");
            var list = Descendants(tabs.SelectedTab!).OfType<ListView>().First();
            var address = Find("AddAddressBtn"); var discovery = Find("AddBtn");
            int topGap = Bounds(address).Top - Bounds(list).Bottom;
            int lowerGap = Bounds(discovery).Top - Bounds(Find("DiscoverySetupView")).Bottom;
            Check("address-gap", Math.Abs(topGap-S(4))<=1, $"Gap={topGap}; expected={S(4)}");
            Check("discovery-gap", Math.Abs(lowerGap-S(4))<=1, $"Gap={lowerGap}; expected={S(4)}");
            Check("matching-button-gaps", Math.Abs(topGap-lowerGap)<=1);
            Check("shared-footer-in-active-tab", Descendants(tabs.SelectedTab!).Contains(address) && Descendants(form).Count(c => c.Name=="AddAddressBtn")==1);
            Check("address-right-edge", Math.Abs(Bounds(Find("RemoveAddressBtb")).Right-Bounds(list).Right)<=1);
        }
        if (form is GXDLMSSecuritySetupView)
        {
            Inline("SuiteTB", "UpdateKeyBtn");
            var activate = (GXButton)Find("ActivateSecurityBtn");
            var update = (GXButton)Find("UpdateKeyBtn");
            var policy = Find("SecurityPolicyTB").Parent!;
            if (activate.Visible && update.Visible)
            {
                var a = Bounds(activate); var u = Bounds(update); var p = Bounds(policy);
                Check("security-action-right-edges", a.Right==u.Right && a.Width==u.Width, $"Activate={a}; Update={u}");
                Check("activation-policy-gap", a.Left >= p.Right+S(12) || a.Top >= p.Bottom+S(4), $"Policy={p}; Activate={a}");
                Check("activation-policy-alignment", Math.Abs(a.Top-p.Top)<=1 || a.Left==p.Left);
                Check("activation-original-method", activate.Index==1 && activate.Action==ActionType.Action && update.Index==2);
                var checks = (CheckedListBox)Find("SecurityPolicyTB");
                if (checks.Visible && checks.Items.Count>0)
                    Check("complete-policy-list", checks.GetItemRectangle(checks.Items.Count-1).Bottom <= checks.ClientSize.Height);
            }
            var text = (TextBox)Find("SystemTitleDescriptionTb");
            if (text.Visible)
            {
                var frame = text.Parent!;
                Check("description-standard-width", Bounds(frame).Left == Bounds(Find("ClientSystemTitleTB")).Left && Bounds(frame).Right == Bounds(Find("ClientSystemTitleTB")).Right && frame.Width <= S(640));
                Check("description-five-lines", text.ClientSize.Height >= text.Font.Height * 5 && text.ClientSize.Height < text.Font.Height*6);
                Check("description-readonly", text.ReadOnly && text.ScrollBars==ScrollBars.Vertical);
            }
        }
        if (form is GXDLMSCompactDataView)
        {
            var remove = Find("ColumnRemoveBtn"); var captureButton = Find("CaptureBtn");
            if (remove.Visible && captureButton.Visible)
                Check("method-footer-right-edge", Math.Abs(Bounds(remove).Right-Bounds(captureButton).Right)<=1, $"Remove={Bounds(remove)}; Capture={Bounds(captureButton)}");
        }
    }
}
