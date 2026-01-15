using System;
using System.ComponentModel.DataAnnotations;

namespace CSR.Models
{
    public class ReqFile
    {
        public int FILEID { get; set; }
        public int REQID { get; set; }
        public int HISTORYID { get; set; }
        public string REQTYPE { get; set; }
        public string UPLOAD_FILENAME { get; set; }
        public string REAL_FILENAME { get; set; }
        public string FILEPATH { get; set; }
        public DateTime REG_DATE { get; set; }
        public string REG_USERID { get; set; }
        public string USEYN { get; set; }
    }
}
