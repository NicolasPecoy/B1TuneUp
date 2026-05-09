using System.Collections.Generic;
using System.Linq;
using B1TuneUp.Models;

namespace B1TuneUp.Modules
{
    public static class FunctionalTemplateService
    {
        public static IList<FunctionalTemplateEntry> GetSamples()
        {
            return new List<FunctionalTemplateEntry>
            {
                new FunctionalTemplateEntry { Code = "VAL_BP_TAXID", Module = "Validation", Category = "Compliance", Tags = "BP,Tax", Name = "BP Tax ID validation", Description = "Bloquea Business Partners sin tax id.", Payload = "SELECT CASE WHEN $[$5.0.0] = '' THEN 'N' ELSE 'Y' END" },
                new FunctionalTemplateEntry { Code = "PD_INVOICE_EMAIL", Module = "PrintDelivery", Category = "AR", Tags = "Invoice,Email,PDF", Name = "Invoice email delivery", Description = "Plantilla base para envio de factura por email/PDF.", Payload = "EMAIL" },
                new FunctionalTemplateEntry { Code = "SEARCH_ITEMS_ADV", Module = "Search", Category = "Inventory", Tags = "Items,Warehouse", Name = "Advanced item search", Description = "Busqueda por codigo, descripcion y almacen.", Payload = "SELECT ItemCode, ItemName FROM OITM WHERE ItemCode LIKE '%search%' OR ItemName LIKE '%search%'" },
                new FunctionalTemplateEntry { Code = "ITEM_HIDE_MARGIN", Module = "ItemPlacement", Category = "Sales", Tags = "UI,Margin", Name = "Hide margin fields", Description = "Ejemplo de layout por rol para ocultar campos sensibles.", Payload = "Hide" },
                new FunctionalTemplateEntry { Code = "DASH_OPEN_ORDERS", Module = "Dashboard", Category = "KPI", Tags = "Sales,KPI", Name = "Open sales orders KPI", Description = "KPI de pedidos abiertos.", Payload = "SELECT COUNT(*) FROM ORDR WHERE DocStatus = 'O'" },
                new FunctionalTemplateEntry { Code = "CRYSTAL_SCHEDULE", Module = "Reports", Category = "Scheduling", Tags = "Crystal,Server", Name = "Scheduled Crystal report", Description = "Base para programar reporte y entregar resultado.", Payload = "CrystalReport" }
            };
        }

        public static void InstallSample(FunctionalTemplateEntry sample)
        {
            if (sample == null) return;
            if (sample.Module == "Validation")
            {
                UniversalFunctionService.Save(new UniversalFunctionEntry
                {
                    Code = sample.Code,
                    Name = sample.Name,
                    Type = "SQL",
                    Category = sample.Category,
                    Tags = sample.Tags,
                    Payload = sample.Payload,
                    Notes = sample.Description
                });
            }
            else
            {
                UniversalFunctionService.Save(new UniversalFunctionEntry
                {
                    Code = sample.Code,
                    Name = sample.Name,
                    Type = sample.Payload == "CrystalReport" ? "CrystalReport" : "Macro",
                    Category = sample.Category,
                    Tags = sample.Tags,
                    Payload = sample.Payload,
                    Notes = sample.Description
                });
            }
            AuditLogManager.LogAction("FunctionalTemplate", $"Sample {sample.Code} instalado.", "Install");
        }

        public static FunctionalTemplateEntry GetByCode(string code)
        {
            return GetSamples().FirstOrDefault(s => s.Code == code);
        }
    }
}
