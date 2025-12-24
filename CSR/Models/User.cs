using Newtonsoft.Json;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace CSR.Models
{
    [Table("TB_USER_INFO")]
    public class User : BaseModel
    {
        [PrimaryKey("USERID", false)] // Oracle VARCHAR2 PK, so false for autoincrement
        public string UserId { get; set; } = string.Empty;

        [Column("USERPWD")]
        public string UserPwd { get; set; } = string.Empty;

        [Column("USERNAME")]
        public string UserName { get; set; } = string.Empty;

        [Column("EMPNO")]
        public string EmpNo { get; set; } = string.Empty;

        [Column("DEPTCD")]
        public string DeptCd { get; set; } = string.Empty;

        [Column("DEPTNAME")]
        public string DeptName { get; set; } = string.Empty;

        [Column("TEAMCD")]
        public string TeamCd { get; set; } = string.Empty;

        [Column("TEAMNAME")]
        public string TeamName { get; set; } = string.Empty;

        [Column("CORCD")]
        public string CorCd { get; set; } = string.Empty;

        [Column("SYSCD")]
        public string SysCd { get; set; } = string.Empty;

        [Column("BIZCD")]
        public string BizCd { get; set; } = string.Empty;

        [Column("TELNO")]
        public string TelNo { get; set; } = string.Empty;

        [Column("MOB_PHONE_NO")]
        public string MobPhoneNo { get; set; } = string.Empty;

        [Column("EMAIL_ADDR")]
        public string EmailAddr { get; set; } = string.Empty;

        [Column("RETIRE_DATE")]
        public string RetireDate { get; set; } = string.Empty; // Assuming this is stored as a string like 'YYYY-MM-DD'

        [Column("CORNAME")]
        public string CorName { get; set; } = string.Empty;

        [Column("SYSNAME")]
        public string SysName { get; set; } = string.Empty;

        [Column("BIZNAME")]
        public string BizName { get; set; } = string.Empty;

        [Column("ADMIN_FLAG")]
        public bool AdminFlag { get; set; } = false;

        [Column("CUSTCD")]
        public string CustCd { get; set; } = string.Empty;

        [Column("VENDCD")]
        public string VendCd { get; set; } = string.Empty;

        [Column("AUTH_FLAG")]
        public int AuthFlag { get; set; } = 0;

        [Column("USER_DIV")]
        public string UserDiv { get; set; } = string.Empty;

        [Column("PW_MISS_COUNT")]
        public int PwMissCount { get; set; } = 0;

        [Column("REG_DATE")]
        public DateTime? RegDate { get; set; } // Nullable DateTime

        [Column("REG_USERID")]
        public string RegUserId { get; set; } = string.Empty;

        [Column("UPDATE_DATE")]
        public DateTime? UpdateDate { get; set; } // Nullable DateTime

        [Column("UPDATE_USERID")]
        public string UpdateUserId { get; set; } = string.Empty;
    }
}