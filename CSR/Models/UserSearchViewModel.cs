using System.ComponentModel.DataAnnotations;

namespace CSR.Models
{
    public class UserSearchViewModel
    {
        [Display(Name = "사용자 ID")]
        public string? UserId { get; set; }

        [Display(Name = "사용자 이름")]
        public string? UserName { get; set; }

        [Display(Name = "법인코드")]
        public string? CorCd { get; set; }

        [Display(Name = "사업부코드")]
        public string? BizCd { get; set; }

        [Display(Name = "부서코드")]
        public string? DeptCd { get; set; }
        
        [Display(Name = "팀코드")]
        public string? TeamCd { get; set; }
    }
}
