// Modified for the superintelligentmeter fork, 2026-10-16: .NET 10 migration, UI modernization and compatibility fixes. GPL-2.0-only; original notices retained.
using System;
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using Gurux.DLMS.Objects;

namespace Gurux.DLMS.UI
{
    // Keeps objects without a dedicated editor navigable without enabling writes.
    [GXDLMSView(typeof(GXDLMSObject))]
    public class GXDLMSReadOnlyObjectView : Form, IGXDLMSView
    {
        private GXDLMSObject target;
        private readonly Label heading;
        private readonly Label logicalName;
        protected readonly ListView Values;
        private readonly ErrorProvider errors;

        public GXDLMSReadOnlyObjectView()
        {
            AutoScaleMode = AutoScaleMode.Dpi;
            BackColor = Color.White;
            Padding = new Padding(12);
            var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, BackColor = Color.White };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            heading = new Label { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, AutoEllipsis = true };
            logicalName = new Label { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };
            Values = new ListView { Dock = DockStyle.Fill, View = View.Details, FullRowSelect = true, BorderStyle = BorderStyle.FixedSingle, BackColor = Color.White, HideSelection = false };
            Values.Columns.Add("Attribute", 220);
            Values.Columns.Add("Value (read only)", 400);
            Values.Resize += (_, _) => ResizeColumns();
            layout.Controls.Add(heading, 0, 0);
            layout.Controls.Add(logicalName, 0, 1);
            layout.Controls.Add(Values, 0, 2);
            Controls.Add(layout);
            errors = new ErrorProvider { ContainerControl = this };
        }

        private void ResizeColumns()
        {
            if (Values.Columns.Count == 1) Values.Columns[0].Width = Math.Max(180, Values.ClientSize.Width - 24);
            else Values.Columns[1].Width = Math.Max(180, Values.ClientSize.Width - Values.Columns[0].Width - 24);
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public GXDLMSObject Target
        {
            get => target;
            set { target = value; RefreshValues(); }
        }

        protected virtual void RefreshValues()
        {
            Values.Items.Clear();
            if (target == null) return;
            heading.Text = target.ObjectType + " — read-only view";
            logicalName.Text = "Logical Name:   " + target.LogicalName;
            var names = (target as IGXDLMSBase)?.GetNames();
            var values = target.GetValues();
            Values.BeginUpdate();
            try
            {
                for (int i = 0; i < values.Length; ++i)
                    Values.Items.Add(new ListViewItem(new[] { names != null && i < names.Length ? names[i] : "Attribute " + (i + 1), FormatValue(values[i]) }));
            }
            finally { Values.EndUpdate(); }
        }

        private static string FormatValue(object value)
        {
            if (value == null) return "";
            if (value is byte[] bytes) return GXDLMSTranslator.ToHex(bytes);
            if (value is IEnumerable items && !(value is string))
            {
                var parts = new System.Collections.Generic.List<string>();
                foreach (var item in items) parts.Add(FormatValue(item));
                return string.Join(", ", parts);
            }
            return Convert.ToString(value);
        }

        protected void SetHeading(string text) { heading.Text = text; }
        public void OnValueChanged(GXDLMSViewArguments arg) { RefreshValues(); }
        public void OnAccessRightsChange(GXDLMSViewArguments arg) { }
        public void OnMethodAccessRightsChange(GXDLMSViewArguments arg) { }
        public void OnDirtyChange(int index, bool dirty) { }
        public void PreAction(GXActionArgs arg) { }
        public void PostAction(GXActionArgs arg) { }
        public ErrorProvider ErrorProvider => errors;
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string Description { get => null; set { } }
        protected override void Dispose(bool disposing) { if (disposing) errors.Dispose(); base.Dispose(disposing); }
    }

    [GXDLMSView(typeof(GXDLMSSFSKReportingSystemList))]
    public sealed class GXDLMSSFSKReportingSystemListView : GXDLMSReadOnlyObjectView
    {
        public GXDLMSSFSKReportingSystemListView()
        {
            Values.Columns.Clear();
            Values.Columns.Add("Reporting system title (hex)", 500);
        }

        protected override void RefreshValues()
        {
            base.RefreshValues();
            SetHeading("S-FSK Reporting System List");
            Values.Items.Clear();
            if (Target is GXDLMSSFSKReportingSystemList item && item.ReportingSystemList != null)
                foreach (byte[] title in item.ReportingSystemList)
                    Values.Items.Add(GXDLMSTranslator.ToHex(title));
        }
    }
}
