using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace CSR.Models
{
    public class Corp
    {
        [Display(Name = "법인코드")]
        [Required(ErrorMessage = "법인코드를 입력하세요.")]
        public string CorCd { get; set; } = string.Empty;

        [Display(Name = "법인명")]
        public string? CorNm { get; set; }

        [Display(Name = "국가코드")]
        public string? NationCd { get; set; }

        [Display(Name = "통화코드")]
        public string? CoinCd { get; set; }

        [Display(Name = "언어코드")]
        public string? Language { get; set; }

        [Display(Name = "계정과목")]
        public string? AccTitle { get; set; }
    }
}
