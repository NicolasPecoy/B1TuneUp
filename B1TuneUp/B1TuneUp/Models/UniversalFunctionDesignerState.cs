namespace B1TuneUp.Models
{
    public class UniversalFunctionDesignerState
    {
        public string HttpUrl { get; set; }
        public string HttpMethod { get; set; } = "GET";
        public string HttpHeaders { get; set; }
        public string HttpAuthMode { get; set; } = "None";
        public string HttpAuthUser { get; set; }
        public string HttpAuthSecret { get; set; }
        public string HttpBody { get; set; }
        public string HttpTimeoutSeconds { get; set; } = "60";
        public string DiObjectType { get; set; }
        public string DiMode { get; set; } = "Add";
        public string DiKey { get; set; }
        public string DiFieldsJson { get; set; } = "{}";
        public string DiUserFieldsJson { get; set; } = "{}";
        public string EmailTemplateCode { get; set; }
        public string ReportCode { get; set; }
        public string ReportParameters { get; set; }
        public string FilePath { get; set; }
        public string FileAction { get; set; } = "Open";
        public string FileTarget { get; set; }
        public string FileContent { get; set; }
        public string MatrixId { get; set; }
        public string LoopMacro { get; set; }
        public string ResultVariable { get; set; }
        public string OnSuccess { get; set; }
        public string OnError { get; set; }
        public string Condition { get; set; }
    }
}
