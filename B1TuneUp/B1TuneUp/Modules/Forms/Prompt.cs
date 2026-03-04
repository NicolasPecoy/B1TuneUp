using B1TuneUp.Utils;
using System;
using System.Windows.Forms;

namespace B1TuneUp.Modules.Forms
{
    public static class Prompt
    {
        public static string ShowDialog(string text, string caption, string defaultValue = "")
        {
            Form prompt = new Form()
            {
                Width = 500,
                Height = 150,
                Text = LocalizationManager.GetString(caption),
                StartPosition = FormStartPosition.CenterParent
            };
            Label textLabel = new Label() { Left = 10, Top = 10, Text = LocalizationManager.GetString(text), Width = 460 };
            TextBox textBox = new TextBox() { Left = 10, Top = 30, Width = 460, Text = defaultValue };
            Button confirmation = new Button() { Text = LocalizationManager.GetString("Btn.Ok"), Left = 300, Width = 80, Top = 60, DialogResult = DialogResult.OK };
            Button cancel = new Button() { Text = LocalizationManager.GetString("Btn.Cancel"), Left = 390, Width = 80, Top = 60, DialogResult = DialogResult.Cancel };
            confirmation.Click += (sender, e) => { prompt.Close(); };
            prompt.Controls.Add(textBox);
            prompt.Controls.Add(confirmation);
            prompt.Controls.Add(cancel);
            prompt.Controls.Add(textLabel);
            prompt.AcceptButton = confirmation;
            var res = prompt.ShowDialog();
            if (res == DialogResult.OK) return textBox.Text; else return null;
        }
    }
}
