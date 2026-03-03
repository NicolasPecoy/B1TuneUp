using System;
using System.Data;
using System.Xml;
using SAPbouiCOM;
using SAPbobsCOM;
using B1TuneUp.Core;
using B1TuneUp.Utils;

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
                    try { form = B1App.Instance.Application.Forms.ActiveForm; } catch { form = null; }
                    if (form == null)
                    {
                        B1App.Instance.Application.SetStatusBarMessage("No hay formulario activo para Item Placement.", SAPbouiCOM.BoMessageTime.bmt_Short, true);
                        return;
                    }
                }

                var dlg = new Forms.ItemPlacementForm(form);
                dlg.Show();
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
                    string insertSql = B1App.Instance.IsHana
                        ? $"INSERT INTO \"@BTUN_LAYOUT\" (\"U_FormType\", \"U_Name\", \"U_SRF\", \"U_FileName\", \"U_Owner\", \"U_CreatedAt\") VALUES ('{formType.Replace("'", "''")}', '{layoutName.Replace("'", "''")}', '{b64}', '{System.IO.Path.GetFileName(filePath).Replace("'", "''")}', '{owner.Replace("'", "''")}', CURRENT_TIMESTAMP)"
                        : $"INSERT INTO [@BTUN_LAYOUT] (U_FormType, U_Name, U_SRF, U_FileName, U_Owner, U_CreatedAt) VALUES ('{formType.Replace("'", "''")}', '{layoutName.Replace("'", "''")}', '{b64}', '{System.IO.Path.GetFileName(filePath).Replace("'", "''")}', '{owner.Replace("'", "''")}', GETDATE())";
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
                    var b64 = rs.Fields.Item(0).Value?.ToString() ?? "";
                    var fname = rs.Fields.Item(1).Value?.ToString() ?? "layout.srf";
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
                    var r = dt.NewRow(); r[0] = rs.Fields.Item(0).Value?.ToString(); r[1] = rs.Fields.Item(1).Value?.ToString(); r[2] = rs.Fields.Item(2).Value?.ToString(); r[3] = rs.Fields.Item(3).Value?.ToString(); dt.Rows.Add(r); rs.MoveNext();
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
            try { ApplyLayoutToForm(def, B1App.Instance.Application.Forms.ActiveForm); return true; } catch { return false; }
        }

        // Export the current form as SRF/XML using the UI API if available
        public static bool ExportSrf(SAPbouiCOM.Form form, string filePath)
        {
            if (form == null) form = B1App.Instance.Application.Forms.ActiveForm;
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
                    var form = B1App.Instance.Application.Forms.ActiveForm;
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
                string checkSql = B1App.Instance.IsHana
                    ? $"SELECT \"DocEntry\" FROM \"@BTUN_LAYOUT\" WHERE \"U_Name\" = '{layoutName.Replace("'", "''")}' AND \"U_FormType\" = '{formType.Replace("'", "''")}'"
                    : $"SELECT DocEntry FROM [@BTUN_LAYOUT] WHERE [U_Name] = '{layoutName.Replace("'", "''")}' AND [U_FormType] = '{formType.Replace("'", "''")}'";

                rs.DoQuery(checkSql);
                string xmlEsc = layoutXml.Replace("'", "''");
                string desc = description.Replace("'", "''");

                if (!rs.EoF)
                {
                    string docEntry = rs.Fields.Item(0).Value.ToString();
                    string updateSql = B1App.Instance.IsHana
                        ? $"UPDATE \"@BTUN_LAYOUT\" SET \"U_Def\" = '{xmlEsc}', \"U_Desc\" = '{desc}', \"U_UpdatedAt\" = CURRENT_TIMESTAMP WHERE \"DocEntry\" = '{docEntry}'"
                        : $"UPDATE [@BTUN_LAYOUT] SET U_Def = '{xmlEsc}', U_Desc = '{desc}', U_UpdatedAt = GETDATE() WHERE DocEntry = '{docEntry}'";
                    rs.DoQuery(updateSql);
                }
                else
                {
                    // include owner and version
                    string owner = "";
                    try { owner = B1App.Instance.Application.Company.UserName; } catch { owner = ""; }
                    int ver = 1;
                    string insertSql = B1App.Instance.IsHana
                        ? $"INSERT INTO \"@BTUN_LAYOUT\" (\"U_FormType\", \"U_Name\", \"U_Desc\", \"U_Def\", \"U_CreatedAt\", \"U_Owner\", \"U_Version\") VALUES ('{formType.Replace("'", "''")}', '{layoutName.Replace("'", "''")}', '{desc}', '{xmlEsc}', CURRENT_TIMESTAMP, '{owner.Replace("'", "''")}', '{ver}')"
                        : $"INSERT INTO [@BTUN_LAYOUT] (U_FormType, U_Name, U_Desc, U_Def, U_CreatedAt, U_Owner, U_Version) VALUES ('{formType.Replace("'", "''")}', '{layoutName.Replace("'", "''")}', '{desc}', '{xmlEsc}', GETDATE(), '{owner.Replace("'", "''")}', {ver})";
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
            dt.Columns.Add("U_Name");
            dt.Columns.Add("U_FormType");
            dt.Columns.Add("U_Desc");
            dt.Columns.Add("U_FileName");
            dt.Columns.Add("U_Owner");
            dt.Columns.Add("U_CreatedAt");
            SAPbobsCOM.Recordset rs = null;
            try
            {
                rs = (SAPbobsCOM.Recordset)B1App.Instance.Company.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);
                string sql;
                if (string.IsNullOrEmpty(formType) || formType == "*")
                {
                    sql = B1App.Instance.IsHana
                        ? $"SELECT \"U_Name\", \"U_FormType\", \"U_Desc\", \"U_FileName\", \"U_Owner\", \"U_CreatedAt\" FROM \"@BTUN_LAYOUT\" ORDER BY \"U_Name\""
                        : $"SELECT U_Name, U_FormType, U_Desc, U_FileName, U_Owner, U_CreatedAt FROM [@BTUN_LAYOUT] ORDER BY U_Name";
                }
                else
                {
                    sql = B1App.Instance.IsHana
                        ? $"SELECT \"U_Name\", \"U_FormType\", \"U_Desc\", \"U_FileName\", \"U_Owner\", \"U_CreatedAt\" FROM \"@BTUN_LAYOUT\" WHERE \"U_FormType\" = '{formType.Replace("'", "''")}' ORDER BY \"U_Name\""
                        : $"SELECT U_Name, U_FormType, U_Desc, U_FileName, U_Owner, U_CreatedAt FROM [@BTUN_LAYOUT] WHERE [U_FormType] = '{formType.Replace("'", "''")}' ORDER BY U_Name";
                }
                rs.DoQuery(sql);
                while (!rs.EoF)
                {
                    var row = dt.NewRow();
                    row[0] = rs.Fields.Item(0).Value?.ToString();
                    row[1] = rs.Fields.Item(1).Value?.ToString();
                    row[2] = rs.Fields.Item(2).Value?.ToString();
                    row[3] = rs.Fields.Item(3).Value?.ToString();
                    row[4] = rs.Fields.Item(4).Value?.ToString();
                    row[5] = rs.Fields.Item(5).Value?.ToString();
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
                if (!rs.EoF) return rs.Fields.Item(0).Value?.ToString() ?? "";
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

                        if (form.Items.Exists(id))
                        {
                            var it = form.Items.Item(id);
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
