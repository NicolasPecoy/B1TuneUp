using SAPbobsCOM;

namespace B1TuneUp.Models
{
    public class MetadataDefinitionEntry
    {
        public string TableName { get; set; }
        public string TableDescription { get; set; }
        public string FieldName { get; set; }
        public string FieldDescription { get; set; }
        public BoFieldTypes FieldType { get; set; }
        public int Size { get; set; }
        public string DefaultValue { get; set; }
        public string ValidValues { get; set; }
        public bool IsTable { get; set; }
        public bool Exists { get; set; }
        public string Status => Exists ? "OK" : "Missing";
        public string DisplayName => IsTable ? TableName : $"{TableName}.{FieldName}";
    }
}
