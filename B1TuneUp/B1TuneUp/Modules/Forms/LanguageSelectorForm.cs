using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using B1TuneUp.Utils;

namespace B1TuneUp.Modules.Forms
{
    public class LanguageSelectorForm : Form
    {
        private ComboBox _cmbLang;
        private Button _btnSave, _btnCancel;

        public LanguageSelectorForm()
        {
            Text = LocalizationManager.GetString("LanguageSelector.Title");
            Width = 400; Height = 140;
            StartPosition = FormStartPosition.CenterParent;

            Label lbl = new Label() { Left = 10, Top = 10, Width = 360, Text = LocalizationManager.GetString("LanguageSelector.Select") };
            Controls.Add(lbl);

            _cmbLang = new ComboBox() { Left = 10, Top = 40, Width = 200 }; 
            Controls.Add(_cmbLang);

            _btnSave = new Button() { Left = 220, Top = 40, Width = 70, Text = LocalizationManager.GetString("Btn.Save") };
            _btnCancel = new Button() { Left = 300, Top = 40, Width = 70, Text = LocalizationManager.GetString("Btn.Cancel") };

            _btnSave.Click += BtnSave_Click;
            _btnCancel.Click += (s, e) => Close();

            // Localize the dialog using FormLocalizer
            try { FormLocalizer.LocalizeForm(this); } catch { }

            Controls.Add(_btnSave); Controls.Add(_btnCancel);

            LoadLanguages();
        }

        private void LoadLanguages()
        {
            try
            {
                var basePath = AppDomain.CurrentDomain.BaseDirectory;
                var langFolder = Path.Combine(basePath, "Resources", "lang");
                if (!Directory.Exists(langFolder)) return;
                var files = Directory.GetFiles(langFolder, "*.json");
                foreach (var f in files)
                {
                    var code = Path.GetFileNameWithoutExtension(f);
                    _cmbLang.Items.Add(code);
                }
                var current = LocalizationManager.CurrentLanguage;
                if (!string.IsNullOrEmpty(current) && _cmbLang.Items.Contains(current)) _cmbLang.SelectedItem = current;
                else if (_cmbLang.Items.Count > 0) _cmbLang.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error cargando idiomas: " + ex.Message);
            }
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            try
            {
                var sel = _cmbLang.SelectedItem as string;
                if (string.IsNullOrEmpty(sel)) { MessageBox.Show(LocalizationManager.GetString("LanguageSelector.NoSelect")); return; }
                SettingsManager.SetSetting("Language", sel);
                SettingsManager.SyncToDatabase();
                LocalizationManager.Init(sel);
                MessageBox.Show(LocalizationManager.GetString("LanguageSelector.Saved"));
                Logger.Info("Language changed to " + sel);
                Close();
            }
            catch (Exception ex)
            {
                Logger.Error("Error saving language", ex);
                MessageBox.Show("Error saving language: " + ex.Message);
            }
        }
    }
}
