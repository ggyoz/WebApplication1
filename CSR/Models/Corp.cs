using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
using System.ComponentModel.DataAnnotations;

namespace CSR.Models
{
    [Table("TB_COR_INFO")]
    public class Corp : BaseModel
    {
        [PrimaryKey("CORCD", false)]
        [Display(Name = "법인코드")]
        [Required(ErrorMessage = "법인코드를 입력하세요.")]
        public string CorCd { get; set; } = string.Empty;

        [Column("CORNM")]
        [Display(Name = "법인명")]
        public string? CorNm { get; set; }

        [Column("NATIONCD")]
        [Display(Name = "국가코드")]
        public string? NationCd { get; set; }

        [Column("COINCD")]
        [Display(Name = "통화코드")]
        public string? CoinCd { get; set; }

        [Column("LANGUAGE")]
        [Display(Name = "언어코드")]
        public string? Language { get; set; }

        [Column("ACC_TITLE")]
        [Display(Name = "계정과목")]
        public string? AccTitle { get; set; }
    }
}
