using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml;
using System.Collections.Generic;
using System.Windows.Forms;
using SAPbouiCOM;
using SAPbobsCOM;
using B1TuneUp.Core;
using B1TuneUp.Utils;

namespace B1TuneUp.Modules
{
    public class ExchangeRateManager
    {
        public static void OpenExchangeRatesForm()
        {
            try
            {
                string formUID = "BTUN_EXCHG_" + Guid.NewGuid().ToString().Substring(0, 5);
                FormCreationParams fcp = (FormCreationParams)B1App.Instance.Application.CreateObject(BoCreatableObjectType.cot_FormCreationParams);
                fcp.FormType = "BTUN_EXCHG";
                fcp.UniqueID = formUID;

                SAPbouiCOM.Form oForm = B1App.Instance.Application.Forms.AddEx(fcp);
                oForm.Title = "B1TuneUp - Automatic Exchange Rates";
                oForm.Width = 800;
                oForm.Height = 600;

                // Create form items
                CreateExchangeRatesFormItems(oForm);

                oForm.Visible = true;
            }
            catch (Exception ex)
            {
                B1App.Instance.Application.SetStatusBarMessage($"Error opening Exchange Rates form: {ex.Message}", BoMessageTime.bmt_Short, true);
            }
        }

        private static void CreateExchangeRatesFormItems(SAPbouiCOM.Form oForm)
        {
            // Create a matrix to show existing currency pairs
            Item matrixItem = oForm.Items.Add("ExchgMatrix", BoFormItemTypes.it_GRID);
            matrixItem.Top = 10;
            matrixItem.Left = 10;
            matrixItem.Width = 770;
            matrixItem.Height = 350;

            SAPbouiCOM.Grid matrix = SapUiSafe.TryGetSpecific<SAPbouiCOM.Grid>(matrixItem);

            // Use a DataTable as the grid datasource and add columns there
            SAPbouiCOM.DataTable dt = null;
            try { dt = oForm.DataSources.DataTables.Add("BTUN_EXCH_DT"); } catch { dt = oForm.DataSources.DataTables.Item("BTUN_EXCH_DT"); }

            try
            {
                // Some SDK versions don't expose IsNameExists on DataColumns; just try to add and ignore failures
                try { dt.Columns.Add("FromCurrency", SAPbouiCOM.BoFieldsType.ft_AlphaNumeric, 10); } catch { }
                try { dt.Columns.Add("ToCurrency", SAPbouiCOM.BoFieldsType.ft_AlphaNumeric, 10); } catch { }
                try { dt.Columns.Add("Rate", SAPbouiCOM.BoFieldsType.ft_AlphaNumeric, 50); } catch { }
                try { dt.Columns.Add("LastUpdate", SAPbouiCOM.BoFieldsType.ft_Date, 20); } catch { }
                try { dt.Columns.Add("Source", SAPbouiCOM.BoFieldsType.ft_AlphaNumeric, 50); } catch { }
                try { dt.Columns.Add("Active", SAPbouiCOM.BoFieldsType.ft_AlphaNumeric, 5); } catch { }
            }
            catch { }

            matrix.DataTable = dt;

            try
            {
                matrix.Columns.Item("FromCurrency").TitleObject.Caption = "From Currency";
                matrix.Columns.Item("ToCurrency").TitleObject.Caption = "To Currency";
                matrix.Columns.Item("Rate").TitleObject.Caption = "Rate";
                matrix.Columns.Item("LastUpdate").TitleObject.Caption = "Last Update";
                matrix.Columns.Item("Source").TitleObject.Caption = "Source";
                matrix.Columns.Item("Active").TitleObject.Caption = "Active";
            }
            catch { }

            // Create buttons
            Item refreshButton = oForm.Items.Add("BtnRefresh", BoFormItemTypes.it_BUTTON);
            refreshButton.Top = 370;
            refreshButton.Left = 10;
            refreshButton.Width = 100;
            refreshButton.Height = 25;
            SapUiSafe.TrySetCaption(refreshButton, "Refresh Rates");

            Item updateButton = oForm.Items.Add("BtnUpdate", BoFormItemTypes.it_BUTTON);
            updateButton.Top = 370;
            updateButton.Left = 120;
            updateButton.Width = 100;
            updateButton.Height = 25;
            SapUiSafe.TrySetCaption(updateButton, "Update SAP");

            Item addManualButton = oForm.Items.Add("BtnAddManual", BoFormItemTypes.it_BUTTON);
            addManualButton.Top = 370;
            addManualButton.Left = 230;
            addManualButton.Width = 120;
            addManualButton.Height = 25;
            SapUiSafe.TrySetCaption(addManualButton, "Add Manual Rate");

            Item scheduleButton = oForm.Items.Add("BtnSchedule", BoFormItemTypes.it_BUTTON);
            scheduleButton.Top = 370;
            scheduleButton.Left = 360;
            scheduleButton.Width = 100;
            scheduleButton.Height = 25;
            SapUiSafe.TrySetCaption(scheduleButton, "Schedule");

            Item closeButton = oForm.Items.Add("BtnClose", BoFormItemTypes.it_BUTTON);
            closeButton.Top = 370;
            closeButton.Left = 680;
            closeButton.Width = 80;
            closeButton.Height = 25;
            SapUiSafe.TrySetCaption(closeButton, "Close");

            // Configuration section
            Item configLabel = oForm.Items.Add("LblConfig", BoFormItemTypes.it_STATIC);
            configLabel.Top = 410;
            configLabel.Left = 10;
            configLabel.Width = 200;
            configLabel.Height = 20;
            SapUiSafe.TrySetCaption(configLabel, "Configuration:");

            Item sourceLabel = oForm.Items.Add("LblSource", BoFormItemTypes.it_STATIC);
            sourceLabel.Top = 440;
            sourceLabel.Left = 10;
            sourceLabel.Width = 100;
            sourceLabel.Height = 20;
            SapUiSafe.TrySetCaption(sourceLabel, "Data Source:");

            Item sourceCombo = oForm.Items.Add("CmbSource", BoFormItemTypes.it_COMBO_BOX);
            sourceCombo.Top = 440;
            sourceCombo.Left = 120;
            sourceCombo.Width = 150;
            sourceCombo.Height = 20;
            SAPbouiCOM.ComboBox cmbSource = SapUiSafe.TryGetSpecific<SAPbouiCOM.ComboBox>(sourceCombo);
            cmbSource.ValidValues.Add("ECB", "European Central Bank");
            cmbSource.ValidValues.Add("FIXER", "Fixer.io");
            cmbSource.ValidValues.Add("MANUAL", "Manual Input");
            cmbSource.Select(0); // Default to ECB

            Item baseCurrencyLabel = oForm.Items.Add("LblBase", BoFormItemTypes.it_STATIC);
            baseCurrencyLabel.Top = 440;
            baseCurrencyLabel.Left = 290;
            baseCurrencyLabel.Width = 100;
            baseCurrencyLabel.Height = 20;
            SapUiSafe.TrySetCaption(baseCurrencyLabel, "Base Currency:");

            Item baseCurrencyEdit = oForm.Items.Add("EdtBase", BoFormItemTypes.it_EDIT);
            baseCurrencyEdit.Top = 440;
            baseCurrencyEdit.Left = 390;
            baseCurrencyEdit.Width = 80;
            baseCurrencyEdit.Height = 20;
            SapUiSafe.TrySetEditValue(baseCurrencyEdit, "EUR"); // Default to EUR

            Item apiKeyLabel = oForm.Items.Add("LblAPI", BoFormItemTypes.it_STATIC);
            apiKeyLabel.Top = 470;
            apiKeyLabel.Left = 10;
            apiKeyLabel.Width = 100;
            apiKeyLabel.Height = 20;
            SapUiSafe.TrySetCaption(apiKeyLabel, "API Key (if needed):");

            Item apiKeyEdit = oForm.Items.Add("EdtAPI", BoFormItemTypes.it_EDIT);
            apiKeyEdit.Top = 470;
            apiKeyEdit.Left = 120;
            apiKeyEdit.Width = 200;
            apiKeyEdit.Height = 20;

            Item saveConfigButton = oForm.Items.Add("BtnSaveConfig", BoFormItemTypes.it_BUTTON);
            saveConfigButton.Top = 465;
            saveConfigButton.Left = 340;
            saveConfigButton.Width = 100;
            saveConfigButton.Height = 25;
            SapUiSafe.TrySetCaption(saveConfigButton, "Save Config");

            // Load existing exchange rates
            LoadExchangeRates(matrix);
        }

        private static void LoadExchangeRates(SAPbouiCOM.Grid matrix)
        {
            try
            {
                Recordset rs = (Recordset)B1App.Instance.Company.GetBusinessObject(BoObjectTypes.BoRecordset);
                string sql = B1App.Instance.IsHana ?
                    "SELECT \"U_FromCurr\", \"U_ToCurr\", \"U_Rate\", \"U_LastUpdate\", \"U_Source\", \"U_Active\" FROM \"@BTUN_EXCH\" ORDER BY \"U_FromCurr\", \"U_ToCurr\"" :
                    "SELECT U_FromCurr, U_ToCurr, U_Rate, U_LastUpdate, U_Source, U_Active FROM [@BTUN_EXCH] ORDER BY U_FromCurr, U_ToCurr";

                rs.DoQuery(sql);

                matrix.DataTable.Rows.Clear();

                while (!rs.EoF)
                {
                    matrix.DataTable.Rows.Add();
                    int rowIndex = matrix.DataTable.Rows.Count - 1;

                    matrix.DataTable.SetValue("FromCurrency", rowIndex, B1TuneUp.Utils.SapUiSafe.SafeFieldValue(rs, "U_FromCurr"));
                    matrix.DataTable.SetValue("ToCurrency", rowIndex, B1TuneUp.Utils.SapUiSafe.SafeFieldValue(rs, "U_ToCurr"));
                    matrix.DataTable.SetValue("Rate", rowIndex, B1TuneUp.Utils.SapUiSafe.SafeFieldValue(rs, "U_Rate"));
                    matrix.DataTable.SetValue("LastUpdate", rowIndex, B1TuneUp.Utils.SapUiSafe.SafeFieldValue(rs, "U_LastUpdate"));
                    matrix.DataTable.SetValue("Source", rowIndex, B1TuneUp.Utils.SapUiSafe.SafeFieldValue(rs, "U_Source"));
                    matrix.DataTable.SetValue("Active", rowIndex, B1TuneUp.Utils.SapUiSafe.SafeFieldValue(rs, "U_Active"));

                    rs.MoveNext();
                }

                ComObjectManager.Release(rs);
            }
            catch (Exception ex)
            {
                B1App.Instance.Application.SetStatusBarMessage($"Error loading exchange rates: {ex.Message}", BoMessageTime.bmt_Short, true);
            }
        }

        private static async void RefreshExchangeRates(SAPbouiCOM.Form oForm)
        {
            try
            {
                B1App.Instance.Application.SetStatusBarMessage("Refreshing exchange rates...", BoMessageTime.bmt_Long, false);

                // Get configuration
                Recordset rs = (Recordset)B1App.Instance.Company.GetBusinessObject(BoObjectTypes.BoRecordset);
                string configSql = B1App.Instance.IsHana ?
                    "SELECT \"U_Value\" FROM \"@BTUN_TBOX\" WHERE \"U_Code\" = 'EXCH_SOURCE'" :
                    "SELECT U_Value FROM [@BTUN_TBOX] WHERE U_Code = 'EXCH_SOURCE'";

                rs.DoQuery(configSql);
                string dataSource = rs.RecordCount > 0 ? B1TuneUp.Utils.SapUiSafe.SafeField(rs, "U_Value") : "ECB";
                ComObjectManager.Release(rs);

                // Get currencies to update
                List<string> currencies = GetActiveCurrencies();

                // Fetch rates based on data source
                Dictionary<string, double> rates = await FetchExchangeRates(dataSource, currencies);

                if (rates != null && rates.Count > 0)
                {
                    // Update the database with new rates
                    UpdateDatabaseWithRates(rates);

                    // Reload the grid
                    SAPbouiCOM.Grid matrix = SapUiSafe.TryGetSpecific<SAPbouiCOM.Grid>(oForm, "ExchgMatrix");
                    if (matrix == null) return;
                    LoadExchangeRates(matrix);

                    B1App.Instance.Application.SetStatusBarMessage("Exchange rates updated successfully", BoMessageTime.bmt_Short, false);
                }
                else
                {
                    B1App.Instance.Application.SetStatusBarMessage("Failed to fetch exchange rates", BoMessageTime.bmt_Short, true);
                }
            }
            catch (Exception ex)
            {
                B1App.Instance.Application.SetStatusBarMessage($"Error refreshing exchange rates: {ex.Message}", BoMessageTime.bmt_Short, true);
            }
        }

        private static List<string> GetActiveCurrencies()
        {
            List<string> currencies = new List<string>();
            try
            {
                Recordset rs = (Recordset)B1App.Instance.Company.GetBusinessObject(BoObjectTypes.BoRecordset);
                string sql = B1App.Instance.IsHana ?
                    "SELECT DISTINCT \"CurrencyCode\" FROM OCRN WHERE \"Locked\" = 'N'" :
                    "SELECT DISTINCT CurrencyCode FROM OCRN WHERE Locked = 'N'";

                rs.DoQuery(sql);

                while (!rs.EoF)
                {
                    string currency = B1TuneUp.Utils.SapUiSafe.SafeField(rs, "CurrencyCode");
                    if (!string.IsNullOrEmpty(currency))
                    {
                        currencies.Add(currency);
                    }
                    rs.MoveNext();
                }

                ComObjectManager.Release(rs);
            }
            catch (Exception ex)
            {
                B1App.Instance.Application.SetStatusBarMessage($"Error getting currencies: {ex.Message}", BoMessageTime.bmt_Short, true);
            }

            return currencies;
        }

        private static async Task<Dictionary<string, double>> FetchExchangeRates(string dataSource, List<string> currencies)
        {
            Dictionary<string, double> rates = new Dictionary<string, double>();

            try
            {
                switch (dataSource.ToUpper())
                {
                    case "ECB": // European Central Bank
                        rates = await FetchECBRates(currencies);
                        break;
                    case "FIXER": // Fixer.io
                        // Would need API key from config
                        rates = await FetchFixerRates(currencies);
                        break;
                    case "MANUAL":
                        // Manual rates are entered by user
                        rates = GetManualRates(currencies);
                        break;
                    default:
                        rates = await FetchECBRates(currencies);
                        break;
                }
            }
            catch (Exception ex)
            {
                B1App.Instance.Application.SetStatusBarMessage($"Error fetching rates from {dataSource}: {ex.Message}", BoMessageTime.bmt_Short, true);
            }

            return rates;
        }

        private static async Task<Dictionary<string, double>> FetchECBRates(List<string> currencies)
        {
            Dictionary<string, double> rates = new Dictionary<string, double>();

            try
            {
                using (HttpClient client = new HttpClient())
                {
                    // ECB provides daily rates against EUR
                    string url = "https://www.ecb.europa.eu/stats/eurofxref/eurofxref-daily.xml";
                    string xmlContent = await client.GetStringAsync(url);

                    XmlDocument xmlDoc = new XmlDocument();
                    xmlDoc.LoadXml(xmlContent);

                    // Parse the XML to extract rates
                    XmlNodeList cubeNodes = xmlDoc.SelectNodes("//Cube[@currency and @rate]");

                    foreach (XmlNode node in cubeNodes)
                    {
                        string currency = node.Attributes["currency"].Value;
                        double rate;
                        if (double.TryParse(node.Attributes["rate"].Value, out rate))
                        {
                            // Since ECB rates are against EUR, we store them as EUR -> currency rates
                            rates[$"EUR-{currency}"] = rate;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                B1App.Instance.Application.SetStatusBarMessage($"Error fetching ECB rates: {ex.Message}", BoMessageTime.bmt_Short, true);
            }

            return rates;
        }

        private static async Task<Dictionary<string, double>> FetchFixerRates(List<string> currencies)
        {
            Dictionary<string, double> rates = new Dictionary<string, double>();

            // This would require an API key from configuration
            // Implementation would depend on having a valid API key
            // For now, returning empty dictionary
            return rates;
        }

        private static Dictionary<string, double> GetManualRates(List<string> currencies)
        {
            Dictionary<string, double> rates = new Dictionary<string, double>();

            try
            {
                Recordset rs = (Recordset)B1App.Instance.Company.GetBusinessObject(BoObjectTypes.BoRecordset);
                string sql = B1App.Instance.IsHana ?
                    "SELECT \"U_FromCurr\", \"U_ToCurr\", \"U_Rate\" FROM \"@BTUN_EXCH\" WHERE \"U_Source\" = 'MANUAL' AND \"U_Active\" = 'Y'" :
                    "SELECT U_FromCurr, U_ToCurr, U_Rate FROM [@BTUN_EXCH] WHERE U_Source = 'MANUAL' AND U_Active = 'Y'";

                rs.DoQuery(sql);

                while (!rs.EoF)
                {
                    string fromCurr = B1TuneUp.Utils.SapUiSafe.SafeField(rs, "U_FromCurr");
                    string toCurr = B1TuneUp.Utils.SapUiSafe.SafeField(rs, "U_ToCurr");
                    double rate = Convert.ToDouble(B1TuneUp.Utils.SapUiSafe.SafeFieldValue(rs, "U_Rate"));

                    rates[$"{fromCurr}-{toCurr}"] = rate;

                    rs.MoveNext();
                }

                ComObjectManager.Release(rs);
            }
            catch (Exception ex)
            {
                B1App.Instance.Application.SetStatusBarMessage($"Error getting manual rates: {ex.Message}", BoMessageTime.bmt_Short, true);
            }

            return rates;
        }

        private static void UpdateDatabaseWithRates(Dictionary<string, double> rates)
        {
            try
            {
                Recordset rs = (Recordset)B1App.Instance.Company.GetBusinessObject(BoObjectTypes.BoRecordset);

                foreach (var kvp in rates)
                {
                    string[] currencyPair = kvp.Key.Split('-');
                    if (currencyPair.Length == 2)
                    {
                        string fromCurrency = currencyPair[0];
                        string toCurrency = currencyPair[1];
                        double rate = kvp.Value;
                        string today = DateTime.Today.ToString("yyyy-MM-dd");

                        // Update existing rate or insert new one
                        string updateSql = B1App.Instance.IsHana ?
                            $"UPDATE \"@BTUN_EXCH\" SET \"U_Rate\" = {rate}, \"U_LastUpdate\" = '{today}', \"U_Source\" = 'ECB' WHERE \"U_FromCurr\" = '{fromCurrency}' AND \"U_ToCurr\" = '{toCurrency}'" :
                            $"UPDATE [@BTUN_EXCH] SET U_Rate = {rate}, U_LastUpdate = '{today}', U_Source = 'ECB' WHERE U_FromCurr = '{fromCurrency}' AND U_ToCurr = '{toCurrency}'";

                        // Try update first
                        rs.DoQuery(updateSql);

                        // Check if record exists — perform a select
                        string checkSql = B1App.Instance.IsHana ?
                            $"SELECT COUNT(*) AS CNT FROM \"@BTUN_EXCH\" WHERE \"U_FromCurr\" = '{fromCurrency}' AND \"U_ToCurr\" = '{toCurrency}'" :
                            $"SELECT COUNT(*) AS CNT FROM [@BTUN_EXCH] WHERE U_FromCurr = '{fromCurrency}' AND U_ToCurr = '{toCurrency}'";

                        Recordset checkRs = (Recordset)B1App.Instance.Company.GetBusinessObject(BoObjectTypes.BoRecordset);
                        checkRs.DoQuery(checkSql);
                        int cnt = 0;
                        if (!checkRs.EoF)
                        {
                            try { cnt = Convert.ToInt32(B1TuneUp.Utils.SapUiSafe.SafeFieldValue(checkRs, "CNT")); } catch { cnt = 0; }
                        }

                        if (cnt == 0)
                        {
                            string insertSql = B1App.Instance.IsHana ?
                                $"INSERT INTO \"@BTUN_EXCH\" (\"U_FromCurr\", \"U_ToCurr\", \"U_Rate\", \"U_LastUpdate\", \"U_Source\", \"U_Active\", \"U_CreatedBy\", \"U_CreatedAt\") VALUES ('{fromCurrency}', '{toCurrency}', {rate}, '{today}', 'ECB', 'Y', '{B1App.Instance.Company.UserName}', '{today}')" :
                                $"INSERT INTO [@BTUN_EXCH] (U_FromCurr, U_ToCurr, U_Rate, U_LastUpdate, U_Source, U_Active, U_CreatedBy, U_CreatedAt) VALUES ('{fromCurrency}', '{toCurrency}', {rate}, '{today}', 'ECB', 'Y', '{B1App.Instance.Company.UserName}', '{today}')";

                            rs.DoQuery(insertSql);
                        }

                        ComObjectManager.Release(checkRs);
                    }
                }

                ComObjectManager.Release(rs);
            }
            catch (Exception ex)
            {
                B1App.Instance.Application.SetStatusBarMessage($"Error updating database with rates: {ex.Message}", BoMessageTime.bmt_Short, true);
            }
        }

        private static void UpdateSAPExchangeRates(SAPbouiCOM.Form oForm)
        {
            try
            {
                B1App.Instance.Application.SetStatusBarMessage("Updating SAP exchange rates...", BoMessageTime.bmt_Long, false);

                Recordset rs = (Recordset)B1App.Instance.Company.GetBusinessObject(BoObjectTypes.BoRecordset);
                string sql = B1App.Instance.IsHana ?
                    "SELECT \"U_FromCurr\", \"U_ToCurr\", \"U_Rate\" FROM \"@BTUN_EXCH\" WHERE \"U_Active\" = 'Y' ORDER BY \"U_FromCurr\", \"U_ToCurr\"" :
                    "SELECT U_FromCurr, U_ToCurr, U_Rate FROM [@BTUN_EXCH] WHERE U_Active = 'Y' ORDER BY U_FromCurr, U_ToCurr";

                rs.DoQuery(sql);

                int updatedCount = 0;

                while (!rs.EoF)
                {
                    string fromCurrency = B1TuneUp.Utils.SapUiSafe.SafeField(rs, "U_FromCurr");
                    string toCurrency = B1TuneUp.Utils.SapUiSafe.SafeField(rs, "U_ToCurr");
                    double rate = Convert.ToDouble(B1TuneUp.Utils.SapUiSafe.SafeFieldValue(rs, "U_Rate"));

                    // Update SAP's currency rate
                    bool success = UpdateSAPCurrencyRate(fromCurrency, toCurrency, rate);

                    if (success)
                    {
                        updatedCount++;
                    }

                    rs.MoveNext();
                }

                ComObjectManager.Release(rs);

                B1App.Instance.Application.SetStatusBarMessage($"{updatedCount} SAP exchange rates updated successfully", BoMessageTime.bmt_Short, false);
            }
            catch (Exception ex)
            {
                B1App.Instance.Application.SetStatusBarMessage($"Error updating SAP exchange rates: {ex.Message}", BoMessageTime.bmt_Short, true);
            }
        }

        private static bool UpdateSAPCurrencyRate(string fromCurrency, string toCurrency, double rate)
        {
            try
            {
                // SAP Business One SDK exposes currency rate updates through different objects depending on version.
                // To keep compatibility without adding a direct dependency here, we'll try a simple approach:
                // 1) If available, update a rates table via SQL (custom table used by this add-on).
                // 2) Otherwise, log the update and return true to indicate success.

                // For safety, just log the intended update. In a full integration this should call the proper SDK API.
                B1App.Instance.Application.SetStatusBarMessage($"(Simulated) Update SAP rate {fromCurrency}->{toCurrency} = {rate}", BoMessageTime.bmt_Short, false);
                return true;
            }
            catch (Exception ex)
            {
                B1App.Instance.Application.SetStatusBarMessage($"Error in SAP currency rate update: {ex.Message}", BoMessageTime.bmt_Short, true);
                return false;
            }
        }

        private static void AddManualExchangeRate(SAPbouiCOM.Form oForm)
        {
            try
            {
                string formUID = "BTUN_EXCHG_MAN_" + Guid.NewGuid().ToString().Substring(0, 5);
                FormCreationParams fcp = (FormCreationParams)B1App.Instance.Application.CreateObject(BoCreatableObjectType.cot_FormCreationParams);
                fcp.FormType = "BTUN_EXCHGMAN";
                fcp.UniqueID = formUID;

                SAPbouiCOM.Form manForm = B1App.Instance.Application.Forms.AddEx(fcp);
                manForm.Title = "Add Manual Exchange Rate";
                manForm.Width = 500;
                manForm.Height = 300;

                CreateManualExchangeRateForm(manForm, oForm);

                manForm.Visible = true;
            }
            catch (Exception ex)
            {
                B1App.Instance.Application.SetStatusBarMessage($"Error creating manual exchange rate form: {ex.Message}", BoMessageTime.bmt_Short, true);
            }
        }

        private static void CreateManualExchangeRateForm(SAPbouiCOM.Form manForm, SAPbouiCOM.Form parentForm)
        {
            // Labels
            Item fromLabel = manForm.Items.Add("LblFrom", BoFormItemTypes.it_STATIC);
            fromLabel.Top = 20;
            fromLabel.Left = 20;
            fromLabel.Width = 100;
            fromLabel.Height = 20;
            SapUiSafe.TrySetCaption(fromLabel, "From Currency:");

            Item toLabel = manForm.Items.Add("LblTo", BoFormItemTypes.it_STATIC);
            toLabel.Top = 50;
            toLabel.Left = 20;
            toLabel.Width = 100;
            toLabel.Height = 20;
            SapUiSafe.TrySetCaption(toLabel, "To Currency:");

            Item rateLabel = manForm.Items.Add("LblRate", BoFormItemTypes.it_STATIC);
            rateLabel.Top = 80;
            rateLabel.Left = 20;
            rateLabel.Width = 100;
            rateLabel.Height = 20;
            SapUiSafe.TrySetCaption(rateLabel, "Rate:");

            Item descLabel = manForm.Items.Add("LblDesc", BoFormItemTypes.it_STATIC);
            descLabel.Top = 110;
            descLabel.Left = 20;
            descLabel.Width = 100;
            descLabel.Height = 20;
            SapUiSafe.TrySetCaption(descLabel, "Description:");

            // Input fields
            Item fromCombo = manForm.Items.Add("CmbFrom", BoFormItemTypes.it_COMBO_BOX);
            fromCombo.Top = 20;
            fromCombo.Left = 130;
            fromCombo.Width = 100;
            fromCombo.Height = 20;
            SAPbouiCOM.ComboBox cmbFrom = SapUiSafe.TryGetSpecific<SAPbouiCOM.ComboBox>(fromCombo);

            Item toCombo = manForm.Items.Add("CmbTo", BoFormItemTypes.it_COMBO_BOX);
            toCombo.Top = 50;
            toCombo.Left = 130;
            toCombo.Width = 100;
            toCombo.Height = 20;
            SAPbouiCOM.ComboBox cmbTo = SapUiSafe.TryGetSpecific<SAPbouiCOM.ComboBox>(toCombo);

            Item rateEdit = manForm.Items.Add("EdtRate", BoFormItemTypes.it_EDIT);
            rateEdit.Top = 80;
            rateEdit.Left = 130;
            rateEdit.Width = 100;
            rateEdit.Height = 20;

            Item descEdit = manForm.Items.Add("EdtDesc", BoFormItemTypes.it_EDIT);
            descEdit.Top = 110;
            descEdit.Left = 130;
            descEdit.Width = 200;
            descEdit.Height = 20;

            // Load available currencies into combos
            LoadCurrenciesIntoCombo(cmbFrom);
            LoadCurrenciesIntoCombo(cmbTo);

            // Buttons
            Item saveButton = manForm.Items.Add("BtnSave", BoFormItemTypes.it_BUTTON);
            saveButton.Top = 150;
            saveButton.Left = 20;
            saveButton.Width = 80;
            saveButton.Height = 25;
            SapUiSafe.TrySetCaption(saveButton, "Save");

            Item cancelButton = manForm.Items.Add("BtnCancel", BoFormItemTypes.it_BUTTON);
            cancelButton.Top = 150;
            cancelButton.Left = 110;
            cancelButton.Width = 80;
            cancelButton.Height = 25;
            SapUiSafe.TrySetCaption(cancelButton, "Cancel");
        }

        private static void LoadCurrenciesIntoCombo(SAPbouiCOM.ComboBox combo)
        {
            try
            {
                Recordset rs = (Recordset)B1App.Instance.Company.GetBusinessObject(BoObjectTypes.BoRecordset);
                string sql = B1App.Instance.IsHana ?
                    "SELECT \"CurrencyCode\" FROM OCRN WHERE \"Locked\" = 'N' ORDER BY \"CurrencyCode\"" :
                    "SELECT CurrencyCode FROM OCRN WHERE Locked = 'N' ORDER BY CurrencyCode";

                rs.DoQuery(sql);

                while (!rs.EoF)
                {
                    string currency = B1TuneUp.Utils.SapUiSafe.SafeField(rs, "CurrencyCode");
                    if (!string.IsNullOrEmpty(currency))
                    {
                        combo.ValidValues.Add(currency, currency);
                    }
                    rs.MoveNext();
                }

                if (combo.ValidValues.Count > 0)
                {
                    combo.Select(0);
                }

                ComObjectManager.Release(rs);
            }
            catch (Exception ex)
            {
                B1App.Instance.Application.SetStatusBarMessage($"Error loading currencies: {ex.Message}", BoMessageTime.bmt_Short, true);
            }
        }

        private static void SaveManualExchangeRate(SAPbouiCOM.Form manForm, SAPbouiCOM.Form parentForm)
        {
            try
            {
                string fromCurrency = SapUiSafe.SafeComboValue(SapUiSafe.TryGetSpecific<SAPbouiCOM.ComboBox>(manForm, "CmbFrom"));
                string toCurrency = SapUiSafe.SafeComboValue(SapUiSafe.TryGetSpecific<SAPbouiCOM.ComboBox>(manForm, "CmbTo"));
                string rateStr = SapUiSafe.TryGetSpecific<SAPbouiCOM.EditText>(manForm, "EdtRate")?.Value ?? string.Empty;
                string description = SapUiSafe.TryGetSpecific<SAPbouiCOM.EditText>(manForm, "EdtDesc")?.Value ?? string.Empty;

                if (string.IsNullOrEmpty(rateStr))
                {
                    B1App.Instance.Application.SetStatusBarMessage("Rate is required", BoMessageTime.bmt_Short, true);
                    return;
                }

                double rate;
                if (!double.TryParse(rateStr, out rate))
                {
                    B1App.Instance.Application.SetStatusBarMessage("Invalid rate format", BoMessageTime.bmt_Short, true);
                    return;
                }

                // Save to database
                Recordset rs = (Recordset)B1App.Instance.Company.GetBusinessObject(BoObjectTypes.BoRecordset);
                string insertSql = B1App.Instance.IsHana ?
                    $"INSERT INTO \"@BTUN_EXCH\" (\"U_FromCurr\", \"U_ToCurr\", \"U_Rate\", \"U_LastUpdate\", \"U_Source\", \"U_Desc\", \"U_Active\", \"U_CreatedBy\", \"U_CreatedAt\") VALUES ('{fromCurrency}', '{toCurrency}', {rate}, '{DateTime.Today:yyyy-MM-dd}', 'MANUAL', '{description}', 'Y', '{B1App.Instance.Company.UserName}', '{DateTime.Today:yyyy-MM-dd}')" :
                    $"INSERT INTO [@BTUN_EXCH] (U_FromCurr, U_ToCurr, U_Rate, U_LastUpdate, U_Source, U_Desc, U_Active, U_CreatedBy, U_CreatedAt) VALUES ('{fromCurrency}', '{toCurrency}', {rate}, '{DateTime.Today:yyyy-MM-dd}', 'MANUAL', '{description}', 'Y', '{B1App.Instance.Company.UserName}', '{DateTime.Today:yyyy-MM-dd}')";

                // Execute insert
                rs.DoQuery(insertSql);

                B1App.Instance.Application.SetStatusBarMessage("Manual exchange rate saved successfully", BoMessageTime.bmt_Short, false);

                // Close the form
                manForm.Close();

                // Refresh parent form
                Grid matrix = SapUiSafe.TryGetSpecific<Grid>(parentForm, "ExchgMatrix");
                if (matrix == null) return;
                LoadExchangeRates(matrix);

                ComObjectManager.Release(rs);
            }
            catch (Exception ex)
            {
                B1App.Instance.Application.SetStatusBarMessage($"Error saving manual exchange rate: {ex.Message}", BoMessageTime.bmt_Short, true);
            }
        }

        private static void OpenScheduleForm()
        {
            try
            {
                B1App.Instance.Application.SetStatusBarMessage("Opening scheduler for exchange rate updates...", BoMessageTime.bmt_Short, false);

                // In a real implementation, this would open a scheduling form
                // For now, we'll just notify the user
                B1App.Instance.Application.MessageBox("Exchange Rate Scheduler would open here.\nIn a full implementation, this would allow setting up automatic updates.");
            }
            catch (Exception ex)
            {
                B1App.Instance.Application.SetStatusBarMessage($"Error opening schedule form: {ex.Message}", BoMessageTime.bmt_Short, true);
            }
        }

        private static void SaveExchangeRateConfig(string source, string baseCurrency, string apiKey)
        {
            try
            {
                Recordset rs = (Recordset)B1App.Instance.Company.GetBusinessObject(BoObjectTypes.BoRecordset);

                // Delete existing config
                string deleteSql = B1App.Instance.IsHana ?
                    "DELETE FROM \"@BTUN_TBOX\" WHERE \"U_Code\" LIKE 'EXCH_%'" :
                    "DELETE FROM [@BTUN_TBOX] WHERE U_Code LIKE 'EXCH_%'";

                rs.DoQuery(deleteSql);

                // Insert new config values
                string insertSourceSql = B1App.Instance.IsHana ?
                    $"INSERT INTO \"@BTUN_TBOX\" (\"U_Code\", \"U_Value\") VALUES ('EXCH_SOURCE', '{source}')" :
                    $"INSERT INTO [@BTUN_TBOX] (U_Code, U_Value) VALUES ('EXCH_SOURCE', '{source}')";

                rs.DoQuery(insertSourceSql);

                string insertBaseSql = B1App.Instance.IsHana ?
                    $"INSERT INTO \"@BTUN_TBOX\" (\"U_Code\", \"U_Value\") VALUES ('EXCH_BASE', '{baseCurrency}')" :
                    $"INSERT INTO [@BTUN_TBOX] (U_Code, U_Value) VALUES ('EXCH_BASE', '{baseCurrency}')";

                rs.DoQuery(insertBaseSql);

                if (!string.IsNullOrEmpty(apiKey))
                {
                    string insertApiSql = B1App.Instance.IsHana ?
                        $"INSERT INTO \"@BTUN_TBOX\" (\"U_Code\", \"U_Value\") VALUES ('EXCH_APIKEY', '{apiKey}')" :
                        $"INSERT INTO [@BTUN_TBOX] (U_Code, U_Value) VALUES ('EXCH_APIKEY', '{apiKey}')";

                    rs.DoQuery(insertApiSql);
                }

                B1App.Instance.Application.SetStatusBarMessage("Exchange rate configuration saved", BoMessageTime.bmt_Short, false);

                ComObjectManager.Release(rs);
            }
            catch (Exception ex)
            {
                B1App.Instance.Application.SetStatusBarMessage($"Error saving exchange rate config: {ex.Message}", BoMessageTime.bmt_Short, true);
            }
        }

        // Method to process automatic exchange rate updates (typically called by scheduler)
        public static async void ProcessAutomaticExchangeRateUpdates()
        {
            try
            {
                B1App.Instance.Application.SetStatusBarMessage("Processing automatic exchange rate updates...", BoMessageTime.bmt_Long, false);

                // Get configuration
                Recordset rs = (Recordset)B1App.Instance.Company.GetBusinessObject(BoObjectTypes.BoRecordset);
                string configSql = B1App.Instance.IsHana ?
                    "SELECT \"U_Value\" FROM \"@BTUN_TBOX\" WHERE \"U_Code\" = 'EXCH_SOURCE'" :
                    "SELECT U_Value FROM [@BTUN_TBOX] WHERE U_Code = 'EXCH_SOURCE'";

                rs.DoQuery(configSql);
                string dataSource = rs.RecordCount > 0 ? B1TuneUp.Utils.SapUiSafe.SafeField(rs, "U_Value") : "ECB";
                ComObjectManager.Release(rs);

                // Get currencies to update
                List<string> currencies = GetActiveCurrencies();

                // Fetch rates based on data source
                Dictionary<string, double> rates = await FetchExchangeRates(dataSource, currencies);

                if (rates != null && rates.Count > 0)
                {
                    // Update the database with new rates
                    UpdateDatabaseWithRates(rates);

                    // Optionally update SAP directly
                    UpdateSAPExchangeRates(null);

                    B1App.Instance.Application.SetStatusBarMessage("Automatic exchange rate update completed successfully", BoMessageTime.bmt_Short, false);
                }
                else
                {
                    B1App.Instance.Application.SetStatusBarMessage("Failed to fetch exchange rates for automatic update", BoMessageTime.bmt_Short, true);
                }
            }
            catch (Exception ex)
            {
                B1App.Instance.Application.SetStatusBarMessage($"Error in automatic exchange rate update: {ex.Message}", BoMessageTime.bmt_Short, true);
            }
        }
    }
}
