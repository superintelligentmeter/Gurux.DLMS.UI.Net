// superintelligentmeter contributors, 2026-10-30. Offline Image Transfer viewport checks. GPL-2.0-only.
// SPDX-License-Identifier: GPL-2.0-only
using Gurux.DLMS.Objects;
using Gurux.DLMS.Objects.Enums;
using Gurux.DLMS.UI;

internal static partial class Program
{
    static readonly string[] ImageTransferInputs = ["DescriptionTB", "LogicalNameTB", "ImageBlockSizeTB", "ImageFirstNotTransferredBlockNumberTB", "ImageTransferredBlocksStatusTB", "ImageTransferEnabledCB", "ImageTransferStatusTb", "ImagesView", "ManualBtn", "DelayTb", "ActivateImageBtn", "VerifyImageBtn", "UpdateImageBtn", "DescriptionList"];
    static readonly Dictionary<Form, Dictionary<Control, (string Family, float Points, int Fore, int Back)>> ImageAppearance = new();
    static readonly Dictionary<Form, float> ImageBodyPoints = new();
    static int ImageS(Control c, int value) => (int)Math.Round(value * c.DeviceDpi / 96d);
    [System.Runtime.InteropServices.DllImport("user32.dll", EntryPoint = "GetDpiForWindow")]
    static extern uint ImageGetDpiForWindow(IntPtr handle);
    static int ImageViewportS(Control c, int value) => (int)Math.Round(value * ImageGetDpiForWindow(c.Handle) / 96d);
    static Size ImageNormal(Control c) => new(ImageViewportS(c, 1530), ImageViewportS(c, 780));

    [System.Runtime.InteropServices.DllImport("user32.dll", EntryPoint = "MonitorFromPoint")]
    static extern IntPtr ImageMonitorFromPoint(Point point, uint flags);
    [System.Runtime.InteropServices.DllImport("shcore.dll", EntryPoint = "GetScaleFactorForMonitor")]
    static extern int ImageGetMonitorScale(IntPtr monitor, out int scale);
    static Size[] ImageFixtureSizes(Control shell, int monitor)
    {
        // A newly hosted Form can retain startup DPI until its first resize.
        // Choose the requested viewport from the selected physical monitor;
        // captured HWND/editor DPI remains the authoritative rendered evidence.
        var bounds = Screen.AllScreens[Math.Max(0, monitor)].Bounds;
        int hr = ImageGetMonitorScale(ImageMonitorFromPoint(new Point(bounds.X + bounds.Width / 2, bounds.Y + bounds.Height / 2), 2), out int percent);
        int dpi = hr == 0 && percent > 0 ? (int)Math.Round(percent * 96d / 100d) : (int)ImageGetDpiForWindow(shell.Handle);
        int S(int value) => (int)Math.Round(value * dpi / 96d);
        return [new(S(1530), S(780)), new(S(1250), S(780)), new(1250, 760), new(620, 560), new(S(1530), S(780))];
    }

    static void PopulateImageTransferFixture(GXDLMSImageTransfer model, string state)
    {
        model.ImageBlockSize = 512;
        model.ImageTransferredBlocksStatus = state == "blank" ? "" : string.Concat(Enumerable.Repeat("11110000", state == "long-images" ? 256 : 8));
        model.ImageTransferEnabled = state != "blank";
        model.ImageTransferStatus = ImageTransferStatus.NotInitiated;
        model.ImageActivateInfo = state == "blank" ? [] : Enumerable.Range(1, state == "long-images" ? 40 : 3).Select(i => new GXDLMSImageActivateInfo
        {
            Size = (uint)(i * 4096),
            Identification = System.Text.Encoding.ASCII.GetBytes(state == "long-images" ? "Laboratory firmware release image with a deliberately long identifier, meter platform and version - " + i : "Firmware image " + i),
            Signature = System.Text.Encoding.ASCII.GetBytes(state == "long-images" ? "Signature metadata retained in full for offline rendering and native list scrolling - " + i : "SIG-" + i)
        }).ToArray();
        if (state == "long-images") model.Description = "Image Transfer commissioning configuration with many firmware images and deliberately long identification and signature metadata";
    }

    static void PopulateImageTransferLog(Form editor, string state)
    {
        var log = Descendants(editor).OfType<ListView>().Single(c => c.Name == "DescriptionList");
        for (int i = 0; i < (state == "blank" ? 0 : state == "long-images" ? 40 : 3); ++i)
        {
            var row = log.Items.Add("14:30:" + i.ToString("00"));
            row.SubItems.Add(state == "long-images" ? "Offline retained transfer log fixture with a deliberately long description, sequence " + i : "Offline transfer log fixture " + i);
        }
    }

    static void CheckImageTransfer(Form editor, GXDLMSImageTransfer model, Dictionary<string, Control> originals, CaseResult result, int step, bool requireNoOuterScroll)
    {
        void Check(string name, bool pass, string detail = "") => result.BehaviorChecks.Add(new() { Name = "ImageTransfer/" + result.State + "/step" + step + "/" + name, Passed = pass, Detail = detail });
        Control Find(string name) => Descendants(editor).Single(c => c.Name == name);
        Rectangle Visible(Control c)
        {
            var r = c.RectangleToScreen(c.ClientRectangle);
            for (Control? p = c.Parent; p != null; p = p.Parent) r = Rectangle.Intersect(r, p.RectangleToScreen(p.ClientRectangle));
            return r;
        }
        var current = ImageTransferInputs.Select(Find).ToArray();
        Check("original-instances-retained", current.All(c => ReferenceEquals(c, originals[c.Name])));
        Check("bound-attribute-indices", ((GXValueField)Find("LogicalNameTB")).Index == 1 && ((GXValueField)Find("ImageBlockSizeTB")).Index == 2 && ((GXValueField)Find("ImageFirstNotTransferredBlockNumberTB")).Index == 4);
        Check("description-and-block-status-complete", Find("DescriptionTB").Text == (model.Description ?? "") && Find("ImageTransferredBlocksStatusTB").Text == model.ImageTransferredBlocksStatus);
        Check("enabled-and-status-bound", ((CheckBox)Find("ImageTransferEnabledCB")).Checked == model.ImageTransferEnabled && Find("ImageTransferStatusTb").Text == model.ImageTransferStatus.ToString());
        var images = (ListView)Find("ImagesView"); var log = (ListView)Find("DescriptionList");
        var expected = model.ImageActivateInfo ?? [];
        Check("image-collection-and-tags-retained", images.Items.Count == expected.Length && images.Items.Cast<ListViewItem>().Select((r, i) => ReferenceEquals(r.Tag, expected[i]) && r.Text == expected[i].Size.ToString() && r.SubItems[1].Text == System.Text.Encoding.ASCII.GetString(expected[i].Identification) && r.SubItems[2].Text == System.Text.Encoding.ASCII.GetString(expected[i].Signature)).All(v => v));
        foreach (var list in new[] { images, log })
        {
            Check(list.Name + "/header-and-rows-readable", list.ClientSize.Height >= list.Font.Height * 3 + 2, "Client=" + list.ClientSize + "; font=" + list.Font.Height);
            Check(list.Name + "/native-data-surface-retained", list.Scrollable && list.View == View.Details && list.Columns.Count == (list == images ? 3 : 2));
            if (list.Items.Count > 10)
            {
                list.Items[^1].EnsureVisible(); Pump(); var last = list.Items[^1].Bounds;
                Check(list.Name + "/last-native-row-scroll-reachable", last.Top >= -1 && last.Bottom <= list.ClientSize.Height + 2, "Last=" + last + "; client=" + list.ClientSize);
                list.TopItem = list.Items[0]; Pump();
            }
        }
        if (images.Parent is TableLayoutPanel rows)
        {
            int row = rows.GetRow(images);
            Check("compact-image-row-current-dpi", rows.RowStyles[row].SizeType == SizeType.Absolute && Math.Abs(rows.GetRowHeights()[row] - ImageS(editor, 80)) <= 2, "Actual=" + rows.GetRowHeights()[row] + "; dpi=" + editor.DeviceDpi);
        }
        Check("actions-precede-transfer-log", Find("UpdateImageBtn").Parent!.Bottom <= log.Top + 2);
        if (requireNoOuterScroll)
        {
            foreach (var c in current) { var r=Visible(c); Check(c.Name + "/fully-visible-at-scroll-top", r.Width+2 >= c.ClientSize.Width && r.Height+2 >= c.ClientSize.Height, "Bounds=" + c.Bounds + "; visible=" + r); }
            Check("no-outer-vertical-scroll", !editor.VerticalScroll.Visible);
        }
        var appearance = current.ToDictionary(c => c, c => (c.Font.FontFamily.Name, c.Font.SizeInPoints, c.ForeColor.ToArgb(), c.BackColor.ToArgb()));
        if (step == 0) { ImageAppearance[editor] = appearance; ImageBodyPoints[editor] = editor.Font.SizeInPoints; }
        else if (step == 5)
        {
            // On the other physical display, compare roles with the CURRENT
            // hosted body rather than forcing the startup point size. Return
            // still requires exact original points/colors to catch accumulation.
            Check("font-roles-colors-follow-current-body", appearance.All(p =>
                p.Value.Item1 == ImageAppearance[editor][p.Key].Family &&
                Math.Abs(p.Value.Item2 / editor.Font.SizeInPoints - ImageAppearance[editor][p.Key].Points / ImageBodyPoints[editor]) < 0.01f &&
                p.Value.Item3 == ImageAppearance[editor][p.Key].Fore && p.Value.Item4 == ImageAppearance[editor][p.Key].Back));
        }
        else Check("fonts-colors-retained", appearance.All(p => ImageAppearance[editor][p.Key] == p.Value));
        if (step == 4)
        {
            var initial = result.Captures.First(c => c.Variant == "default");
            foreach (var c in new[] { images, log })
            {
                var before=initial.Controls.First(x => x.Name==c.Name);
                Check(c.Name + "/restored-size", Math.Abs(before.Width-c.Width)<=2 && Math.Abs(before.Height-c.Height)<=2);
            }
        }
    }

    static void CaptureImageTransferReveals(Form editor, CaseResult result, string output, Size size, string variant)
    {
        foreach (var c in Descendants(editor).Where(c => ImageTransferInputs.Contains(c.Name)).ToArray())
        {
            bool captured=result.Captures.Where(x=>x.Width==size.Width && x.Height==size.Height).SelectMany(x=>x.LayoutMetrics).Where(x=>x.Name==c.Name).Any(x=>x.VisibleWidth+2>=x.ClientWidth && x.VisibleHeight+2>=x.ClientHeight);
            if(captured)continue;
            for(Control? parent=c.Parent;parent!=null;parent=parent.Parent)if(parent is ScrollableControl sc && sc.AutoScroll){sc.ScrollControlIntoView(c);Pump();}
            CaptureAtPosition(editor,result,output,size,variant+"-reveal-"+c.Name);
        }
        foreach(var sc in Descendants(editor).Prepend(editor).OfType<ScrollableControl>().Where(c=>c.AutoScroll))sc.AutoScrollPosition=Point.Empty;
        Pump();
    }

    static void CheckImageTransferReachability(CaseResult result)
    {
        foreach(var size in result.Captures.Select(c=>(c.Width,c.Height)).Distinct())
        foreach(string name in ImageTransferInputs)
        {
            var data=result.Captures.Where(c=>c.Width==size.Width && c.Height==size.Height).SelectMany(c=>c.LayoutMetrics).Where(c=>c.Name==name);
            result.BehaviorChecks.Add(new(){Name="ImageTransfer/"+result.State+"/"+size+"/"+name+"/reachable",Passed=data.Any(c=>c.VisibleWidth+2>=c.ClientWidth && c.VisibleHeight+2>=c.ClientHeight),Detail="Normal screenshot-equivalent logical viewports require no outer scrolling; smaller physical windows may use the existing outer scrollbar."});
        }
        result.FixtureNotes.Add("Image Transfer fixtures are offline; manual log rows are test data. No Activate/Verify/Update, image file, transport or dialog action invoked. Image update uses unchanged GXImageDlg(manufacturer settings, client). Settings provider is memory-only.");
    }

    static void ImageTransferMonitorRoundtrip(Form shell, Form editor, GXDLMSImageTransfer model, Dictionary<string,Control> originals, CaseResult result, string output, int monitor)
    {
        if(monitor!=0 || result.State!="representative" || Screen.AllScreens.Length<2)return;
        foreach(int target in new[]{1,0})
        {
            var bounds=Screen.AllScreens[target].WorkingArea;
            shell.Location=new Point(bounds.X+24,bounds.Y+24);for(int i=0;i<8;i++)Pump();
            shell.ClientSize=ImageNormal(editor);shell.PerformLayout();editor.PerformLayout();for(int i=0;i<6;i++)Pump();
            shell.WindowState=FormWindowState.Minimized;Pump();shell.WindowState=FormWindowState.Normal;for(int i=0;i<6;i++)Pump();
            shell.ClientSize=ImageNormal(editor);editor.Bounds=shell.ClientRectangle;shell.PerformLayout();editor.PerformLayout();for(int i=0;i<6;i++)Pump();
            CheckImageTransfer(editor,model,originals,result,target==0?6:5,true);
            Capture(editor,result,output,shell.ClientSize,"monitor-roundtrip-"+target+"-dpi"+editor.DeviceDpi);
        }
        result.FixtureNotes.Add("Representative monitor0 fixture moved to the other real monitor and back, with owned-shell minimize/restore. No global keys, pointer movement or meter actions. Known native test-host activation limitation remains; foreground isolation is not claimed.");
    }
}
