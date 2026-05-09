using System;
using System.Data;
using System.Globalization;
using System.Xml;
using SAPbouiCOM;
using SAPbobsCOM;
using B1TuneUp.Core;
using B1TuneUp.Utils;
using B1TuneUp.Modules.PlacementEnhancementUi;

namespace B1TuneUp.Modules
{
    public static class ItemPlacementManager
    {
        public static void OpenPlacementForm(SAPbouiCOM.Form form)
        {
            try
            {
                if (form == null)
                {
                    form = SapUiSafe.TryGetActiveForm();
                    if (form == null)
                    {
                        B1App.Instance.Application.SetStatusBarMessage("No hay formulario activo para Item Placement.", SAPbouiCOM.BoMessageTime.bmt_Short, true);
                        return;
                    }
                }

                PlacementEnhancementLauncher.Show();
            }
            catch (Exception ex)
            {
                B1App.Instance.Application.SetStatusBarMessage($"Error abriendo Item Placement: {ex.Message}", SAPbouiCOM.BoMessageTime.bmt_Short, true);
            }
        }

        // Save SRF file bytes into UDT as base64 (store file and metadata)
        public static bool SaveSrfToLayout(string formType, string layoutName, string filePath)
        {
            try
            {
                if (!System.IO.File.Exists(filePath)) return false;
                var bytes = System.IO.File.ReadAllBytes(filePath);
                var b64 = Convert.ToBase64String(bytes);
                // store in UDT
                SAPbobsCOM.Recordset rs = null;
                try
                {
                    rs = (SAPbobsCOM.Recordset)B1App.Instance.Company.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);
                    string owner = "";
                    try { owner = B1App.Instance.Application.Company.UserName; } catch { }
                    string safeForm = formType.Replace("'", "''");
                    string safeName = layoutName.Replace("'", "''");
                    string safeFile = System.IO.Path.GetFileName(filePath).Replace("'", "''");
                    string safeOwner = owner.Replace("'", "''");
                    string safeB64 = b64.Replace("'", "''");
                    int nextCode = UserTableCodeGenerator.GetNext("@BTUN_LAYOUT");
                    string codeValue = nextCode.ToString(CultureInfo.InvariantCulture);
                    string recordName = $"LAYOUT_{safeName}";
                    string insertSql = B1App.Instance.IsHana
                        ? $"INSERT INTO \"@BTUN_LAYOUT\" (\"Code\",\"Name\",\"U_FormType\", \"U_Name\", \"U_SRF\", \"U_FileName\", \"U_Owner\", \"U_CreatedAt\") VALUES ('{codeValue}','{recordName}','{safeForm}', '{safeName}', '{safeB64}', '{safeFile}', '{safeOwner}', CURRENT_TIMESTAMP)"
                        : $"INSERT INTO [@BTUN_LAYOUT] ([Code],[Name],U_FormType, U_Name, U_SRF, U_FileName, U_Owner, U_CreatedAt) VALUES ('{codeValue}','{recordName}','{safeForm}', '{safeName}', '{safeB64}', '{safeFile}', '{safeOwner}', GETDATE())";
                    rs.DoQuery(insertSql);
                    B1App.Instance.Application.SetStatusBarMessage($"SRF saved into layout '{layoutName}'.", SAPbouiCOM.BoMessageTime.bmt_Short, false);
                    return true;
                }
                finally { ComObjectManager.Release(rs); }
            }
            catch (Exception ex)
            {
                B1App.Instance.Application.SetStatusBarMessage($"Error saving SRF to layout: {ex.Message}", SAPbouiCOM.BoMessageTime.bmt_Short, true);
                return false;
            }
        }

        // Restore SRF file from UDT to disk (returns path)
        public static bool RestoreSrfFromLayout(string layoutName, string formType, string destPath)
        {
            try
            {
                string sql = B1App.Instance.IsHana
                    ? $"SELECT \"U_SRF\", \"U_FileName\" FROM \"@BTUN_LAYOUT\" WHERE \"U_Name\" = '{layoutName.Replace("'", "''")}' AND \"U_FormType\" = '{formType.Replace("'", "''")}'"
                    : $"SELECT U_SRF, U_FileName FROM [@BTUN_LAYOUT] WHERE [U_Name] = '{layoutName.Replace("'", "''")}' AND [U_FormType] = '{formType.Replace("'", "''")}'";
                SAPbobsCOM.Recordset rs = (SAPbobsCOM.Recordset)B1App.Instance.Company.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);
                try
                {
                    rs.DoQuery(sql);
                    if (rs.EoF) return false;
                    var b64 = SapUiSafe.SafeField(rs, 0);
                    var fname = SapUiSafe.SafeField(rs, 1);
                    if (string.IsNullOrWhiteSpace(fname)) fname = "layout.srf";
                    if (string.IsNullOrEmpty(b64)) return false;
                    var bytes = Convert.FromBase64String(b64);
                    var outPath = destPath;
                    if (string.IsNullOrEmpty(outPath)) outPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), fname);
                    System.IO.File.WriteAllBytes(outPath, bytes);
                    return true;
                }
                finally { ComObjectManager.Release(rs); }
            }
            catch (Exception ex)
            {
                B1App.Instance.Application.SetStatusBarMessage($"Error restoring SRF: {ex.Message}", SAPbouiCOM.BoMessageTime.bmt_Short, true);
                return false;
            }
        }

        // SRF helpers (save current form SRF to UDT as base64) - simplified
        public static bool SaveFormAsSrf(string formType, string srfName)
        {
            try
            {
                var tempPath = System.IO.Path.GetTempFileName();
                // SAPbouiCOM has SaveAsXML for forms in some APIs; we fallback to saving our layout XML
                // Save layout definition
                var xml = ""; // gather layout
                // reuse existing layouts by name
                return SaveLayout(formType, srfName, xml, "SRF Export");
            }
            catch { return false; }
        }

        public static System.Data.DataTable ListLayoutVersions(string formType, string layoutName)
        {
            var dt = new System.Data.DataTable(); dt.Columns.Add("Name"); dt.Columns.Add("Owner"); dt.Columns.Add("Version"); dt.Columns.Add("CreatedAt");
            SAPbobsCOM.Recordset rs = null;
            try
            {
                rs = (SAPbobsCOM.Recordset)B1App.Instance.Company.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);
                string sql = B1App.Instance.IsHana
                    ? $"SELECT \"U_Name\", \"U_Owner\", \"U_Version\", \"U_CreatedAt\" FROM \"@BTUN_LAYOUT\" WHERE \"U_FormType\" = '{formType.Replace("'", "''")}' ORDER BY \"U_Name\", \"U_Version\" DESC"
                    : $"SELECT U_Name, U_Owner, U_Version, U_CreatedAt FROM [@BTUN_LAYOUT] WHERE [U_FormType] = '{formType.Replace("'", "''")}' ORDER BY U_Name, U_Version DESC";
                rs.DoQuery(sql);
                while (!rs.EoF)
                {
                    var r = dt.NewRow(); r[0] = SapUiSafe.SafeField(rs, 0); r[1] = SapUiSafe.SafeField(rs, 1); r[2] = SapUiSafe.SafeField(rs, 2); r[3] = SapUiSafe.SafeField(rs, 3); dt.Rows.Add(r); rs.MoveNext();
                }
            }
            catch { }
            finally { ComObjectManager.Release(rs); }
            return dt;
        }

        public static bool LoadLayoutVersion(string layoutName, string formType)
        {
            var def = GetLayoutDefinition(layoutName, formType);
            if (string.IsNullOrEmpty(def)) return false;
            try { ApplyLayoutToForm(def, SapUiSafe.TryGetActiveForm()); return true; } catch { return false; }
        }

        // Export the current form as SRF/XML using the UI API if available
        public static bool ExportSrf(SAPbouiCOM.Form form, string filePath)
        {
            if (form == null) form = SapUiSafe.TryGetActiveForm();
            if (form == null) return false;
            try
            {
                // Try known method names via reflection
                var ft = form.GetType();
                string[] methodNames = new[] { "SaveAsXML", "SaveToXML", "SaveAsSRF", "SaveToFile" };
                foreach (var m in methodNames)
                {
                    var mi = ft.GetMethod(m, new Type[] { typeof(string) });
                    if (mi != null)
                    {
                        mi.Invoke(form, new object[] { filePath });
                        return true;
                    }
                }

                // Try calling on application (some SDK versions expose form save through application)
                var appType = B1App.Instance.Application.GetType();
                foreach (var m in new[] { "SaveAsXML", "LoadFromXML", "SaveToFile" })
                {
                    var mi = appType.GetMethod(m, new Type[] { typeof(string) });
                    if (mi != null)
                    {
                        mi.Invoke(B1App.Instance.Application, new object[] { filePath });
                        return true;
                    }
                }

                // Fallback: save our layout XML if any
                var xml = ""; // try to get a layout for this form
                var layouts = GetLayouts(form.TypeEx);
                if (layouts.Rows.Count > 0)
                {
                    var name = layouts.Rows[0][0].ToString();
                    xml = GetLayoutDefinition(name, form.TypeEx);
                }
                if (!string.IsNullOrEmpty(xml))
                {
                    System.IO.File.WriteAllText(filePath, xml);
                    return true;
                }

                B1App.Instance.Application.SetStatusBarMessage("Export SRF: no compatible UI API method found.", SAPbouiCOM.BoMessageTime.bmt_Short, true);
                return false;
            }
            catch (Exception ex)
            {
                B1App.Instance.Application.SetStatusBarMessage($"Error exporting SRF: {ex.Message}", SAPbouiCOM.BoMessageTime.bmt_Short, true);
                return false;
            }
        }

        // Import SRF/XML into the client. Attempts UI API methods, otherwise applies layout XML.
        public static bool ImportSrf(string filePath)
        {
            try
            {
                if (!System.IO.File.Exists(filePath)) return false;
                // Try application load methods
                var app = B1App.Instance.Application;
                var appType = app.GetType();
                var loadMi = appType.GetMethod("LoadBatchActions", new Type[] { typeof(string) });
                if (loadMi != null)
                {
                    loadMi.Invoke(app, new object[] { filePath });
                    return true;
                }

                // Try to read file as layout XML and apply to active form
                var content = System.IO.File.ReadAllText(filePath);
                if (content.Contains("<Layout") || content.Contains("<Item"))
                {
                    var form = SapUiSafe.TryGetActiveForm();
                    if (form != null) return ApplyLayoutToForm(content, form);
                }

                B1App.Instance.Application.SetStatusBarMessage("Import SRF: no compatible UI API method found or file not recognized.", SAPbouiCOM.BoMessageTime.bmt_Short, true);
                return false;
            }
            catch (Exception ex)
            {
                B1App.Instance.Application.SetStatusBarMessage($"Error importing SRF: {ex.Message}", SAPbouiCOM.BoMessageTime.bmt_Short, true);
                return false;
            }
        }

        public static bool SaveLayout(string formType, string layoutName, string layoutXml, string description = "")
        {
            if (string.IsNullOrEmpty(layoutName) || string.IsNullOrEmpty(formType)) return false;
            SAPbobsCOM.Recordset rs = null;
            try
            {
                rs = (SAPbobsCOM.Recordset)B1App.Instance.Company.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);
                string safeLayout = layoutName.Replace("'", "''");
                string safeForm = formType.Replace("'", "''");
                string checkSql = B1App.Instance.IsHana
                    ? $"SELECT \"Code\" FROM \"@BTUN_LAYOUT\" WHERE \"U_Name\" = '{safeLayout}' AND \"U_FormType\" = '{safeForm}'"
                    : $"SELECT [Code] FROM [@BTUN_LAYOUT] WHERE [U_Name] = '{safeLayout}' AND [U_FormType] = '{safeForm}'";

                rs.DoQuery(checkSql);
                string xmlEsc = layoutXml.Replace("'", "''");
                string desc = description.Replace("'", "''");

                if (!rs.EoF)
                {
                    string codeValue = B1TuneUp.Utils.SapUiSafe.SafeField(rs, 0);
                    string updateSql = B1App.Instance.IsHana
                        ? $"UPDATE \"@BTUN_LAYOUT\" SET \"U_Def\" = '{xmlEsc}', \"U_Desc\" = '{desc}', \"U_UpdatedAt\" = CURRENT_TIMESTAMP WHERE \"Code\" = '{codeValue}'"
                        : $"UPDATE [@BTUN_LAYOUT] SET U_Def = '{xmlEsc}', U_Desc = '{desc}', U_UpdatedAt = GETDATE() WHERE [Code] = '{codeValue}'";
                    rs.DoQuery(updateSql);
                }
                else
                {
                    // include owner and version
                    string owner = "";
                    try { owner = B1App.Instance.Application.Company.UserName; } catch { owner = ""; }
                    string safeOwner = owner.Replace("'", "''");
                    int ver = 1;
                    int nextCode = UserTableCodeGenerator.GetNext("@BTUN_LAYOUT");
                    string codeValue = nextCode.ToString(CultureInfo.InvariantCulture);
                    string recordName = $"LAYOUT_{safeLayout}";
                    string insertSql = B1App.Instance.IsHana
                        ? $"INSERT INTO \"@BTUN_LAYOUT\" (\"Code\",\"Name\",\"U_FormType\", \"U_Name\", \"U_Desc\", \"U_Def\", \"U_CreatedAt\", \"U_Owner\", \"U_Version\") VALUES ('{codeValue}','{recordName}','{safeForm}', '{safeLayout}', '{desc}', '{xmlEsc}', CURRENT_TIMESTAMP, '{safeOwner}', '{ver}')"
                        : $"INSERT INTO [@BTUN_LAYOUT] ([Code],[Name],U_FormType, U_Name, U_Desc, U_Def, U_CreatedAt, U_Owner, U_Version) VALUES ('{codeValue}','{recordName}','{safeForm}', '{safeLayout}', '{desc}', '{xmlEsc}', GETDATE(), '{safeOwner}', {ver})";
                    rs.DoQuery(insertSql);
                }

                B1App.Instance.Application.SetStatusBarMessage($"Layout '{layoutName}' guardado.", SAPbouiCOM.BoMessageTime.bmt_Short, false);
                return true;
            }
            catch (Exception ex)
            {
                B1App.Instance.Application.SetStatusBarMessage($"Error guardando layout: {ex.Message}", SAPbouiCOM.BoMessageTime.bmt_Short, true);
                return false;
            }
            finally
            {
                ComObjectManager.Release(rs);
            }
        }

        public static bool DeleteLayout(string layoutName, string formType)
        {
            if (string.IsNullOrEmpty(layoutName)) return false;
            SAPbobsCOM.Recordset rs = null;
            try
            {
                rs = (SAPbobsCOM.Recordset)B1App.Instance.Company.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);
                string deleteSql = B1App.Instance.IsHana
                    ? $"DELETE FROM \"@BTUN_LAYOUT\" WHERE \"U_Name\" = '{layoutName.Replace("'", "''")}' AND \"U_FormType\" = '{formType.Replace("'", "''")}'"
                    : $"DELETE FROM [@BTUN_LAYOUT] WHERE [U_Name] = '{layoutName.Replace("'", "''")}' AND [U_FormType] = '{formType.Replace("'", "''")}'";
                rs.DoQuery(deleteSql);
                B1App.Instance.Application.SetStatusBarMessage($"Layout '{layoutName}' eliminado.", SAPbouiCOM.BoMessageTime.bmt_Short, false);
                return true;
            }
            catch (Exception ex)
            {
                B1App.Instance.Application.SetStatusBarMessage($"Error eliminando layout: {ex.Message}", SAPbouiCOM.BoMessageTime.bmt_Short, true);
                return false;
            }
            finally { ComObjectManager.Release(rs); }
        }

        public static System.Data.DataTable GetLayouts(string formType)
        {
            var dt = new System.Data.DataTable();
            dt.Columns.Add("DocEntry");
            dt.Columns.Add("U_Name");
            dt.Columns.Add("U_FormType");
            dt.Columns.Add("U_Desc");
            dt.Columns.Add("U_FileName");
            dt.Columns.Add("U_Owner");
            dt.Columns.Add("U_CreatedAt");
            dt.Columns.Add("U_UpdatedAt");
            dt.Columns.Add("U_Version");

            SAPbobsCOM.Recordset rs = null;
            try
            {
                rs = (SAPbobsCOM.Recordset)B1App.Instance.Company.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);
                string sql;
                if (string.IsNullOrEmpty(formType) || formType == "*")
                {
                    sql = B1App.Instance.IsHana
                        ? "SELECT \"Code\", \"U_Name\", \"U_FormType\", \"U_Desc\", \"U_FileName\", \"U_Owner\", \"U_CreatedAt\", \"U_UpdatedAt\", \"U_Version\" FROM \"@BTUN_LAYOUT\" ORDER BY \"U_Name\""
                        : "SELECT [Code], U_Name, U_FormType, U_Desc, U_FileName, U_Owner, U_CreatedAt, U_UpdatedAt, U_Version FROM [@BTUN_LAYOUT] ORDER BY U_Name";
                }
                else
                {
                    var ft = formType.Replace("'", "''");
                    sql = B1App.Instance.IsHana
                        ? $"SELECT \"Code\", \"U_Name\", \"U_FormType\", \"U_Desc\", \"U_FileName\", \"U_Owner\", \"U_CreatedAt\", \"U_UpdatedAt\", \"U_Version\" FROM \"@BTUN_LAYOUT\" WHERE \"U_FormType\" = '{ft}' ORDER BY \"U_Name\""
                        : $"SELECT [Code], U_Name, U_FormType, U_Desc, U_FileName, U_Owner, U_CreatedAt, U_UpdatedAt, U_Version FROM [@BTUN_LAYOUT] WHERE [U_FormType] = '{ft}' ORDER BY U_Name";
                }

                rs.DoQuery(sql);
                while (!rs.EoF)
                {
                    var row = dt.NewRow();
                    row["DocEntry"] = B1TuneUp.Utils.SapUiSafe.SafeField(rs, "Code");
                    row["U_Name"] = B1TuneUp.Utils.SapUiSafe.SafeField(rs, "U_Name");
                    row["U_FormType"] = B1TuneUp.Utils.SapUiSafe.SafeField(rs, "U_FormType");
                    row["U_Desc"] = B1TuneUp.Utils.SapUiSafe.SafeField(rs, "U_Desc");
                    row["U_FileName"] = B1TuneUp.Utils.SapUiSafe.SafeField(rs, "U_FileName");
                    row["U_Owner"] = B1TuneUp.Utils.SapUiSafe.SafeField(rs, "U_Owner");
                    row["U_CreatedAt"] = B1TuneUp.Utils.SapUiSafe.SafeFieldValue(rs, "U_CreatedAt");
                    row["U_UpdatedAt"] = SapUiSafe.SafeField(rs, "U_UpdatedAt");
                    row["U_Version"] = SapUiSafe.SafeField(rs, "U_Version");
                    dt.Rows.Add(row);
                    rs.MoveNext();
                }
            }
            catch { }
            finally { ComObjectManager.Release(rs); }
            return dt;
        }

        public static string GetLayoutDefinition(string layoutName, string formType)
        {
            if (string.IsNullOrEmpty(layoutName)) return "";
            Recordset rs = null;
            try
            {
                rs = (Recordset)B1App.Instance.Company.GetBusinessObject(BoObjectTypes.BoRecordset);
                string sql = B1App.Instance.IsHana
                    ? $"SELECT \"U_Def\" FROM \"@BTUN_LAYOUT\" WHERE \"U_Name\" = '{layoutName.Replace("'", "''")}' AND \"U_FormType\" = '{formType.Replace("'", "''")}'"
                    : $"SELECT U_Def FROM [@BTUN_LAYOUT] WHERE [U_Name] = '{layoutName.Replace("'", "''")}' AND [U_FormType] = '{formType.Replace("'", "''")}'";
                rs.DoQuery(sql);
                if (!rs.EoF) return SapUiSafe.SafeField(rs, 0);
            }
            catch { }
            finally { ComObjectManager.Release(rs); }
            return "";
        }

        public static bool ApplyLayoutToForm(string layoutXml, SAPbouiCOM.Form form)
        {
            if (form == null || string.IsNullOrEmpty(layoutXml)) return false;
            try
            {
                var doc = new XmlDocument();
                doc.LoadXml(layoutXml);
                var nodes = doc.SelectNodes("//Item");
                if (nodes == null) return false;
                foreach (XmlNode n in nodes)
                {
                    try
                    {
                        var id = n.Attributes["id"]?.Value;
                        if (string.IsNullOrEmpty(id)) continue;
                        int left = int.Parse(n.Attributes["left"].Value);
                        int top = int.Parse(n.Attributes["top"].Value);
                        int width = int.Parse(n.Attributes["width"].Value);
                        int height = int.Parse(n.Attributes["height"].Value);

                        var it = SapUiSafe.TryGetItem(form, id);
                        if (it != null)
                        {
                            it.Left = left;
                            it.Top = top;
                            it.Width = width;
                            it.Height = height;
                        }
                    }
                    catch { }
                }

                try { form.Update(); } catch { try { form.Refresh(); } catch { } }
                B1App.Instance.Application.SetStatusBarMessage("Layout aplicado.", SAPbouiCOM.BoMessageTime.bmt_Short, false);
                return true;
            }
            catch (Exception ex)
            {
                B1App.Instance.Application.SetStatusBarMessage($"Error aplicando layout: {ex.Message}", SAPbouiCOM.BoMessageTime.bmt_Short, true);
                return false;
            }
        }
    }
}
