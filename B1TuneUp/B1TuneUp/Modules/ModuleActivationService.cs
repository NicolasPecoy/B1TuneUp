using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using B1TuneUp.Models;

namespace B1TuneUp.Modules
{
    public static class ModuleActivationService
    {
        private const string Prefix = "MODULECFG_";
        private static readonly JsonSerializerOptions SerializerOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        private static readonly List<ModuleConfigurationEntry> Definitions = new List<ModuleConfigurationEntry>
        {
            new ModuleConfigurationEntry { Key = "Toolbox", Name = "Toolbox", Description = "Configuración general y comportamiento global.", SortOrder = 10 },
            new ModuleConfigurationEntry { Key = "RightClick", Name = "Right-Click Configuration", Description = "Menús contextuales y accesos guiados desde formularios SAP.", SortOrder = 20 },
            new ModuleConfigurationEntry { Key = "UiCustomization", Name = "Item Placement / UI", Description = "Customizaciones visuales, layout, visibilidad y componentes agregados.", SortOrder = 30 },
            new ModuleConfigurationEntry { Key = "Validation", Name = "Validation System", Description = "Validaciones por evento, condiciones, macros y bloqueo de operaciones.", SortOrder = 40 },
            new ModuleConfigurationEntry { Key = "MandatoryFields", Name = "Mandatory Fields", Description = "Campos obligatorios dinámicos por formulario e item.", SortOrder = 50 },
            new ModuleConfigurationEntry { Key = "DefaultValues", Name = "Default Values", Description = "Valores por defecto en carga y cambio.", SortOrder = 60 },
            new ModuleConfigurationEntry { Key = "LockFields", Name = "Lock Fields", Description = "Bloqueo dinámico de campos y controles.", SortOrder = 70 },
            new ModuleConfigurationEntry { Key = "QuickCopy", Name = "Quick Copy", Description = "Copiado rápido entre documentos de marketing.", SortOrder = 80 },
            new ModuleConfigurationEntry { Key = "UseFlags", Name = "Use Flags", Description = "Banderas visuales por país en BP y documentos.", SortOrder = 90 },
            new ModuleConfigurationEntry { Key = "ActionPad", Name = "Action Pad", Description = "Botoneras contextuales y automatizaciones inline.", SortOrder = 100 },
            new ModuleConfigurationEntry { Key = "Search", Name = "B1 Search", Description = "Búsquedas configurables y apertura de resultados.", SortOrder = 110 }
        };

        public static IReadOnlyList<ModuleConfigurationEntry> GetAll()
        {
            var result = new List<ModuleConfigurationEntry>();
            foreach (var definition in Definitions.OrderBy(m => m.SortOrder))
            {
                result.Add(GetModule(definition.Key));
            }
            return result;
        }

        public static ModuleConfigurationEntry GetModule(string moduleKey)
        {
            var definition = Definitions.FirstOrDefault(m => string.Equals(m.Key, moduleKey, StringComparison.OrdinalIgnoreCase));
            if (definition == null)
            {
                return null;
            }

            var setting = ToolboxSettingService.GetByCode(BuildSettingCode(definition.Key));
            if (setting == null || string.IsNullOrWhiteSpace(setting.Value))
            {
                return definition.Clone();
            }

            try
            {
                var payload = JsonSerializer.Deserialize<ModuleConfigurationEntry>(setting.Value, SerializerOptions);
                if (payload == null)
                {
                    return definition.Clone();
                }

                payload.Key = definition.Key;
                payload.Name = string.IsNullOrWhiteSpace(payload.Name) ? definition.Name : payload.Name;
                payload.Description = string.IsNullOrWhiteSpace(payload.Description) ? definition.Description : payload.Description;
                payload.SortOrder = definition.SortOrder;
                return payload;
            }
            catch
            {
                return definition.Clone();
            }
        }

        public static void Save(ModuleConfigurationEntry entry)
        {
            if (entry == null || string.IsNullOrWhiteSpace(entry.Key))
            {
                throw new ArgumentNullException(nameof(entry));
            }

            var definition = Definitions.FirstOrDefault(m => string.Equals(m.Key, entry.Key, StringComparison.OrdinalIgnoreCase)) ?? entry;
            var payload = entry.Clone();
            payload.Key = definition.Key;
            payload.Name = string.IsNullOrWhiteSpace(payload.Name) ? definition.Name : payload.Name;
            payload.Description = string.IsNullOrWhiteSpace(payload.Description) ? definition.Description : payload.Description;
            payload.SortOrder = definition.SortOrder;

            ToolboxSettingService.Save(new ToolboxSettingEntry
            {
                Code = BuildSettingCode(payload.Key),
                Category = "Modules",
                Description = payload.Description,
                Value = JsonSerializer.Serialize(payload, SerializerOptions)
            });
        }

        public static bool IsEnabled(string moduleKey, bool defaultValue = true)
        {
            var config = GetModule(moduleKey);
            if (config == null)
            {
                return defaultValue;
            }

            return AuthorizationScopeService.MatchesModule(config);
        }

        public static string BuildSettingCode(string moduleKey)
        {
            return $"{Prefix}{moduleKey?.Trim()}";
        }
    }
}
