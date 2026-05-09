using System;
using System.Collections.Generic;
using SAPbouiCOM;
using SAPbobsCOM;
using B1TuneUp.Core;
using B1TuneUp.Utils;

namespace B1TuneUp.Modules
{
    /// <summary>
    /// Gestiona la copia rápida de documentos SAP B1 con un solo clic.
    /// Equivalente al "Quick Copy" del B1 Usability Package de Boyum IT.
    /// Agrega botones dinámicos al formulario configurados en @BTUN_QCOPY.
    /// Al presionar el botón, usa el DI API para crear una copia del documento actual
    /// en el tipo de documento destino indicado, usando vinculación por BaseEntry/BaseLine.
    /// </summary>
    public static class QuickCopyManager
    {
        // Prefijo para los UIDs de botones generados
        private const string BtnPrefix = "BTQC";

        // Mapeo: "FormUID|BtnUID" -> (srcObjType, tgtObjType, postMacro)
        private static readonly Dictionary<string, (string SrcObjType, string TgtObjType, string PostMacro)> _btnMap
            = new Dictionary<string, (string, string, string)>();

        /// <summary>
        /// Agrega botones de copia rápida al formulario SAP B1 según la configuración en @BTUN_QCOPY.
        /// Llamar en et_FORM_LOAD (after).
        /// </summary>
        public static void AddQuickCopyButtons(Form oForm)
        {
            if (oForm == null) return;
            Recordset rs = (Recordset)B1App.Instance.Company.GetBusinessObject(BoObjectTypes.BoRecordset);
            try
            {
                string safeFormType = (oForm.TypeEx ?? string.Empty).Replace("'", "''");
                string sql = B1App.Instance.IsHana
                    ? $"SELECT * FROM \"@BTUN_QCOPY\" WHERE \"U_SrcFormType\" = '{safeFormType}' AND \"U_Active\" = 'Y' ORDER BY \"Code\""
                    : $"SELECT * FROM [@BTUN_QCOPY] WHERE [U_SrcFormType] = '{safeFormType}' AND [U_Active] = 'Y' ORDER BY [Code]";

                rs.DoQuery(sql);
                int idx = 0;

                while (!rs.EoF)
                {
                    string docEntry  = SapUiSafe.SafeField(rs, "Code");
                    string srcObj    = SapUiSafe.SafeField(rs, "U_SrcObjType");
                    string tgtObj    = SapUiSafe.SafeField(rs, "U_TgtObjType");
                    string btnLabel  = SapUiSafe.SafeField(rs, "U_BtnLabel");
                    string postMacro = SapUiSafe.SafeField(rs, "U_PostMacro");

                    string btnUID = BtnPrefix + docEntry.PadLeft(4, '0');

                    try
                    {
                        if (!ItemExists(oForm, btnUID))
                        {
                            Item btn = oForm.Items.Add(btnUID, BoFormItemTypes.it_BUTTON);
                            btn.Left     = 5 + idx * 92;
                            btn.Top      = oForm.Height - 26;
                            btn.Width    = 88;
                            btn.Height   = 19;
                            btn.FromPane = 1;
                            btn.ToPane   = 1;
                            var button = SapUiSafe.TryGetSpecific<SAPbouiCOM.Button>(btn);
                            if (button != null) button.Caption = btnLabel;
                        }

                        _btnMap[oForm.UniqueID + "|" + btnUID] = (srcObj, tgtObj, postMacro);
                        idx++;
                    }
                    catch { }

                    rs.MoveNext();
                }
            }
            catch (Exception ex)
            {
                B1App.Instance.Application.SetStatusBarMessage(
                    $"Error agregando botones de copia rápida: {ex.Message}",
                    BoMessageTime.bmt_Short, true);
                ExceptionLogger.LogHandled(ex, $"QuickCopyManager.AddQuickCopyButtons:{oForm?.TypeEx}");
            }
            finally
            {
                ComObjectManager.Release(rs);
            }
        }

        /// <summary>
        /// Maneja el clic de un botón de copia rápida.
        /// Devuelve true si el evento fue manejado por este manager.
        /// </summary>
        public static bool HandleButtonClick(string formUID, string itemUID, Form oForm)
        {
            if (!itemUID.StartsWith(BtnPrefix)) return false;

            string mapKey = formUID + "|" + itemUID;
            if (!_btnMap.TryGetValue(mapKey, out var cfg)) return false;

            ExecuteCopy(oForm, cfg.SrcObjType, cfg.TgtObjType, cfg.PostMacro);
            return true;
        }

        public static void ClearForm(string formUID)
        {
            if (string.IsNullOrEmpty(formUID)) return;
            string prefix = formUID + "|";
            foreach (var key in new List<string>(_btnMap.Keys))
            {
                if (key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    _btnMap.Remove(key);
                }
            }
        }

        /// <summary>
        /// Realiza la copia usando el DI API de SAP B1:
        /// Lee el documento fuente y crea uno nuevo vinculando líneas via BaseEntry/BaseLine/BaseType.
        /// </summary>
        private static void ExecuteCopy(Form oForm, string srcObjType, string tgtObjType, string postMacro)
        {
            SAPbobsCOM.Documents srcDoc = null;
            SAPbobsCOM.Documents tgtDoc = null;
            try
            {
                int srcDocEntry = GetDocEntryFromForm(oForm);
                if (srcDocEntry <= 0)
                {
                    B1App.Instance.Application.SetStatusBarMessage(
                        "No se pudo obtener el DocEntry. Guarde el documento primero.",
                        BoMessageTime.bmt_Short, true);
                    return;
                }

                if (!Enum.TryParse(srcObjType, out BoObjectTypes srcEnum) ||
                    !Enum.TryParse(tgtObjType, out BoObjectTypes tgtEnum))
                {
                    B1App.Instance.Application.SetStatusBarMessage(
                        $"Tipo de objeto inválido: {srcObjType} / {tgtObjType}",
                        BoMessageTime.bmt_Short, true);
                    return;
                }

                if (!IsMarketingDocumentType(srcEnum) || !IsMarketingDocumentType(tgtEnum))
                {
                    B1App.Instance.Application.SetStatusBarMessage(
                        $"Quick Copy solo soporta documentos de marketing DI API. Configuracion: {srcObjType} -> {tgtObjType}.",
                        BoMessageTime.bmt_Short, true);
                    return;
                }

                // Cargar documento fuente
                srcDoc = (SAPbobsCOM.Documents)B1App.Instance.Company.GetBusinessObject(srcEnum);
                if (!srcDoc.GetByKey(srcDocEntry))
                {
                    B1App.Instance.Application.SetStatusBarMessage(
                        $"Documento fuente DocEntry {srcDocEntry} no encontrado.",
                        BoMessageTime.bmt_Short, true);
                    return;
                }

                // Crear documento destino
                tgtDoc = (SAPbobsCOM.Documents)B1App.Instance.Company.GetBusinessObject(tgtEnum);
                tgtDoc.CardCode    = srcDoc.CardCode;
                tgtDoc.DocDate     = DateTime.Today;
                tgtDoc.DocDueDate  = srcDoc.DocDueDate;
                tgtDoc.Comments    = srcDoc.Comments;

                // Vincular líneas al documento base (forma estándar en SAP B1 DI API)
                int srcTypeInt  = (int)srcEnum;
                int lineCount   = srcDoc.Lines.Count;
                int copiedLines = 0;
                for (int i = 0; i < lineCount; i++)
                {
                    srcDoc.Lines.SetCurrentLine(i);
                    if (IsSourceLineClosed(srcDoc.Lines))
                        continue;

                    if (copiedLines > 0) tgtDoc.Lines.Add();
                    tgtDoc.Lines.BaseType  = srcTypeInt;
                    tgtDoc.Lines.BaseEntry = srcDocEntry;
                    tgtDoc.Lines.BaseLine  = i;
                    copiedLines++;
                }

                if (copiedLines == 0)
                {
                    B1App.Instance.Application.SetStatusBarMessage(
                        "El documento no tiene lineas abiertas disponibles para copiar.",
                        BoMessageTime.bmt_Short, true);
                    return;
                }

                int retCode = tgtDoc.Add();
                if (retCode == 0)
                {
                    string newKey = B1App.Instance.Company.GetNewObjectKey();
                    AuditLogManager.LogAction("QuickCopy",
                        $"Copia de DocEntry {srcDocEntry} ({srcObjType}) -> DocEntry {newKey} ({tgtObjType})");
                    B1App.Instance.Application.SetStatusBarMessage(
                        $"Copia rápida exitosa. Nuevo documento: {newKey}",
                        BoMessageTime.bmt_Medium, false);

                    if (!string.IsNullOrEmpty(postMacro))
                        MacroEngine.ExecuteMacro(postMacro, oForm);
                }
                else
                {
                    string err = B1App.Instance.Company.GetLastErrorDescription();
                    B1App.Instance.Application.SetStatusBarMessage(
                        $"Error al crear la copia: {err}", BoMessageTime.bmt_Short, true);
                }
            }
            catch (Exception ex)
            {
                B1App.Instance.Application.SetStatusBarMessage(
                    $"Error en copia rápida: {ex.Message}", BoMessageTime.bmt_Short, true);
                ExceptionLogger.LogHandled(ex, $"QuickCopyManager.ExecuteCopy:{srcObjType}->{tgtObjType}");
            }
            finally
            {
                ComObjectManager.Release(srcDoc);
                ComObjectManager.Release(tgtDoc);
            }
        }

        private static int GetDocEntryFromForm(Form oForm)
        {
            try
            {
                DBDataSource ds = oForm.DataSources.DBDataSources.Item(0);
                string val = ds.GetValue("DocEntry", 0).Trim();
                if (int.TryParse(val, out int docEntry) && docEntry > 0)
                    return docEntry;
            }
            catch { }
            return -1;
        }

        private static bool ItemExists(Form oForm, string itemUID)
        {
            return SapUiSafe.TryGetItem(oForm, itemUID) != null;
        }

        private static bool IsMarketingDocumentType(BoObjectTypes type)
        {
            string name = type.ToString();
            switch (name)
            {
                case "oOrders":
                case "oDeliveryNotes":
                case "oInvoices":
                case "oPurchaseOrders":
                case "oPurchaseDeliveryNotes":
                case "oPurchaseInvoices":
                case "oQuotations":
                case "oPurchaseQuotations":
                case "oReturns":
                case "oCreditNotes":
                case "oPurchaseReturns":
                case "oPurchaseCreditNotes":
                case "oDrafts":
                    return true;
                default:
                    return false;
            }
        }

        private static bool IsSourceLineClosed(object lines)
        {
            try
            {
                var prop = lines.GetType().GetProperty("LineStatus");
                var value = prop?.GetValue(lines, null)?.ToString();
                return string.Equals(value, "bost_Close", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(value, "Closed", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(value, "C", StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }
    }
}
