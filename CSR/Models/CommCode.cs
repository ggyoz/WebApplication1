using System;
using System.ComponentModel.DataAnnotations;

namespace CSR.Models
{
    /// <summary>
    /// Represents a common code item.
    /// The properties are mapped from the TB_COMM_CODE table in the Oracle database.
    /// </summary>
    public class CommCode
    {
        // Columns from TB_COMM_CODE
        [Display(Name = "코드 ID")]
        public int CODEID { get; set; }

        [Display(Name = "부모 ID")]
        [Required(ErrorMessage = "부모 ID는 필수입니다.")]
        public int PARENTID { get; set; }

        [Display(Name = "이름")]
        [Required(ErrorMessage = "이름은 필수입니다.")]
        [StringLength(20, ErrorMessage = "이름은 20자를 넘을 수 없습니다.")]
        public string CODENM { get; set; }

        [Display(Name = "코드")]
        [StringLength(10, ErrorMessage = "코드는 10자를 넘을 수 없습니다.")]
        public string? CODE { get; set; }

        [Display(Name = "정렬 순서")]
        public int? SORTORDER { get; set; }

        [Display(Name = "설명")]
        [StringLength(1000, ErrorMessage = "설명은 1000자를 넘을 수 없습니다.")]
        public string? NOTE { get; set; }

        [Display(Name = "사용 여부")]
        [Required(ErrorMessage = "사용 여부는 필수입니다.")]
        [StringLength(1, MinimumLength = 1, ErrorMessage = "사용 여부는 'Y' 또는 'N'이어야 합니다.")]
        public string USEYN { get; set; } = "Y";

        [Display(Name = "등록일")]
        public DateTime? REG_DATE { get; set; }

        [Display(Name = "등록자")]
        public string? REG_USERID { get; set; }

        [Display(Name = "수정일")]
        public DateTime? UPDATE_DATE { get; set; }

        [Display(Name = "수정자")]
        public string? UPDATE_USERID { get; set; }

        // Navigation properties
        public List<CommCode>? Children { get; set; }
        public CommCode? Parent { get; set; }
    }
}
