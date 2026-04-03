using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace B1TuneUp.Models
{
    public class ToolboxSettingEntry : INotifyPropertyChanged
    {
        private string _code;
        private string _value;
        private string _category;
        private string _description;

        public string Code
        {
            get => _code;
            set
            {
                if (_code == value) return;
                _code = value;
                OnPropertyChanged();
            }
        }

        public string Value
        {
            get => _value;
            set
            {
                if (_value == value) return;
                _value = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(BoolValue));
            }
        }

        public string Category
        {
            get => _category;
            set
            {
                if (_category == value) return;
                _category = value;
                OnPropertyChanged();
            }
        }

        public string Description
        {
            get => _description;
            set
            {
                if (_description == value) return;
                _description = value;
                OnPropertyChanged();
            }
        }

        public bool IsBoolean => ToolboxSettingMetadata.IsBoolean(Code);

        public bool? BoolValue
        {
            get
            {
                var normalized = (Value ?? string.Empty).Trim().ToUpperInvariant();
                if (normalized == "Y" || normalized == "YES" || normalized == "TRUE" || normalized == "1")
                {
                    return true;
                }
                if (normalized == "N" || normalized == "NO" || normalized == "FALSE" || normalized == "0")
                {
                    return false;
                }
                return null;
            }
            set
            {
                if (!IsBoolean) return;
                Value = value == true ? "Y" : "N";
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public static class ToolboxSettingMetadata
    {
        public static string DetermineCategory(string code)
        {
            if (string.IsNullOrWhiteSpace(code)) return "General";
            if (code.StartsWith("SMTP_", StringComparison.OrdinalIgnoreCase)) return "Email / SMTP";
            if (code.StartsWith("EXCH_", StringComparison.OrdinalIgnoreCase)) return "Exchange Rates";
            if (code.StartsWith("SYS_", StringComparison.OrdinalIgnoreCase)) return "Sistema";
            if (code.StartsWith("SCHED_", StringComparison.OrdinalIgnoreCase)) return "Scheduler";
            if (code.StartsWith("INTEGRATION_", StringComparison.OrdinalIgnoreCase) || code.StartsWith("INT_", StringComparison.OrdinalIgnoreCase)) return "Integraciones";
            if (code.StartsWith("PERIOD", StringComparison.OrdinalIgnoreCase) || code.StartsWith("GENERAL", StringComparison.OrdinalIgnoreCase)) return "General";
            if (code.StartsWith("NOTIF", StringComparison.OrdinalIgnoreCase)) return "Notificaciones";
            return "Personalizado";
        }

        public static bool IsBoolean(string code)
        {
            if (string.IsNullOrWhiteSpace(code)) return false;
            return code.StartsWith("SYS_", StringComparison.OrdinalIgnoreCase)
                   || code.EndsWith("_ENABLED", StringComparison.OrdinalIgnoreCase)
                   || code.IndexOf("LOCK", StringComparison.OrdinalIgnoreCase) >= 0
                   || string.Equals(code, "PERIOD_LOCK", StringComparison.OrdinalIgnoreCase)
                   || string.Equals(code, "GENERAL_VALIDATION", StringComparison.OrdinalIgnoreCase);
        }

        public static string Describe(string code)
        {
            if (string.IsNullOrWhiteSpace(code)) return string.Empty;
            switch (code)
            {
                case "PERIOD_LOCK":
                    return "Bloquea periodos contables (Y/N).";
                case "GENERAL_VALIDATION":
                    return "Activa validaciones generales (Y/N).";
                case "SMTP_Server":
                case "SMTP_SERVER":
                    return "Servidor SMTP para notificaciones.";
                case "SMTP_Port":
                case "SMTP_PORT":
                    return "Puerto SMTP (587, 465, etc.).";
                case "SMTP_EnableSSL":
                case "SMTP_ENABLESSL":
                    return "Usar SSL para SMTP (true/false).";
                case "EXCH_SOURCE":
                    return "Fuente de datos de tipo de cambio (Manual, ECB, Fixer).";
                case "SYS_THEME":
                    return "Tema aplicado a B1TuneUp (Light/Dark).";
                default:
                    return string.Empty;
            }
        }
    }
}
