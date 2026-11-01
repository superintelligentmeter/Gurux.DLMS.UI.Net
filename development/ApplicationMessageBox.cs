// Superintelligent Meter contributors, 2026-10-31. GPL-2.0-only.
using System;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace Gurux.DLMS.UI
{
    /// <summary>Standard messages rendered at the owning monitor's DPI by native Task Dialogs.</summary>
    public static class ApplicationMessageBox
    {
        private static WeakReference<Form> defaultOwner;

        public static void SetDefaultOwner(Form owner) => defaultOwner = new WeakReference<Form>(owner);

        public static DialogResult Show(string text, string caption = "", MessageBoxButtons buttons = MessageBoxButtons.OK,
            MessageBoxIcon icon = MessageBoxIcon.None, MessageBoxDefaultButton defaultButton = MessageBoxDefaultButton.Button1,
            MessageBoxOptions options = 0) => Show(null, text, caption, buttons, icon, defaultButton, options);

        public static DialogResult Show(IWin32Window owner, string text, string caption = "",
            MessageBoxButtons buttons = MessageBoxButtons.OK, MessageBoxIcon icon = MessageBoxIcon.None,
            MessageBoxDefaultButton defaultButton = MessageBoxDefaultButton.Button1, MessageBoxOptions options = 0,
            string helpFilePath = null)
        {
            // Desktop/service notification options have different ownership semantics.
            // Retain the platform implementation for these specialized, non-app dialogs.
            if (options != 0)
                return helpFilePath == null
                    ? System.Windows.Forms.MessageBox.Show(owner, text, caption, buttons, icon, defaultButton, options)
                    : System.Windows.Forms.MessageBox.Show(owner, text, caption, buttons, icon, defaultButton, options, helpFilePath);

            Control control = owner as Control;
            if (control?.IsDisposed == true) owner = control = null;
            if (owner == null)
            {
                control = Control.FromHandle(GetActiveWindow());
                if (control == null && defaultOwner?.TryGetTarget(out var main) == true && !main.IsDisposed)
                    control = main;
                owner = control;
            }
            if (control != null && control.IsHandleCreated && control.InvokeRequired)
                return (DialogResult)control.Invoke(new Func<DialogResult>(() =>
                    Show(control, text, caption, buttons, icon, defaultButton, options, helpFilePath)));
            if (control != null) owner = control.FindForm() ?? control;

            // Startup/background errors without a UI owner still need an STA thread.
            if (Thread.CurrentThread.GetApartmentState() != ApartmentState.STA)
            {
                DialogResult result = DialogResult.None;
                Exception failure = null;
                IWin32Window actualOwner = owner;
                var thread = new Thread(() =>
                {
                    try { result = Show(actualOwner, text, caption, buttons, icon, defaultButton, options, helpFilePath); }
                    catch (Exception ex) { failure = ex; }
                });
                thread.SetApartmentState(ApartmentState.STA);
                thread.Start();
                thread.Join();
                if (failure != null) ExceptionDispatchInfo.Capture(failure).Throw();
                return result;
            }

            if (!Enum.IsDefined(buttons)) throw new InvalidEnumArgumentException(nameof(buttons), (int)buttons, typeof(MessageBoxButtons));
            if (!Enum.IsDefined(icon)) throw new InvalidEnumArgumentException(nameof(icon), (int)icon, typeof(MessageBoxIcon));
            if (!Enum.IsDefined(defaultButton)) throw new InvalidEnumArgumentException(nameof(defaultButton), (int)defaultButton, typeof(MessageBoxDefaultButton));
            var choices = buttons switch
            {
                MessageBoxButtons.OK => new[] { (TaskDialogButton.OK, DialogResult.OK) },
                MessageBoxButtons.OKCancel => new[] { (TaskDialogButton.OK, DialogResult.OK), (TaskDialogButton.Cancel, DialogResult.Cancel) },
                MessageBoxButtons.YesNo => new[] { (TaskDialogButton.Yes, DialogResult.Yes), (TaskDialogButton.No, DialogResult.No) },
                MessageBoxButtons.YesNoCancel => new[] { (TaskDialogButton.Yes, DialogResult.Yes), (TaskDialogButton.No, DialogResult.No), (TaskDialogButton.Cancel, DialogResult.Cancel) },
                MessageBoxButtons.RetryCancel => new[] { (TaskDialogButton.Retry, DialogResult.Retry), (TaskDialogButton.Cancel, DialogResult.Cancel) },
                MessageBoxButtons.AbortRetryIgnore => new[] { (TaskDialogButton.Abort, DialogResult.Abort), (TaskDialogButton.Retry, DialogResult.Retry), (TaskDialogButton.Ignore, DialogResult.Ignore) },
                MessageBoxButtons.CancelTryContinue => new[] { (TaskDialogButton.Cancel, DialogResult.Cancel), (TaskDialogButton.TryAgain, DialogResult.TryAgain), (TaskDialogButton.Continue, DialogResult.Continue) },
                _ => throw new InvalidEnumArgumentException(nameof(buttons), (int)buttons, typeof(MessageBoxButtons))
            };
            using var questionIcon = icon == MessageBoxIcon.Question ? new TaskDialogIcon(SystemIcons.Question) : null;
            var page = new TaskDialogPage
            {
                Caption = caption ?? "", Text = text ?? "", SizeToContent = true,
                AllowCancel = buttons == MessageBoxButtons.OK,
                Icon = icon switch
                {
                    MessageBoxIcon.Error => TaskDialogIcon.Error,
                    MessageBoxIcon.Warning => TaskDialogIcon.Warning,
                    MessageBoxIcon.Information => TaskDialogIcon.Information,
                    MessageBoxIcon.Question => questionIcon,
                    _ => TaskDialogIcon.None
                }
            };
            foreach (var choice in choices) page.Buttons.Add(choice.Item1);
            int defaultIndex = (int)defaultButton / 256;
            page.DefaultButton = choices[defaultIndex < choices.Length ? defaultIndex : 0].Item1;
            if (!string.IsNullOrEmpty(helpFilePath))
            {
                page.Buttons.Add(TaskDialogButton.Help);
                page.HelpRequest += (_, _) => Help.ShowHelp(owner as Control, helpFilePath);
            }
            // The native MessageBox runs in system-DPI context even in a PMv2 app.
            // TaskDialog keeps per-monitor rendering, including moves while open.
            IntPtr previous = SetThreadDpiAwarenessContext(new IntPtr(-4));
            TaskDialogButton selected;
            try { selected = TaskDialog.ShowDialog(owner, page, TaskDialogStartupLocation.CenterOwner); }
            finally { if (previous != IntPtr.Zero) SetThreadDpiAwarenessContext(previous); }
            foreach (var choice in choices)
                if (ReferenceEquals(selected, choice.Item1)) return choice.Item2;
            return buttons == MessageBoxButtons.OK ? DialogResult.OK : DialogResult.Cancel;
        }

        [DllImport("user32.dll")]
        private static extern IntPtr GetActiveWindow();
        [DllImport("user32.dll")]
        private static extern IntPtr SetThreadDpiAwarenessContext(IntPtr context);
    }
}
