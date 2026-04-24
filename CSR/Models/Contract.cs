using System;
using System.ComponentModel.DataAnnotations;

namespace CSR.Models
{
    public class Contract
    {
        [Display(Name = "계약 ID")]
        public int CONTRACT_ID { get; set; }

        [Required(ErrorMessage = "업체를 선택해주세요.")]
        [Display(Name = "업체")]
        public int? VENDORID { get; set; }

        [Required(ErrorMessage = "계약연도를 입력해주세요.")]
        [Display(Name = "계약연도")]
        public int CONTRACT_YEAR { get; set; } = DateTime.Now.Year;

        [Required(ErrorMessage = "계약시작일을 입력해주세요.")]
        [Display(Name = "계약시작일")]
        [DataType(DataType.Date)]
        public DateTime CONTRACT_START { get; set; }

        [Required(ErrorMessage = "계약종료일을 입력해주세요.")]
        [Display(Name = "계약종료일")]
        [DataType(DataType.Date)]
        public DateTime CONTRACT_END { get; set; }

        [Display(Name = "계약기간(년)")]
        public int? CONTRACT_YEARS { get; set; }

        [Display(Name = "계약기간(개월)")]
        public int? CONTRACT_MONTHS { get; set; }

        [Required(ErrorMessage = "구분을 선택해주세요.")]
        [Display(Name = "구분")]
        public string CONTRACT_TYPE { get; set; }

        [Display(Name = "계약내역")]
        public string? CONTRACT_DESC { get; set; }

        [Required]
        [Display(Name = "사용여부")]
        public string USEYN { get; set; } = "Y";

        [Display(Name = "등록일")]
        public DateTime? REG_DATE { get; set; }

        [Display(Name = "등록자ID")]
        public string? REG_USERID { get; set; }

        [Display(Name = "수정일")]
        public DateTime? UPDATE_DATE { get; set; }

        [Display(Name = "수정자ID")]
        public string? UPDATE_USERID { get; set; }

        // 조회용 (JOIN)
        public string? VENDOR_NAME { get; set; }
        public string? VENDOR_CODE { get; set; }
    }
}
