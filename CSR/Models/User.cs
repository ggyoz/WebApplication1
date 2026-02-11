using System; // For DateTime
using System.ComponentModel.DataAnnotations; // Added for [Display]
using System.Collections.Generic;

namespace CSR.Models
{
    public class User
    {
        [Display(Name = "사용자 ID")]
        public string? UserId { get; set; } = string.Empty;

        [Display(Name = "비밀번호")]
        public string? UserPwd { get; set; } = string.Empty;

        [Display(Name = "사용자 이름")]
        public string? UserName { get; set; } = string.Empty;

        [Display(Name = "사원 번호")]
        public string? EmpNo { get; set; } = string.Empty;

        [Display(Name = "법인 코드")]
        public string? CorCd { get; set; } = string.Empty;

        [Display(Name = "부서 코드")]
        public string? DeptCd { get; set; } = string.Empty;

        [Display(Name = "실 코드")]
        public string? OfficeCd { get; set; } = string.Empty;

        [Display(Name = "팀 코드")]
        public string? TeamCd { get; set; } = string.Empty;

        [Display(Name = "시스템 코드")]
        public string? SysCd { get; set; } = string.Empty;

        [Display(Name = "사업장 코드")]
        public string? BizCd { get; set; } = string.Empty;

        [Display(Name = "전화 번호")]
        public string? TelNo { get; set; } = string.Empty;

        [Display(Name = "휴대폰 번호")]
        public string? MobPhoneNo { get; set; } = string.Empty;

        [Display(Name = "전자메일 주소")]
        public string? EmailAddr { get; set; } = string.Empty;

        [Display(Name = "계정 상태")]
        public string? UserStat { get; set; } = string.Empty;

        [Display(Name = "퇴사일")]
        public DateTime? RetireDate { get; set; }

        [Display(Name = "관리자 권한")]
        public bool AdminFlag { get; set; } = false;

        [Display(Name = "고객사 코드")]
        public string? CustCd { get; set; } = string.Empty;

        [Display(Name = "협력사 코드")]
        public string? VendCd { get; set; } = string.Empty;

        [Display(Name = "메뉴 부여 권한")]
        public int AuthFlag { get; set; } = 0;

        [Display(Name = "사용자 구분")]
        public string? UserDiv { get; set; } = string.Empty;

        [Display(Name = "로그인 실패 횟수")]
        public int PwMissCount { get; set; } = 0;

        [Display(Name = "등록일")]
        public DateTime RegDate { get; set; }

        [Display(Name = "등록자 ID")]
        public string RegUserId { get; set; } = string.Empty;

        [Display(Name = "수정일")]
        public DateTime? UpdateDate { get; set; }

        [Display(Name = "수정자 ID")]
        public string UpdateUserId { get; set; } = string.Empty;
        
        [Display(Name = "사용 여부")]
        public string UseYn { get; set; } = "Y";

        // Properties from joined tables
        public string? CorpName { get; set; }
        public string? DeptName { get; set; }
        public string? OfficeName { get; set; }
        public string? TeamName { get; set; }
        public List<string> AssignedResponsibilities { get; set; } = new List<string>();
    }
}