using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CSR.Models
{
    public class ReqInfo
    {
        [Display(Name = "요청번호")]
        public int REQID { get; set; }

        [Display(Name = "상위요청번호")]
        public int? PARENTID { get; set; }

        [Required(ErrorMessage = "제목을 입력해주세요.")]
        [StringLength(50)]
        [Display(Name = "제목")]
        public string TITLE { get; set; }

        [Display(Name = "내용(HTML)")]
        public string? CONTENTS_HTML { get; set; }

        [Display(Name = "내용(TEXT)")]
        public string? CONTENTS_TEXT { get; set; }
        
        [Required(ErrorMessage = "요청일을 입력해주세요.")]
        [Display(Name = "요청일")]
        public DateTime REQDATE { get; set; }

        [Display(Name = "처리기한")]
        public DateTime? DUEDATE { get; set; }

        [Display(Name = "완료예정일")]
        public DateTime? EXPECTDATE { get; set; }

        [Display(Name = "실제 시작일")]
        public DateTime? STARTDATE { get; set; }

        [Display(Name = "실제 종료일")]
        public DateTime? ENDDATE { get; set; }

        [Required(ErrorMessage = "요청유형을 선택해주세요.")]
        [Display(Name = "요청유형")]
        public string REQTYPE { get; set; }

        [Required(ErrorMessage = "대상시스템을 선택해주세요.")]
        [Display(Name = "대상시스템")]
        public string SYSTEMCD { get; set; }

        [Display(Name = "요청메뉴")]
        public string? REQMENU { get; set; }

        [Display(Name = "기타메뉴")]
        public string? REQMENU_ETC { get; set; }

        [Display(Name = "BXT문의번호")]
        public string? BXTID { get; set; }

        [Display(Name = "요청사항 TCODE")]
        public string? REQTCODE { get; set; } 

        [Required]
        [Display(Name = "요청자")]
        public string REQUSERID { get; set; }

        [Display(Name = "조치자")]
        public string? RESUSERID { get; set; }

        [Display(Name = "중요도")]
        public string? IMPTCD { get; set; }

        [Display(Name = "난이도")]
        public string? DFCLTCD { get; set; }

        [Required(ErrorMessage = "긴급도를 선택해주세요.")]
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
        
        public DateTime REG_DATE { get; set; }
        
        public string REG_USERID { get; set; }
        
        public DateTime? UPDATE_DATE { get; set; }
        
        public string? UPDATE_USERID { get; set; }
        
        public string USEYN { get; set; } = "Y";

        // Navigation properties
        public List<ReqFile> AttachFiles { get; set; } = new List<ReqFile>();
        public List<ReqHist> History { get; set; } = new List<ReqHist>();

        // For Display
        public string? RegUserName { get; set; }
        public string? ReqUserName { get; set; }
        public string? ResUserName { get; set; }
        public string? ReqUserEmail { get; set; }
        public string? ReqUserTel { get; set; }
        public string? CorpName { get; set; }
        public string? DeptName { get; set; }
        public string? OfficeName { get; set; }
        public string? TeamName { get; set; }
        public string? ReqTypeName { get; set; }
        public string? SystemName { get; set; }
        public string? ReqMenuName { get; set; }
        public string? PriorityName { get; set; }
        public string? ProcStatusName { get; set; }
    }
}
