namespace B1TuneUp.Models
{
    public class AuthSimulationResult
    {
        public string UserCode { get; set; }
        public string ObjectCode { get; set; }
        public string ObjectType { get; set; }
        public bool Allowed { get; set; }
        public string Detail { get; set; }
    }
}
