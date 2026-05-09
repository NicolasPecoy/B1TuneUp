using System;
using System.CodeDom.Compiler;
using System.Reflection;
using Microsoft.CSharp;
using SAPbouiCOM;
using SAPbobsCOM;
using B1TuneUp.Core;
using B1TuneUp.Utils;
using Company = SAPbobsCOM.Company;

namespace B1TuneUp.Modules
{
    public static class DynamicCodeEngine
    {
        public static void RunCode(string codeName, Form oForm = null)
        {
            Recordset rs = (Recordset)B1App.Instance.Company.GetBusinessObject(BoObjectTypes.BoRecordset);
            try
            {
                string sql = B1App.Instance.IsHana
                    ? $"SELECT \"U_Source\" FROM \"@BTUN_CODE\" WHERE \"U_CodeName\" = '{codeName}'"
                    : $"SELECT [U_Source] FROM [@BTUN_CODE] WHERE [U_CodeName] = '{codeName}'";
                
                rs.DoQuery(sql);
                if (rs.RecordCount > 0)
                {
                    string source = B1TuneUp.Utils.SapUiSafe.SafeField(rs, 0);
                    Execute(source, oForm);
                }
            }
            catch (Exception ex)
            {
                B1App.Instance.Application.SetStatusBarMessage($"Error cargando código dinámico {codeName}: {ex.Message}", BoMessageTime.bmt_Short, true);
            }
            finally
            {
                ComObjectManager.Release(rs);
            }
        }

        private static void Execute(string source, Form oForm)
        {
            CSharpCodeProvider provider = new CSharpCodeProvider();
            CompilerParameters parameters = new CompilerParameters();

            // Referencias básicas necesarias
            parameters.ReferencedAssemblies.Add("System.dll");
            parameters.ReferencedAssemblies.Add("System.Core.dll");
            parameters.ReferencedAssemblies.Add("System.Data.dll");
            parameters.ReferencedAssemblies.Add("System.Windows.Forms.dll");
            
            // Referencias de SAP B1 (Usando el path de la DLL si es necesario o cargadas en el dominio)
            parameters.ReferencedAssemblies.Add(typeof(Application).Assembly.Location); // SAPbouiCOM
            parameters.ReferencedAssemblies.Add(typeof(Company).Assembly.Location);     // SAPbobsCOM
            
            parameters.GenerateInMemory = true;

            string fullSource = @"
                using System;
                using SAPbouiCOM;
                using SAPbobsCOM;

                namespace B1TuneUp.Dynamic
                {
                    public class Script
                    {
                        public void Execute(Application SBO_Application, Company oCompany, Form oForm)
                        {
                            " + source + @"
                        }
                    }
                }";

            CompilerResults results = provider.CompileAssemblyFromSource(parameters, fullSource);

            if (results.Errors.HasErrors)
            {
                string errors = "Errores de compilación C#:\n";
                foreach (CompilerError err in results.Errors)
                {
                    errors += $"Línea {err.Line}: {err.ErrorText}\n";
                }
                B1App.Instance.Application.MessageBox(errors);
                return;
            }

            Assembly assembly = results.CompiledAssembly;
            object instance = assembly.CreateInstance("B1TuneUp.Dynamic.Script");
            MethodInfo method = instance.GetType().GetMethod("Execute");
            
            method.Invoke(instance, new object[] { B1App.Instance.Application, B1App.Instance.Company, oForm });
        }
    }
}
