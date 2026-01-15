using System;
using System.ComponentModel.DataAnnotations;

namespace CSR.Models
{
    public class ReqHist
    {
        [Display(Name = "히스토리번호")]
        public int HISTORYID { get; set; }

        [Display(Name = "요청번호")]
        public int REQID { get; set; }

        [Display(Name = "상위요청번호")]
        public int? PARENTID { get; set; }

        [Required]
        [StringLength(50)]
        [Display(Name = "제목")]
        public string TITLE { get; set; }

        [Display(Name = "내용(HTML)")]
        public string? CONTENTS_HTML { get; set; }

        [Display(Name = "내용(TEXT)")]
        public string? CONTENTS_TEXT { get; set; }
        
        [Required]
        [Display(Name = "요청일")]
        public DateTime REQDATE { get; set; }

        [Display(Name = "처리기한")]
        public DateTime? DUEDATE { get; set; }

        [Required]
        [Display(Name = "완료예정일")]
        public DateTime EXPECTDATE { get; set; }

        [Required]
        [Display(Name = "실제 시작일")]
        public DateTime STARTDATE { get; set; }

        [Required]
        [Display(Name = "실제 종료일")]
        public DateTime ENDDATE { get; set; }

        [Required]
        [Display(Name = "요청유형")]
        public string REQTYPE { get; set; }

        [Required]
        [Display(Name = "대상시스템")]
        public string SYSTEMCD { get; set; }

        [Display(Name = "요청메뉴")]
        public string? REQMENU { get; set; }

        [Display(Name = "기타메뉴")]
        public string? REQMENU_ETC { get; set; }

        [Display(Name = "BXT문의번호")]
        public string? BXTID { get; set; }

        [Required]
        [Display(Name = "요청자")]
        public string REQUSERID { get; set; }

        [Display(Name = "조치자")]
        public string? RESUSERID { get; set; }

        [Display(Name = "중요도")]
        public string? IMPTCD { get; set; }

        [Display(Name = "난이도")]
        public string? DFCLTCD { get; set; }

        [Required]
        [Display(Name = "긴급도")]
        public string PRIORITYCD { get; set; }

        [Display(Name = "투입공수")]
        public double? MAN_DAY { get; set; }

        [Display(Name = "진행상태")]
        public string? PROC_STATUS { get; set; }
        
        [Display(Name = "진행률")]
        public int? PROC_RATE { get; set; }

        [Display(Name = "답변내용(HTML)")]
        public string? ANSWER_HTML { get; set; }

        [Display(Name = "답변내용(TEXT)")]
        public string? ANSWER_TEXT { get; set; }

        [Display(Name = "지연사유(HTML)")]
        public string? DELAYREASON_HTML { get; set; }

        [Display(Name = "지연사유(TEXT)")]
        public string? DELAYREASON_TEXT { get; set; }

        [Required]
        public string CORCD { get; set; }
        [Required]
        public string DEPTCD { get; set; }
        [Required]
        public string OFFICECD { get; set; }
        [Required]
        public string TEAMCD { get; set; }

        [Display(Name = "조치내역(HTML)")]
        public string? NOTE_HTML { get; set; }

        [Display(Name = "조치내역(TEXT)")]
        public string? NOTE_TEXT { get; set; }
        
        [StringLength(1000)]
        public string? REQHISTORY { get; set; }
        
        public DateTime REG_DATE { get; set; }
        
        public string REG_USERID { get; set; }
        
        public string USEYN { get; set; } = "Y";
    }
}
