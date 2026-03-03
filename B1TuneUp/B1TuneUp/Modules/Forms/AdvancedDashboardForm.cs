using System;
using System.Windows.Forms;
using SAPbouiCOM;
using B1TuneUp.Core;

namespace B1TuneUp.Modules.Forms
{
    public class AdvancedDashboardForm : System.Windows.Forms.Form
    {
        public AdvancedDashboardForm(string sqlQuery)
        {
            Text = "Advanced Dashboard";
            Width = 1000;
            Height = 700;

            var lbl = new System.Windows.Forms.Label();
            lbl.Text = "Advanced dashboard with charts is available in full environment.";
            lbl.Left = 10;
            lbl.Top = 10;
            lbl.Width = 900;
            this.Controls.Add(lbl);
        }
    }
}
