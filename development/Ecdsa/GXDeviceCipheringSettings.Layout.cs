// Superintelligent Meter contributors, 2026-10-31. Grouped ciphering presentation. GPL-2.0-only.
// SPDX-License-Identifier: GPL-2.0-only
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Gurux.DLMS.Enums;
using Gurux.DLMS.Objects.Enums;

namespace Gurux.DLMS.UI.Ecdsa
{
    public partial class GXDeviceCipheringSettings
    {
        private TabPage framingPage;
        private GroupBox securedKeysGroup;
        private readonly List<Action> refreshKeyFeedback = new List<Action>();

        private void InitializeGroupedCipheringLayout()
        {
            // Build the page with every container's layout suspended, then let each container
            // lay out once (deepest first). Laying out the nested AutoSize tables after every
            // added row made constructing the dialog several times slower.
            var batch = new List<Control>();
            T Batched<T>(T container) where T : Control
            {
                container.SuspendLayout();
                batch.Add(container);
                return container;
            }
            void SuspendTree(Control control)
            {
                if (control.Controls.Count == 0) return;
                Batched(control);
                foreach (Control child in control.Controls) SuspendTree(child);
            }
            SuspendTree(this);
            try
            {
                TableLayoutPanel Surface(Control parent)
                {
                    var rows = Batched(new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true,
                        AutoSizeMode = AutoSizeMode.GrowAndShrink, ColumnCount = 3,
                        Margin = Padding.Empty, TabStop = false });
                    rows.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 144));
                    rows.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
                    rows.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 88));
                    parent.Controls.Add(rows);
                    if (parent is Panel panel && parent is not TabPage) { panel.AutoSize = true; panel.AutoSizeMode = AutoSizeMode.GrowAndShrink; }
                    if (parent is GroupBox group) { group.AutoSize = true; group.AutoSizeMode = AutoSizeMode.GrowAndShrink; }
                    return rows;
                }
                var common = Surface(CipheringTab);
                common.Name = "SecuredGroups";
                TableLayoutPanel Group(string name, string caption, out GroupBox group)
                {
                    group = Batched(new GroupBox { Name = name, Text = caption, Dock = DockStyle.Top });
                    var rows = Surface(group);
                    rows.Name = name + "Fields";
                    int S(int value) => (int)Math.Round(value * rows.DeviceDpi / 96d);
                    group.Padding = new Padding(S(8));
                    CipheringLayout.Span(common, group);
                    return rows;
                }
                var security = Group("SecurityGroup", "Security", out _);
                CipheringLayout.Row(security, label1, SecuritySuiteCb);
                var modes = Batched(new TableLayoutPanel { Name = "SecuritySigningFields", Dock = DockStyle.Fill,
                    AutoSize = true, ColumnCount = 3, RowCount = 1, Margin = Padding.Empty, TabStop = false });
                modes.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
                modes.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
                modes.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
                SigningLbl.Text = "Signing:";
                SecurityCB.Dock = SigningCb.Dock = DockStyle.Top;
                SecurityCB.Margin = SigningCb.Margin = Padding.Empty;
                SigningLbl.AutoSize = true; SigningLbl.Anchor = AnchorStyles.Left;
                modes.Controls.Add(SecurityCB, 0, 0); modes.Controls.Add(SigningLbl, 1, 0); modes.Controls.Add(SigningCb, 2, 0);
                CipheringLayout.Row(security, SecurityLbl, modes);

                var titles = Group("SystemTitlesGroup", "System titles", out _);
                label7.Text = "Client system title:";
                CipheringLayout.Row(titles, label7, SystemTitleTb, SystemTitleAsciiCb);
                CipheringLayout.Span(titles, UsePreEstablishedApplicationAssociations);
                ServerSystemTitleTb.AccessibleName = "Server system title";
                CipheringLayout.Row(titles, new Label { Text = "Server system title:" }, ServerSystemTitleTb);

                var keys = Group("KeysGroup", "Keys", out securedKeysGroup);
                var tooltips = new ToolTip(components);
                void Key(TableLayoutPanel rows, Label caption, TextBox editor, CheckBox ascii = null, bool challenge = false, bool optional = false)
                {
                    CipheringLayout.Row(rows, caption, editor, ascii);
                    var feedback = new Label { Name = editor.Name + "Validation", AutoSize = true,
                        Dock = DockStyle.Fill, Margin = new Padding(0, (int)Math.Round(4*rows.DeviceDpi/96d), 0, (int)Math.Round(4*rows.DeviceDpi/96d)), TabStop = false,
                        ForeColor = SystemInformation.HighContrast ? SystemColors.WindowText : Color.Firebrick, Visible = false,
                        AccessibleName = caption.Text.TrimEnd(':') + " validation" };
                    int row = rows.RowCount++;
                    rows.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                    rows.Controls.Add(feedback, 1, row); rows.SetColumnSpan(feedback, 2);
                    void RefreshFeedback()
                    {
                        int bytes = SecuritySuiteCb.SelectedItem is SecuritySuite selected && selected == SecuritySuite.Suite2 ? 32 : 16;
                        bool asAscii = ascii != null && ascii.Checked;
                        string expected = challenge ? "Optional hex bytes; leave blank for an automatic challenge."
                            : (optional ? "Optional; " : "") + bytes + " bytes (" +
                              (asAscii ? bytes + " ASCII characters" : bytes * 2 + " hex digits") + ").";
                        editor.PlaceholderText = challenge ? "Optional hexadecimal byte pairs"
                            : asAscii ? bytes + " ASCII characters" : bytes * 2 + " hex digits (" + bytes + " bytes)";
                        tooltips.SetToolTip(editor, expected);
                        string message = "";
                        if (securedKeysGroup.Enabled && editor.Text.Length > 0)
                        {
                            try
                            {
                                int length;
                                if (asAscii)
                                {
                                    if (editor.Text.Any(c => c > 127)) message = "Use ASCII characters only.";
                                    length = Encoding.ASCII.GetByteCount(editor.Text);
                                }
                                else
                                {
                                    string compact = editor.Text.Replace(" ", "");
                                    if (compact.Length % 2 != 0) message = "Enter complete hexadecimal byte pairs.";
                                    length = GXDLMSTranslator.HexToBytes(editor.Text).Length;
                                }
                                if (!challenge && message.Length == 0 && length != bytes)
                                    message = "Expected " + bytes + " bytes; currently " + length + ".";
                            }
                            catch { message = "Enter valid hexadecimal byte pairs."; }
                        }
                        feedback.Text = message;
                        feedback.Visible = message.Length > 0;
                        editor.AccessibleDescription = expected + (message.Length == 0 ? "" : " " + message);
                    }
                    editor.TextChanged += (_, _) => RefreshFeedback();
                    if (ascii != null) ascii.CheckedChanged += (_, _) => RefreshFeedback();
                    refreshKeyFeedback.Add(RefreshFeedback);
                }
                Key(keys, BlockCipherKeyLbl, BlockCipherKeyTB, BlockCipherKeyAsciiCb);
                Key(keys, AuthenticationKey0Lbl, AuthenticationKeyTB, AuthenticationKeyAsciiCb);
                Key(keys, BroadcastKeyLbl, BroadcastKeyTb, BroadcastKeyAsciiCb, optional: true);
                var suite0 = Surface(Cipheringv0);
                Key(suite0, DedicatedKeyLbl, DedicatedKeyTb, DedicatedKeyAsciiCb, optional: true);
                Key(suite0, ChallengeLbl, ChallengeTB, challenge: true);

                var suite1 = Surface(Cipheringv1);
                label3.Text = "Client signing key:"; label4.Text = "Client agreement key:";
                label6.Text = "Server signing key:"; label5.Text = "Server agreement key:";
                CipheringLayout.Row(suite1, label3, ClientSigningKeysCb, ClientSigningKeysInfoBtn);
                CipheringLayout.Row(suite1, label4, ClientAgreementKeysCb, ClientAgreementKeyInfoBtn);
                CipheringLayout.Row(suite1, label6, ServerSigningKeysCb, ServerSigningKeyInfoBtn);
                CipheringLayout.Row(suite1, label5, ServerAgreementKeysCb, ServerAgreementKeyInfoBtn);
                groupBox1.Dispose(); groupBox2.Dispose();
                CipheringLayout.Span(suite1, SignInitiateRequestResponseCb);
                CipheringLayout.HostSuites(tabControl1, CipheringTab, CipheringPanel, Cipheringv0, Cipheringv1, CipheringV0Tab, CipheringV1Tab);
                CipheringLayout.Span(keys, CipheringPanel);
                CipheringPanel.Margin = Padding.Empty;

                GenerateDedicatedKeyBtn.Text = "Generate keys";
                tooltips.SetToolTip(GenerateDedicatedKeyBtn, "Generate a dedicated key using the existing generator.");
                GenerateDedicatedKeyBtn.AccessibleDescription = "Generate a dedicated key.";
                tooltips.SetToolTip(UpdateBtn, "Select stored keys whose certificates match the client and server system titles.");
                // One trailing action row: the original Find (signing keys) and
                // dedicated-key generator commands alternate with the signing mode.
                var actions = Batched(new ActionButtonPanel { Name = "KeysActions", Dock = DockStyle.Fill });
                ButtonLayoutPolicy.Apply(actions, new[] { UpdateBtn, GenerateDedicatedKeyBtn, ClientCertificateBtn },
                    minimumWidth: 100, topInset: 0, bottomInset: 0, equalWidths: false, wrap: false);
                // The Security None hint shares this row: it starts on the caption rail and the
                // actions end at the input edge (the same reserved accessory column as the ASCII
                // checks; never stretch individual buttons). The actions sit in a measured column,
                // so they stay on one row. The buttons set the row height, so showing or hiding
                // the hint never moves Keys or the Invocation counter group.
                var hint = new GroupStateHint { Name = "KeysDisabledHint", AutoSize = true, Anchor = AnchorStyles.Left,
                    Margin = Padding.Empty, TabStop = false, ForeColor = ModernPresentation.Muted,
                    Text = "Select a security mode to enable the key settings." };
                var footer = Batched(new TableLayoutPanel { Name = "KeysFooter", AutoSize = true,
                    AutoSizeMode = AutoSizeMode.GrowAndShrink, ColumnCount = 2, RowCount = 1,
                    Margin = Padding.Empty, Padding = Padding.Empty, TabStop = false });
                footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
                footer.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
                footer.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                footer.Controls.Add(hint, 0, 0);
                footer.Controls.Add(actions, 1, 0);
                actions.TabIndex = 1;
                CipheringLayout.Span(keys, footer);
                keys.SetColumnSpan(footer, 2);
                void RefreshState()
                {
                    securedKeysGroup.Enabled = SecurityCB.SelectedItem is Security mode && mode != Security.None;
                    hint.Visible = !securedKeysGroup.Enabled;
                    // Same visibility as the original signing / symmetric-key panels.
                    bool signingKeys = SigningCb.SelectedItem is Signing signing && signing != Signing.None;
                    UpdateBtn.Visible = signingKeys;
                    GenerateDedicatedKeyBtn.Visible = !signingKeys;
                    foreach (var refresh in refreshKeyFeedback) refresh();
                }
                SecurityCB.SelectedIndexChanged += (_, _) => RefreshState();
                SecuritySuiteCb.SelectedIndexChanged += (_, _) => RefreshState();
                SigningCb.SelectedIndexChanged += (_, _) => RefreshState();
                RefreshState();

                framingPage = Batched(new TabPage { Name = "CipheringFramingTab", Text = "Framing", Padding = new Padding(8) });
                IgnoreSNRM.Dock = DockStyle.Top; IgnoreSNRM.Margin = Padding.Empty;
                framingPage.Controls.Add(IgnoreSNRM);
                tabControl1.TabPages.Add(framingPage);
                // Rebuild logical spacing after reparenting and monitor changes rather than
                // scaling the last physical margins (which can lose pixels on a round trip).
                foreach (var rows in new[] { common, security, titles, keys, suite0, suite1 })
                {
                    bool applying = false;
                    void ApplySpacing()
                    {
                        if (applying || rows.IsDisposed) return;
                        applying = true;
                        try
                        {
                            int S(int v) => (int)Math.Round(v * rows.DeviceDpi / 96d);
                            rows.ColumnStyles[0].Width = S(144);
                            rows.ColumnStyles[2].Width = S(88);
                            if (ReferenceEquals(rows, common)) rows.MaximumSize = new Size(S(800), 0);
                            SigningLbl.Margin = new Padding(S(16), 0, S(12), 0);
                            if (ReferenceEquals(rows, security))
                            {
                                int modeHeight = Math.Max(SecurityCB.PreferredHeight, SigningCb.PreferredHeight);
                                modes.MinimumSize = new Size(0, modeHeight);
                                rows.RowStyles[1].SizeType = SizeType.Absolute;
                                rows.RowStyles[1].Height = modeHeight + S(8);
                            }
                            if (rows.Parent is GroupBox group) group.Padding = new Padding(S(8));
                            foreach (Control cell in rows.Controls)
                            {
                                int column = rows.GetColumn(cell);
                                int span = rows.GetColumnSpan(cell);
                                if (column == 1 && (cell is TextBoxBase || cell is ComboBox || ReferenceEquals(cell, modes)))
                                    rows.SetColumnSpan(cell, 1);
                                cell.Margin = ReferenceEquals(cell, CipheringPanel) ? Padding.Empty
                                    : new Padding(column == 2 ? S(8) : 0, S(4),
                                        column == 0 && span == 1 ? S(12) : 0, S(4));
                            }
                        }
                        finally { applying = false; }
                    }
                    rows.Layout += (_, _) => ApplySpacing();
                    rows.DpiChangedAfterParent += (_, _) => ApplySpacing();
                    ApplySpacing();
                }
                CipheringLayout.TrackScrollExtent(CipheringTab, common);
            }
            finally
            {
                foreach (var container in batch.OrderByDescending(LayoutDepth).ToArray())
                    if (!container.IsDisposed) container.ResumeLayout(true);
            }
        }

        private static int LayoutDepth(Control control)
        {
            int depth = 0;
            for (var parent = control.Parent; parent != null; parent = parent.Parent) ++depth;
            return depth;
        }

        /// <summary>Presentation page containing the original framing option.</summary>
        public TabPage GetFramingTab() => framingPage;

        /// <summary>
        /// Owner for the original message boxes. The editors are hosted by another
        /// dialog, so this hidden form's Parent is null; use the visible host instead.
        /// </summary>
        private IWin32Window DialogOwner => SystemTitleTb.FindForm() is Form host && host.Visible ? host : null;

        /// <summary>
        /// Explains why its group is disabled. It sits inside that group, where a native
        /// label would paint in the faint disabled color; this one keeps its own color.
        /// </summary>
        private sealed class GroupStateHint : Label
        {
            protected override void OnPaint(PaintEventArgs e)
            {
                var flags = TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis;
                if (!UseMnemonic) flags |= TextFormatFlags.NoPrefix;
                if (RightToLeft == RightToLeft.Yes) flags |= TextFormatFlags.Right | TextFormatFlags.RightToLeft;
                TextRenderer.DrawText(e.Graphics, Text, Font, ClientRectangle, ForeColor, flags);
            }
        }
    }
}
