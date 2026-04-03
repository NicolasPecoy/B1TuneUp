namespace B1TuneUp.Models
{
    public class MandatoryFieldEntry
    {
        public string Code { get; set; }
        public string Name { get; set; }
        public string FormType { get; set; }
        public string ItemId { get; set; }
        public string ColumnId { get; set; }
        public string Condition { get; set; }
        public string ErrorMessage { get; set; }
        public bool Active { get; set; } = true;

        public MandatoryFieldEntry Clone()
        {
            return (MandatoryFieldEntry)MemberwiseClone();
        }
    }
}
