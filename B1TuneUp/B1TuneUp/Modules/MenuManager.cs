using System;
using System.Collections.Generic;
using SAPbouiCOM;
using B1TuneUp.Core;
using B1TuneUp.Modules;

namespace B1TuneUp.Modules
{
    public class MenuManager
    {
        private static Dictionary<string, string> _menuActions = new Dictionary<string, string>();

        public static void LoadCustomMenus()
        {
            SAPbobsCOM.Recordset rs = (SAPbobsCOM.Recordset)B1App.Instance.Company.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);
            try
            {
                string sql = B1App.Instance.IsHana
                    ? "SELECT * FROM \"@BTUN_MENUS\" ORDER BY \"U_Position\" ASC"
                    : "SELECT * FROM [@BTUN_MENUS] ORDER BY [U_Position] ASC";
                
                rs.DoQuery(sql);
                while (!rs.EoF)
                {
                    string parentId = rs.Fields.Item("U_ParentID").Value.ToString();
                    string menuId = rs.Fields.Item("U_MenuID").Value.ToString();
                    string name = rs.Fields.Item("U_Name").Value.ToString();
                    int pos = (int)rs.Fields.Item("U_Position").Value;
                    string action = rs.Fields.Item("U_Action").Value.ToString();

                    AddMenuItem(parentId, menuId, name, pos);
                    _menuActions[menuId] = action;

                    rs.MoveNext();
                }
            }
            catch (Exception ex)
            {
                B1App.Instance.Application.SetStatusBarMessage($"Error cargando menús: {ex.Message}", BoMessageTime.bmt_Short, true);
            }
            finally
            {
                Utils.ComObjectManager.Release(rs);
            }
        }

        private static void AddMenuItem(string parentId, string menuId, string name, int position)
        {
            try
            {
                Menus menus = B1App.Instance.Application.Menus;
                if (!menus.Exists(menuId))
                {
                    MenuItem parentMenu = menus.Item(parentId);
                    MenuCreationParams creationParams = (MenuCreationParams)B1App.Instance.Application.CreateObject(BoCreatableObjectType.cot_MenuCreationParams);
                    creationParams.Type = BoMenuType.mt_STRING;
                    creationParams.UniqueID = menuId;
                    creationParams.String = name;
                    creationParams.Position = position;
                    parentMenu.SubMenus.AddEx(creationParams);
                }
            }
            catch (Exception ex)
            {
                B1App.Instance.Application.SetStatusBarMessage($"Error creando menú {menuId}: {ex.Message}", BoMessageTime.bmt_Short, true);
            }
        }

        public static void HandleMenuEvent(ref MenuEvent pVal)
        {
            if (!pVal.BeforeAction && _menuActions.ContainsKey(pVal.MenuUID))
            {
                MacroEngine.ExecuteMacro(_menuActions[pVal.MenuUID]);
            }
        }
    }
}
