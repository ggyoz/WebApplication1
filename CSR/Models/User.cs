using Newtonsoft.Json;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
using System; // For DateTime
using System.ComponentModel.DataAnnotations; // Added for [Display]

namespace CSR.Models
{
    [Table("TB_USER_INFO")]
    public class User : BaseModel
    {
        [PrimaryKey("USERID", false)] // Oracle VARCHAR2 PK, so false for autoincrement
        [Display(Name = "사용자 ID")]
        public string? UserId { get; set; } = string.Empty;

        [Column("USERPWD")]
        [Display(Name = "비밀번호")]
        public string? UserPwd { get; set; } = string.Empty;

        [Column("USERNAME")]
        [Display(Name = "사용자 이름")]
        public string? UserName { get; set; } = string.Empty;

        [Column("EMPNO")]
        [Display(Name = "사원 번호")]
        public string? EmpNo { get; set; } = string.Empty;

        [Column("CORCD")]
        [Display(Name = "법인 코드")]
        public string? CorCd { get; set; } = string.Empty;

        [Column("DEPTCD")]
        [Display(Name = "부서 코드")]
        public string? DeptCd { get; set; } = string.Empty;

        [Column("OFFICECD")] // NEW
        [Display(Name = "실 코드")]
        public string? OfficeCd { get; set; } = string.Empty;

        [Column("TEAMCD")]
        [Display(Name = "팀 코드")]
        public string? TeamCd { get; set; } = string.Empty;

        [Column("SYSCD")]
        [Display(Name = "시스템 코드")]
        public string? SysCd { get; set; } = string.Empty;

        [Column("BIZCD")]
        [Display(Name = "사업장 코드")]
        public string? BizCd { get; set; } = string.Empty;

        [Column("TELNO")]
        [Display(Name = "전화 번호")]
        public string? TelNo { get; set; } = string.Empty;

        [Column("MOB_PHONE_NO")]
        [Display(Name = "휴대폰 번호")]
        public string? MobPhoneNo { get; set; } = string.Empty;

        [Column("EMAIL_ADDR")]
        [Display(Name = "전자메일 주소")]
        public string? EmailAddr { get; set; } = string.Empty;

        [Column("USERSTAT")] // NEW
        [Display(Name = "계정 상태")]
        public string? UserStat { get; set; } = string.Empty;

        [Column("RETIRE_DATE")]
        [Display(Name = "퇴사일")]
        public DateTime? RetireDate { get; set; } // Changed from string to DateTime?

        [Column("ADMIN_FLAG")]
        [Display(Name = "관리자 권한")]
        public bool AdminFlag { get; set; } = false;

        [Column("CUSTCD")]
        [Display(Name = "고객사 코드")]
        public string? CustCd { get; set; } = string.Empty;

        [Column("VENDCD")]
        [Display(Name = "협력사 코드")]
        public string? VendCd { get; set; } = string.Empty;

        [Column("AUTH_FLAG")]
        [Display(Name = "메뉴 부여 권한")]
        public int AuthFlag { get; set; } = 0;

        [Column("USER_DIV")]
        [Display(Name = "사용자 구분")]
        public string? UserDiv { get; set; } = string.Empty;

        [Column("PW_MISS_COUNT")]
        [Display(Name = "로그인 실패 횟수")]
        public int PwMissCount { get; set; } = 0;

        [Column("REG_DATE")]
        [Display(Name = "등록일")]
        public DateTime RegDate { get; set; } // Changed from DateTime? to DateTime (NOT NULL)

        [Column("REG_USERID")]
        [Display(Name = "등록자 ID")]
        public string RegUserId { get; set; } = string.Empty;

        [Column("UPDATE_DATE")]
        [Display(Name = "수정일")]
        public DateTime? UpdateDate { get; set; } // Nullable DateTime

        [Column("UPDATE_USERID")]
        [Display(Name = "수정자 ID")]
        public string UpdateUserId { get; set; } = string.Empty;
        
        [Column("USEYN")] // NEW
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