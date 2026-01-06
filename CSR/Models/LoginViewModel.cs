using System.ComponentModel.DataAnnotations;

namespace CSR.Models
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "사용자 ID를 입력해주세요.")]        
        public string? UserId { get; set; }

        [Required(ErrorMessage = "비밀번호를 입력해주세요.")]
        [DataType(DataType.Password)]
        
        public string? Password { get; set; }

        [Display(Name = "로그인 상태 유지")]
        public bool RememberMe { get; set; }
    }
}
