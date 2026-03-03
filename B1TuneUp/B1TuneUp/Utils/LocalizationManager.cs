using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization.Json;
using System.Runtime.Serialization;
using System.Text;

namespace B1TuneUp.Utils
{
    public static class LocalizationManager
    {
        private static Dictionary<string, string> _strings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        public static string CurrentLanguage { get; private set; } = "en";

        public static void Init(string language = null)
        {
            try
            {
                if (!string.IsNullOrEmpty(language)) CurrentLanguage = language;

                var basePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? AppDomain.CurrentDomain.BaseDirectory;
                var langFolder = Path.Combine(basePath, "Resources", "lang");
                var filePath = Path.Combine(langFolder, CurrentLanguage + ".json");
                if (!File.Exists(filePath))
                {
                    // fallback to english
                    filePath = Path.Combine(langFolder, "en.json");
                }

                if (File.Exists(filePath))
                {
                    var json = File.ReadAllText(filePath, Encoding.UTF8);
                    using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(json)))
                    {
                        var ser = new DataContractJsonSerializer(typeof(Dictionary<string, string>));
                        var obj = ser.ReadObject(ms) as Dictionary<string, string>;
                        if (obj != null) _strings = obj;
                    }
                }
            }
            catch
            {
                // If localization fails, keep defaults (empty dictionary)
                _strings = new Dictionary<string, string>();
            }
        }

        public static string GetString(string key)
        {
            if (string.IsNullOrEmpty(key)) return string.Empty;
            if (_strings.TryGetValue(key, out var val)) return val;
            // as last resort return the key itself
            return key;
        }
    }
}
