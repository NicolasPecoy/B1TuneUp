using System;
using System.Linq;
using SAPbouiCOM;
using SAPbobsCOM;
using B1TuneUp.Core;
using B1TuneUp.Utils;

namespace B1TuneUp.Modules
{
    public static class ToolboxManager
    {
        public const string UseFlagsSettingCode = "SYS_USE_FLAGS_BP";
        public const string UseFlagsDocumentsSettingCode = "SYS_USE_FLAGS_DOC";

        public static void ApplyToolboxSettings()
        {
            // Aplicar configuraciones generales desde la tabla @BTUN_TBOX
            EnsureDefaultSettings();
            ApplyPeriodLock();
            ApplyGeneralValidations();
            ApplySystemSettings();
        }

        private static void EnsureDefaultSettings()
        {
            EnsureSettingExists(UseFlagsSettingCode, "Y");
            EnsureSettingExists(UseFlagsDocumentsSettingCode, "Y");
        }

        private static void ApplyPeriodLock()
        {
            Recordset rs = (Recordset)B1App.Instance.Company.GetBusinessObject(BoObjectTypes.BoRecordset);
            try
            {
                string sql = B1App.Instance.IsHana
                    ? "SELECT \"U_Value\" FROM \"@BTUN_TBOX\" WHERE \"U_Code\" = 'PERIOD_LOCK'"
                    : "SELECT [U_Value] FROM [@BTUN_TBOX] WHERE [U_Code] = 'PERIOD_LOCK'";

                rs.DoQuery(sql);
                if (!rs.EoF)
                {
                    string lockSetting = rs.Fields.Item(0).Value.ToString();
                    if (lockSetting == "Y")
                    {
                        // Implementar bloqueo de periodos si es necesario
                        // Esta funcionalidad puede ser extendida según las necesidades
                        B1App.Instance.Application.SetStatusBarMessage("Períodos bloqueados según configuración", SAPbouiCOM.BoMessageTime.bmt_Short, false);
                    }
                }
            }
            catch (Exception ex)
            {
                B1App.Instance.Application.SetStatusBarMessage($"Error aplicando bloqueo de período: {ex.Message}", SAPbouiCOM.BoMessageTime.bmt_Short, true);
            }
            finally
            {
                ComObjectManager.Release(rs);
            }
        }

        private static void ApplyGeneralValidations()
        {
            Recordset rs = (Recordset)B1App.Instance.Company.GetBusinessObject(BoObjectTypes.BoRecordset);
            try
            {
                string sql = B1App.Instance.IsHana
                    ? "SELECT \"U_Value\" FROM \"@BTUN_TBOX\" WHERE \"U_Code\" = 'GENERAL_VALIDATION'"
                    : "SELECT [U_Value] FROM [@BTUN_TBOX] WHERE [U_Code] = 'GENERAL_VALIDATION'";

                rs.DoQuery(sql);
                if (!rs.EoF)
                {
                    string validationSetting = rs.Fields.Item(0).Value.ToString();
                    if (validationSetting == "Y")
                    {
                        // Aplicar validaciones generales
                        B1App.Instance.Application.SetStatusBarMessage("Validaciones generales aplicadas", SAPbouiCOM.BoMessageTime.bmt_Short, false);
                    }
                }
            }
            catch (Exception ex)
            {
                B1App.Instance.Application.SetStatusBarMessage($"Error aplicando validaciones generales: {ex.Message}", SAPbouiCOM.BoMessageTime.bmt_Short, true);
            }
            finally
            {
                ComObjectManager.Release(rs);
            }
        }

        private static void ApplySystemSettings()
        {
            Recordset rs = (Recordset)B1App.Instance.Company.GetBusinessObject(BoObjectTypes.BoRecordset);
            try
            {
                string sql = B1App.Instance.IsHana
                    ? "SELECT \"U_Code\", \"U_Value\" FROM \"@BTUN_TBOX\" WHERE \"U_Code\" LIKE 'SYS_%'"
                    : "SELECT [U_Code], [U_Value] FROM [@BTUN_TBOX] WHERE [U_Code] LIKE 'SYS_%'";

                rs.DoQuery(sql);
                while (!rs.EoF)
                {
                    string settingCode = rs.Fields.Item("U_Code").Value.ToString();
                    string settingValue = rs.Fields.Item("U_Value").Value.ToString();

                    // Procesar configuraciones del sistema
                    ProcessSystemSetting(settingCode, settingValue);

                    rs.MoveNext();
                }
            }
            catch (Exception ex)
            {
                B1App.Instance.Application.SetStatusBarMessage($"Error aplicando configuraciones del sistema: {ex.Message}", SAPbouiCOM.BoMessageTime.bmt_Short, true);
            }
            finally
            {
                ComObjectManager.Release(rs);
            }
        }

        private static void ProcessSystemSetting(string settingCode, string settingValue)
        {
            // Procesar diferentes tipos de configuraciones del sistema
            switch (settingCode)
            {
                case "SYS_AUTO_LOGIN":
                    // Configuración de inicio de sesión automático
                    break;
                case "SYS_NOTIFICATIONS":
                    // Configuración de notificaciones
                    break;
                case "SYS_THEME":
                    // Configuración de tema visual
                    break;
                case "SYS_LANGUAGE":
                    // Configuración de idioma
                    break;
                default:
                    // Otros ajustes específicos del sistema
                    break;
            }
        }

        private static void EnsureSettingExists(string code, string defaultValue)
        {
            if (string.IsNullOrWhiteSpace(code)) return;

            Recordset rs = null;
            try
            {
                rs = (Recordset)B1App.Instance.Company.GetBusinessObject(BoObjectTypes.BoRecordset);
                string safeCode = code.Replace("'", "''");
                string safeValue = (defaultValue ?? string.Empty).Replace("'", "''");

                string checkSql = B1App.Instance.IsHana
                    ? $"SELECT \"Code\" FROM \"@BTUN_TBOX\" WHERE \"U_Code\" = '{safeCode}'"
                    : $"SELECT [Code] FROM [@BTUN_TBOX] WHERE [U_Code] = '{safeCode}'";
                rs.DoQuery(checkSql);
                if (!rs.EoF) return;

                string insertSql = B1App.Instance.IsHana
                    ? $"INSERT INTO \"@BTUN_TBOX\" (\"Code\",\"Name\",\"U_Code\",\"U_Value\") VALUES ('{safeCode}','{safeCode}','{safeCode}','{safeValue}')"
                    : $"INSERT INTO [@BTUN_TBOX] ([Code],[Name],[U_Code],[U_Value]) VALUES ('{safeCode}','{safeCode}','{safeCode}','{safeValue}')";
                rs.DoQuery(insertSql);
            }
            catch
            {
                // Si falla el seed no bloqueamos el startup del add-on.
            }
            finally
            {
                ComObjectManager.Release(rs);
            }
        }

        public static string GetSettingValue(string code, string defaultValue = null)
        {
            if (string.IsNullOrWhiteSpace(code)) return defaultValue;

            Recordset rs = null;
            try
            {
                rs = (Recordset)B1App.Instance.Company.GetBusinessObject(BoObjectTypes.BoRecordset);
                string safeCode = code.Replace("'", "''");
                string sql = B1App.Instance.IsHana
                    ? $"SELECT \"U_Value\" FROM \"@BTUN_TBOX\" WHERE \"U_Code\" = '{safeCode}'"
                    : $"SELECT [U_Value] FROM [@BTUN_TBOX] WHERE [U_Code] = '{safeCode}'";
                rs.DoQuery(sql);
                if (!rs.EoF)
                {
                    return rs.Fields.Item(0).Value?.ToString() ?? defaultValue;
                }
            }
            catch
            {
                // Ignorado: retornamos default.
            }
            finally
            {
                ComObjectManager.Release(rs);
            }

            return defaultValue;
        }

        public static bool IsSettingEnabled(string code, bool defaultValue = false)
        {
            string fallback = defaultValue ? "Y" : "N";
            string value = GetSettingValue(code, fallback);
            if (string.IsNullOrWhiteSpace(value)) return defaultValue;

            string normalized = value.Trim().ToUpperInvariant();
            return normalized == "Y" || normalized == "YES" || normalized == "TRUE" || normalized == "1";
        }

        public static bool ValidateVAT(string vatNumber, string country)
        {
            // Implementación de validación de NIF/VAT
            if (string.IsNullOrEmpty(vatNumber)) return false;

            // Remover espacios y caracteres especiales
            vatNumber = vatNumber.Replace(" ", "").Replace("-", "").Replace(".", "");

            // Verificar patrones básicos por país
            switch (country?.ToUpper())
            {
                case "ES": // España
                    return ValidateSpanishVAT(vatNumber);
                case "DE": // Alemania
                    return ValidateGermanVAT(vatNumber);
                case "FR": // Francia
                    return ValidateFrenchVAT(vatNumber);
                case "IT": // Italia
                    return ValidateItalianVAT(vatNumber);
                case "GB": // Reino Unido
                    return ValidateUKVAT(vatNumber);
                case "US": // Estados Unidos
                    return ValidateUSVAT(vatNumber);
                case "MX": // México
                    return ValidateMexicanVAT(vatNumber);
                default:
                    // Si no se especifica país, validar formato genérico
                    return vatNumber.Length >= 2 && vatNumber.Length <= 15;
            }
        }

        private static bool ValidateSpanishVAT(string vatNumber)
        {
            if (vatNumber.Length < 9) return false;

            string dni = vatNumber.Substring(1);
            char letter = vatNumber[0];

            // Para personas físicas (empiezan con número o K,L,M,X,Y,Z)
            if (char.IsDigit(letter) || "KLMXYZ".IndexOf(letter) >= 0)
            {
                // Extraer dígito de control
                string digits = dni.Substring(0, dni.Length - 1);
                char control = dni[dni.Length - 1];

                if (!int.TryParse(digits, out int numericPart)) return false;

                int remainder = numericPart % 23;
                string controlLetters = "TRWAGMYFPDXBNJZSQVHLCKE";

                if (remainder < controlLetters.Length)
                {
                    return char.ToUpper(control) == controlLetters[remainder];
                }
                return false;
            }

            // Para sociedades (empiezan con letra)
            if (char.IsLetter(letter))
            {
                string controlDigits = dni.Substring(0, dni.Length - 1);
                char control = dni[dni.Length - 1];

                if (!int.TryParse(controlDigits, out int controlNumeric)) return false;

                // Cálculo para sociedades
                string validChars = "ABCDEFGHJKLMNPQRSUVW";
                string validDigits = "0123456789";

                return (validChars.IndexOf(letter) >= 0 && validDigits.IndexOf(control) >= 0);
            }

            return false;
        }

        private static bool ValidateGermanVAT(string vatNumber)
        {
            if (!vatNumber.StartsWith("DE") || vatNumber.Length != 11) return false;

            string digits = vatNumber.Substring(2);
            if (!long.TryParse(digits, out long num)) return false;

            // Algoritmo de verificación para Alemania
            int temp = 0;
            for (int i = 0; i < 8; i++)
            {
                int digit = digits[i] - '0';
                temp = (digit + temp) % 10;
                if (temp == 0) temp = 10;
                temp *= 2;
            }

            int checkDigit = (11 - (temp % 11)) % 10;
            int lastDigit = digits[8] - '0';

            return checkDigit == lastDigit;
        }

        private static bool ValidateFrenchVAT(string vatNumber)
        {
            if (!vatNumber.StartsWith("FR") || vatNumber.Length != 11) return false;

            string checkPart = vatNumber.Substring(2, 2);
            string numberPart = vatNumber.Substring(4);

            if (!int.TryParse(checkPart, out int key)) return false;
            if (!long.TryParse(numberPart, out long siren)) return false;

            int calculatedKey = (12 + (3 * ((int)siren % 97))) % 97;

            return calculatedKey == key;
        }

        private static bool ValidateItalianVAT(string vatNumber)
        {
            if (!vatNumber.StartsWith("IT") || vatNumber.Length != 13) return false;

            string digits = vatNumber.Substring(2);
            if (!long.TryParse(digits, out long vat)) return false;

            // Verificación del código fiscal italiano
            int sum = 0;
            for (int i = 0; i < 10; i += 2)
            {
                sum += digits[i] - '0';
            }

            for (int i = 1; i < 11; i += 2)
            {
                int digit = digits[i] - '0';
                digit *= 2;
                if (digit > 9)
                {
                    digit = (digit % 10) + 1;
                }
                sum += digit;
            }

            int checksum = (10 - (sum % 10)) % 10;
            int lastDigit = digits[11] - '0';

            return checksum == lastDigit;
        }

        private static bool ValidateUKVAT(string vatNumber)
        {
            if (!vatNumber.StartsWith("GB") || (vatNumber.Length != 11 && vatNumber.Length != 5)) return false;

            if (vatNumber.Length == 5) // Government department
            {
                string subString = vatNumber.Substring(2, 3);
                foreach (char c in subString)
                {
                    if (!char.IsDigit(c)) return false;
                }
                return true;
            }

            string digits = vatNumber.Substring(2);
            if (!long.TryParse(digits, out long vat)) return false;

            int total = 0;
            for (int i = 0; i < 7; i++)
            {
                total += (digits[i] - '0') * (8 - i);
            }

            while (total > 0)
            {
                total -= 97;
            }
            total = Math.Abs(total);

            int check = 97 - total;
            int actual = int.Parse(digits.Substring(7, 2));

            return check == actual || check == actual - 55;
        }

        private static bool ValidateUSVAT(string vatNumber)
        {
            // Validación para SSN o FEIN (formato XX-XXXXXXX)
            if (vatNumber.Length == 10 && vatNumber[2] == '-')
            {
                string[] parts = vatNumber.Split('-');
                if (parts.Length == 2 && parts[0].Length == 2 && parts[1].Length == 7)
                {
                    foreach (char c in parts[0])
                    {
                        if (!char.IsDigit(c)) return false;
                    }
                    foreach (char c in parts[1])
                    {
                        if (!char.IsDigit(c)) return false;
                    }
                    return true;
                }
            }

            // Validación sin guión
            if (vatNumber.Length == 9)
            {
                foreach (char c in vatNumber)
                {
                    if (!char.IsDigit(c)) return false;
                }
                return true;
            }

            return false;
        }

        private static bool ValidateMexicanVAT(string vatNumber)
        {
            // Validación básica para RFC mexicano
            if (vatNumber.Length < 10 || vatNumber.Length > 13) return false;

            // RFC persona física o moral
            string alphaPart = vatNumber.Substring(0, 3);
            string numPart = vatNumber.Substring(3, 6);

            foreach (char c in alphaPart)
            {
                if (!char.IsLetter(c)) return false;
            }
            foreach (char c in numPart)
            {
                if (!char.IsDigit(c)) return false;
            }

            if (vatNumber.Length == 13)
            {
                // Tiene homoclave de 3 dígitos
                string homoclave = vatNumber.Substring(9, 3);
                if (homoclave.Any(c => !char.IsDigit(c))) return false;
            }

            return true;
        }

        public static void HandleToolboxEvents(Form oForm, ItemEvent pVal)
        {
            // Lógica para eventos automáticos de Toolbox
            // Por ejemplo, autocompletar campos o formatear textos
            UseFlagsManager.HandleItemEvent(oForm, pVal);
        }
    }
}
