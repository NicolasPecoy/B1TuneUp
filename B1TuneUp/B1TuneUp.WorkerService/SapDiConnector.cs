using System;
using System.Reflection;

namespace B1TuneUp.WorkerService
{
    public sealed class SapDiConnector : IDisposable
    {
        private readonly WorkerSettings _settings;
        private object _company;

        public SapDiConnector(WorkerSettings settings)
        {
            _settings = settings;
        }

        public void Connect()
        {
            var type = Type.GetTypeFromProgID("SAPbobsCOM.Company");
            if (type == null) throw new InvalidOperationException("SAP DI API COM registration was not found.");
            _company = Activator.CreateInstance(type);
            Set("Server", _settings.SapServer);
            Set("CompanyDB", _settings.SapCompanyDb);
            Set("UserName", _settings.SapUser);
            Set("Password", _settings.SapPassword);
            Set("DbUserName", _settings.DbUser);
            Set("DbPassword", _settings.DbPassword);
            SetEnum("DbServerType", _settings.DbServerType);
            SetEnum("language", _settings.SapLanguage);
            int result = Convert.ToInt32(_company.GetType().InvokeMember("Connect", BindingFlags.InvokeMethod, null, _company, null));
            if (result != 0)
            {
                string error = GetLastError();
                throw new InvalidOperationException("SAP DI API Connect returned " + result + ". " + error);
            }
        }

        public void Dispose()
        {
            try
            {
                if (_company == null) return;
                _company.GetType().InvokeMember("Disconnect", BindingFlags.InvokeMethod, null, _company, null);
            }
            catch (Exception ex)
            {
                WorkerLogger.Error("SAP DI API disconnect failed.", ex);
            }
        }

        private void Set(string property, string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return;
            try { _company.GetType().InvokeMember(property, BindingFlags.SetProperty, null, _company, new object[] { value }); }
            catch (Exception ex) { WorkerLogger.Error("Unable to set DI API property " + property + ".", ex); }
        }

        private void SetEnum(string property, string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return;
            try
            {
                var prop = _company.GetType().GetProperty(property);
                if (prop == null) return;
                var enumValue = Enum.Parse(prop.PropertyType, value, true);
                prop.SetValue(_company, enumValue, null);
            }
            catch (Exception ex)
            {
                WorkerLogger.Error("Unable to set DI API enum property " + property + ".", ex);
            }
        }

        private string GetLastError()
        {
            try
            {
                object code = 0;
                object message = string.Empty;
                _company.GetType().InvokeMember("GetLastError", BindingFlags.InvokeMethod, null, _company, new[] { code, message });
                return Convert.ToString(message);
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}
