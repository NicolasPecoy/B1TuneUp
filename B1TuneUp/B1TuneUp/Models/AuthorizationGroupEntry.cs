namespace B1TuneUp.Models
{
    public class AuthorizationGroupEntry
    {
        public string Code { get; set; }
        public string Name { get; set; }
        public string Users { get; set; }
        public string Notes { get; set; }

        public AuthorizationGroupEntry Clone()
        {
            return (AuthorizationGroupEntry)MemberwiseClone();
        }
    }
}
