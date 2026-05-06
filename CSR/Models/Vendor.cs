using System;
using System.ComponentModel.DataAnnotations;

namespace CSR.Models
{
    public class Vendor
    {
        [Display(Name = "업체 ID")]
        public int VENDORID { get; set; }
        
        [Display(Name = "업체 코드")]
        public string? VENDOR_CODE { get; set; }

        [Required]
        [Display(Name = "업체명")]
        public string VENDOR_NAME { get; set; }
        
        [Display(Name = "업체 구분")]
        public string? VENDOR_TYPE { get; set; }

        [Display(Name = "사업자등록번호")]
        public string? BIZ_REG_NO { get; set; }

        [Display(Name = "업태")]
        public string? BIZ_TYPE { get; set; }

        [Display(Name = "종목")]
        public string? BIZ_ITEM { get; set; }

        [Display(Name = "대표자명")]
        public string? CEO_NAME { get; set; }

        [Display(Name = "대표전화번호")]
        public string? TEL_NO { get; set; }

        [Display(Name = "대표이메일")]
        public string? EMAIL { get; set; }

        [Display(Name = "회사주소")]
        public string? ADDRESS { get; set; }

        [Display(Name = "담당자명")]
        public string? MANAGER_NAME { get; set; }

        [Display(Name = "담당자 연락처")]
        public string? MANAGER_PHONE { get; set; }

        [Display(Name = "담당자 이메일")]
        public string? MANAGER_EMAIL { get; set; }

        [Display(Name = "거래상태")]
        public string? STATUS { get; set; }

        [Display(Name = "비고")]
        public string? REMARKS { get; set; }

        [Required]
        [Display(Name = "사용여부")]
        public string USEYN { get; set; } = "Y";

        [Display(Name = "등록일")]
        public DateTime REG_DATE { get; set; }

        [Required]
        [Display(Name = "등록자ID")]
        public string REG_USERID { get; set; }

        [Display(Name = "수정일")]
        public DateTime? UPDATE_DATE { get; set; }

        [Display(Name = "수정자ID")]
        public string? UPDATE_USERID { get; set; }
    }
}
