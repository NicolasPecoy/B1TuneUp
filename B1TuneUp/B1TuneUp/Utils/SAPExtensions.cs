using System;

namespace SAPbouiCOM
{
    public static class SAPExtensions
    {
        public static bool IsNameExists(this DataColumns cols, string name)
        {
            try { var _ = cols.Item(name); return true; } catch { return false; }
        }

        public static bool Exists(this Items items, string id)
        {
            try { var _ = items.Item(id); return true; } catch { return false; }
        }

        public static bool Exists(this Columns cols, string id)
        {
            try { var _ = cols.Item(id); return true; } catch { return false; }
        }

        public static int Item(this ISelectedRows rows, int index)
        {
            return rows.Item(index, BoOrderType.ot_SelectionOrder);
        }
    }
}
