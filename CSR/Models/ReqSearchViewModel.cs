using System;

namespace CSR.Models
{
    public class ReqSearchViewModel
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? ReqTypeCd { get; set; }
        public DateTime? ReqDate { get; set; }
        public DateTime? DueDate { get; set; }
        public DateTime? ExpectDate { get; set; }
        public string? ProcStatusCd { get; set; }
        public string? PriorityCd { get; set; }
        public string? RegId { get; set; }
        public string? ReqUserName { get; set; }
        public string? ResUserName { get; set; }
        public string? SearchValue { get; set; } // for title search
        public string? CorCd {get; set;}
        public string? DeptCd {get; set;}
        public string? OfficeCd {get; set;}
        public string? TeamCd {get; set;}
        public List<string> AssignedResponsibilities { get; set; } = new List<string>();


    }
}
