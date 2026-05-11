using System;
using System.Collections.Generic;
using System.Text.Json;
using B1TuneUp.Models;

namespace B1TuneUp.Modules
{
    public static class UniversalFunctionDesignerService
    {
        public static UniversalFunctionEntry ApplyBuilder(UniversalFunctionEntry entry, UniversalFunctionDesignerState state, string type)
        {
            if (entry == null) throw new ArgumentNullException(nameof(entry));
            if (state == null) state = new UniversalFunctionDesignerState();
            entry.Type = type ?? entry.Type ?? "Macro";
            entry.Condition = state.Condition;
            entry.ResultVariable = state.ResultVariable;
            entry.OnSuccessFunction = state.OnSuccess;
            entry.OnErrorFunction = state.OnError;

            if (entry.Type.Equals("HTTP", StringComparison.OrdinalIgnoreCase))
            {
                entry.Payload = state.HttpUrl;
                entry.Parameters = JsonSerializer.Serialize(new Dictionary<string, string>
                {
                    ["method"] = state.HttpMethod ?? "GET",
                    ["headers"] = state.HttpHeaders ?? string.Empty,
                    ["authMode"] = state.HttpAuthMode ?? "None",
                    ["authUser"] = state.HttpAuthUser ?? string.Empty,
                    ["authSecret"] = state.HttpAuthSecret ?? string.Empty,
                    ["body"] = state.HttpBody ?? string.Empty,
                    ["timeoutSeconds"] = state.HttpTimeoutSeconds ?? "60"
                });
            }
            else if (entry.Type.Equals("DIObject", StringComparison.OrdinalIgnoreCase))
            {
                entry.Payload = JsonSerializer.Serialize(new
                {
                    objectType = state.DiObjectType,
                    mode = state.DiMode,
                    key = state.DiKey,
                    fields = ReadJson(state.DiFieldsJson),
                    userFields = ReadJson(state.DiUserFieldsJson)
                });
                entry.Parameters = string.Empty;
            }
            else if (entry.Type.Equals("Email", StringComparison.OrdinalIgnoreCase))
            {
                entry.Payload = state.EmailTemplateCode;
                entry.Parameters = string.Empty;
            }
            else if (entry.Type.Equals("CrystalReport", StringComparison.OrdinalIgnoreCase))
            {
                entry.Payload = state.ReportCode;
                entry.Parameters = state.ReportParameters;
            }
            else if (entry.Type.Equals("File", StringComparison.OrdinalIgnoreCase))
            {
                entry.Payload = state.FilePath;
                entry.Parameters = JsonSerializer.Serialize(new Dictionary<string, string>
                {
                    ["action"] = state.FileAction ?? "Open",
                    ["target"] = state.FileTarget ?? string.Empty,
                    ["content"] = state.FileContent ?? string.Empty
                });
            }
            else if (entry.Type.Equals("LineLoop", StringComparison.OrdinalIgnoreCase))
            {
                entry.Parameters = state.MatrixId;
                entry.Payload = state.LoopMacro;
            }

            return entry;
        }

        private static object ReadJson(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return new Dictionary<string, object>();
            try { return JsonSerializer.Deserialize<Dictionary<string, object>>(raw); }
            catch { return new Dictionary<string, object>(); }
        }
    }
}
