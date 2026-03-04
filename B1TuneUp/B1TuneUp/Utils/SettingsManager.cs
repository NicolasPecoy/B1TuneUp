using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Web.Script.Serialization;
using B1TuneUp.Core;

namespace B1TuneUp.Utils
{
    public static class SettingsManager
    {
        private static readonly string _folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "B1TuneUp");
        private static readonly string _file = Path.Combine(_folder, "settings.json");
        private static Dictionary<string, string> _cache;

        static SettingsManager()
        {
            try
            {
                if (!Directory.Exists(_folder)) Directory.CreateDirectory(_folder);
                if (File.Exists(_file))
                {
                    var json = File.ReadAllText(_file, Encoding.UTF8);
                    var js = new JavaScriptSerializer();
                    _cache = js.Deserialize<Dictionary<string, string>>(json) ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                }
                else
                {
                    _cache = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                }
            }
            catch
            {
                _cache = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            }
        }

        public static string GetSetting(string key, string defaultValue = null)
        {
            if (string.IsNullOrEmpty(key)) return defaultValue;
            if (_cache.TryGetValue(key, out var val)) return val;
            return defaultValue;
        }

        public static void SetSetting(string key, string value)
        {
            if (string.IsNullOrEmpty(key)) return;
            _cache[key] = value ?? string.Empty;
            Persist();
        }

        private static void Persist()
        {
            try
            {
                var js = new JavaScriptSerializer();
                var json = js.Serialize(_cache);
                File.WriteAllText(_file, json, Encoding.UTF8);
            }
            catch { }
        }

        public static void SyncToDatabase()
        {
            try
            {
                // attempt to save settings to BTUN_TBOX (Code/Value)
                if (B1App.Instance != null && B1App.Instance.Company != null)
                {
                    var lang = GetSetting("Language", null);
                    if (lang == null) return;

                    var company = B1App.Instance.Company;
                    var rs = (SAPbobsCOM.Recordset)company.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);
                    try
                    {
                        var isHana = B1App.Instance.IsHana;
                        string checkSql = isHana
                            ? $"SELECT * FROM \"@BTUN_TBOX\" WHERE \"U_Code\" = 'Language'"
                            : "SELECT * FROM [@BTUN_TBOX] WHERE [U_Code] = 'Language'";
                        rs.DoQuery(checkSql);
                        if (rs.RecordCount > 0)
                        {
                            string updateSql = isHana
                                ? $"UPDATE \"@BTUN_TBOX\" SET \"U_Value\" = '{lang}' WHERE \"U_Code\" = 'Language'"
                                : $"UPDATE [@BTUN_TBOX] SET [U_Value] = '{lang}' WHERE [U_Code] = 'Language'";
                            rs.DoQuery(updateSql);
                        }
                        else
                        {
                            string insertSql = isHana
                                ? $"INSERT INTO \"@BTUN_TBOX\" (\"U_Code\",\"U_Value\") VALUES ('Language','{lang}')"
                                : $"INSERT INTO [@BTUN_TBOX] ([U_Code],[U_Value]) VALUES ('Language','{lang}')";
                            rs.DoQuery(insertSql);
                        }
                    }
                    finally { Utils.ComObjectManager.Release(rs); }
                }
            }
            catch { }
        }
    }
}
