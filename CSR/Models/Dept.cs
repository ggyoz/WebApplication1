using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CSR.Models
{
    public class Dept
    {
        // Columns from TB_DEPT_INFO
        // Using long for DEPTID and PARENTID as they are NUMBER in Oracle.
        public long DeptId { get; set; }
        public string? DeptCd { get; set; }
        public long? ParentId { get; set; }
        
        [Required]
        public string DeptName { get; set; } = string.Empty;

        [Required]
        public string CorCd { get; set; } = string.Empty;

        public int? SortOrder { get; set; }
        public string? Note { get; set; }
        
        [Required]
        public string UseYn { get; set; } = "Y";

        // --- Helper Properties for Tree View ---
        public int DeptLevel { get; set; }
        public List<Dept>? Children { get; set; }
        public Dept? Parent { get; set; }
        public int ChildCount { get; set; }
    }
}
