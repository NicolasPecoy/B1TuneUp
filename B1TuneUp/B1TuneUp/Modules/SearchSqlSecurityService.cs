using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace B1TuneUp.Modules
{
    public static class SearchSqlSecurityService
    {
        private static readonly string[] ForbiddenTokens =
        {
            "INSERT", "UPDATE", "DELETE", "DROP", "ALTER", "CREATE", "TRUNCATE", "MERGE", "EXEC", "EXECUTE",
            "CALL", "GRANT", "REVOKE", "COMMIT", "ROLLBACK", "UNION ALL SELECT"
        };

        public static void ValidateSelectOnly(string sql)
        {
            if (string.IsNullOrWhiteSpace(sql)) throw new InvalidOperationException("La consulta de busqueda esta vacia.");
            string normalized = Regex.Replace(sql, @"\s+", " ").Trim();
            if (!normalized.StartsWith("SELECT ", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("B1 Search solo permite consultas SELECT.");
            }
            if (normalized.Contains(";") || normalized.Contains("--") || normalized.Contains("/*") || normalized.Contains("*/"))
            {
                throw new InvalidOperationException("B1 Search no permite separadores ni comentarios SQL.");
            }
            if (ForbiddenTokens.Any(t => Regex.IsMatch(normalized, @"\b" + Regex.Escape(t) + @"\b", RegexOptions.IgnoreCase)))
            {
                throw new InvalidOperationException("B1 Search detecto una palabra reservada no permitida.");
            }
        }

        public static string ApplyServerPaging(string sql, int page, int pageSize, bool isHana)
        {
            ValidateSelectOnly(sql);
            if (sql.IndexOf("{offset}", StringComparison.OrdinalIgnoreCase) >= 0 ||
                sql.IndexOf("{limit}", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return sql;
            }
            int offset = Math.Max(0, page * pageSize);
            int limit = Math.Max(1, pageSize);
            if (Regex.IsMatch(sql, @"\bTOP\s+\d+\b", RegexOptions.IgnoreCase) ||
                Regex.IsMatch(sql, @"\bLIMIT\s+\d+\b", RegexOptions.IgnoreCase) ||
                Regex.IsMatch(sql, @"\bOFFSET\s+\d+\b", RegexOptions.IgnoreCase))
            {
                return sql;
            }
            if (isHana) return sql + " LIMIT " + limit + " OFFSET " + offset;
            string orderBy = Regex.IsMatch(sql, @"\bORDER\s+BY\b", RegexOptions.IgnoreCase) ? string.Empty : " ORDER BY 1";
            return sql + orderBy + " OFFSET " + offset + " ROWS FETCH NEXT " + limit + " ROWS ONLY";
        }
    }
}
