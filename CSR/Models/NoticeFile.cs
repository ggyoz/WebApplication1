namespace CSR.Models
{
    public class NoticeFile
    {
        public int FILEID { get; set; }
        public int NOTICEID { get; set; }
        public string? REQTYPE { get; set; }
        public string? UPLOAD_FILENAME { get; set; }
        public string? REAL_FILENAME { get; set; }
        public string? FILEPATH { get; set; }
        public DateTime REG_DATE { get; set; }
        public string? REG_USERID { get; set; }
        public string? USEYN { get; set; }
    }
}
