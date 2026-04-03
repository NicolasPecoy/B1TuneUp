namespace B1TuneUp.Models
{
    public class FormSettingEntry
    {
        public int DocEntry { get; set; }
        public string FormType { get; set; }
        public string UserCode { get; set; }
        public string Data { get; set; }

        public FormSettingEntry Clone() => (FormSettingEntry)MemberwiseClone();
    }
}
