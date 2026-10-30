// superintelligentmeter contributors, 2026-10-30. Native dropdown layout hosts. GPL-2.0-only.
// SPDX-License-Identifier: GPL-2.0-only
using System;
using System.Drawing;
using System.Windows.Forms;

namespace Gurux.DLMS.UI
{
    public static partial class ModernPresentation
    {
        // Native combo boxes choose their own height from the current font/DPI.
        // Keep their one native frame and center the actual (possibly coerced)
        // height in the layout slot, without painting another input behind it.
        internal static void LayoutNativeCombo(ComboBox combo, Rectangle bounds)
        {
            combo.Dock = DockStyle.None;
            int height = combo.DropDownStyle == ComboBoxStyle.Simple ? bounds.Height : combo.PreferredHeight;
            combo.SetBounds(bounds.Left, bounds.Top, Math.Max(0, bounds.Width), Math.Max(0, height));
            combo.Top = bounds.Top + Math.Max(0, (bounds.Height - combo.Height) / 2);
        }

        private sealed class NativeComboHost : Panel
        {
            private readonly ComboBox editor;
            private bool arranging;

            internal NativeComboHost(ComboBox editor)
            {
                this.editor = editor;
                Name = editor.Name + "Host";
                TabStop = false;
                TabIndex = editor.TabIndex;
                editor.Dock = DockStyle.None;
                editor.Anchor = AnchorStyles.Top | AnchorStyles.Left;
                editor.Margin = Padding.Empty;
                editor.TabIndex = 0;
                Controls.Add(editor);
                editor.FontChanged += (_, _) => PerformLayout();
            }

            protected override void OnLayout(LayoutEventArgs e)
            {
                base.OnLayout(e);
                if (editor == null || arranging) return;
                arranging = true;
                try
                {
                    BackColor = Parent?.BackColor ?? Color.White;
                    LayoutNativeCombo(editor, ClientRectangle);
                }
                finally { arranging = false; }
            }

            protected override void OnParentChanged(EventArgs e) { base.OnParentChanged(e); PerformLayout(); }
            protected override void OnDpiChangedAfterParent(EventArgs e) { base.OnDpiChangedAfterParent(e); PerformLayout(); }
            public override Size GetPreferredSize(Size proposedSize) => editor == null ? base.GetPreferredSize(proposedSize) : editor.GetPreferredSize(proposedSize);
        }
    }
}
