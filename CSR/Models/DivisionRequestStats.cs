namespace CSR.Models
{
    public class DivisionRequestStats
    {
        public string UserName { get; set; }
        public string DivisionName { get; set; }
        public int VeryHighCount { get; set; }
        public int HighCount { get; set; }
        public int MediumCount { get; set; }
        public int LowCount { get; set; }
        public int VeryLowCount { get; set; }
        public int TotalCount { get; set; }
        public int EndStatus { get; set; }
        public int IngStatus { get; set; }
        public int EndPercent { get; set; }
    }
}