using CSR.Models;
using Dapper;
using System.Data;

namespace CSR.Services
{
    public interface INoticeService
    {
        Task<PagedResult<Notice>> GetNoticesAsync(int page, int pageSize, string? searchField, string? searchValue);
        Task<Notice?> GetNoticeByIdAsync(int id);
        Task<int> CreateNoticeAsync(Notice notice);
        Task<bool> UpdateNoticeAsync(Notice notice);
        Task<bool> DeleteNoticeAsync(int id);
        Task<NoticeFile?> GetNoticeFileByIdAsync(int fileId);
        Task AddNoticeFilesAsync(int noticeId, IEnumerable<IFormFile> files, string userId);
        Task DeleteNoticeFileAsync(int fileId);
    }

    public class NoticeService : INoticeService
    {
        private readonly IDbConnection _dbConnection;
        private readonly IWebHostEnvironment _hostingEnvironment;

        public NoticeService(IDbConnection dbConnection, IWebHostEnvironment hostingEnvironment)
        {
            _dbConnection = dbConnection;
            _hostingEnvironment = hostingEnvironment;
            // Ensure the upload folder exists
            var uploadFolder = Path.Combine(_hostingEnvironment.WebRootPath, "uploads", "notice");
            if (!Directory.Exists(uploadFolder))
            {
                Directory.CreateDirectory(uploadFolder);
            }
        }

        public async Task<int> CreateNoticeAsync(Notice notice)
        {
            var sql = @"
                INSERT INTO TB_NOTICE (ID, TITLE, CONTENTS_HTML, CONTENTS_TEXT, NOTICETYPE, CORCD, REG_USERID, USEYN)
                VALUES (SEQ_NOTICE.NEXTVAL, :TITLE, :CONTENTS_HTML, :CONTENTS_TEXT, :NOTICETYPE, :CORCD, :REG_USERID, 'Y')
                RETURNING ID INTO :ID";
            var parameters = new DynamicParameters(notice);
            parameters.Add(":ID", dbType: DbType.Int32, direction: ParameterDirection.Output);
            await _dbConnection.ExecuteAsync(sql, parameters);
            return parameters.Get<int>(":ID");
        }

        public async Task<bool> DeleteNoticeAsync(int id)
        {
            var sql = "UPDATE TB_NOTICE SET USEYN = 'N' WHERE ID = :ID";
            var affectedRows = await _dbConnection.ExecuteAsync(sql, new { ID = id });
            return affectedRows > 0;
        }

        public async Task<Notice?> GetNoticeByIdAsync(int id)
        {
            var sql = @"SELECT N.*, U.USERNAME as RegUserName 
                        FROM TB_NOTICE N
                        LEFT JOIN TB_USER_INFO U ON N.REG_USERID = U.USERID
                        WHERE N.ID = :ID AND N.USEYN = 'Y'";
            var notice = await _dbConnection.QueryFirstOrDefaultAsync<Notice>(sql, new { ID = id });

            if (notice != null)
            {
                var fileSql = "SELECT * FROM TB_NOTICE_FILE WHERE NOTICEID = :NOTICEID AND USEYN = 'Y'";
                notice.AttachFiles = (await _dbConnection.QueryAsync<NoticeFile>(fileSql, new { NOTICEID = id })).ToList();
            }

            return notice;
        }
        
        public async Task<PagedResult<Notice>> GetNoticesAsync(int page, int pageSize, string? searchField, string? searchValue)
        {
            var baseQuery = @"
                FROM TB_NOTICE N
                LEFT JOIN TB_USER_INFO U ON N.REG_USERID = U.USERID
                WHERE N.USEYN = 'Y'";
            
            var parameters = new DynamicParameters();
            if (!string.IsNullOrEmpty(searchValue))
            {
                if (searchField == "TITLE")
                {
                    baseQuery += " AND N.TITLE LIKE :SearchValue";
                    parameters.Add("SearchValue", "%" + searchValue + "%");
                }
                else if (searchField == "REG_USERID")
                {
                    baseQuery += " AND U.USERNAME LIKE :SearchValue";
                    parameters.Add("SearchValue", "%" + searchValue + "%");
                }
            }
            
            var countSql = "SELECT COUNT(*) " + baseQuery;
            var totalCount = await _dbConnection.ExecuteScalarAsync<int>(countSql, parameters);

            var offset = (page - 1) * pageSize;
            var dataSql = $@"
                SELECT * FROM (
                    SELECT a.*, ROWNUM rnum FROM (
                        SELECT N.ID, N.TITLE, N.REG_DATE, N.UPDATE_DATE, U.USERNAME as RegUserName
                        {baseQuery}
                        ORDER BY N.ID DESC
                    ) a WHERE ROWNUM <= :EndRow
                ) WHERE rnum > :StartRow";

            parameters.Add("StartRow", offset);
            parameters.Add("EndRow", offset + pageSize);

            var items = await _dbConnection.QueryAsync<Notice>(dataSql, parameters);

            return new PagedResult<Notice>(items.ToList(), totalCount, page, pageSize);
        }

        public async Task<bool> UpdateNoticeAsync(Notice notice)
        {
            var sql = @"
                UPDATE TB_NOTICE SET
                    TITLE = :TITLE,
                    CONTENTS_HTML = :CONTENTS_HTML,
                    CONTENTS_TEXT = :CONTENTS_TEXT,
                    NOTICETYPE = :NOTICETYPE,
                    CORCD = :CORCD,
                    UPDATE_DATE = sysdate,
                    UPDATE_USERID = :UPDATE_USERID
                WHERE ID = :ID";
            var affectedRows = await _dbConnection.ExecuteAsync(sql, notice);
            return affectedRows > 0;
        }

        public async Task AddNoticeFilesAsync(int noticeId, IEnumerable<IFormFile> files, string userId)
        {
            foreach (var file in files)
            {
                if (file.Length > 0)
                {
                    var fileExtension = Path.GetExtension(file.FileName);
                    var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";
                    var filePath = Path.Combine("uploads", "notice", uniqueFileName);
                    var absolutePath = Path.Combine(_hostingEnvironment.WebRootPath, filePath);

                    using (var stream = new FileStream(absolutePath, FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }
                    
                    var noticeFile = new NoticeFile
                    {
                        NOTICEID = noticeId,
                        REQTYPE = "F", // General file type, adjust if needed
                        UPLOAD_FILENAME = uniqueFileName,
                        REAL_FILENAME = file.FileName,
                        FILEPATH = filePath,
                        REG_USERID = userId,
                        USEYN = "Y"
                    };

                    var sql = @"
                        INSERT INTO TB_NOTICE_FILE (ID, NOTICEID, REQTYPE, UPLOAD_FILENAME, REAL_FILENAME, FILEPATH, REG_USERID, USEYN)
                        VALUES (SEQ_NOTICE_FILE.NEXTVAL, :NOTICEID, :REQTYPE, :UPLOAD_FILENAME, :REAL_FILENAME, :FILEPATH, :REG_USERID, 'Y')";
                    
                    await _dbConnection.ExecuteAsync(sql, noticeFile);
                }
            }
        }

        public async Task DeleteNoticeFileAsync(int fileId)
        {
            // Optionally, delete the physical file as well
            // var fileInfo = await GetNoticeFileByIdAsync(fileId);
            // if (fileInfo != null) { ... File.Delete(...) ... }
            
            var sql = "UPDATE TB_NOTICE_FILE SET USEYN = 'N' WHERE FILEID = :FILEID";
            await _dbConnection.ExecuteAsync(sql, new { FILEID = fileId });
        }

        public async Task<NoticeFile?> GetNoticeFileByIdAsync(int fileId)
        {
            var sql = "SELECT * FROM TB_NOTICE_FILE WHERE FILEID = :FILEID";
            return await _dbConnection.QueryFirstOrDefaultAsync<NoticeFile>(sql, new { FILEID = fileId });
        }
    }
}
