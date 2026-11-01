// superintelligentmeter contributors, 2026-11-01: shared RoundTabs for tab controls outside the object editors. GPL-2.0-only; original notices retained.
// superintelligentmeter contributors, 2026-10-30: aligned Activity Calendar rows and responsive profile tables. GPL-2.0-only; original notices retained.
// superintelligentmeter contributors, 2026-10-30: consistent native combo presentation across object editors. GPL-2.0-only; original notices retained.
// superintelligentmeter contributors, 2026-10-30: native Clock base combo without a visible wrapper. GPL-2.0-only; original notices retained.
// superintelligentmeter contributors, 2026-10-30: measured full-width sort reference row. GPL-2.0-only; original notices retained.
// superintelligentmeter contributors, 2026-10-29: responsive editor rows and structured-value sizing. GPL-2.0-only; original notices retained.
// Modified for the superintelligentmeter fork, 2026-10-16: .NET 10 migration, UI modernization and compatibility fixes. GPL-2.0-only; original notices retained.
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Gurux.DLMS.UI
{
    // Presentation only. Existing fields, handlers and DLMS attribute indices remain intact.
    public static partial class ModernPresentation
    {
        private static readonly Font BodyFont = ApplicationTypography.BodyFont;
        private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<Form, object> Styled = new();
        private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<TabControl, TabHeaderPainter> TabPainters = new();

        private sealed class TabHeaderPainter : NativeWindow
        {
            private readonly TabControl tabs;
            internal TabHeaderPainter(TabControl tabs) { this.tabs = tabs; AssignHandle(tabs.Handle); }

            protected override void WndProc(ref Message message)
            {
                base.WndProc(ref message);
                if (message.Msg == 0x000F) // WM_PAINT
                {
                    IntPtr hdc = GetDC(Handle);
                    if (hdc != IntPtr.Zero)
                    {
                        using (var graphics = Graphics.FromHdc(hdc)) PaintRemainder(graphics);
                        ReleaseDC(Handle, hdc);
                    }
                }
                else if ((message.Msg == 0x0317 || message.Msg == 0x0318) && message.WParam != IntPtr.Zero)
                {
                    using var graphics = Graphics.FromHdc(message.WParam);
                    PaintRemainder(graphics);
                }
            }

            private void PaintRemainder(Graphics graphics)
            {
                if (tabs.IsDisposed || tabs.TabPages.Count == 0) return;
                // The native header paints its 3-D edges after owner drawing; repaint the rounded tabs over them.
                for (int i = 0; i < tabs.TabPages.Count; i++)
                    DrawRoundedTab(tabs, new DrawItemEventArgs(graphics, tabs.Font, tabs.GetTabRect(i), i,
                        i == tabs.SelectedIndex ? DrawItemState.Selected : DrawItemState.None));
                Rectangle last = tabs.GetTabRect(tabs.TabPages.Count - 1);
                if (last.Right < tabs.ClientSize.Width)
                    graphics.FillRectangle(Brushes.White, last.Right, 0,
                        tabs.ClientSize.Width - last.Right, Math.Max(tabs.ItemSize.Height, last.Bottom));
            }
        }

        [DllImport("user32.dll")]
        private static extern IntPtr GetDC(IntPtr hwnd);
        [DllImport("user32.dll")]
        private static extern int ReleaseDC(IntPtr hwnd, IntPtr hdc);

        private sealed class ModernFieldFrame : Panel
        {
            public ModernFieldFrame(Control editor, Color backColor)
            {
                BackColor = backColor;
                Controls.Add(editor);
                editor.Enter += (_, _) => Invalidate();
                editor.Leave += (_, _) => Invalidate();
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);
                Color color = SystemInformation.HighContrast ? SystemColors.WindowText :
                    ContainsFocus ? Accent : Line;
                using var pen = new Pen(color);
                e.Graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
            }
        }

        /// <summary>Apply the shared presentation once, retaining existing controls and event handlers.</summary>
        public static void Apply(Form form)
        {
            if (Styled.TryGetValue(form, out _)) { StyleTree(form); return; }
            Styled.Add(form, new object());
            if (ReflowRemainingObject(form))
            {
                StyleTree(form);
                ApplicationTypography.Apply(form);
                ConfigureEditorTabs(form);
                form.SystemColorsChanged += (_, _) => StyleTree(form);
                return;
            }
            if (form is GXDLMSDataView || form is GXDLMSRegisterView)
                CompactObject(form, form is GXDLMSRegisterView);
            else if (form is GXDLMSRegisterActivationView)
                ReflowRegisterActivation(form);
            else if (form is GXDLMSScriptTableView)
                ReflowScriptTable(form);
            else if (form is GXDLMSProfileGenericView)
                ReflowProfileGeneric(form);
            else if (form is GXDLMSClockView)
                CompactClock(form);
            else if (form is GXDLMSActionScheduleView)
                ReflowActionSchedule(form);
            else if (form is GXDLMSParameterMonitorView)
                ReflowParameterMonitor(form);
            StyleTree(form);
            ApplicationTypography.Apply(form);
            ConfigureEditorTabs(form);
            ReflowOverlappingLabelFields(form);
            if (form is GXDLMSIECLocalPortSetupView) ReflowIecLocalPortSetup(form);
            else if (form is GXDLMSAutoConnectView) ReflowAutoConnect(form);
            else if (form is GXDLMSImageTransferView) ReflowImageTransfer(form);
            else if (form is GXDLMSGprsSetupView) ReflowGprsSetup(form);
            else if (form is GXDLMSGSMDiagnosticView) ReflowGsmDiagnostic(form);
            else if (form is GXDLMSActivityCalendarView) ReflowActivityCalendar(form);
            form.SystemColorsChanged += (_, _) => StyleTree(form);
            if (form is GXDLMSActivityCalendarView) StyleActivityCalendarSurfaces(form);
        }

        private static int LabelTextWidth(Label label) => TextRenderer.MeasureText(label.Text, label.Font, Size.Empty,
            TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix).Width;

        private static void ReflowAutoConnect(Form form)
        {
            var group = Find(form, "groupBox1");
            var labels = new[] { (Label)Find(group, "LogicalNameLbl"), (Label)Find(group, "CommunicationSpeedLbl"),
                (Label)Find(group, "RepetitionsLbl"), (Label)Find(group, "RepetitionDelayLbl") };
            var fields = new[] { Find(group, "LogicalNameTB"), Find(group, "ModeTB"), Find(group, "RepetitionsTB"), Find(group, "RepetitionDelayTB") };
            var callingLabel = (Label)Find(group, "CallingWindowLbl");
            var callingList = Find(group, "CallingWindowLV");

            void LayoutRows()
            {
                int fieldLeft = labels.Max(label => label.Left + LabelTextWidth(label)) + Scale(group, 12);
                foreach (Control field in fields)
                {
                    field.Left = fieldLeft;
                    field.Width = Math.Max(Scale(group, 120), group.ClientSize.Width - fieldLeft - Scale(group, 8));
                    field.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
                }
                callingList.Left = callingLabel.Left + LabelTextWidth(callingLabel) + Scale(group, 12);
                callingList.Width = Math.Max(Scale(group, 150), group.ClientSize.Width - callingList.Left - Scale(group, 8));
                callingList.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            }

            group.Resize += (_, _) => LayoutRows();
            group.FontChanged += (_, _) => LayoutRows();
            form.DpiChanged += (_, _) => LayoutRows();
            LayoutRows();
        }

        private static void ReflowImageTransfer(Form form)
        {
            var group = Find(form, "groupBox1");
            var blockLabel = (Label)Find(group, "ImageBlockSizeLbl");
            var blockField = Find(group, "ImageBlockSizeTB");
            var nextLabel = (Label)Find(group, "ImageFirstNotTransferredBlockNumberLbl");
            var nextField = Find(group, "ImageFirstNotTransferredBlockNumberTB");
            var activate = Find(group, "ActivateImageBtn");
            var verify = Find(group, "VerifyImageBtn");
            var update = Find(group, "UpdateImageBtn");

            void LayoutRows()
            {
                int gap = Scale(group, 12);
                blockField.Left = blockLabel.Left + LabelTextWidth(blockLabel) + gap;
                blockField.Width = Math.Max(Scale(group, 72), nextLabel.Left - gap - blockField.Left);
                blockField.Anchor = AnchorStyles.Top | AnchorStyles.Left;

                nextField.Left = nextLabel.Left + LabelTextWidth(nextLabel) + gap;
                nextField.Width = Math.Max(Scale(group, 90), group.ClientSize.Width - nextField.Left - Scale(group, 8));
                nextField.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

                int buttonGap = Scale(group, 8);
                int buttonWidth = Scale(group, 112);
                update.Width = verify.Width = activate.Width = buttonWidth;
                int buttonHeight = Scale(group, 30);
                update.Height = verify.Height = activate.Height = buttonHeight;
                update.Left = group.ClientSize.Width - Scale(group, 12) - buttonWidth;
                verify.Left = update.Left - buttonGap - buttonWidth;
                activate.Left = verify.Left - buttonGap - buttonWidth;
                int buttonTop = Math.Max(activate.Top, Math.Max(verify.Top, update.Top));
                activate.Top = verify.Top = update.Top = buttonTop;
                var descriptionList = Find(group, "DescriptionList");
                descriptionList.Top = buttonTop + buttonHeight + Scale(group, 6);
                update.Anchor = verify.Anchor = activate.Anchor = AnchorStyles.Top | AnchorStyles.Right;
                descriptionList.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            }

            group.Resize += (_, _) => LayoutRows();
            group.FontChanged += (_, _) => LayoutRows();
            form.DpiChanged += (_, _) => LayoutRows();
            LayoutRows();
        }

        private static void ReflowActivityCalendar(Form form)
        {
            var oldGroup = Find(form, "groupBox1");
            var controls = Descendants(oldGroup).Where(control => !string.IsNullOrEmpty(control.Name))
                .GroupBy(control => control.Name).ToDictionary(items => items.Key, items => items.First());
            var calendars = (TabControl)controls["tabControl1"];
            form.SuspendLayout();
            try
            {
                form.AutoScaleMode = AutoScaleMode.Dpi;
                form.Padding = new Padding(Scale(form, 16));
                form.AutoScroll = true;
                form.AutoScrollMinSize = Size.Empty;
                var root = new TableLayoutPanel
                {
                    Name = "groupBox1", Dock = DockStyle.Top, ColumnCount = 1, RowCount = 2,
                    BackColor = Color.White, Margin = Padding.Empty, Padding = Padding.Empty
                };
                root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
                root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
                form.Controls.Add(root);

                TableLayoutPanel PropertyRows(Control parent, string name)
                {
                    var table = new TableLayoutPanel
                    {
                        Name = name, Dock = DockStyle.Top, AutoSize = true,
                        AutoSizeMode = AutoSizeMode.GrowAndShrink, ColumnCount = 3, RowCount = 0,
                        BackColor = Color.White, Margin = Padding.Empty, Padding = Padding.Empty
                    };
                    table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, Scale(form, EditorLayoutPolicy.LabelColumnWidth)));
                    table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
                    table.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
                    parent.Controls.Add(table);
                    ConfigureCompactSurface(table);
                    return table;
                }

                void Property(TableLayoutPanel table, string captionName, string fieldName, Control action = null)
                {
                    var caption = (Label)controls[captionName];
                    int row = table.RowCount;
                    CompactRow(table, caption.Text, controls[fieldName], action, actionWidth: action == null ? 0 : 100);
                    // Retain the existing caption's name, typography and color too.
                    var generated = (Label)table.GetControlFromPosition(0, row);
                    generated.Name = caption.Name;
                    generated.Font = caption.Font;
                    generated.ForeColor = caption.ForeColor;
                    caption.Parent.Controls.Remove(caption);
                    caption.Dispose();
                }

                var metadata = PropertyRows(root, "ActivityCalendarProperties");
                // Native tab pages inset their content by four logical pixels.
                // These margins give all three property tables the same screen edge.
                metadata.Margin = new Padding(Scale(form, 12), 0, Scale(form, 12), Scale(form, 8));
                Section(metadata, "Activity Calendar");
                Property(metadata, "LogicalNameLbl", "LogicalNameTB");
                Property(metadata, "ActivatePassiveCalendarLbl", "ActivatePassiveCalendarTb");
                calendars.Dock = DockStyle.Fill;
                calendars.Margin = Padding.Empty;
                root.Controls.Add(calendars, 0, 1);

                foreach (var names in new[] {
                    new[] { "ActiveTab", "tabControl2", "ACalendarNameLbl", "ACalendarNameTB" },
                    new[] { "PassiveTab", "tabControl3", "PCalendarNameLbl", "PCalendarNameTB" } })
                {
                    var page = (TabPage)controls[names[0]];
                    page.Padding = Padding.Empty;
                    var body = new TableLayoutPanel
                    {
                        Name = names[0] + "Content", Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2,
                        BackColor = Color.White, Margin = Padding.Empty, Padding = Padding.Empty
                    };
                    body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
                    body.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                    body.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
                    page.Controls.Add(body);
                    var properties = PropertyRows(body, names[0] + "Properties");
                    properties.Margin = new Padding(Scale(form, 8));
                    Property(properties, names[2], names[3], names[0] == "PassiveTab" ? controls["ActivatePassiveCalendarBtn"] : null);
                    var profiles = controls[names[1]];
                    profiles.Dock = DockStyle.Fill;
                    profiles.Margin = Padding.Empty;
                    body.Controls.Add(profiles, 0, 1);
                }

                form.Controls.Remove(oldGroup);
                oldGroup.Dispose();
                bool fitting = false;
                bool layoutPending = false;
                void QueueLayout()
                {
                    if (layoutPending || !root.IsHandleCreated || root.IsDisposed) return;
                    layoutPending = true;
                    root.BeginInvoke((Action)(() =>
                    {
                        layoutPending = false;
                        if (!root.IsDisposed) root.PerformLayout();
                    }));
                }
                void FitHeight()
                {
                    if (fitting || root.IsDisposed || (form.TopLevelControl as Form)?.WindowState == FormWindowState.Minimized) return;
                    fitting = true;
                    try
                    {
                        int header = metadata.GetPreferredSize(new Size(Math.Max(1, root.ClientSize.Width - metadata.Margin.Horizontal), 0)).Height;
                        // Keep the profile editor usable at short heights; scroll the
                        // surface instead of clipping its lists or editing commands.
                        int height = Math.Max(form.ClientSize.Height - form.Padding.Vertical,
                            header + metadata.Margin.Vertical + Scale(root, 280));
                        if (root.Height != height)
                        {
                            root.Height = height;
                            // SizeChanged can run inside a parent layout transaction.
                            // Recompute percent rows once its cached bounds have settled.
                            QueueLayout();
                        }
                    }
                    finally { fitting = false; }
                }
                form.SizeChanged += (_, _) => FitHeight();
                root.SizeChanged += (_, _) => FitHeight();
                metadata.SizeChanged += (_, _) => FitHeight();
                root.DpiChangedAfterParent += (_, _) => { FitHeight(); QueueLayout(); };
                root.HandleCreated += (_, _) => QueueLayout();
                FitHeight();
            }
            finally { form.ResumeLayout(true); }
        }

        private static void StyleActivityCalendarSurfaces(Form form)
        {
            Color white = Color.White;
            var group = Find(form, "groupBox1");
            group.BackColor = white;
            form.BackColor = white;
            foreach (TabControl tabs in Descendants(group).OfType<TabControl>())
            {
                tabs.BackColor = white;
                foreach (TabPage page in tabs.TabPages)
                {
                    page.BackColor = white;
                    page.UseVisualStyleBackColor = false;
                }
            }
            var controls = Descendants(group).Where(control => !string.IsNullOrEmpty(control.Name))
                .GroupBy(control => control.Name).ToDictionary(items => items.Key, items => items.First());
            // The calendar and profile tabs are arranged by their layout tables.
            // Do not apply the legacy anchored coordinates on top of that layout.
            foreach (TabControl tabs in Descendants(group).OfType<TabControl>())
                tabs.Resize += (_, _) => tabs.Invalidate();
            foreach (Panel panel in Descendants(group).OfType<Panel>().Where(panel => panel.Dock == DockStyle.Bottom))
            {
                panel.BackColor = white;

            }
            ConfigureProfileList("SeasonProfileActiveLV", "panel1", "SeasonProfileActiveAddBtn", "SeasonProfileActiveEditBtn", "SeasonProfileActiveRemoveBtn");
            ConfigureProfileList("AWeekProfileLV", "panel2", "WeekProfileActiveAddBtn", "WeekProfileActiveEditBtn", "WeekProfileActiveRemoveBtn");
            ConfigureProfileList("SeasonProfilePassiveLV", "panel4", "SeasonProfilePassiveAddBtn", "SeasonProfilePassiveEditBtn", "SeasonProfilePassiveRemoveBtn");
            ConfigureProfileList("PWeekProfileLV", "panel5", "WeekProfilePassiveAddBtn", "WeekProfilePassiveEditBtn", "WeekProfilePassiveRemoveBtn");
            ConfigureDayProfile("DayProfileActiveTab", "ActiveDaysList", "ActiveActionList", "splitter1", "panel3",
                "AddDayActiveBtn", "ActiveActionAddBtn", "DayProfileActiveEditBtn", "DayProfileActiveRemoveBtn");
            ConfigureDayProfile("DayProfilePassiveTab", "PassiveDaysList", "PassiveActionList", null, "panel6",
                "AddDayPassiveBtn", "PassiveActionAddBtn", "DayProfilePassiveEditBtn", "DayProfilePassiveRemoveBtn");

            void FitProfileColumns(ListView list, params int[] logicalMinimums)
            {
                if (list.Columns.Count != logicalMinimums.Length) return;
                int[] widths = logicalMinimums.Select(value => Scale(list, value)).ToArray();
                int available = Math.Max(0, list.ClientSize.Width - SystemInformation.VerticalScrollBarWidth - Scale(list, 4));
                int extra = Math.Max(0, available - widths.Sum());
                // Names and references benefit most from extra space; numeric day
                // and selector columns retain a predictable, readable width.
                int flexible = list.Columns.Count == 8 ? 1 : list.Columns.Count;
                for (int index = 0; index < widths.Length; ++index)
                    list.Columns[index].Width = widths[index] + (index < flexible ? extra / flexible : 0);
            }

            FlowLayoutPanel CalendarActions(Panel panel, string[] names)
            {
                var footer = new ActionButtonPanel { Name = panel.Name + "Actions", Dock = DockStyle.Fill,
                    Margin = Padding.Empty, BackColor = white };
                panel.Controls.Add(footer);
                ButtonLayoutPolicy.Apply(footer, names.Select(name => (Button)controls[name]).ToArray(),
                    minimumWidth: 92, horizontalInset: 0);
                // The outer panel and its fitter own the complete wrapped bounds.
                // Avoid a competing dock/AutoSize pass reusing one-row height.
                footer.AutoSize = false;
                footer.Dock = DockStyle.None;
                footer.Anchor = AnchorStyles.Top | AnchorStyles.Left;
                return footer;
            }

            int CalendarFooterHeight(Panel panel, FlowLayoutPanel footer, Control page)
            {
                // Docked panels respect the native tab-page padding; the lists
                // use their own explicit inset. Match those actual content rails.
                int edge = Scale(page, 4);
                panel.Padding = new Padding(Math.Max(0, edge - panel.Left), 0,
                    Math.Max(0, panel.Right - (page.ClientSize.Width - edge)), 0);
                int width = Math.Max(1, panel.ClientSize.Width - panel.Padding.Horizontal);
                int height = footer.GetPreferredSize(new Size(width, 0)).Height;
                var bounds = new Rectangle(panel.Padding.Left, panel.Padding.Top, width, height);
                if (footer.Bounds != bounds) footer.Bounds = bounds;
                return height + panel.Padding.Vertical;
            }

            void ConfigureProfileList(string listName, string panelName, params string[] buttonNames)
            {
                var list = (ListView)controls[listName];
                var panel = (Panel)controls[panelName];
                var footer = CalendarActions(panel, buttonNames);
                bool arranging = false;
                list.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
                Layout();
                list.Parent!.SizeChanged += (_, _) => Layout();
                footer.Layout += (_, _) => Layout();
                footer.DpiChangedAfterParent += (_, _) => Layout();
                footer.HandleCreated += (_, _) => Layout();
                panel.SizeChanged += (_, _) => Layout();
                panel.LocationChanged += (_, _) => Layout();
                panel.Parent!.SizeChanged += (_, _) => Layout();
                foreach (var button in buttonNames.Select(name => (Button)controls[name]))
                    button.FontChanged += (_, _) => Layout();

                void Layout()
                {
                    if (arranging || panel.IsDisposed || (form.TopLevelControl as Form)?.WindowState == FormWindowState.Minimized) return;
                    arranging = true;
                    try
                    {
                        int footerHeight = CalendarFooterHeight(panel, footer, panel.Parent);
                        if (panel.Height != footerHeight) panel.Height = footerHeight;
                        int margin = Scale(list, 4);
                        int gap = 0;
                        int panelTop = panel.Top;
                        list.Left = margin;
                        list.Top = margin;
                        list.Width = Math.Max(Scale(list, 100), panel.Parent.ClientSize.Width - 2 * margin);
                        list.Height = Math.Max(Scale(list, 64), panelTop - list.Top - gap);
                        FitProfileColumns(list, list.Columns.Count == 3 ? new[] { 120, 180, 120 }
                            : new[] { 120, 64, 64, 64, 64, 64, 64, 64 });
                    }
                    finally { arranging = false; }
                }
            }

            void ConfigureDayProfile(string pageName, string daysName, string actionsName, string? splitterName, string panelName, params string[] buttonNames)
            {
                var page = controls[pageName];
                var days = (ListView)controls[daysName];
                var actions = (ListView)controls[actionsName];
                var splitter = splitterName == null ? null : controls[splitterName];
                var panel = (Panel)controls[panelName];
                var footer = CalendarActions(panel, buttonNames);
                bool arranging = false;
                int daysWidth = days.Width;
                days.Dock = DockStyle.None;
                days.Width = daysWidth;
                days.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left;
                Layout();
                page.SizeChanged += (_, _) => Layout();
                footer.Layout += (_, _) => Layout();
                footer.DpiChangedAfterParent += (_, _) => Layout();
                footer.HandleCreated += (_, _) => Layout();
                panel.SizeChanged += (_, _) => Layout();
                panel.LocationChanged += (_, _) => Layout();
                panel.Parent!.SizeChanged += (_, _) => Layout();
                foreach (var button in buttonNames.Select(name => (Button)controls[name]))
                    button.FontChanged += (_, _) => Layout();

                void Layout()
                {
                    if (arranging || panel.IsDisposed || (form.TopLevelControl as Form)?.WindowState == FormWindowState.Minimized) return;
                    arranging = true;
                    try
                    {
                        int footerHeight = CalendarFooterHeight(panel, footer, page);
                        if (panel.Height != footerHeight) panel.Height = footerHeight;
                        int margin = Scale(page, 4);
                        int separator = Scale(page, splitter == null ? 3 : splitter.Width);
                        int panelTop = panel.Top;
                        int bottom = Math.Max(Scale(page, 64), panelTop);
                        days.Left = margin;
                        days.Top = margin;
                        days.Width = daysWidth;
                        days.Height = bottom - margin;
                        actions.Left = days.Right + separator;
                        actions.Top = margin;
                        actions.Width = Math.Max(Scale(page, 100), page.ClientSize.Width - margin - actions.Left);
                        actions.Height = bottom - margin;
                        FitProfileColumns(days, 48);
                        FitProfileColumns(actions, 100, 150, 100);
                        actions.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
                        if (splitter != null)
                        {
                            splitter.Left = days.Right;
                            splitter.Top = margin;
                            splitter.Height = bottom - margin;
                            splitter.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left;
                        }
                    }
                    finally { arranging = false; }
                }
            }


            foreach (TabPage page in Descendants(group).OfType<TabPage>())
            {
                page.BackColor = white;
                page.UseVisualStyleBackColor = false;
            }
        }

        private static void ReflowGprsSetup(Form form)
        {
            var root = Find(form, "groupBox1");
            var defaults = Find(root, "groupBox2");
            var requested = Find(root, "groupBox3");
            var topFields = new[] { Find(root, "LogicalNameTB"), Find(root, "APNTB"), Find(root, "PINCodeTB") };
            var topLabels = new[] { (Label)Find(root, "LogicalNameLbl"), (Label)Find(root, "APNLbl"), (Label)Find(root, "PINCodeLbl") };
            var defaultFields = new[] { Find(defaults, "CPrecedenceTB"), Find(defaults, "CDelayTB"), Find(defaults, "CReliabilityTB"), Find(defaults, "CPeakThroughputTB"), Find(defaults, "CMeanThroughputTB") };
            var defaultLabels = new[] { (Label)Find(defaults, "CPrecedenceLbl"), (Label)Find(defaults, "CDelayLbl"), (Label)Find(defaults, "CReliabilityLbl"), (Label)Find(defaults, "CPeakThroughputLbl"), (Label)Find(defaults, "CMeanThroughputLbl") };
            var requestedFields = new[] { Find(requested, "MPrecedenceTB"), Find(requested, "MDelayTB"), Find(requested, "MReliabilityTB"), Find(requested, "MPeakThroughputTB"), Find(requested, "MMeanThroughputTB") };
            var requestedLabels = new[] { (Label)Find(requested, "MPrecedenceLbl"), (Label)Find(requested, "MDelayLbl"), (Label)Find(requested, "MReliabilityLbl"), (Label)Find(requested, "MPeakThroughputLbl"), (Label)Find(requested, "MMeanThroughputLbl") };
            void LayoutGroups()
            {
                int gap = Scale(root, 8);
                int left = Scale(root, 6);
                int fieldLeft = left + topLabels.Max(label => LabelTextWidth(label)) + Scale(root, 10);
                foreach (Control field in topFields)
                {
                    field.Left = fieldLeft;
                    field.Width = Math.Max(Scale(root, 100), root.ClientSize.Width - fieldLeft - Scale(root, 8));
                    field.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
                }
                defaults.Left = left;
                defaults.Width = Math.Max(Scale(root, 280), root.ClientSize.Width - defaults.Left * 2);
                defaults.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
                defaults.Top = topFields.Max(field => field.Bottom) + gap;
                defaults.Height = Scale(defaults, 154);
                int qosFieldLeft = defaultLabels.Max(label => label.Left + LabelTextWidth(label)) + Scale(defaults, 10);
                foreach (Control field in defaultFields)
                {
                    field.Left = qosFieldLeft;
                    field.Width = Math.Max(Scale(defaults, 100), defaults.ClientSize.Width - qosFieldLeft - Scale(defaults, 8));
                    field.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
                }
                requested.Left = defaults.Left;
                requested.Top = defaults.Bottom + gap;
                requested.Width = defaults.Width;
                requested.Height = Scale(requested, 154);
                requested.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
                int requestedFieldLeft = requestedLabels.Max(label => label.Left + LabelTextWidth(label)) + Scale(requested, 10);
                foreach (Control field in requestedFields)
                {
                    field.Left = requestedFieldLeft;
                    field.Width = Math.Max(Scale(requested, 100), requested.ClientSize.Width - requestedFieldLeft - Scale(requested, 8));
                    field.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
                }
                root.Height = Math.Max(root.Height, requested.Bottom + Scale(root, 8));
            }
            root.Resize += (_, _) => LayoutGroups();
            root.FontChanged += (_, _) => LayoutGroups();
            form.DpiChanged += (_, _) => LayoutGroups();
            LayoutGroups();
        }

        private static void ReflowGsmDiagnostic(Form form)
        {
            var root = Find(form, "groupBox1");
            var cellInfo = Find(root, "CellInfoCb");
            var wideFields = new[] { Find(root, "LogicalNameTB"), Find(root, "OperatorTb"), Find(root, "StatusTB"),
                Find(root, "CSAttachmentTb"), Find(root, "PSStatusTb"), Find(root, "CaptureTimeTb") };
            var wideLabels = new[] { (Label)Find(root, "LogicalNameLbl"), (Label)Find(root, "OperatorLbl"), (Label)Find(root, "StatusLbl"),
                (Label)Find(root, "CSAttachmentLbl"), (Label)Find(root, "PSStatusLbl"), (Label)Find(root, "CaptureTimeLbl") };
            var adjacent = Find(root, "AdjacentCellsLV");
            var adjacentLabel = (Label)Find(root, "AdjacentCellsLbl");
            var cellRows = new (string LeftLabel, string LeftField, string? RightLabel, string? RightField)[]
            {
                ("CellIDLbl", "CellIDTb", null, null),
                ("LocationIDLbl", "LocationIDTb", "SignalQualityLbl", "SignalQualityTb"),
                ("BerLbl", "BerTb", "MccLbl", "MccTb"),
                ("MncLbl", "MncTb", "ChannelNumberLbl", "ChannelNumberTb")
            };
            void LayoutRows()
            {
                int wideLeft = wideLabels.Max(label => label.Left + LabelTextWidth(label)) + Scale(root, 12);
                foreach (Control field in wideFields)
                {
                    field.Left = wideLeft;
                    field.Width = Math.Max(Scale(root, 120), root.ClientSize.Width - wideLeft - Scale(root, 8));
                    field.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
                }

                int padding = Scale(cellInfo, 8);
                int leftLabelX = padding;
                var leftLabels = cellRows.Select(row => (Label)Find(cellInfo, row.LeftLabel)).ToArray();
                int leftFieldX = leftLabelX + leftLabels.Max(LabelTextWidth) + Scale(cellInfo, 8);
                int rightLabelX = cellInfo.ClientSize.Width / 2;
                var rightPairs = cellRows.Where(row => row.RightLabel != null).ToArray();
                int rightLabelWidth = rightPairs.Max(row => LabelTextWidth((Label)Find(cellInfo, row.RightLabel!)));
                int rightFieldX = rightLabelX + rightLabelWidth + Scale(cellInfo, 8);
                int rowHeight = Scale(cellInfo, 26);
                int y = Scale(cellInfo, 17);
                foreach (var row in cellRows)
                {
                    Label leftLabel = (Label)Find(cellInfo, row.LeftLabel);
                    Control leftField = Find(cellInfo, row.LeftField);
                    leftLabel.Left = leftLabelX;
                    leftLabel.Top = y + Scale(cellInfo, 3);
                    leftField.Left = leftFieldX;
                    leftField.Top = y;
                    leftField.Width = row.RightLabel == null ? Math.Max(Scale(cellInfo, 80), cellInfo.ClientSize.Width - leftFieldX - padding)
                        : Math.Max(Scale(cellInfo, 60), rightLabelX - leftFieldX - Scale(cellInfo, 8));
                    leftField.Anchor = AnchorStyles.Top | AnchorStyles.Left | (row.RightLabel == null ? AnchorStyles.Right : AnchorStyles.None);
                    if (row.RightLabel != null)
                    {
                        Label rightLabel = (Label)Find(cellInfo, row.RightLabel);
                        Control rightField = Find(cellInfo, row.RightField!);
                        rightLabel.Left = rightLabelX;
                        rightLabel.Top = y + Scale(cellInfo, 3);
                        rightField.Left = rightFieldX;
                        rightField.Top = y;
                        rightField.Width = Math.Max(Scale(cellInfo, 56), cellInfo.ClientSize.Width - rightFieldX - padding);
                        rightField.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
                    }
                    y += rowHeight;
                }
                cellInfo.Height = y + Scale(cellInfo, 10);
                int adjacentLeft = adjacentLabel.Left + LabelTextWidth(adjacentLabel) + Scale(root, 10);
                adjacent.Left = adjacentLeft;
                adjacent.Width = Math.Max(Scale(root, 120), root.ClientSize.Width - adjacentLeft - Scale(root, 8));
                adjacent.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            }
            cellInfo.Resize += (_, _) => LayoutRows();
            root.Resize += (_, _) => LayoutRows();
            root.FontChanged += (_, _) => LayoutRows();
            form.DpiChanged += (_, _) => LayoutRows();
            LayoutRows();
        }

        private static void ReflowIecLocalPortSetup(Form form)
        {
            var group = Find(form, "groupBox1");
            var rows = new (string Label, string Field)[]
            {
                ("LogicalNameLbl", "LogicalNameTB"),
                ("DefaultModeLbl", "DefaultModeTB"),
                ("DefaultBaudrateLbl", "DefaultBaudrateTB"),
                ("MaximumBaudrateLbl", "MaximumBaudrateTB"),
                ("ResponseTimeLbl", "ResponseTimeTB"),
                ("DeviceAddressLbl", "DeviceAddressTB"),
                ("Password1Lbl", "Password1TB"),
                ("Password2Lbl", "Password2TB"),
                ("Password5Lbl", "Password5TB")
            }.Select(row => ((Label)Find(group, row.Label), Find(group, row.Field))).ToArray();

            void LayoutRows()
            {
                int labelLeft = Scale(group, 6);
                int labelWidth = rows.Max(row => TextRenderer.MeasureText(row.Item1.Text, row.Item1.Font,
                    Size.Empty, TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix).Width);
                int fieldLeft = labelLeft + labelWidth + Scale(group, 12);
                foreach (var (label, field) in rows)
                {
                    label.Left = labelLeft;
                    field.Left = fieldLeft;
                    field.Width = Math.Max(Scale(group, 120), group.ClientSize.Width - fieldLeft - Scale(group, 8));
                    field.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
                }
            }

            group.Resize += (_, _) => LayoutRows();
            group.FontChanged += (_, _) => LayoutRows();
            form.DpiChanged += (_, _) => LayoutRows();
            LayoutRows();
        }

        private static void AlignScriptIdRow(Form form)
        {
            var group = Find(form, "groupBox1");
            var label = (Label)Find(group, "label1");
            var field = Find(group, "ScriptId");
            var logicalName = Find(group, "LogicalNameTB");
            void AlignLabel()
            {
                int textWidth = TextRenderer.MeasureText(label.Text, label.Font, Size.Empty,
                    TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix).Width;
                label.Left = field.Left - textWidth - Scale(group, 10);
                logicalName.Width = Math.Max(Scale(group, 120), label.Left - logicalName.Left - Scale(group, 12));
            }
            group.Resize += (_, _) => AlignLabel();
            group.FontChanged += (_, _) => AlignLabel();
            form.DpiChanged += (_, _) => AlignLabel();
            AlignLabel();
        }

        private static void AlignProfileGenericLogicalName(Form form)
        {
            var group = Find(form, "groupBox1");
            var label = (Label)Find(group, "LogicalNameLbl");
            var field = Find(group, "LogicalNameTB");
            void AlignField()
            {
                int measured = TextRenderer.MeasureText(label.Text, label.Font, Size.Empty,
                    TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix).Width;
                int minimumLeft = label.Left + Math.Max(label.Width, measured) + Scale(group, 14);
                int shift = Math.Max(0, minimumLeft - field.Left);
                if (shift == 0) return;
                field.Left += shift;
                field.Width = Math.Max(Scale(group, 88), field.Width - shift);
            }
            group.Resize += (_, _) => AlignField();
            group.FontChanged += (_, _) => AlignField();
            form.DpiChanged += (_, _) => AlignField();
            AlignField();
        }

        private static void ReflowOverlappingLabelFields(Control root)
        {
            bool IsEditor(Control control) => control is GXValueField or TextBoxBase or ComboBox or NumericUpDown or DateTimePicker;
            bool IsLayoutManaged(Control control) => control is TableLayoutPanel or FlowLayoutPanel;

            void ReflowContainer(Control container)
            {
                if (IsLayoutManaged(container)) return;
                var editors = container.Controls.Cast<Control>().Where(control => IsEditor(control) && control.Dock == DockStyle.None).ToArray();
                var labels = container.Controls.OfType<Label>().Where(label => label.Visible).ToArray();
                if (editors.Length == 0 || labels.Length == 0) return;

                int tolerance = Scale(container, 8);
                int gap = Scale(container, 16);
                var columns = new List<List<Control>>();
                foreach (Control editor in editors.OrderBy(field => field.Left))
                {
                    var column = columns.FirstOrDefault(items => Math.Abs(items[0].Left - editor.Left) <= tolerance);
                    if (column == null) columns.Add(new List<Control> { editor });
                    else column.Add(editor);
                }

                foreach (var column in columns)
                {
                    int shift = 0;
                    var labelsToMove = new HashSet<Label>();
                    foreach (Control editor in column)
                    {
                        foreach (Label label in labels)
                        {
                            if (Math.Abs(label.Top - editor.Top) > tolerance || label.Left >= editor.Left) continue;
                            int labelRight = label.Left + Math.Max(label.Width, TextRenderer.MeasureText(label.Text, label.Font,
                                Size.Empty, TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix).Width);
                            int requiredShift = labelRight + gap - editor.Left;
                            if (requiredShift > 0)
                            {
                                shift = Math.Max(shift, requiredShift);
                                labelsToMove.Add(label);
                            }
                        }
                    }
                    if (shift <= 0) continue;

                    bool rightAligned = column.All(editor => (editor.Anchor & AnchorStyles.Right) != 0 && (editor.Anchor & AnchorStyles.Left) == 0);
                    if (rightAligned)
                    {
                        foreach (Label label in labelsToMove) label.Left -= shift;
                    }
                    else
                    {
                        foreach (Control editor in column)
                        {
                            editor.Left += shift;
                            editor.Width = Math.Max(Scale(container, 120), editor.Width - shift);
                            editor.Anchor |= AnchorStyles.Right;
                        }
                    }
                }
            }

            var containers = new List<Control>();
            void Walk(Control parent)
            {
                if (!IsLayoutManaged(parent)) containers.Add(parent);
                foreach (Control child in parent.Controls) Walk(child);
            }
            Walk(root);
            foreach (Control container in containers)
            {
                container.SizeChanged += (_, _) => ReflowContainer(container);
                container.FontChanged += (_, _) => ReflowContainer(container);
                ReflowContainer(container);
            }
        }

        private sealed class ContrastState
        {
            internal Color Back, Fore;
            internal FlatStyle? Flat;
            internal DataGridViewCellStyle GridDefault;
        }
        private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<Control, ContrastState> ContrastColors = new();

        private static void StyleTree(Control control)
        {
            PrepareContrast(control);
            StyleTreeCore(control);
        }

        private static void PrepareContrast(Control control)
        {
            if (SystemInformation.HighContrast && !ContrastColors.TryGetValue(control, out _))
                ContrastColors.Add(control, new ContrastState { Back = control.BackColor, Fore = control.ForeColor,
                    Flat = control is Button b ? b.FlatStyle : null,
                    GridDefault = control is DataGridView grid ? grid.DefaultCellStyle.Clone() : null });
            else if (!SystemInformation.HighContrast && ContrastColors.TryGetValue(control, out var previous))
            {
                control.BackColor = previous.Back;
                control.ForeColor = previous.Fore;
                if (control is Button b && previous.Flat.HasValue) b.FlatStyle = previous.Flat.Value;
                if (control is DataGridView grid && previous.GridDefault != null) grid.DefaultCellStyle = previous.GridDefault;
                ContrastColors.Remove(control);
            }
            foreach (Control child in control.Controls) PrepareContrast(child);
        }

        private static void StyleTreeCore(Control control)
        {
            if (control is ToolStrip strip)
            {
                strip.RenderMode = ToolStripRenderMode.Professional;
                strip.BackColor = Color.FromArgb(245, 247, 251);
                strip.ForeColor = Ink;
            }
            if (control is Button button && (button is GXButton || button.FindForm() is GXDLMSActivityCalendarView or GXDLMSModemConfigurationView))
            {
                // Keep the existing font/size on fixed-coordinate editors.
                button.FlatStyle = FlatStyle.Flat;
                button.FlatAppearance.BorderColor = Line;
                button.BackColor = Color.White;
                button.ForeColor = Ink;
                button.UseVisualStyleBackColor = false;
            }
            if (control is GXValueField field) field.ApplyModernStyle();
            if (control is DataGridView grid)
            {
                if (grid.Font.Name != "Segoe UI") grid.Font = BodyFont;
                grid.DefaultCellStyle.Font = grid.Font;
                grid.ColumnHeadersDefaultCellStyle.Font = grid.Font;
                grid.BackgroundColor = Color.White;
                grid.BorderStyle = BorderStyle.None;
                grid.GridColor = Line;
                grid.EnableHeadersVisualStyles = false;
                grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(240, 243, 249);
                grid.ColumnHeadersDefaultCellStyle.ForeColor = Ink;
                grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 250, 253);
            }
            if (SystemInformation.HighContrast)
            {
                control.BackColor = control is TextBoxBase || control is ListControl
                    ? SystemColors.Window : SystemColors.Control;
                control.ForeColor = SystemColors.ControlText;
                if (control is Button nativeButton) nativeButton.FlatStyle = FlatStyle.System;
                if (control is ToolStrip nativeStrip) nativeStrip.RenderMode = ToolStripRenderMode.System;
                if (control is DataGridView nativeGrid)
                {
                    nativeGrid.BackgroundColor = SystemColors.Window;
                    nativeGrid.GridColor = SystemColors.ControlDark;
                    nativeGrid.EnableHeadersVisualStyles = true;
                    nativeGrid.DefaultCellStyle.BackColor = SystemColors.Window;
                    nativeGrid.DefaultCellStyle.ForeColor = SystemColors.WindowText;
                    nativeGrid.DefaultCellStyle.SelectionBackColor = SystemColors.Highlight;
                    nativeGrid.DefaultCellStyle.SelectionForeColor = SystemColors.HighlightText;
                    nativeGrid.AlternatingRowsDefaultCellStyle.BackColor = SystemColors.Window;
                }
            }
            foreach (Control child in control.Controls) StyleTreeCore(child);
        }
        public static readonly Color Ink = Color.FromArgb(27, 39, 59);
        public static readonly Color Muted = Color.FromArgb(101, 115, 137);
        public static readonly Color Line = Color.FromArgb(219, 225, 234);
        public static readonly Color Accent = Color.FromArgb(48, 86, 211);

        public static Form Create(bool register, bool modern)
        {
            Form form = register ? new GXDLMSRegisterView() : new GXDLMSDataView();
            form.TopLevel = false;
            form.FormBorderStyle = FormBorderStyle.None;
            form.Dock = DockStyle.Fill;
            if (modern) Apply(form);
            else form.Font = new Font("Microsoft Sans Serif", 8.25f);
            return form;
        }

        private static Control Find(Control root, string name) => root.Controls.Find(name, true)[0];

        // Director has a dense navigation shell. Bound the editor width and use a
        // compact, consistent rhythm instead of stretching the standalone demo layout.
        public static TableLayoutPanel CompactSurface(Form form, bool includeActionColumn = true)
        {
            form.AutoScaleMode = AutoScaleMode.Dpi;
            form.Font = BodyFont;
            form.BackColor = Color.White;
            form.ForeColor = Ink;
            form.AutoScroll = true;
            form.Padding = new Padding(Scale(form, 16));
            form.AutoScrollMinSize = Size.Empty;
            var table = new TableLayoutPanel
            {
                Name = "ModernContent", Dock = DockStyle.Top, AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink, ColumnCount = includeActionColumn ? 3 : 2,
                MaximumSize = new Size(Scale(form, 800), 0), MinimumSize = Size.Empty,
                Padding = new Padding(0, 0, Scale(form, 16), Scale(form, 12)), BackColor = Color.White
            };
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, Scale(form, 144)));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            if (includeActionColumn)
                table.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            ConfigureCompactSurface(table);
            return table;
        }

        public static void Section(TableLayoutPanel table, string title)
        {
            int row = table.RowCount++;
            table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            var label = new Label
            {
                Text = title, AutoSize = true, ForeColor = Ink,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Margin = new Padding(0, row == 0 ? 0 : 16, 0, 10)
            };
            table.Controls.Add(label, 0, row);
            table.SetColumnSpan(label, table.ColumnCount);
        }

        public static void CompactRow(TableLayoutPanel table, string label, Control field, Control action = null, int height = 40, int actionWidth = 0)
        {
            int row = table.RowCount++;
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, Scale(table, height)));
            var caption = new Label
            {
                Text = label, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = Muted, Margin = new Padding(0, 0, 12, 0), AutoSize = false
            };
            table.Controls.Add(caption, 0, row);
            field.Dock = DockStyle.Fill;
            var fieldMargin = new Padding(0, Scale(table, 4), 0, Scale(table, 4));
            field.Margin = fieldMargin;
            field.Font = table.Parent?.Font ?? BodyFont;
            if (!string.IsNullOrEmpty(label)) field.AccessibleName = label;
            field.TabIndex = row * 2;
            Control cell = field;
            if (field is GXValueField valueField) valueField.ApplyModernStyle(compact: true);
            AlignTallCaption(caption, field);
            if (field is TextBox textBox)
            {
                textBox.BorderStyle = BorderStyle.None;
                textBox.BackColor = Color.FromArgb(245, 247, 251);
                textBox.Dock = textBox.Multiline ? DockStyle.Fill : DockStyle.Top;
                textBox.Margin = Padding.Empty;
                var frame = new ModernFieldFrame(textBox, textBox.BackColor)
                {
                    Dock = DockStyle.Fill, Padding = new Padding(8, 7, 8, 5), Margin = fieldMargin, TabIndex = field.TabIndex
                };
                cell = frame;
            }
            if (field is ComboBox combo)
            {
                // Native combo boxes have a fixed preferred height. Stretching a
                // flat combo to fill the row leaves stale arrow painting on resize.
                combo.FlatStyle = FlatStyle.Standard;
                combo.BackColor = Color.FromArgb(245, 247, 251);
                combo.Dock = DockStyle.Top;
                combo.Margin = Padding.Empty;
                cell = new NativeComboHost(combo)
                {
                    Dock = DockStyle.Fill, Margin = fieldMargin
                };
            }
            if (action == null)
            {
                table.Controls.Add(cell, 1, row);
                table.SetColumnSpan(cell, table.ColumnCount - 1);
                if (field is GXValueField adaptive) ConfigureValueRow(table, row, caption, adaptive, height);
            }
            else
            {
                if (table.ColumnCount < 3)
                    throw new InvalidOperationException("An action control requires an action column.");
                action.Margin = new Padding(12, 4, 0, 4);
                action.TabIndex = row * 2 + 1;
                action.Anchor = AnchorStyles.Left | AnchorStyles.Top;
                if (action is Button button)
                {
                    CompactButton(button);
                    if (actionWidth > 0)
                    {
                        button.AutoSize = false;
                        button.Size = new Size(Scale(table, actionWidth), Scale(table, 32));
                        button.MinimumSize = button.MaximumSize = button.Size;
                    }
                    ButtonLayoutPolicy.ApplyToButton(button, minimumWidth: actionWidth > 0 ? actionWidth : 100);
                }
                var actionRow = new CompactActionRow(table, row, cell, action, height) { TabIndex = row * 2 };
                table.Controls.Add(actionRow, 1, row);
                table.SetColumnSpan(actionRow, table.ColumnCount - 1);
                caption.TextAlign = ContentAlignment.TopLeft;
                caption.Padding = new Padding(0, Scale(table, 12), 0, 0);
            }
            EditorLayoutPolicy.ConfigurePropertyTable(table);
        }

        public static void CompactButton(Button button)
        {
            button.Font = BodyFont;
            button.AutoSize = true;
            button.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            button.MinimumSize = new Size(0, Scale(button, 32));
            // Native button chrome already reserves vertical text insets. Extra
            // top/bottom padding can clip the line inside the standard32px action.
            button.Padding = new Padding(Scale(button, 10), 0, Scale(button, 10), 0);
            button.Dock = DockStyle.None;
        }

        /// <summary>Native actions on the same field rail and height as adjacent property inputs.</summary>
        public static void CompactActions(TableLayoutPanel table, string caption, params Button[] buttons)
        {
            var actions = new ActionButtonPanel
            {
                Name = "PropertyActions", Dock = DockStyle.Fill, AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink, WrapContents = true,
                Margin = Padding.Empty, Padding = Padding.Empty, BackColor = Color.White,
                MinimumSize = new Size(0, Scale(table, 32))
            };
            for (int i = 0; i < buttons.Length; ++i)
            {
                var button = buttons[i];
                CompactButton(button);
                button.MinimumSize = new Size(0, Scale(table, 32));
                button.Margin = new Padding(0, 0, i + 1 == buttons.Length ? 0 : Scale(table, 8), 0);
                button.TabIndex = i;
                actions.Controls.Add(button);
            }
            ButtonLayoutPolicy.Apply(actions, buttons, alignRight: false);
            CompactRow(table, caption, actions, height: 40);
            // The group owns the row's four-pixel inset; do not double it
            // with the generic property's cell margins.
            actions.Margin = Padding.Empty;
            // A very narrow field cell may wrap several actions; reserve their
            // measured height instead of clipping the second native button.
            table.RowStyles[table.RowCount - 1].SizeType = SizeType.AutoSize;
        }

        private static void CompactObject(Form form, bool register)
        {
            var oldGroup = Find(form, "groupBox1");
            // These editors place actions inside a field row, so they need only a
            // label column and a field column.
            var table = CompactSurface(form, includeActionColumn: false);
            Section(table, register ? "Register" : "Data object");
            string[] names = register
                ? new[] { "DescriptionTB", "LogicalNameTB", "ValueTB", "ScalerTB", "UnitTB" }
                : new[] { "DescriptionTB", "LogicalNameTB", "ValueTB" };
            string[] labels = { "Description", "OBIS code", "Value", "Scaler", "Unit" };
            for (int i = 0; i < names.Length; ++i) CompactRow(table, labels[i], Find(oldGroup, names[i]));
            if (register)
            {
                var button = (Button)Find(oldGroup, "ResetBtn");
                CompactActions(table, "Action", button);
            }
            form.Controls.Add(table);
            form.Controls.Remove(oldGroup);
            oldGroup.Dispose();
            form.ResumeLayout(true);
        }

        private static void CompactClock(Form form)
        {
            // Capture before reparenting: these are the original bound controls.
            string[] names = { "LogicalNameTB", "TimeTB", "TimeZoneTB", "TimeZoneCb", "StatusTB", "ClockBaseTB",
                "EnabledCB", "BeginTB", "EndTB", "DeviationTB", "UpdateTimeBtn", "CurrentTimeZoneBtn",
                "AdjustToQuarterBtn", "AdjustToMeasuringPeriodBtn", "AdjustToMinuteBtn", "AdjustToPresetTimeBtn",
                "PresetAdjustingTimeBtn", "ShiftTimeBtn" };
            var controls = new System.Collections.Generic.Dictionary<string, Control>();
            foreach (string name in names) controls.Add(name, Find(form, name));
            var oldGroups = new[] { Find(form, "groupBox1"), Find(form, "groupBox2"), Find(form, "groupBox3") };
            var table = CompactSurface(form);
            Section(table, "Clock");
            CompactRow(table, "OBIS code", controls["LogicalNameTB"]);
            CompactRow(table, "Meter time", controls["TimeTB"], controls["UpdateTimeBtn"], actionWidth: 140);
            CompactRow(table, "Time zone (min)", controls["TimeZoneTB"], controls["CurrentTimeZoneBtn"], actionWidth: 140);
            ButtonLayoutPolicy.MatchInlineWidths((Button)controls["UpdateTimeBtn"], (Button)controls["CurrentTimeZoneBtn"]);
            var zone = (CheckBox)controls["TimeZoneCb"];
            zone.Text = "Use time zone";
            CompactRow(table, "", zone, height: 32);
            CompactRow(table, "Status", controls["StatusTB"], height: 120);
            var clockBase = (GXValueField)controls["ClockBaseTB"];
            CompactRow(table, "Clock base", clockBase);
            Section(table, "Daylight saving");
            var enabled = (CheckBox)controls["EnabledCB"];
            enabled.Text = "Enabled";
            CompactRow(table, "Daylight saving", enabled, height: 32);
            CompactRow(table, "Begins", controls["BeginTB"]);
            CompactRow(table, "Ends", controls["EndTB"]);
            CompactRow(table, "Deviation (min)", controls["DeviationTB"]);
            Section(table, "Clock actions");
            // Keep complete action names; the legacy trailing dots looked like truncation.
            ((Button)controls["PresetAdjustingTimeBtn"]).Text = "Preset Adjusting Time";
            var actionNames = names.Where(name => controls[name] is GXButton &&
                name != "UpdateTimeBtn" && name != "CurrentTimeZoneBtn").ToArray();
            var actions = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 2, RowCount = (actionNames.Length + 1) / 2, Margin = Padding.Empty,
                Padding = Padding.Empty, BackColor = Color.Transparent
            };
            actions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            actions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            for (int i = 0; i < actions.RowCount; ++i)
                actions.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            for (int i = 0; i < actionNames.Length; ++i)
            {
                string name = actionNames[i];
                var button = (GXButton)controls[name];
                CompactButton(button);
                button.AutoSize = false;
                button.Dock = DockStyle.Fill;
                button.Margin = new Padding(i % 2 == 0 ? 0 : Scale(form, 4), Scale(form, 4), i % 2 == 0 ? Scale(form, 4) : 0, Scale(form, 4));
                button.MinimumSize = new Size(0, Scale(form, 32));
                actions.Controls.Add(button, i % 2, i / 2);
            }
            int row = table.RowCount++;
            table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            table.Controls.Add(actions, 1, row);
            bool fittingActions = false;
            void FitClockActions()
            {
                if (fittingActions || actions.IsDisposed || actions.ClientSize.Width < 1 ||
                    (form.TopLevelControl as Form)?.WindowState == FormWindowState.Minimized) return;
                fittingActions = true;
                actions.SuspendLayout();
                try
                {
                    var buttons = actionNames.Select(name => (Button)controls[name]).ToArray();
                    int widest = buttons.Select(button => ButtonLayoutPolicy.GetPreferredSize(button).Width).Max();
                    int columns = actions.ClientSize.Width >= widest * 2 + Scale(form, 8) ? 2 : 1;
                    if (actions.ColumnCount != columns)
                    {
                        actions.ColumnCount = columns;
                        actions.ColumnStyles.Clear();
                        for (int column = 0; column < columns; ++column)
                            actions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f / columns));
                        actions.RowCount = (buttons.Length + columns - 1) / columns;
                        actions.RowStyles.Clear();
                        for (int actionRow = 0; actionRow < actions.RowCount; ++actionRow)
                            actions.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                    }
                    for (int i = 0; i < buttons.Length; ++i)
                    {
                        actions.SetCellPosition(buttons[i], new TableLayoutPanelCellPosition(i % columns, i / columns));
                        buttons[i].Margin = new Padding(i % columns > 0 ? Scale(form, 4) : 0, Scale(form, 4),
                            i % columns + 1 < columns ? Scale(form, 4) : 0, Scale(form, 4));
                        buttons[i].MinimumSize = new Size(0, Scale(form, 32));
                    }
                }
                finally { actions.ResumeLayout(true); fittingActions = false; }
            }
            actions.SizeChanged += (_, _) => FitClockActions();
            actions.DpiChangedAfterParent += (_, _) => FitClockActions();
            foreach (var button in actionNames.Select(name => (Button)controls[name]))
                button.FontChanged += (_, _) => FitClockActions();
            FitClockActions();
            table.SetColumnSpan(actions, table.ColumnCount - 1);
            form.Controls.Add(table);
            foreach (Control group in oldGroups) { form.Controls.Remove(group); group.Dispose(); }
            form.ResumeLayout(true);
        }

        private static int Scale(Control control, int value) => (int)Math.Round(value * control.DeviceDpi / 96d);

        private static void ReflowProfileGeneric(Form form)
        {
            var group = Find(form, "groupBox1");
            var controls = Descendants(group).Where(c => !string.IsNullOrEmpty(c.Name))
                .GroupBy(c => c.Name).ToDictionary(g => g.Key, g => g.First());
            form.SuspendLayout();
            try
            {
                form.AutoScaleMode = AutoScaleMode.Dpi;
                form.Font = BodyFont;
                form.BackColor = Color.White;
                var root = new TableLayoutPanel { Name = "ProfileGenericContent", Dock = DockStyle.Top, BackColor = Color.White,
                    Padding = new Padding(Scale(form, 16)), ColumnCount = 1, RowCount = 0 };
                root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
                form.Controls.Add(root);
                void Add(Control control, SizeType sizing, float height)
                {
                    int row = root.RowCount++;
                    root.RowStyles.Add(new RowStyle(sizing, height));
                    control.Dock = DockStyle.Fill;
                    control.Margin = Padding.Empty;
                    root.Controls.Add(control, 0, row);
                }
                var header = new TableLayoutPanel
                {
                    Name = "ProfileHeader", ColumnCount = 2, RowCount = 2,
                    BackColor = Color.White, Margin = Padding.Empty, Padding = Padding.Empty
                };
                header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
                header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, Scale(form, 136)));
                header.RowStyles.Add(new RowStyle(SizeType.Absolute, Scale(form, 28)));
                header.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
                header.Controls.Add(new Label { Text = "Profile Generic", Font = new Font(BodyFont, FontStyle.Bold),
                    Dock = DockStyle.Fill, Margin = Padding.Empty, TextAlign = ContentAlignment.MiddleLeft }, 0, 0);
                var metadata = new TableLayoutPanel { Name = "ProfileMetadata", Dock = DockStyle.Fill,
                    Margin = Padding.Empty, ColumnCount = 4, RowCount = 3, BackColor = Color.White };
                metadata.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, Scale(form, 112)));
                metadata.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
                metadata.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, Scale(form, 100)));
                metadata.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
                string[] names = { "LogicalNameTB", "CapturePeriodTB", "EntriesInUseTB", "EntriesTB", "SortModeTB" };
                string[] labels = { "Logical Name", "Period", "Entries in use", "Entries", "Sort mode" };
                for (int row = 0; row < 3; ++row) metadata.RowStyles.Add(new RowStyle(SizeType.Absolute, Scale(form, 40)));
                for (int i = 0; i < names.Length; ++i)
                {
                    int column = (i % 2) * 2;
                    metadata.Controls.Add(new Label { Text = labels[i], Dock = DockStyle.Fill, Margin = Padding.Empty,
                        TextAlign = i == 4 ? ContentAlignment.TopLeft : ContentAlignment.MiddleLeft,
                        Padding = i == 4 ? new Padding(0, Scale(form, 10), 0, 0) : Padding.Empty,
                        ForeColor = Muted }, column, i / 2);
                    var field = (GXValueField)controls[names[i]];
                    field.Dock = DockStyle.Top;
                    field.Height = Scale(form, 32);
                    field.MaximumSize = new Size(0, Scale(form, 32));
                    field.Margin = new Padding(0, Scale(form, 4), Scale(form, 8), Scale(form, 4));
                    field.ApplyModernStyle(compact: true);
                    metadata.Controls.Add(field, column + 1, i / 2);
                }
                EditorLayoutPolicy.ConstrainSection(metadata);
                header.Controls.Add(metadata, 0, 1);
                var reference = (GXObjectReferenceView)controls["SortObjectReference"];
                metadata.Controls.Add(new Label { Text = "Sort object", Dock = DockStyle.Fill, Margin = Padding.Empty,
                    TextAlign = ContentAlignment.TopLeft, Padding = new Padding(0, Scale(form, 10), 0, 0), ForeColor = Muted }, 2, 2);
                reference.Dock = DockStyle.Fill;
                reference.AutoScroll = true;
                reference.Margin = new Padding(0, Scale(form, 4), Scale(form, 8), Scale(form, 4));
                metadata.Controls.Add(reference, 3, 2);
                var topActions = new TableLayoutPanel
                {
                    Name = "ProfileHeaderActions", ColumnCount = 1, RowCount = 3,
                    Dock = DockStyle.Top, BackColor = Color.White,
                    Margin = new Padding(Scale(form, 16), Scale(form, 4), 0, 0), Padding = Padding.Empty
                };
                topActions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
                topActions.RowStyles.Add(new RowStyle(SizeType.Absolute, Scale(form, 32)));
                topActions.RowStyles.Add(new RowStyle(SizeType.Absolute, Scale(form, 8)));
                topActions.RowStyles.Add(new RowStyle(SizeType.Absolute, Scale(form, 32)));
                var topButtons = new[] { (Button)controls["ResetBtn"], (Button)controls["CaptureBtn"] };
                for (int i = 0; i < topButtons.Length; ++i)
                {
                    var button = topButtons[i];
                    CompactButton(button); StyleButton(button, false);
                    button.Font = BodyFont; button.AutoSize = false;
                    button.Dock = DockStyle.Fill; button.Margin = Padding.Empty; button.TabIndex = i;
                    topActions.Controls.Add(button, 0, i * 2);
                }
                header.Controls.Add(topActions, 1, 0);
                header.SetRowSpan(topActions, 2);
                int headerRowIndex = root.RowCount;
                Add(header, SizeType.Absolute, Scale(form, 148));
                var reading = new TableLayoutPanel { ColumnCount = 4, RowCount = 4, BackColor = Color.White };
                reading.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, Scale(form, 112)));
                reading.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
                reading.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, Scale(form, 100)));
                reading.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
                string[][] readingRows = {
                    new[] { "ReadEntryBtn", "StartEntry", "label2", "EndEntry" },
                    new[] { "ReadLastRB", "ReadLastTB", "DaysLbl" },
                    new[] { "ReadFromRB", "StartPick", "ToLbl", "ToPick" },
                    new[] { "ReadAllRB" } };
                for (int row = 0; row < readingRows.Length; ++row)
                {
                    reading.RowStyles.Add(new RowStyle(SizeType.Percent, 25));
                    for (int col = 0; col < readingRows[row].Length; ++col)
                    {
                        Control field = controls[readingRows[row][col]];
                        field.Dock = DockStyle.Fill;
                        field.Margin = new Padding(0, Scale(form, 4), Scale(form, 8), Scale(form, 4));
                        if (field is Label label)
                        {
                            label.AutoSize = false;
                            label.TextAlign = ContentAlignment.MiddleLeft;
                            label.Margin = new Padding(0, Scale(form, 4), Scale(form, 8), Scale(form, 4));
                            label.ForeColor = Muted;
                        }
                        // Keep the bound editors and native arrows. Dates retain
                        // their preferred height so the native checkbox stays compact.
                        if (field is NumericUpDown) UseStandardInputHeight(field);
                        if (field is DateTimePicker)
                        {
                            field.Dock = DockStyle.None;
                            field.Anchor = AnchorStyles.Left | AnchorStyles.Right;
                        }
                        reading.Controls.Add(field, col, row);
                    }
                }
                EditorLayoutPolicy.ConstrainSection(reading);
                int readingRowIndex = root.RowCount;
                Add(reading, SizeType.Absolute, Scale(form, 160));
                bool? stackedReading = null;
                bool IsMinimized() => (form.TopLevelControl as Form)?.WindowState == FormWindowState.Minimized;
                bool fittingMetadata = false;
                void FitMetadataHeights()
                {
                    if (fittingMetadata || metadata.IsDisposed || IsMinimized()) return;
                    fittingMetadata = true;
                    metadata.SuspendLayout();
                    try
                    {
                        // Restore logical sizes after the native parenting/DPI pass;
                        // a construction-time maximum can otherwise remain scaled down.
                        int height = Scale(metadata, 32);
                        for (int row = 0; row < 2; ++row)
                            if (metadata.RowStyles[row].Height != Scale(metadata, 40))
                                metadata.RowStyles[row].Height = Scale(metadata, 40);
                        foreach (string name in names)
                        {
                            var field = (GXValueField)controls[name];
                            var maximum = new Size(field.MaximumSize.Width, height);
                            if (field.MaximumSize != maximum) field.MaximumSize = maximum;
                            if (field.Height != height) field.Height = height;
                            var margin = new Padding(0, Scale(metadata, 4), Scale(metadata, 8), Scale(metadata, 4));
                            if (field.Margin != margin) field.Margin = margin;
                        }
                    }
                    finally { metadata.ResumeLayout(true); fittingMetadata = false; }
                }
                void AlignProfileColumns()
                {
                    if (IsMinimized() || root.ClientSize.Width < Scale(form, 150)) return;
                    FitMetadataHeights();
                    bool narrow = root.ClientSize.Width < Scale(form, 720);
                    // Keep the actions above the metadata on narrow screens, so
                    // their reserved column cannot squeeze the paired inputs.
                    header.SuspendLayout();
                    try
                    {
                        header.ColumnStyles[1].Width = Scale(form, 136);
                        header.RowStyles[0].Height = Scale(form, narrow ? 76 : 28);
                        if (narrow)
                        { header.SetRowSpan(topActions, 1); header.SetColumnSpan(metadata, 2); }
                        else
                        { header.SetColumnSpan(metadata, 1); header.SetRowSpan(topActions, 2); }
                        var actionSize = new Size(Scale(form, 120), Scale(form, 72));
                        topActions.MinimumSize = topActions.MaximumSize = actionSize;
                        topActions.Size = actionSize;
                        topActions.Margin = new Padding(Scale(form, 16), Scale(form, 4), 0, 0);
                        topActions.RowStyles[0].Height = topActions.RowStyles[2].Height = Scale(form, 32);
                        topActions.RowStyles[1].Height = Scale(form, 8);
                        foreach (var button in topButtons)
                            button.MinimumSize = button.MaximumSize = new Size(Scale(form, 120), Scale(form, 32));
                    }
                    finally { header.ResumeLayout(true); }
                    metadata.ColumnStyles[0].Width = reading.ColumnStyles[0].Width = Scale(form, 112);
                    metadata.ColumnStyles[2].Width = Scale(form, narrow ? 80 : 100);
                    int propertyWidth = Math.Max(1, Math.Min(Scale(form, 800),
                        root.ClientSize.Width - root.Padding.Horizontal - (narrow ? 0 : Scale(form, 136))));
                    if (reading.MaximumSize.Width != propertyWidth)
                        reading.MaximumSize = new Size(propertyWidth, reading.MaximumSize.Height);
                    if (stackedReading == narrow) return;
                    stackedReading = narrow;
                    reading.SuspendLayout();
                    try
                    {
                        reading.RowCount = narrow ? 6 : 4;
                        reading.RowStyles.Clear();
                        for (int row = 0; row < reading.RowCount; ++row)
                            reading.RowStyles.Add(new RowStyle(SizeType.Percent, 100f / reading.RowCount));
                        reading.ColumnStyles[1].SizeType = SizeType.Percent;
                        reading.ColumnStyles[1].Width = narrow ? 100 : 50;
                        reading.ColumnStyles[2].Width = Scale(form, narrow ? 0 : 100);
                        reading.ColumnStyles[3].SizeType = narrow ? SizeType.Absolute : SizeType.Percent;
                        reading.ColumnStyles[3].Width = narrow ? Scale(form, 52) : 50;
                        void Place(string name, int column, int row, int span = 1)
                        {
                            var field = controls[name];
                            reading.SetColumnSpan(field, span);
                            reading.SetCellPosition(field, new TableLayoutPanelCellPosition(column, row));
                        }
                        if (narrow)
                        {
                            Place("ReadEntryBtn", 0, 0); Place("StartEntry", 1, 0, 3);
                            Place("label2", 0, 1); Place("EndEntry", 1, 1, 3);
                            Place("ReadLastRB", 0, 2); Place("ReadLastTB", 1, 2, 2); Place("DaysLbl", 3, 2);
                            Place("ReadFromRB", 0, 3); Place("StartPick", 1, 3, 3);
                            Place("ToLbl", 0, 4); Place("ToPick", 1, 4, 3);
                            Place("ReadAllRB", 0, 5);
                        }
                        else
                        {
                            for (int row = 0; row < readingRows.Length; ++row)
                                for (int column = 0; column < readingRows[row].Length; ++column)
                                    Place(readingRows[row][column], column, row);
                        }
                        root.RowStyles[readingRowIndex].Height = Scale(form, narrow ? 240 : 160);
                    }
                    finally { reading.ResumeLayout(true); }
                }
                root.SizeChanged += (_, _) => AlignProfileColumns();
                AlignProfileColumns();
                var tabs = (TabControl)controls["tabControl1"];
                Add(tabs, SizeType.Percent, 100);
                FlowLayoutPanel Footer(params string[] buttonNames)
                {
                    var footer = new ActionButtonPanel { AutoSize = true, Dock = DockStyle.Bottom,
                        FlowDirection = FlowDirection.RightToLeft, BackColor = Color.White,
                        Padding = new Padding(0, Scale(form, 8), 0, 0) };
                    foreach (string name in buttonNames.Reverse())
                    {
                        var button = (Button)controls[name];
                        CompactButton(button);
                        StyleButton(button, false);
                        button.Font = BodyFont;
                        button.AutoSize = false;
                        button.Size = new Size(Scale(form, 120), Scale(form, 32));
                        button.MinimumSize = button.MaximumSize = button.Size;
                        button.Margin = new Padding(Scale(form, 8), 0, 0, Scale(form, 4));
                        footer.Controls.Add(button);
                    }
                    ButtonLayoutPolicy.Apply(footer, buttonNames.Select(name => (Button)controls[name]).ToArray(), minimumWidth: 120);
                    return footer;
                }
                foreach (TabPage page in tabs.TabPages)
                { page.BackColor = Color.White; page.UseVisualStyleBackColor = false; page.Padding = new Padding(Scale(form, 8)); }
                var data = (DataGridView)controls["ProfileGenericView"];
                data.Dock = DockStyle.Fill;
                data.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
                data.ColumnHeadersDefaultCellStyle.WrapMode = DataGridViewTriState.True;
                data.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
                data.ColumnAdded += (_, args) => args.Column.MinimumWidth = Scale(data, 120);
                var capturePage = (TabPage)controls["tabPage2"];
                var list = (ListView)controls["CaptureObjectsLv"];
                list.Dock = DockStyle.Fill;
                list.BackColor = Color.White;
                void FitCaptureColumns()
                {
                    if (list.Columns.Count == 0) return;
                    int width = Math.Max(1, list.ClientSize.Width - SystemInformation.VerticalScrollBarWidth - 4);
                    foreach (ColumnHeader column in list.Columns)
                        column.Width = Math.Max(Scale(list, 120), width / list.Columns.Count);
                }
                list.SizeChanged += (_, _) => FitCaptureColumns();
                FitCaptureColumns();
                var captureFooter = Footer("ColumnAddBtn", "ColumnEditBtn", "ColumnRemoveBtn");
                capturePage.Controls.Add(captureFooter);
                list.BringToFront();
                var actions = Footer("AddWhiteBtn", "AddBlackBtn", "RemoveBtn");
                Add(actions, SizeType.AutoSize, 0);
                // Fixed metadata plus wrapping actions must not consume the data
                // area. Keep several rows visible and let the outer editor scroll.
                form.AutoScroll = true;
                bool fitting = false;
                void FitProfileHeight()
                {
                    if (fitting || root.IsDisposed || IsMinimized() || root.ClientSize.Width < Scale(form, 150)) return;
                    fitting = true;
                    try
                    {
                        int contentWidth = Math.Max(1, root.ClientSize.Width - root.Padding.Horizontal);
                        int fixedHeight = (int)Math.Ceiling(root.RowStyles.Cast<RowStyle>().Where(style => style.SizeType == SizeType.Absolute).Sum(style => style.Height));
                        int minimum = fixedHeight + Scale(root, 220) + root.Padding.Vertical + actions.GetPreferredSize(new Size(contentWidth, 0)).Height;
                        int height = Math.Max(form.ClientSize.Height - form.Padding.Vertical, minimum);
                        if (root.Height != height) root.Height = height;
                    }
                    finally { fitting = false; }
                }
                form.SizeChanged += (_, _) => FitProfileHeight();
                root.SizeChanged += (_, _) => FitProfileHeight();
                root.DpiChangedAfterParent += (_, _) => FitProfileHeight();
                actions.SizeChanged += (_, _) => FitProfileHeight();
                bool fittingReference = false;
                void FitReferenceHeight()
                {
                    if (fittingReference || reference.IsDisposed || IsMinimized()) return;
                    fittingReference = true;
                    try
                    {
                        // Use the settled row width, not a child's transient width
                        // while TableLayoutPanel is arranging or the host is minimized.
                        AlignProfileColumns();
                        int[] widths = metadata.GetColumnWidths();
                        int width = widths.Length == 4 ? widths[3] - reference.Margin.Horizontal : 0;
                        if (width < Scale(form, 80)) return;
                        int height = reference.GetPreferredSize(new Size(width, 0)).Height + reference.Margin.Vertical;
                        height = Math.Max(Scale(form, 40), Math.Min(Scale(form, 128), height));
                        if (Math.Abs(metadata.RowStyles[2].Height - height) > 1)
                            metadata.RowStyles[2].Height = height;
                        int headingHeight = Scale(form, root.ClientSize.Width < Scale(form, 720) ? 76 : 28);
                        int headerHeight = headingHeight + Scale(form, 80) + height;
                        if (Math.Abs(root.RowStyles[headerRowIndex].Height - headerHeight) > 1)
                            root.RowStyles[headerRowIndex].Height = headerHeight;
                        FitProfileHeight();
                    }
                    finally { fittingReference = false; }
                }
                // Coalesce notifications until the native layout/DPI transaction
                // finishes. Updating a parent row inside its child's OnLayout can
                // otherwise retain the height measured at an intermediate width.
                bool referenceLayoutPending = false;
                void QueueReferenceLayout()
                {
                    if (referenceLayoutPending || form.IsDisposed || !form.IsHandleCreated) return;
                    referenceLayoutPending = true;
                    form.BeginInvoke((Action)(() =>
                    {
                        referenceLayoutPending = false;
                        if (form.IsDisposed || root.IsDisposed) return;
                        FitReferenceHeight();
                    }));
                }
                metadata.HandleCreated += (_, _) => QueueReferenceLayout();
                metadata.DpiChangedAfterParent += (_, _) => QueueReferenceLayout();
                foreach (string name in names)
                {
                    var field = (GXValueField)controls[name];
                    field.HandleCreated += (_, _) => QueueReferenceLayout();
                    field.DpiChangedAfterParent += (_, _) => QueueReferenceLayout();
                    field.FontChanged += (_, _) => QueueReferenceLayout();
                    field.PresentationLayoutChanged += (_, _) => QueueReferenceLayout();
                }
                reference.ContentSizeChanged += (_, _) => QueueReferenceLayout();
                metadata.SizeChanged += (_, _) => QueueReferenceLayout();
                reference.DpiChangedAfterParent += (_, _) => QueueReferenceLayout();
                form.SizeChanged += (_, _) => QueueReferenceLayout();
                root.SizeChanged += (_, _) => QueueReferenceLayout();
                Form layoutHost = null;
                void HostResized(object sender, EventArgs args) => QueueReferenceLayout();
                void TrackLayoutHost()
                {
                    if (layoutHost != null) layoutHost.Resize -= HostResized;
                    layoutHost = form.TopLevelControl as Form;
                    if (layoutHost != null) layoutHost.Resize += HostResized;
                    QueueReferenceLayout();
                }
                form.ParentChanged += (_, _) => TrackLayoutHost();
                form.HandleCreated += (_, _) => TrackLayoutHost();
                form.Disposed += (_, _) => { if (layoutHost != null) layoutHost.Resize -= HostResized; };
                TrackLayoutHost();
                FitReferenceHeight();
                form.Controls.Remove(group);
                group.Dispose();
            }
            finally { form.ResumeLayout(true); }
        }

        private static void ReflowScriptTable(Form form)
        {
            var group = Find(form, "groupBox1");
            var logicalName = Find(group, "LogicalNameTB");
            var scriptId = Find(group, "ScriptId");
            var execute = (Button)Find(group, "UpdateTimeBtn");
            var tree = (TreeView)Find(group, "ScriptsTree");
            var buttons = new[] { (Button)Find(group, "AddBtn"), (Button)Find(group, "EditBtn"),
                (Button)Find(group, "RemoveBtn") };
            form.SuspendLayout();
            try
            {
                form.AutoScaleMode = AutoScaleMode.Dpi;
                form.Font = BodyFont;
                form.BackColor = Color.White;
                form.Padding = Padding.Empty;
                var content = new TableLayoutPanel
                {
                    Name = "ScriptTableContent", Dock = DockStyle.Fill, BackColor = Color.White,
                    Padding = new Padding(Scale(form, 16)), ColumnCount = 3, RowCount = 0
                };
                content.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, Scale(form, 144)));
                content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
                content.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
                form.Controls.Add(content);
                Section(content, group.Text);
                CompactRow(content, "Logical Name", logicalName);
                CompactRow(content, "Script Id", scriptId, execute, actionWidth: 100);
                Section(content, "Scripts");
                int treeRow = content.RowCount++;
                content.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
                tree.Dock = DockStyle.Fill;
                tree.Margin = Padding.Empty;
                tree.BackColor = Color.White;
                tree.ForeColor = Ink;
                tree.BorderStyle = BorderStyle.FixedSingle;
                content.Controls.Add(tree, 0, treeRow);
                content.SetColumnSpan(tree, 3);
                var footer = new ActionButtonPanel
                {
                    Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft,
                    WrapContents = false, BackColor = Color.White, Margin = Padding.Empty,
                    Padding = new Padding(0, Scale(form, 8), 0, 0)
                };
                foreach (Button button in buttons.Reverse())
                {
                    CompactButton(button);
                    button.AutoSize = false;
                    button.Size = new Size(Scale(form, 100), Scale(form, 32));
                    button.MinimumSize = button.MaximumSize = button.Size;
                    button.Margin = new Padding(Scale(form, 8), 0, 0, 0);
                    StyleButton(button, false);
                    button.Font = BodyFont;
                    footer.Controls.Add(button);
                }
                ButtonLayoutPolicy.Apply(footer, buttons);
                int footerRow = content.RowCount++;
                content.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                content.Controls.Add(footer, 0, footerRow);
                content.SetColumnSpan(footer, 3);
                form.Controls.Remove(group);
                group.Dispose();
            }
            finally { form.ResumeLayout(true); }
        }

        private static void ReflowRegisterActivation(Form form)
        {
            var oldGroup = Find(form, "groupBox1");
            var description = (TextBox)Find(oldGroup, "DescriptionTB");
            var logicalName = (GXValueField)Find(oldGroup, "LogicalNameTB");
            var tabs = (TabControl)Find(oldGroup, "tabControl1");
            var assignmentsPage = (TabPage)Find(tabs, "tabPage1");
            var masksPage = (TabPage)Find(tabs, "tabPage2");
            var assignments = (ListView)Find(assignmentsPage, "Assigments");
            var assignmentAdd = (GXButton)Find(assignmentsPage, "AssigmentAddBtn");
            var masks = (ListView)Find(masksPage, "Masks");
            var activeLabel = (Label)Find(masksPage, "ActiveLbl");
            var activeMask = (ComboBox)Find(masksPage, "ActiveMasksCb");
            var maskAdd = (GXButton)Find(masksPage, "MaskAddBtn");
            var maskRemove = (GXButton)Find(masksPage, "MaskRemoveBtn");

            form.SuspendLayout();
            try
            {
                form.AutoScaleMode = AutoScaleMode.Dpi;
                form.AutoScroll = false;
                form.BackColor = Color.White;
                form.Padding = Padding.Empty;

                var content = new TableLayoutPanel
                {
                    Name = "RegisterActivationContent", Dock = DockStyle.Fill, BackColor = Color.White,
                    Padding = new Padding(Scale(form, 16)), Margin = Padding.Empty,
                    ColumnCount = 2, RowCount = 0
                };
                content.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, Scale(form, 144)));
                content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
                form.Controls.Add(content);
                Section(content, "Register Activation Object");
                CompactRow(content, "Description", description, height: 36);
                CompactRow(content, "OBIS code", logicalName, height: 36);

                tabs.Dock = DockStyle.Fill;
                tabs.Margin = new Padding(0, Scale(form, 4), 0, 0);
                tabs.BackColor = Color.White;
                tabs.Padding = new Point(2, 2);
                content.RowCount++;
                content.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
                content.Controls.Add(tabs, 0, 3);
                content.SetColumnSpan(tabs, 2);

                ConfigureRegisterActivationPage(assignmentsPage, assignments, new[] { assignmentAdd });
                ConfigureRegisterActivationPage(masksPage, masks, new[] { maskAdd, maskRemove }, activeLabel, activeMask);

                foreach (GXButton button in new[] { assignmentAdd, maskAdd, maskRemove })
                {
                    CompactButton(button);
                    button.AutoSize = false;
                    button.Size = new Size(Scale(form, 100), Scale(form, 32));
                    button.MinimumSize = button.MaximumSize = button.Size;
                }

                form.Controls.Remove(oldGroup);
                oldGroup.Dispose();
            }
            finally
            {
                form.ResumeLayout(true);
            }
        }

        private static void ConfigureRegisterActivationPage(TabPage page, ListView list, Button[] buttons,
            Label activeLabel = null, ComboBox activeMask = null)
        {
            page.Controls.Clear();
            page.BackColor = Color.White;
            page.UseVisualStyleBackColor = false;
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(Scale(page, 8)),
                ColumnCount = activeLabel == null ? 1 : 2,
                RowCount = activeLabel == null ? 2 : 3
            };
            if (activeLabel == null)
            {
                layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
                layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
                layout.RowStyles.Add(new RowStyle(SizeType.Absolute, Scale(page, 40)));
            }
            else
            {
                layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, Scale(page, 120)));
                layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
                layout.RowStyles.Add(new RowStyle(SizeType.Absolute, Scale(page, 40)));
                layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
                layout.RowStyles.Add(new RowStyle(SizeType.Absolute, Scale(page, 40)));
                activeLabel.Dock = DockStyle.Fill;
                activeLabel.AutoSize = false;
                activeLabel.TextAlign = ContentAlignment.MiddleLeft;
                activeLabel.ForeColor = Muted;
                activeLabel.Margin = new Padding(0, 0, Scale(page, 12), 0);
                activeMask.FlatStyle = FlatStyle.Standard;
                var activeMaskHost = new NativeComboHost(activeMask)
                {
                    Dock = DockStyle.Fill,
                    Margin = new Padding(0, Scale(page, 4), 0, Scale(page, 4))
                };
                layout.Controls.Add(activeLabel, 0, 0);
                layout.Controls.Add(activeMaskHost, 1, 0);
            }

            list.Dock = DockStyle.Fill;
            list.Margin = Padding.Empty;
            list.BackColor = Color.White;
            list.ForeColor = Ink;
            list.BorderStyle = BorderStyle.FixedSingle;
            list.GridLines = false;
            list.FullRowSelect = true;
            list.HideSelection = false;
            if (activeLabel == null)
                layout.Controls.Add(list, 0, 0);
            else
            {
                layout.Controls.Add(list, 0, 1);
                layout.SetColumnSpan(list, 2);
            }
            void ResizeColumns()
            {
                if (list.Columns.Count < 2) return;
                int width = Math.Max(Scale(list, 120), list.ClientSize.Width - SystemInformation.VerticalScrollBarWidth - Scale(list, 8));
                list.Columns[0].Width = Scale(list, 150);
                list.Columns[1].Width = Math.Max(Scale(list, 100), width - list.Columns[0].Width);
            }
            list.SizeChanged += (_, _) => ResizeColumns();
            ResizeColumns();

            var footer = new ActionButtonPanel
            {
                Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, WrapContents = false,
                BackColor = Color.White, Margin = Padding.Empty,
                Padding = new Padding(0, Scale(page, 4), 0, Scale(page, 4))
            };
            for (int i = buttons.Length - 1; i >= 0; --i)
            {
                var button = buttons[i];
                // The rightmost action meets the list edge; only peer actions
                // need an inner gap. Native default margins add a stray inset.
                button.Margin = new Padding(i > 0 ? Scale(page, 8) : 0, 0, 0, 0);
                button.Anchor = AnchorStyles.Top | AnchorStyles.Left;
                footer.Controls.Add(button);
            }
            ButtonLayoutPolicy.Apply(footer, buttons);
            if (activeLabel == null)
                layout.Controls.Add(footer, 0, 1);
            else
            {
                layout.Controls.Add(footer, 0, 2);
                layout.SetColumnSpan(footer, 2);
            }
            EditorLayoutPolicy.ConfigurePropertyTable(layout, boundSection: false);
            page.Controls.Add(layout);
        }

        private static void ConfigureEditorTabs(Form form)
        {
            if (form is not IGXDLMSView) return;
            foreach (TabControl tabs in Descendants(form).OfType<TabControl>().ToArray())
            {
                if (tabs is AssociationTabControl) continue;
                RoundTabs(tabs);
            }
        }

        /// <summary>
        /// Give an existing tab control the shared rounded tab headers (as on the main window and the object
        /// editors). The control, its pages and their selection and keyboard behavior are unchanged; only the
        /// header painting, tab size and page colors change.
        /// </summary>
        public static void RoundTabs(TabControl tabs, int headerHeight = 24)
        {
            tabs.DrawMode = TabDrawMode.OwnerDrawFixed;
            int widestCaption = tabs.TabPages.Cast<TabPage>()
                .Select(page => TextRenderer.MeasureText(page.Text, tabs.Font).Width)
                .DefaultIfEmpty(0).Max();
            tabs.SizeMode = TabSizeMode.Fixed;
            tabs.ItemSize = new Size(Math.Min(Scale(tabs, 220), Math.Max(Scale(tabs, 80), widestCaption + Scale(tabs, 24))), Scale(tabs, headerHeight));
            tabs.BackColor = Color.White;
            tabs.HandleCreated += (_, _) => DisableTabTheme(tabs);
            if (tabs.IsHandleCreated) DisableTabTheme(tabs);
            void AttachPainter()
            {
                if (!TabPainters.TryGetValue(tabs, out _))
                    TabPainters.Add(tabs, new TabHeaderPainter(tabs));
            }
            tabs.HandleCreated += (_, _) => AttachPainter();
            if (tabs.IsHandleCreated) AttachPainter();
            tabs.DrawItem += DrawRoundedTab;
            foreach (TabPage page in tabs.TabPages)
            {
                page.BackColor = Color.White;
                page.UseVisualStyleBackColor = false;
            }
        }

        private static void DisableTabTheme(TabControl tabs)
        {
            try { SetWindowTheme(tabs.Handle, "", ""); }
            catch (DllNotFoundException) { }
            catch (EntryPointNotFoundException) { }
            tabs.BackColor = Color.White;
            // Hide the dotted focus rectangle until the keyboard is used (WM_CHANGEUISTATE, UIS_SET, UISF_HIDEFOCUS).
            SendMessage(tabs.Handle, 0x0127, (IntPtr)0x10001, IntPtr.Zero);
            tabs.Invalidate();
        }

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

        [DllImport("uxtheme.dll", CharSet = CharSet.Unicode)]
        private static extern int SetWindowTheme(IntPtr hWnd, string? appName, string? idList);

        private static System.Collections.Generic.IEnumerable<Control> Descendants(Control root)
        {
            foreach (Control child in root.Controls)
            {
                yield return child;
                foreach (Control descendant in Descendants(child)) yield return descendant;
            }
        }

        private static void DrawRoundedTab(object? sender, DrawItemEventArgs e)
        {
            if (sender is not TabControl tabs || e.Index < 0 || e.Index >= tabs.TabPages.Count) return;
            var bounds = tabs.GetTabRect(e.Index);
            bool selected = e.Index == tabs.SelectedIndex;
            // Cover the native header edge and focus cue so only the rounded shape shows.
            using (var cover = new SolidBrush(Color.White))
                e.Graphics.FillRectangle(cover, bounds.Left, 0, bounds.Width + 1, bounds.Bottom + 1);
            bounds.Y = Scale(tabs, 2);
            int join = Math.Max(tabs.GetTabRect(0).Bottom, tabs.DisplayRectangle.Top) - Scale(tabs, 1);
            bounds.Height = Math.Max(1, join - bounds.Top + Scale(tabs, 1));
            int radius = Math.Min(Scale(tabs, 6), Math.Min(bounds.Width / 2, bounds.Height / 2));
            int right = bounds.Right - Scale(tabs, 1);
            using var path = new System.Drawing.Drawing2D.GraphicsPath();
            path.AddLine(bounds.Left, join, bounds.Left, bounds.Top + radius);
            path.AddArc(bounds.Left, bounds.Top, radius * 2, radius * 2, 180, 90);
            path.AddLine(bounds.Left + radius, bounds.Top, right - radius, bounds.Top);
            path.AddArc(right - radius * 2, bounds.Top, radius * 2, radius * 2, 270, 90);
            path.AddLine(right, bounds.Top + radius, right, join);

            var smoothing = e.Graphics.SmoothingMode;
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            Color chrome = Color.FromArgb(243, 243, 243);
            using (var fill = new SolidBrush(selected ? Color.White : chrome)) e.Graphics.FillPath(fill, path);
            using (var outline = new Pen(selected ? Accent : Line)) e.Graphics.DrawPath(outline, path);
            if (selected)
            {
                using var eraseJoin = new Pen(Color.White, Math.Max(2, Scale(tabs, 2)));
                e.Graphics.DrawLine(eraseJoin, bounds.Left + 1, join, right - 1, join);
            }
            e.Graphics.SmoothingMode = smoothing;
            TextRenderer.DrawText(e.Graphics, tabs.TabPages[e.Index].Text, tabs.Font,
                Rectangle.FromLTRB(bounds.Left + Scale(tabs, 3), bounds.Top,
                    bounds.Right - Scale(tabs, 3), join), selected ? Ink : Muted,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis |
                TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding);
        }

        /// <summary>Reparent existing OK/Cancel buttons; leave validation and DialogResult handlers intact.</summary>
        public static FlowLayoutPanel DialogFooter(Form form, Button accept, Button cancel)
        {
            var body = new Panel
            {
                Name = "ModernDialogBody",
                Dock = DockStyle.Fill,
                AutoScroll = true,
                Padding = Padding.Empty,
                BackColor = form.BackColor
            };
            var content = form.Controls.Cast<Control>()
                .Where(control => control != accept && control != cancel)
                .ToArray();
            foreach (Control control in content)
            {
                form.Controls.Remove(control);
                body.Controls.Add(control);
            }
            form.AutoScroll = false;
            form.Controls.Add(body);
            var footer = new ActionButtonPanel { Dock = DockStyle.Bottom, AutoSize = true,
                FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(8), WrapContents = false };
            CompactButton(accept);
            if (!ReferenceEquals(accept, cancel))
                CompactButton(cancel);
            if (!ReferenceEquals(accept, cancel))
                footer.Controls.Add(cancel);
            footer.Controls.Add(accept);
            ButtonLayoutPolicy.Apply(footer, ReferenceEquals(accept, cancel) ? new[] { accept } : new[] { accept, cancel },
                minimumWidth: 80, horizontalInset: 8, topInset: 8, bottomInset: 8);
            form.AcceptButton = accept;
            form.CancelButton = cancel;
            form.Controls.Add(footer);
            footer.BringToFront();
            // Keep the scrolling region above the footer even when legacy callers
            // contribute padding or nested docked containers.
            body.Dock = DockStyle.None;
            void LayoutBody(object sender, EventArgs args)
            {
                int left = body.Left;
                int top = body.Top;
                int right = Math.Max(left, form.ClientSize.Width - left);
                int bottom = Math.Max(top, footer.Top);
                body.Bounds = new Rectangle(left, top, Math.Max(0, right - left), Math.Max(0, bottom - top));
            }
            form.Layout += LayoutBody;
            form.ClientSizeChanged += (sender, args) => LayoutBody(sender, args);
            form.PerformLayout();
            form.StartPosition = FormStartPosition.CenterParent;
            return footer;
        }

        public static void StyleButton(Button button, bool primary)
        {
            button.FlatStyle = SystemInformation.HighContrast ? FlatStyle.System : FlatStyle.Flat;
            button.FlatAppearance.BorderSize = primary ? 0 : 1;
            button.FlatAppearance.BorderColor = Line;
            button.BackColor = SystemInformation.HighContrast ? SystemColors.Control : primary ? Accent : Color.White;
            button.ForeColor = SystemInformation.HighContrast ? SystemColors.ControlText : primary ? Color.White : Ink;
            button.Font = new Font("Segoe UI", 10f, FontStyle.Bold);
            button.Cursor = Cursors.Hand;
            button.UseVisualStyleBackColor = false;
        }
    }

    public partial class GXValueField
    {
        internal event EventHandler PresentationLayoutChanged;
        internal bool RequiresStructuredLayout => Type == Gurux.DLMS.Enums.ValueFieldType.Xml || (Type == Gurux.DLMS.Enums.ValueFieldType.TextBox && textBox1.Multiline);
        private bool modernPresentation;
        private bool compactPresentation;
        private bool spaciousPresentation;
        private bool arrangingNativeCombo;
        private bool NativeComboPresentation => modernPresentation && Type == Gurux.DLMS.Enums.ValueFieldType.CompoBox;

        internal void ApplyModernStyle(bool spacious = false, bool compact = false)
        {
            bool first = !modernPresentation;
            modernPresentation = true;
            compactPresentation |= compact;
            spaciousPresentation |= spacious;
            SetStyle(ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            if (first)
            {
                foreach (Control child in Controls)
                {
                    child.Enter += (_, _) => Invalidate();
                    child.Leave += (_, _) => Invalidate();
                }
                comboBox1.FontChanged += (_, _) => PerformLayout();
            }
            UpdateModernPresentationType();
        }

        // Binding can switch the same field between scalar, enum and structured
        // values. Derive appearance from the current editor, never a sticky flag.
        private void UpdateModernPresentationType()
        {
            if (!modernPresentation) return;
            BackColor = NativeComboPresentation ? Parent?.BackColor ?? Color.White
                : SystemInformation.HighContrast ? SystemColors.Window : Color.FromArgb(245, 247, 251);
            Padding = NativeComboPresentation ? Padding.Empty : compactPresentation ? new Padding(8, 6, 8, 4)
                : spaciousPresentation ? new Padding(12, 10, 12, 7) : Padding.Empty;
            textBox1.BorderStyle = BorderStyle.None;
            textBox1.BackColor = BackColor;
            textBox1.ForeColor = SystemInformation.HighContrast ? SystemColors.WindowText : ModernPresentation.Ink;
            textBox1.Dock = textBox1.Multiline ? DockStyle.Fill : DockStyle.Top;
            comboBox1.FlatStyle = FlatStyle.Standard;
            PerformLayout();
            Invalidate();
        }

        protected override void OnParentChanged(EventArgs e)
        {
            base.OnParentChanged(e);
            UpdateModernPresentationType();
        }

        protected override void OnLayout(LayoutEventArgs e)
        {
            if (NativeComboPresentation && comboBox1 != null)
            {
                if (arrangingNativeCombo) return;
                arrangingNativeCombo = true;
                try
                {
                    BackColor = Parent?.BackColor ?? Color.White;
                    Padding = Padding.Empty;
                    comboBox1.Dock = DockStyle.None;
                    base.OnLayout(e);
                    ModernPresentation.LayoutNativeCombo(comboBox1, ClientRectangle);
                }
                finally { arrangingNativeCombo = false; }
                return;
            }
            if (modernPresentation && textBox1 != null)
                textBox1.Dock = textBox1.Multiline ? DockStyle.Fill : DockStyle.Top;
            if (compactPresentation)
            {
                var inset = new Padding(8, 6, 8, 4);
                if (Padding != inset) Padding = inset;
            }
            base.OnLayout(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if (!modernPresentation || NativeComboPresentation) return;
            using var pen = new Pen(SystemInformation.HighContrast ? SystemColors.WindowText : ContainsFocus ? ModernPresentation.Accent : ModernPresentation.Line);
            e.Graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
        }
    }
}
