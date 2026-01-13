namespace CSR.Models
{
    public class Notice
    {
        public int ID { get; set; }
        public string? TITLE { get; set; }
        public string? CONTENTS_HTML { get; set; }
        public string? CONTENTS_TEXT { get; set; }
        public string? NOTICETYPE { get; set; }
        public string? CORCD { get; set; }
        public DateTime REG_DATE { get; set; }
        public string? REG_USERID { get; set; }
        public DateTime? UPDATE_DATE { get; set; }
        public string? UPDATE_USERID { get; set; }
        public string? USEYN { get; set; }
        public string? RegUserName { get; set; }

        public List<NoticeFile> AttachFiles { get; set; } = new List<NoticeFile>();
    }
}
