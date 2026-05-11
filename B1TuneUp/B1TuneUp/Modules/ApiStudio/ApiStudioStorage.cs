using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace B1TuneUp.Modules.ApiStudio
{
    public static class ApiStudioStorage
    {
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions { WriteIndented = true };

        public static string StoragePath
        {
            get
            {
                var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "B1TuneUp");
                Directory.CreateDirectory(dir);
                return Path.Combine(dir, "api-studio-workspace.json");
            }
        }

        public static ApiStudioWorkspace Load()
        {
            try
            {
                if (File.Exists(StoragePath))
                {
                    var json = File.ReadAllText(StoragePath);
                    var workspace = JsonSerializer.Deserialize<ApiStudioWorkspace>(json, JsonOptions);
                    if (workspace != null)
                    {
                        Normalize(workspace);
                        return workspace;
                    }
                }
            }
            catch { }

            var seeded = CreateSeedWorkspace();
            Save(seeded);
            return seeded;
        }

        public static void Save(ApiStudioWorkspace workspace)
        {
            if (workspace == null) return;
            Normalize(workspace);
            File.WriteAllText(StoragePath, JsonSerializer.Serialize(workspace, JsonOptions));
        }

        private static void Normalize(ApiStudioWorkspace workspace)
        {
            if (workspace.Collections == null) workspace.Collections = new ObservableCollection<ApiCollection>();
            if (workspace.Environments == null) workspace.Environments = new ObservableCollection<ApiEnvironment>();
            if (workspace.History == null) workspace.History = new ObservableCollection<ApiHistoryEntry>();
            foreach (var collection in workspace.Collections)
            {
                if (string.IsNullOrWhiteSpace(collection.Id)) collection.Id = Guid.NewGuid().ToString("N");
                if (collection.Requests == null) collection.Requests = new ObservableCollection<ApiRequest>();
                foreach (var request in collection.Requests)
                {
                    if (string.IsNullOrWhiteSpace(request.Id)) request.Id = Guid.NewGuid().ToString("N");
                }
            }
            foreach (var environment in workspace.Environments)
            {
                if (string.IsNullOrWhiteSpace(environment.Id)) environment.Id = Guid.NewGuid().ToString("N");
                if (environment.Variables == null) environment.Variables = new ObservableCollection<ApiVariable>();
            }
            while (workspace.History.Count > 300) workspace.History.RemoveAt(workspace.History.Count - 1);
        }

        private static ApiStudioWorkspace CreateSeedWorkspace()
        {
            var local = new ApiEnvironment
            {
                Name = "Service Layer Local",
                Variables = new ObservableCollection<ApiVariable>
                {
                    new ApiVariable { Key = "baseUrl", Value = "https://localhost:50000/b1s/v1", Enabled = true },
                    new ApiVariable { Key = "companyDb", Value = "SBODEMOUS", Enabled = true },
                    new ApiVariable { Key = "userName", Value = "manager", Enabled = true },
                    new ApiVariable { Key = "password", Value = "manager", Secret = true, Enabled = true },
                    new ApiVariable { Key = "cardCode", Value = "C20000", Enabled = true },
                    new ApiVariable { Key = "itemCode", Value = "A00001", Enabled = true },
                    new ApiVariable { Key = "docEntry", Value = "1", Enabled = true }
                }
            };

            var collection = CreateServiceLayerCollection();

            return new ApiStudioWorkspace
            {
                ActiveEnvironmentId = local.Id,
                Environments = new ObservableCollection<ApiEnvironment> { local },
                Collections = new ObservableCollection<ApiCollection> { collection },
                History = new ObservableCollection<ApiHistoryEntry>()
            };
        }

        public static ApiCollection CreateServiceLayerCollection()
        {
            var collection = new ApiCollection
            {
                Name = "SAP Business One Service Layer",
                Description = "Coleccion base sembrada desde la referencia Service Layer: login, entidades OData, servicios y queries frecuentes."
            };

            foreach (var request in SeedRequests()) collection.Requests.Add(request);
            return collection;
        }

        private static IEnumerable<ApiRequest> SeedRequests()
        {
            return new[]
            {
                Req("Login", "POST", "{{baseUrl}}/Login", "Content-Type=application/json\nAccept=application/json", "{\n  \"CompanyDB\": \"{{companyDb}}\",\n  \"UserName\": \"{{userName}}\",\n  \"Password\": \"{{password}}\"\n}", "Abre sesion y conserva cookies B1SESSION/ROUTEID para las siguientes llamadas."),
                Req("Logout", "POST", "{{baseUrl}}/Logout", "Accept=application/json", "", "Cierra la sesion actual de Service Layer."),
                Req("Metadata", "GET", "{{baseUrl}}/$metadata", "Accept=application/xml", "", "Obtiene el documento EDMX/OData metadata."),
                Req("Company Info", "GET", "{{baseUrl}}/CompanyService_GetCompanyInfo", "Accept=application/json", "", "Operacion listada en CompanyService."),
                Req("BusinessPartners - list", "GET", "{{baseUrl}}/BusinessPartners?$top=20&$select=CardCode,CardName,CardType", "Accept=application/json", "", "Lista socios de negocio."),
                Req("BusinessPartners - by key", "GET", "{{baseUrl}}/BusinessPartners('{{cardCode}}')", "Accept=application/json", "", "Obtiene un socio por CardCode."),
                Req("BusinessPartners - create customer", "POST", "{{baseUrl}}/BusinessPartners", "Content-Type=application/json\nAccept=application/json", "{\n  \"CardCode\": \"C90001\",\n  \"CardName\": \"Cliente creado desde B1TuneUp API Studio\",\n  \"CardType\": \"cCustomer\",\n  \"Currency\": \"##\"\n}", "Crea un Business Partner de ejemplo."),
                Req("BusinessPartners - update name", "PATCH", "{{baseUrl}}/BusinessPartners('{{cardCode}}')", "Content-Type=application/json\nAccept=application/json", "{\n  \"CardName\": \"Cliente actualizado desde API Studio\"\n}", "Actualiza parcialmente un Business Partner."),
                Req("Items - list", "GET", "{{baseUrl}}/Items?$top=20&$select=ItemCode,ItemName,ItemsGroupCode", "Accept=application/json", "", "Lista articulos."),
                Req("Items - by key", "GET", "{{baseUrl}}/Items('{{itemCode}}')", "Accept=application/json", "", "Obtiene un articulo por ItemCode."),
                Req("Items - create", "POST", "{{baseUrl}}/Items", "Content-Type=application/json\nAccept=application/json", "{\n  \"ItemCode\": \"API-STUDIO-001\",\n  \"ItemName\": \"Item creado desde API Studio\",\n  \"ItemType\": \"itItems\"\n}", "Crea un articulo simple."),
                Req("Orders - list", "GET", "{{baseUrl}}/Orders?$top=20&$select=DocEntry,DocNum,CardCode,DocTotal", "Accept=application/json", "", "Lista ordenes de venta."),
                Req("Orders - by DocEntry", "GET", "{{baseUrl}}/Orders({{docEntry}})", "Accept=application/json", "", "Obtiene una orden de venta por DocEntry."),
                Req("Orders - create", "POST", "{{baseUrl}}/Orders", "Content-Type=application/json\nAccept=application/json", "{\n  \"CardCode\": \"{{cardCode}}\",\n  \"DocDueDate\": \"2026-12-31\",\n  \"DocumentLines\": [\n    {\n      \"ItemCode\": \"{{itemCode}}\",\n      \"Quantity\": 1,\n      \"UnitPrice\": 10\n    }\n  ]\n}", "Crea una orden de venta de ejemplo."),
                Req("OrdersService - approval templates", "POST", "{{baseUrl}}/OrdersService_GetApprovalTemplates", "Content-Type=application/json\nAccept=application/json", "{\n  \"Document\": {\n    \"CardCode\": \"{{cardCode}}\"\n  }\n}", "Operacion OrdersService listada en la referencia."),
                Req("Invoices - list", "GET", "{{baseUrl}}/Invoices?$top=20&$select=DocEntry,DocNum,CardCode,DocTotal", "Accept=application/json", "", "Lista facturas de deudores."),
                Req("Invoices - by DocEntry", "GET", "{{baseUrl}}/Invoices({{docEntry}})", "Accept=application/json", "", "Obtiene una factura por DocEntry."),
                Req("CreditNotes - list", "GET", "{{baseUrl}}/CreditNotes?$top=20&$select=DocEntry,DocNum,CardCode,DocTotal", "Accept=application/json", "", "Lista notas de credito."),
                Req("PurchaseOrders - list", "GET", "{{baseUrl}}/PurchaseOrders?$top=20&$select=DocEntry,DocNum,CardCode,DocTotal", "Accept=application/json", "", "Lista pedidos de compra."),
                Req("Attachments2 - list", "GET", "{{baseUrl}}/Attachments2?$top=20", "Accept=application/json", "", "Lista adjuntos."),
                Req("QueryService - PostQuery", "POST", "{{baseUrl}}/QueryService_PostQuery", "Content-Type=application/json\nAccept=application/json", "{\n  \"QueryPath\": \"$crossjoin(Orders,Orders/DocumentLines)\",\n  \"QueryOption\": \"$expand=Orders($select=DocEntry,DocNum),Orders/DocumentLines($select=ItemCode,Quantity)&$filter=Orders/DocEntry eq Orders/DocumentLines/DocEntry and Orders/DocEntry eq {{docEntry}}\"\n}", "Ejecuta consulta OData avanzada con QueryService."),
                Req("SQLQueries - list", "GET", "{{baseUrl}}/SQLQueries?$top=20", "Accept=application/json", "", "Lista SQLQueries si estan habilitadas."),
                Req("SQLQueries - execute", "GET", "{{baseUrl}}/SQLQueries('MyQuery')/List", "Accept=application/json", "", "Ejecuta una SQLQuery publicada."),
                Req("Users - list", "GET", "{{baseUrl}}/Users?$top=20&$select=UserCode,UserName", "Accept=application/json", "", "Lista usuarios."),
                Req("Warehouses - list", "GET", "{{baseUrl}}/Warehouses?$top=20&$select=WarehouseCode,WarehouseName", "Accept=application/json", "", "Lista almacenes."),
                Req("BusinessPlaces - list", "GET", "{{baseUrl}}/BusinessPlaces?$top=20", "Accept=application/json", "", "Lista sucursales/business places."),
                Req("ActivitiesService - top instances", "POST", "{{baseUrl}}/ActivitiesService_GetTopNActivityInstances", "Content-Type=application/json\nAccept=application/json", "{\n  \"Number\": 10\n}", "Operacion ActivitiesService listada en la referencia."),
                Req("PickListsService - close", "POST", "{{baseUrl}}/PickListsService_Close", "Content-Type=application/json\nAccept=application/json", "{\n  \"Absoluteentry\": {{docEntry}}\n}", "Cierra una pick list por entrada absoluta, validar en ambiente de prueba.")
            };
        }

        private static ApiRequest Req(string name, string method, string url, string headers, string body, string description)
        {
            return new ApiRequest
            {
                Name = name,
                Method = method,
                Url = url,
                Headers = headers,
                Body = body,
                Description = description,
                ContentType = headers != null && headers.IndexOf("xml", StringComparison.OrdinalIgnoreCase) >= 0 ? "application/xml" : "application/json"
            };
        }
    }
}
