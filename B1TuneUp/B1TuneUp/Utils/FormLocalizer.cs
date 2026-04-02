using System;
using System.Windows.Forms;
using System.Collections.Generic;

namespace B1TuneUp.Utils
{
    public static class FormLocalizer
    {
        public static void LocalizeForm(Form form)
        {
            if (form == null) return;
            try
            {
                // Localize form title
                form.Text = LocalizationManager.GetString(form.Text);

                // Localize controls recursively
                LocalizeControls(form.Controls);
            }
            catch
            {
                // best effort
            }
        }

        private static void LocalizeControls(Control.ControlCollection controls)
        {
            foreach (Control c in controls)
            {
                try
                {
                    if (!string.IsNullOrEmpty(c.Text))
                    {
                        c.Text = LocalizationManager.GetString(c.Text);
                    }

                    // ToolTip localization: if control has Tag that indicates tooltip key, skip
                    if (c.HasChildren)
                    {
                        LocalizeControls(c.Controls);
                    }
                }
                catch { }
            }
        }
    }
}
