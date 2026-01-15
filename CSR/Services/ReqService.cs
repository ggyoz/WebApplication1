using CSR.Models;
using Dapper;
using System.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using System;

namespace CSR.Services
{
    public interface IReqService
    {
        Task<PagedResult<ReqInfo>> GetReqInfosAsync(int page, int pageSize, string? searchField, string? searchValue);
        Task<ReqInfo?> GetReqInfoByIdAsync(int id);
        Task<int> CreateReqInfoAsync(ReqInfo reqInfo, IEnumerable<IFormFile> files);
        Task<bool> UpdateReqInfoAsync(ReqInfo reqInfo, IEnumerable<IFormFile> newFiles, List<int> deletedFiles);
        Task<bool> DeleteReqInfoAsync(int id, string userId);
        Task<ReqFile?> GetReqFileByIdAsync(int fileId);
    }

    public class ReqService : IReqService
    {
        private readonly IDbConnection _dbConnection;
        private readonly IWebHostEnvironment _hostingEnvironment;
        private readonly ILogger<ReqService> _logger;

        public ReqService(IDbConnection dbConnection, IWebHostEnvironment hostingEnvironment, ILogger<ReqService> logger)
        {
            _dbConnection = dbConnection;
            _hostingEnvironment = hostingEnvironment;
            _logger = logger;
            // Ensure the upload folder exists
            var uploadFolder = Path.Combine(_hostingEnvironment.WebRootPath, "uploads", "req");
            if (!Directory.Exists(uploadFolder))
            {
                Directory.CreateDirectory(uploadFolder);
            }
        }

        public async Task<int> CreateReqInfoAsync(ReqInfo reqInfo, IEnumerable<IFormFile> files)
        {
            _dbConnection.Open();
            using var transaction = _dbConnection.BeginTransaction();
            try
            {
                // 1. Insert into TB_REQ_INFO
                var reqSql = @"
                    INSERT INTO TB_REQ_INFO (REQID, PARENTID, TITLE, CONTENTS_HTML, CONTENTS_TEXT, REQDATE, DUEDATE, EXPECTDATE, STARTDATE, ENDDATE, REQTYPE, SYSTEMCD, REQMENU, REQMENU_ETC, BXTID, REQUSERID, RESUSERID, IMPTCD, DFCLTCD, PRIORITYCD, MAN_DAY, PROC_STATUS, PROC_RATE, ANSWER_HTML, ANSWER_TEXT, DELAYREASON_HTML, DELAYREASON_TEXT, CORCD, DEPTCD, OFFICECD, TEAMCD, NOTE_HTML, NOTE_TEXT, REG_USERID, USEYN)
                    VALUES (SEQ_TB_REQ_INFO.NEXTVAL, :PARENTID, :TITLE, :CONTENTS_HTML, :CONTENTS_TEXT, :REQDATE, :DUEDATE, :EXPECTDATE, :STARTDATE, :ENDDATE, :REQTYPE, :SYSTEMCD, :REQMENU, :REQMENU_ETC, :BXTID, :REQUSERID, :RESUSERID, :IMPTCD, :DFCLTCD, :PRIORITYCD, :MAN_DAY, :PROC_STATUS, :PROC_RATE, :ANSWER_HTML, :ANSWER_TEXT, :DELAYREASON_HTML, :DELAYREASON_TEXT, :CORCD, :DEPTCD, :OFFICECD, :TEAMCD, :NOTE_HTML, :NOTE_TEXT, :REG_USERID, 'Y')
                    RETURNING REQID INTO :REQID";
                
                var reqParams = new DynamicParameters(reqInfo);
                reqParams.Add(":REQID", dbType: DbType.Int32, direction: ParameterDirection.Output);
                await _dbConnection.ExecuteAsync(reqSql, reqParams, transaction);
                var newReqId = reqParams.Get<int>(":REQID");
                reqInfo.REQID = newReqId;

                // 2. Insert into TB_REQ_HIST
                var newHistoryId = await CreateReqHistoryAsync(reqInfo, transaction);

                // 3. Add files
                if (files != null && files.Any())
                {
                    await AddReqFilesAsync(newReqId, newHistoryId, files, reqInfo.REG_USERID, transaction);
                }

                transaction.Commit();
                return newReqId;
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                _logger.LogError(ex, "Error creating requirement.");
                throw;
            }
        }
        
        public async Task<bool> UpdateReqInfoAsync(ReqInfo reqInfo, IEnumerable<IFormFile> newFiles, List<int> deletedFiles)
        {
            _dbConnection.Open();
            using var transaction = _dbConnection.BeginTransaction();
            try
            {
                // 1. Update TB_REQ_INFO
                reqInfo.UPDATE_DATE = DateTime.Now;
                var reqSql = @"
                    UPDATE TB_REQ_INFO SET
                        PARENTID = :PARENTID, TITLE = :TITLE, CONTENTS_HTML = :CONTENTS_HTML, CONTENTS_TEXT = :CONTENTS_TEXT, REQDATE = :REQDATE,
                        DUEDATE = :DUEDATE, EXPECTDATE = :EXPECTDATE, STARTDATE = :STARTDATE, ENDDATE = :ENDDATE, REQTYPE = :REQTYPE,
                        SYSTEMCD = :SYSTEMCD, REQMENU = :REQMENU, REQMENU_ETC = :REQMENU_ETC, BXTID = :BXTID, REQUSERID = :REQUSERID,
                        RESUSERID = :RESUSERID, IMPTCD = :IMPTCD, DFCLTCD = :DFCLTCD, PRIORITYCD = :PRIORITYCD, MAN_DAY = :MAN_DAY,
                        PROC_STATUS = :PROC_STATUS, PROC_RATE = :PROC_RATE, ANSWER_HTML = :ANSWER_HTML, ANSWER_TEXT = :ANSWER_TEXT,
                        DELAYREASON_HTML = :DELAYREASON_HTML, DELAYREASON_TEXT = :DELAYREASON_TEXT, CORCD = :CORCD, DEPTCD = :DEPTCD,
                        OFFICECD = :OFFICECD, TEAMCD = :TEAMCD, NOTE_HTML = :NOTE_HTML, NOTE_TEXT = :NOTE_TEXT,
                        UPDATE_DATE = :UPDATE_DATE, UPDATE_USERID = :UPDATE_USERID
                    WHERE REQID = :REQID";
                await _dbConnection.ExecuteAsync(reqSql, reqInfo, transaction);

                // 2. Create new history record
                var newHistoryId = await CreateReqHistoryAsync(reqInfo, transaction);

                // 3. Handle files
                if (newFiles != null && newFiles.Any())
                {
                    await AddReqFilesAsync(reqInfo.REQID, newHistoryId, newFiles, reqInfo.UPDATE_USERID, transaction);
                }
                if (deletedFiles != null && deletedFiles.Any())
                {
                    await DeleteReqFilesAsync(deletedFiles, transaction);
                }

                transaction.Commit();
                return true;
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                _logger.LogError(ex, "Error updating requirement with ID {ReqId}", reqInfo.REQID);
                throw;
            }
        }
        
        public async Task<bool> DeleteReqInfoAsync(int id, string userId)
        {
             _dbConnection.Open();
            using var transaction = _dbConnection.BeginTransaction();
            try
            {
                var reqInfo = await GetReqInfoByIdAsync(id); // Get current state
                if (reqInfo == null) return false;

                reqInfo.USEYN = "N";
                reqInfo.UPDATE_USERID = userId;
                reqInfo.UPDATE_DATE = DateTime.Now;

                // Soft-delete main record
                var sql = "UPDATE TB_REQ_INFO SET USEYN = 'N', UPDATE_DATE = :UPDATE_DATE, UPDATE_USERID = :UPDATE_USERID WHERE REQID = :REQID";
                await _dbConnection.ExecuteAsync(sql, new { REQID = id, UPDATE_DATE = reqInfo.UPDATE_DATE, UPDATE_USERID = userId }, transaction);

                // Create final history record
                await CreateReqHistoryAsync(reqInfo, transaction);
                
                transaction.Commit();
                return true;
            }
            catch(Exception ex)
            {
                transaction.Rollback();
                _logger.LogError(ex, "Error deleting requirement with ID {ReqId}", id);
                throw;
            }
        }

        public async Task<PagedResult<ReqInfo>> GetReqInfosAsync(int page, int pageSize, string? searchField, string? searchValue)
        {
            var baseQuery = @"
                FROM TB_REQ_INFO R
                LEFT JOIN TB_USER_INFO U_REQ ON R.REQUSERID = U_REQ.USERID
                LEFT JOIN TB_USER_INFO U_RES ON R.RESUSERID = U_RES.USERID
                LEFT JOIN TB_COMM_CODE S ON R.SYSTEMCD = S.CODE AND S.CODE = 'SYSTEMCD'
                LEFT JOIN TB_COMM_CODE P ON R.PROC_STATUS = P.CODE AND P.CODE = 'PROC_STATUS'
                WHERE R.USEYN = 'Y'";
            
            var parameters = new DynamicParameters();
            if (!string.IsNullOrEmpty(searchValue))
            {
                baseQuery += " AND R.TITLE LIKE :SearchValue";
                parameters.Add("SearchValue", "%" + searchValue + "%");
            }
            
            var countSql = "SELECT COUNT(*) " + baseQuery;
            var totalCount = await _dbConnection.ExecuteScalarAsync<int>(countSql, parameters);

            var offset = (page - 1) * pageSize;
            var dataSql = $@"
                SELECT * FROM (
                    SELECT a.*, ROWNUM rnum FROM (
                        SELECT 
                            R.REQID, R.TITLE, R.REQDATE, R.EXPECTDATE, R.PROC_RATE,
                            U_REQ.USERNAME as ReqUserName,
                            U_RES.USERNAME as ResUserName,
                            S.CODENM as SystemName,
                            P.CODENM as ProcStatusName
                        {baseQuery}
                        ORDER BY R.REQID DESC
                    ) a WHERE ROWNUM <= :EndRow
                ) WHERE rnum > :StartRow";

            parameters.Add("StartRow", offset);
            parameters.Add("EndRow", offset + pageSize);

            var items = await _dbConnection.QueryAsync<ReqInfo>(dataSql, parameters);

            return new PagedResult<ReqInfo>(items.ToList(), totalCount, page, pageSize);
        }

        public async Task<ReqInfo?> GetReqInfoByIdAsync(int id)
        {
            var sql = "SELECT * FROM TB_REQ_INFO WHERE REQID = :ID AND USEYN = 'Y'";
            var reqInfo = await _dbConnection.QueryFirstOrDefaultAsync<ReqInfo>(sql, new { ID = id });

            if (reqInfo != null)
            {
                var fileSql = "SELECT * FROM TB_REQ_FILE WHERE REQID = :REQID AND USEYN = 'Y'";
                reqInfo.AttachFiles = (await _dbConnection.QueryAsync<ReqFile>(fileSql, new { REQID = id })).ToList();
                
                var histSql = "SELECT * FROM TB_REQ_HIST WHERE REQID = :REQID ORDER BY HISTORYID DESC";
                reqInfo.History = (await _dbConnection.QueryAsync<ReqHist>(histSql, new { REQID = id })).ToList();
            }

            return reqInfo;
        }

        public async Task<ReqFile?> GetReqFileByIdAsync(int fileId)
        {
            var sql = "SELECT * FROM TB_REQ_FILE WHERE FILEID = :FILEID";
            return await _dbConnection.QueryFirstOrDefaultAsync<ReqFile>(sql, new { FILEID = fileId });
        }


        // Private helper methods
        private async Task<int> CreateReqHistoryAsync(ReqInfo reqInfo, IDbTransaction transaction)
        {
            var histSql = @"
                INSERT INTO TB_REQ_HIST (HISTORYID, REQID, PARENTID, TITLE, CONTENTS_HTML, CONTENTS_TEXT, REQDATE, DUEDATE, EXPECTDATE, STARTDATE, ENDDATE, REQTYPE, SYSTEMCD, REQMENU, REQMENU_ETC, BXTID, REQUSERID, RESUSERID, IMPTCD, DFCLTCD, PRIORITYCD, MAN_DAY, PROC_STATUS, PROC_RATE, ANSWER_HTML, ANSWER_TEXT, DELAYREASON_HTML, DELAYREASON_TEXT, CORCD, DEPTCD, OFFICECD, TEAMCD, NOTE_HTML, NOTE_TEXT, REGHISTORY, REG_USERID, USEYN)
                VALUES (SEQ_REQ_HIST.NEXTVAL, :REQID, :PARENTID, :TITLE, :CONTENTS_HTML, :CONTENTS_TEXT, :REQDATE, :DUEDATE, :EXPECTDATE, :STARTDATE, :ENDDATE, :REQTYPE, :SYSTEMCD, :REQMENU, :REQMENU_ETC, :BXTID, :REQUSERID, :RESUSERID, :IMPTCD, :DFCLTCD, :PRIORITYCD, :MAN_DAY, :PROC_STATUS, :PROC_RATE, :ANSWER_HTML, :ANSWER_TEXT, :DELAYREASON_HTML, :DELAYREASON_TEXT, :CORCD, :DEPTCD, :OFFICECD, :TEAMCD, :NOTE_HTML, :NOTE_TEXT, :REQHISTORY, :REG_USERID, :USEYN)
                RETURNING HISTORYID INTO :HISTORYID";

            var histParams = new DynamicParameters(reqInfo);
            // REG_USERID for history should be the user making the change
            histParams.Add("REG_USERID", reqInfo.UPDATE_USERID ?? reqInfo.REG_USERID); 
            histParams.Add(":HISTORYID", dbType: DbType.Int32, direction: ParameterDirection.Output);
            histParams.Add("REQHISTORY", "Created"); // Simplified history log text

            await _dbConnection.ExecuteAsync(histSql, histParams, transaction);
            return histParams.Get<int>(":HISTORYID");
        }
        
        private async Task AddReqFilesAsync(int reqId, int historyId, IEnumerable<IFormFile> files, string userId, IDbTransaction transaction)
        {
            foreach (var file in files)
            {
                if (file.Length > 0)
                {
                    var fileExtension = Path.GetExtension(file.FileName);
                    var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";
                    var filePath = Path.Combine("uploads", "req", uniqueFileName);
                    var absolutePath = Path.Combine(_hostingEnvironment.WebRootPath, filePath);

                    using (var stream = new FileStream(absolutePath, FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }
                    
                    var reqFile = new ReqFile
                    {
                        REQID = reqId,
                        HISTORYID = historyId,
                        REQTYPE = "F", // General file type
                        UPLOAD_FILENAME = uniqueFileName,
                        REAL_FILENAME = file.FileName,
                        FILEPATH = filePath,
                        REG_USERID = userId,
                        USEYN = "Y"
                    };

                    var sql = @"
                        INSERT INTO TB_REQ_FILE (FILEID, REQID, HISTORYID, REQTYPE, UPLOAD_FILENAME, REAL_FILENAME, FILEPATH, REG_USERID, USEYN)
                        VALUES (SEQ_TB_REQ_FILE.NEXTVAL, :REQID, :HISTORYID, :REQTYPE, :UPLOAD_FILENAME, :REAL_FILENAME, :FILEPATH, :REG_USERID, 'Y')";
                    
                    await _dbConnection.ExecuteAsync(sql, reqFile, transaction);
                }
            }
        }

        private async Task DeleteReqFilesAsync(IEnumerable<int> fileIds, IDbTransaction transaction)
        {
            var sql = "UPDATE TB_REQ_FILE SET USEYN = 'N' WHERE FILEID = :FILEID";
            foreach (var fileId in fileIds)
            {
                await _dbConnection.ExecuteAsync(sql, new { FILEID = fileId }, transaction);
            }
        }
    }
}
