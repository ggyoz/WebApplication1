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
using Newtonsoft.Json;
using Dapper.Oracle;
using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;

namespace CSR.Services
{
    public interface IReqService
    {
        Task<PagedResult<ReqInfo>> GetReqInfosAsync(
            int page, 
            int pageSize, 
            string? reqTypeCd,
            string? procStatusCd,
            string? priorityCd,
            string? reqDate,
            string? dueDate,
            string? expectDate,
            string? regId,
            string? reqUserName,
            string? resUserName,
            string? searchValue,
            string? corCd,
            string? deptCd,
            string? officeCd,
            string? teamCd,
            List<string>? assignedResponsibilities
            );
        Task<ReqInfo?> GetReqInfoByIdAsync(int id);
        Task<int> CreateReqInfoAsync(ReqInfo reqInfo, IEnumerable<IFormFile> files);
        Task<bool> UpdateReqInfoAsync(ReqInfo reqInfo, IEnumerable<IFormFile> newFiles, List<int> deletedFiles, string historyReasonValue);
        Task<bool> SaveDraftAsync(ReqInfo reqInfo, IEnumerable<IFormFile> newFiles, List<int> deletedFiles);
        Task<bool> DeleteReqInfoAsync(int id, string userId);
        Task<ReqFile?> GetReqFileByIdAsync(int fileId);
        Task<bool> UpdateProcStatusAsync(int reqid, string statusVal, string userId);
        Task<bool> UpdateExpectDateAsync(int reqId, DateTime expectDate, string userId);
        Task<bool> UpdateResUserAsync(int reqId, string newResUserId, string updateUserId);
        Task<int> GetTeamTotalRequestsCountAsync(string teamCd);
        Task<int> GetMyTotalRequestsCountAsync(string userId);
        Task<int> GetAllRequestsCountAsync();
        Task<int> GetMyPendingRequestsCountAsync(string userId);
        Task<int> GetMyInProgressRequestsCountAsync(string userId);
        Task<int> GetMyInProgressCountAsync(string userId);
        Task<int> GetMyOverdueRequestsCountAsync(string userId);
        Task<int> GetMyCompletedRequestsCountAsync(string userId);
        Task<List<ReqHist>> GetMyRecentReqActivitiesAsync(string userId, int count = 10);
        Task<List<ReqHist>> GetMyAssignedRecentReqActivitiesAsync(string userId, int count = 10);
        Task<int> GetRequestsCountAddedThisWeekAsync();
        Task<List<DivisionRequestStats>> GetRequestsCountByDivisionAndPriorityAsync();
        Task<List<DivisionRequestStats>> GetRequestsCountByOfficeAndPriorityAsync();
        Task<List<DivisionRequestStats>> GetRequestsCountByAdminAndPriorityAsync();
        Task<int> GetRequestsDueThisWeekCountAsync();        
        Task<int> GetCorpTotalRequestsCountAsync(string corCd);
        Task<int> GetDeptTotalRequestsCountAsync(string corCd, string deptCd);
        Task<dynamic> GetMyRequestCountsByStatusAsync(string userId);
        Task<dynamic> GetMyRequestPerformanceAsync(string userId);
        Task<dynamic> GetCorpRequestCountsByStatusAsync(string corCd);
        Task<dynamic> GetCorpRequestPerformanceAsync(string corCd);
        Task<dynamic> GetAllRequestCountsByStatusAsync();
        Task<dynamic> GetAllRequestPerformanceAsync();
    }

    public class ReqService : IReqService
    {
        private readonly IDbConnection _dbConnection;
        private readonly IWebHostEnvironment _hostingEnvironment;
        private readonly ILogger<ReqService> _logger;
        private readonly ICommCodeService _commCodeService;

        public ReqService(IDbConnection dbConnection, IWebHostEnvironment hostingEnvironment, ILogger<ReqService> logger, ICommCodeService commCodeService)
        {
            _dbConnection = dbConnection;
            _hostingEnvironment = hostingEnvironment;
            _logger = logger;
            _commCodeService = commCodeService;
            // Ensure the upload folder exists
            var uploadFolder = Path.Combine(_hostingEnvironment.WebRootPath, "uploads", "req");
            if (!Directory.Exists(uploadFolder))
            {
                Directory.CreateDirectory(uploadFolder);
            }
        }

        public async Task<int> GetTeamTotalRequestsCountAsync(string teamCd)
        {
            var sql = @" SELECT COUNT(*) FROM TB_REQ_INFO WHERE TEAMCD = :TeamCd AND USEYN = 'Y' ";
            return await _dbConnection.ExecuteScalarAsync<int>(sql, new { TeamCd = teamCd });
        }

        public async Task<int> GetMyTotalRequestsCountAsync(string userId)
        {
            var sql = @" SELECT COUNT(*) FROM TB_REQ_INFO WHERE REQUSERID = :UserId AND USEYN = 'Y' ";
            return await _dbConnection.ExecuteScalarAsync<int>(sql, new { UserId = userId });
        }

        public async Task<dynamic> GetMyRequestCountsByStatusAsync(string userId)
        {
            var sql = @"
                SELECT 
                    COUNT(CASE WHEN PROC_STATUS = '61' THEN 1 END) AS WAITCOUNT,
                    COUNT(CASE WHEN PROC_STATUS = '62' THEN 1 END) AS RECEIVEDCOUNT,
                    COUNT(CASE WHEN PROC_STATUS = '68' THEN 1 END) AS CLOSEDCOUNT,
                    COUNT(CASE WHEN PROC_STATUS NOT IN ('61', '62', '68') AND PROC_STATUS IS NOT NULL THEN 1 END) AS INPROGRESSCOUNT
                FROM TB_REQ_INFO
                WHERE REQUSERID = :UserId AND USEYN = 'Y'";

            return await _dbConnection.QuerySingleAsync(sql, new { UserId = userId });
        }
        
        public async Task<dynamic> GetMyRequestPerformanceAsync(string userId)
        {
            var sql = @"
                SELECT 
                    COUNT(*) AS TOTALCOUNT,
                    COUNT(CASE WHEN PROC_STATUS = '68' THEN 1 END) AS COMPLETEDCOUNT,
                    COUNT(CASE WHEN PROC_STATUS != '68' AND EXPECTDATE < TRUNC(SYSDATE) AND EXPECTDATE IS NOT NULL THEN 1 END) AS DELAYEDCOUNT
                FROM TB_REQ_INFO
                WHERE REQUSERID = :UserId AND USEYN = 'Y'";

            var stats = await _dbConnection.QuerySingleAsync(sql, new { UserId = userId });
            
            // Dapper dynamic 결과(decimal)를 double로 안전하게 변환
            double total = Convert.ToDouble(stats.TOTALCOUNT);
            double completed = Convert.ToDouble(stats.COMPLETEDCOUNT);
            double delayed = Convert.ToDouble(stats.DELAYEDCOUNT);

            return new {
                TotalCount = (int)total,
                CompletedCount = (int)completed,
                DelayedCount = (int)delayed,
                CompletionRate = total > 0 ? Math.Round((completed / total) * 100, 1) : 0,
                DelayRate = total > 0 ? Math.Round((delayed / total) * 100, 1) : 0
            };
        }

        public async Task<dynamic> GetCorpRequestCountsByStatusAsync(string corCd)
        {
            var sql = @"
                SELECT 
                    COUNT(CASE WHEN PROC_STATUS = '61' THEN 1 END) AS WAITCOUNT,
                    COUNT(CASE WHEN PROC_STATUS = '62' THEN 1 END) AS RECEIVEDCOUNT,
                    COUNT(CASE WHEN PROC_STATUS = '68' THEN 1 END) AS CLOSEDCOUNT,
                    COUNT(CASE WHEN PROC_STATUS NOT IN ('61', '62', '68') AND PROC_STATUS IS NOT NULL THEN 1 END) AS INPROGRESSCOUNT
                FROM TB_REQ_INFO
                WHERE CORCD = :CorCd AND USEYN = 'Y'";

            return await _dbConnection.QuerySingleAsync(sql, new { CorCd = corCd });
        }

        public async Task<dynamic> GetCorpRequestPerformanceAsync(string corCd)
        {
            var sql = @"
                SELECT 
                    COUNT(*) AS TOTALCOUNT,
                    COUNT(CASE WHEN PROC_STATUS = '68' THEN 1 END) AS COMPLETEDCOUNT,
                    COUNT(CASE WHEN PROC_STATUS != '68' AND EXPECTDATE < TRUNC(SYSDATE) AND EXPECTDATE IS NOT NULL THEN 1 END) AS DELAYEDCOUNT
                FROM TB_REQ_INFO
                WHERE CORCD = :CorCd AND USEYN = 'Y'";

            var stats = await _dbConnection.QuerySingleAsync(sql, new { CorCd = corCd });
            
            double total = Convert.ToDouble(stats.TOTALCOUNT);
            double completed = Convert.ToDouble(stats.COMPLETEDCOUNT);
            double delayed = Convert.ToDouble(stats.DELAYEDCOUNT);

            return new {
                TotalCount = (int)total,
                CompletedCount = (int)completed,
                DelayedCount = (int)delayed,
                CompletionRate = total > 0 ? Math.Round((completed / total) * 100, 1) : 0,
                DelayRate = total > 0 ? Math.Round((delayed / total) * 100, 1) : 0
            };
        }

        public async Task<dynamic> GetAllRequestCountsByStatusAsync()
        {
            var sql = @"
                SELECT 
                    COUNT(CASE WHEN PROC_STATUS = '61' THEN 1 END) AS WAITCOUNT,
                    COUNT(CASE WHEN PROC_STATUS = '62' THEN 1 END) AS RECEIVEDCOUNT,
                    COUNT(CASE WHEN PROC_STATUS = '68' THEN 1 END) AS CLOSEDCOUNT,
                    COUNT(CASE WHEN PROC_STATUS NOT IN ('61', '62', '68') AND PROC_STATUS IS NOT NULL THEN 1 END) AS INPROGRESSCOUNT
                FROM TB_REQ_INFO
                WHERE USEYN = 'Y'";

            return await _dbConnection.QuerySingleAsync(sql);
        }

        public async Task<dynamic> GetAllRequestPerformanceAsync()
        {
            var sql = @"
                SELECT 
                    COUNT(*) AS TOTALCOUNT,
                    COUNT(CASE WHEN PROC_STATUS = '68' THEN 1 END) AS COMPLETEDCOUNT,
                    COUNT(CASE WHEN PROC_STATUS != '68' AND EXPECTDATE < TRUNC(SYSDATE) AND EXPECTDATE IS NOT NULL THEN 1 END) AS DELAYEDCOUNT
                FROM TB_REQ_INFO
                WHERE USEYN = 'Y'";

            var stats = await _dbConnection.QuerySingleAsync(sql);
            
            double total = Convert.ToDouble(stats.TOTALCOUNT);
            double completed = Convert.ToDouble(stats.COMPLETEDCOUNT);
            double delayed = Convert.ToDouble(stats.DELAYEDCOUNT);

            return new {
                TotalCount = (int)total,
                CompletedCount = (int)completed,
                DelayedCount = (int)delayed,
                CompletionRate = total > 0 ? Math.Round((completed / total) * 100, 1) : 0,
                DelayRate = total > 0 ? Math.Round((delayed / total) * 100, 1) : 0
            };
        }

        public async Task<int> GetAllRequestsCountAsync()
        {
            var sql = @" SELECT COUNT(*) FROM TB_REQ_INFO WHERE USEYN = 'Y' ";
            return await _dbConnection.ExecuteScalarAsync<int>(sql);
        }

        public async Task<int> GetDeptTotalRequestsCountAsync(string corCd, string deptCd)
        {
            var sql = @" SELECT COUNT(*) FROM TB_REQ_INFO WHERE CORCD = :CorCd AND DEPTCD = :DeptCd AND USEYN = 'Y' ";
            return await _dbConnection.ExecuteScalarAsync<int>(sql, new { CorCd = corCd, DeptCd = deptCd });
        }

        public async Task<int> GetCorpTotalRequestsCountAsync(string corCd)
        {
            var sql = @" SELECT COUNT(*) FROM TB_REQ_INFO WHERE CORCD = :CorCd AND USEYN = 'Y' ";
            return await _dbConnection.ExecuteScalarAsync<int>(sql, new { CorCd = corCd });
        }
            
        public async Task<int> GetMyPendingRequestsCountAsync(string userId)
        {
            // PROC_STATUS '69' : 대기
            var sql = @" SELECT COUNT(*) FROM TB_REQ_INFO WHERE RESUSERID = :UserId AND PROC_STATUS = '69' AND USEYN = 'Y' ";
            return await _dbConnection.ExecuteScalarAsync<int>(sql, new { UserId = userId });
        }

        // 관리자용
        public async Task<int> GetMyInProgressCountAsync(string userId)
        {
            // PROC_STATUS '64' is '진행중' (In Progress)
            var sql = @" SELECT COUNT(*) FROM TB_REQ_INFO WHERE RESUSERID = :UserId AND PROC_STATUS = '64' AND USEYN = 'Y' ";
            return await _dbConnection.ExecuteScalarAsync<int>(sql, new { UserId = userId });
        }

        // 사용자용
        public async Task<int> GetMyInProgressRequestsCountAsync(string userId)
        {
            // PROC_STATUS '64' is '진행중' (In Progress)
            var sql = @" SELECT COUNT(*) FROM TB_REQ_INFO WHERE REQUSERID = :UserId AND PROC_STATUS = '64' AND USEYN = 'Y' ";
            return await _dbConnection.ExecuteScalarAsync<int>(sql, new { UserId = userId });
        }

        public async Task<int> GetMyOverdueRequestsCountAsync(string userId)
        {
            // Status codes for 'Completed', 'Closed', 'Canceled'
            var completedStatuses = new[] { "63", "68", "82" }; 
            var sql = @" SELECT COUNT(*) FROM TB_REQ_INFO 
                         WHERE EXPECTDATE < TRUNC(SYSDATE)
                         AND PROC_STATUS NOT IN :CompletedStatuses
                         AND USEYN = 'Y' ";
            return await _dbConnection.ExecuteScalarAsync<int>(sql, new { UserId = userId, CompletedStatuses = completedStatuses });
        }

        // 답변완료 조회
        public async Task<int> GetMyCompletedRequestsCountAsync(string userId)
        {
            var sql = @" SELECT COUNT(*) FROM TB_REQ_INFO WHERE REQUSERID = :UserId AND PROC_STATUS = '65' AND USEYN = 'Y' ";
            return await _dbConnection.ExecuteScalarAsync<int>(sql, new { UserId = userId });
        }

        public async Task<List<ReqHist>> GetMyRecentReqActivitiesAsync(string userId, int count = 10)
        {
            var sql = @"
                SELECT * FROM (
                    SELECT 
                        H.REQID, H.HISTORY_REASON, H.REG_DATE, H.TITLE
                    FROM TB_REQ_HIST H
                    INNER JOIN TB_REQ_INFO R ON H.REQID = R.REQID
                    LEFT JOIN TB_COMM_CODE PS ON H.HISTORY_REASON = PS.CODEID 
                    WHERE R.REQUSERID = :UserId AND H.HISTORY_REASON IS NOT NULL
                    ORDER BY H.REG_DATE DESC
                ) WHERE ROWNUM <= :Count";
            
            return (await _dbConnection.QueryAsync<ReqHist>(sql, new { UserId = userId, Count = count })).ToList();
        }

        public async Task<List<ReqHist>> GetMyAssignedRecentReqActivitiesAsync(string userId, int count = 10)
        {
            var sql = @"
                SELECT * FROM (
                    SELECT 
                        H.REQID, H.HISTORY_REASON, H.REG_DATE, H.TITLE
                    FROM TB_REQ_HIST H
                    INNER JOIN TB_REQ_INFO R ON H.REQID = R.REQID
                    LEFT JOIN TB_COMM_CODE PS ON H.HISTORY_REASON = PS.CODEID 
                    WHERE H.RESUSERID = :UserId AND H.HISTORY_REASON IS NOT NULL
                    ORDER BY H.REG_DATE DESC
                ) WHERE ROWNUM <= :Count";
            
            return (await _dbConnection.QueryAsync<ReqHist>(sql, new { UserId = userId, Count = count })).ToList();
        }

        public async Task<int> GetRequestsCountAddedThisWeekAsync()
        {
            var sql = @"
                SELECT COUNT(*) FROM TB_REQ_INFO
                WHERE REG_DATE >= (TRUNC(SYSDATE) - CASE TO_NUMBER(TO_CHAR(SYSDATE, 'D')) WHEN 6 THEN 0 WHEN 7 THEN 1 ELSE TO_NUMBER(TO_CHAR(SYSDATE, 'D')) + 1 END)
                AND REG_DATE < (TRUNC(SYSDATE) - CASE TO_NUMBER(TO_CHAR(SYSDATE, 'D')) WHEN 6 THEN 0 WHEN 7 THEN 1 ELSE TO_NUMBER(TO_CHAR(SYSDATE, 'D')) + 1 END + 7)
                AND USEYN = 'Y'";
            return await _dbConnection.ExecuteScalarAsync<int>(sql);
        }

        public async Task<List<DivisionRequestStats>> GetRequestsCountByDivisionAndPriorityAsync()
        {
            var sql = $@"
                SELECT
                    D.DEPTNAME AS DivisionName,
                    SUM(CASE WHEN R.PRIORITYCD = '2' THEN 1 ELSE 0 END) AS VeryHighCount,
                    SUM(CASE WHEN R.PRIORITYCD = '3' THEN 1 ELSE 0 END) AS HighCount,
                    SUM(CASE WHEN R.PRIORITYCD = '4' THEN 1 ELSE 0 END) AS MediumCount,
                    SUM(CASE WHEN R.PRIORITYCD = '5' THEN 1 ELSE 0 END) AS LowCount,
                    SUM(CASE WHEN R.PRIORITYCD = '6' THEN 1 ELSE 0 END) AS VeryLowCount,                    
                    count(R.PRIORITYCD) AS TotalCount,
                    SUM(CASE WHEN R.PROC_STATUS = '68' THEN 1 ELSE 0 END) AS EndStatus,
                    SUM(CASE WHEN R.PROC_STATUS != '68' THEN 1 ELSE 0 END) AS IngStatus,
                    SUM(CASE WHEN R.PROC_STATUS != '68' AND R.EXPECTDATE < TRUNC(SYSDATE) AND R.EXPECTDATE IS NOT NULL THEN 1 ELSE 0 END) AS DelayedCount,
                    SUM(CASE WHEN R.PROC_STATUS != '68' AND R.EXPECTDATE >= TRUNC(SYSDATE) AND R.EXPECTDATE IS NOT NULL THEN 1 ELSE 0 END) AS ScheduledCount,                    
                    trunc( SUM(CASE WHEN R.PROC_STATUS = '68' THEN 1 ELSE 0 END) / count(R.PRIORITYCD) * 100) AS EndPercent                
                FROM TB_REQ_INFO R
                JOIN TB_DEPT_INFO D ON R.DEPTCD = D.DEPTCD
                WHERE R.USEYN = 'Y'
                GROUP BY D.DEPTNAME
                ORDER BY D.DEPTNAME";            
            var result = await _dbConnection.QueryAsync<DivisionRequestStats>(sql);
            return result.ToList();
        }

        public async Task<List<DivisionRequestStats>> GetRequestsCountByOfficeAndPriorityAsync()
        {
            var sql = $@"
                SELECT
                    D.DEPTNAME AS DivisionName,
                    SUM(CASE WHEN R.PRIORITYCD = '2' THEN 1 ELSE 0 END) AS VeryHighCount,
                    SUM(CASE WHEN R.PRIORITYCD = '3' THEN 1 ELSE 0 END) AS HighCount,
                    SUM(CASE WHEN R.PRIORITYCD = '4' THEN 1 ELSE 0 END) AS MediumCount,
                    SUM(CASE WHEN R.PRIORITYCD = '5' THEN 1 ELSE 0 END) AS LowCount,
                    SUM(CASE WHEN R.PRIORITYCD = '6' THEN 1 ELSE 0 END) AS VeryLowCount,                    
                    count(R.PRIORITYCD) AS TotalCount,
                    SUM(CASE WHEN R.PROC_STATUS = '68' THEN 1 ELSE 0 END) AS EndStatus,
                    SUM(CASE WHEN R.PROC_STATUS != '68' THEN 1 ELSE 0 END) AS IngStatus,
                    SUM(CASE WHEN R.PROC_STATUS != '68' AND R.EXPECTDATE < TRUNC(SYSDATE) AND R.EXPECTDATE IS NOT NULL THEN 1 ELSE 0 END) AS DelayedCount,
                    SUM(CASE WHEN R.PROC_STATUS != '68' AND R.EXPECTDATE >= TRUNC(SYSDATE) AND R.EXPECTDATE IS NOT NULL THEN 1 ELSE 0 END) AS ScheduledCount,
                    trunc( SUM(CASE WHEN R.PROC_STATUS = '68' THEN 1 ELSE 0 END) / count(R.PRIORITYCD) * 100) AS EndPercent
                FROM TB_REQ_INFO R
                JOIN TB_DEPT_INFO D ON R.OFFICECD = D.DEPTCD
                WHERE R.USEYN = 'Y'
                GROUP BY D.DEPTNAME
                ORDER BY D.DEPTNAME";
            
            var result = await _dbConnection.QueryAsync<DivisionRequestStats>(sql);
            return result.ToList();
        }

        public async Task<List<DivisionRequestStats>> GetRequestsCountByAdminAndPriorityAsync()
        {
            var sql = $@"
                SELECT
                    U.USERNAME AS UserName,
                    SUM(CASE WHEN R.PRIORITYCD = '2' THEN 1 ELSE 0 END) AS VeryHighCount,
                    SUM(CASE WHEN R.PRIORITYCD = '3' THEN 1 ELSE 0 END) AS HighCount,
                    SUM(CASE WHEN R.PRIORITYCD = '4' THEN 1 ELSE 0 END) AS MediumCount,
                    SUM(CASE WHEN R.PRIORITYCD = '5' THEN 1 ELSE 0 END) AS LowCount,
                    SUM(CASE WHEN R.PRIORITYCD = '6' THEN 1 ELSE 0 END) AS VeryLowCount,                    
                    count(R.PRIORITYCD) AS TotalCount,
                    SUM(CASE WHEN R.PROC_STATUS = '68' THEN 1 ELSE 0 END) AS EndStatus,
                    SUM(CASE WHEN R.PROC_STATUS != '68' THEN 1 ELSE 0 END) AS IngStatus,
                    CASE WHEN count(R.PRIORITYCD) != 0 THEN TRUNC ( SUM(CASE WHEN R.PROC_STATUS = '68' THEN 1 ELSE 0 END) / count(R.PRIORITYCD) * 100 ) ELSE 0 END AS EndPercent
                FROM TB_USER_INFO U 
                LEFT JOIN TB_REQ_INFO R ON R.RESUSERID = U.USERID AND R.USEYN = 'Y'
                WHERE U.USER_DIV IN ( 'R3', 'R4' ) AND U.USERNAME != 'admin'
                GROUP BY U.USERNAME
                ORDER BY U.USERNAME ";
            
            var result = await _dbConnection.QueryAsync<DivisionRequestStats>(sql);
            return result.ToList();
        }

        public async Task<int> GetRequestsDueThisWeekCountAsync()
        {
            var sql = @"
                SELECT COUNT(*) FROM TB_REQ_INFO
                WHERE EXPECTDATE >= (TRUNC(SYSDATE) - CASE TO_NUMBER(TO_CHAR(SYSDATE, 'D')) WHEN 6 THEN 0 WHEN 7 THEN 1 ELSE TO_NUMBER(TO_CHAR(SYSDATE, 'D')) + 1 END)
                AND EXPECTDATE < (TRUNC(SYSDATE) - CASE TO_NUMBER(TO_CHAR(SYSDATE, 'D')) WHEN 6 THEN 0 WHEN 7 THEN 1 ELSE TO_NUMBER(TO_CHAR(SYSDATE, 'D')) + 1 END + 7)
                AND USEYN = 'Y'";
            return await _dbConnection.ExecuteScalarAsync<int>(sql);
        }

        public async Task<int> CreateReqInfoAsync(ReqInfo reqInfo, IEnumerable<IFormFile> files)
        {
            // 진행상태 - '대기'
            reqInfo.PROC_STATUS = "69";

            using var transaction = _dbConnection.BeginTransaction();
            try
            {
                var parameters = new OracleDynamicParameters();
                parameters.BindByName = true;

                // Add IN parameters for the stored procedure
                parameters.Add("P_PARENTID", reqInfo.PARENTID);
                parameters.Add("P_TITLE", reqInfo.TITLE);                
                parameters.Add("P_REQDATE", reqInfo.REQDATE);
                parameters.Add("P_DUEDATE", reqInfo.DUEDATE);
                parameters.Add("P_EXPECTDATE", reqInfo.EXPECTDATE);
                parameters.Add("P_STARTDATE", reqInfo.STARTDATE);
                parameters.Add("P_ENDDATE", reqInfo.ENDDATE);
                parameters.Add("P_REQTYPE", reqInfo.REQTYPE);
                parameters.Add("P_SYSTEMCD", reqInfo.SYSTEMCD);
                parameters.Add("P_REQMENU", reqInfo.REQMENU);
                parameters.Add("P_REQMENU_ETC", reqInfo.REQMENU_ETC);
                parameters.Add("P_REQTCODE", reqInfo.REQTCODE);
                parameters.Add("P_BXTID", reqInfo.BXTID);
                parameters.Add("P_REQUSERID", reqInfo.REQUSERID);
                parameters.Add("P_RESUSERID", reqInfo.RESUSERID);
                parameters.Add("P_IMPTCD", reqInfo.IMPTCD);
                parameters.Add("P_DFCLTCD", reqInfo.DFCLTCD);
                parameters.Add("P_PRIORITYCD", reqInfo.PRIORITYCD);
                parameters.Add("P_MAN_DAY", reqInfo.MAN_DAY);
                parameters.Add("P_PROC_STATUS", reqInfo.PROC_STATUS);
                parameters.Add("P_CORCD", reqInfo.CORCD);
                parameters.Add("P_DEPTCD", reqInfo.DEPTCD);
                parameters.Add("P_OFFICECD", reqInfo.OFFICECD);
                parameters.Add("P_TEAMCD", reqInfo.TEAMCD);
                parameters.Add("P_REG_USERID", reqInfo.REG_USERID);        
                parameters.Add("P_CONTENTS_HTML", reqInfo.CONTENTS_HTML, OracleMappingType.Clob);
                // Add OUT parameter to get the new REQID
                parameters.Add("P_NEW_REQID", dbType: OracleMappingType.Int64, direction: ParameterDirection.Output);

                // Execute the stored procedure
                await _dbConnection.ExecuteAsync(
                    "SP_REQ_CREATE",
                    parameters,
                    transaction: transaction,
                    commandType: CommandType.StoredProcedure
                );

                // Get the new ID from the OUT parameter
                var newReqId = parameters.Get<int>("P_NEW_REQID");
                reqInfo.REQID = newReqId;

                // 2. Insert into TB_REQ_HIST
                var newHistoryId = await CreateReqHistoryAsync(reqInfo, "71", transaction); // CODEID = "71" 최초등록

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
                _logger.LogError(ex, "Error creating requirement via stored procedure.");
                throw;
            }
        }
        
        public async Task<bool> UpdateReqInfoAsync(ReqInfo reqInfo, IEnumerable<IFormFile> newFiles, List<int> deletedFiles, string historyReasonValue)
        {
            using var transaction = _dbConnection.BeginTransaction();
            try
            {
                // 1. Call SP_REQ_UPDATE stored procedure
                reqInfo.UPDATE_DATE = DateTime.Now; // Update date is handled by SYSDATE in the procedure

                var parameters = new OracleDynamicParameters();
                parameters.BindByName = true;
                
                parameters.Add("P_REQID", reqInfo.REQID);
                parameters.Add("P_PARENTID", reqInfo.PARENTID);
                parameters.Add("P_TITLE", reqInfo.TITLE);
                parameters.Add("P_REQDATE", reqInfo.REQDATE);
                parameters.Add("P_DUEDATE", reqInfo.DUEDATE);
                parameters.Add("P_EXPECTDATE", reqInfo.EXPECTDATE);
                parameters.Add("P_STARTDATE", reqInfo.STARTDATE);
                parameters.Add("P_ENDDATE", reqInfo.ENDDATE);
                parameters.Add("P_REQTYPE", reqInfo.REQTYPE);
                parameters.Add("P_SYSTEMCD", reqInfo.SYSTEMCD);
                parameters.Add("P_REQMENU", reqInfo.REQMENU);
                parameters.Add("P_REQMENU_ETC", reqInfo.REQMENU_ETC);
                parameters.Add("P_BXTID", reqInfo.BXTID);
                parameters.Add("P_REQTCODE", reqInfo.REQTCODE);
                parameters.Add("P_REQUSERID", reqInfo.REQUSERID);
                parameters.Add("P_RESUSERID", reqInfo.RESUSERID);
                parameters.Add("P_IMPTCD", reqInfo.IMPTCD);
                parameters.Add("P_DFCLTCD", reqInfo.DFCLTCD);
                parameters.Add("P_PRIORITYCD", reqInfo.PRIORITYCD);
                parameters.Add("P_MAN_DAY", reqInfo.MAN_DAY);
                parameters.Add("P_PROC_STATUS", reqInfo.PROC_STATUS);
                parameters.Add("P_PROC_RATE", reqInfo.PROC_RATE);
                parameters.Add("P_CORCD", reqInfo.CORCD);
                parameters.Add("P_DEPTCD", reqInfo.DEPTCD);
                parameters.Add("P_OFFICECD", reqInfo.OFFICECD);
                parameters.Add("P_TEAMCD", reqInfo.TEAMCD);
                parameters.Add("P_UPDATE_USERID", reqInfo.UPDATE_USERID);
                
                // CLOB Types
                parameters.Add("P_CONTENTS_HTML", reqInfo.CONTENTS_HTML, OracleMappingType.Clob);
                //parameters.Add("P_CONTENTS_TEXT", reqInfo.CONTENTS_TEXT, OracleMappingType.Clob);
                parameters.Add("P_ANSWER_HTML", reqInfo.ANSWER_HTML, OracleMappingType.Clob);
                //parameters.Add("P_ANSWER_TEXT", reqInfo.ANSWER_TEXT, OracleMappingType.Clob);
                parameters.Add("P_DELAYREASON_HTML", reqInfo.DELAYREASON_HTML, OracleMappingType.Clob);
                //parameters.Add("P_DELAYREASON_TEXT", reqInfo.DELAYREASON_TEXT, OracleMappingType.Clob);
                parameters.Add("P_NOTE_HTML", reqInfo.NOTE_HTML, OracleMappingType.Clob);
                //parameters.Add("P_NOTE_TEXT", reqInfo.NOTE_TEXT, OracleMappingType.Clob);

                await _dbConnection.ExecuteAsync(
                    "SP_REQ_UPDATE", 
                    parameters, 
                    transaction: transaction, 
                    commandType: CommandType.StoredProcedure
                );                
                
                var newHistoryId = 0;                  
                // Answer 페이지에서 수정하면 히스토리 저장 X
                if( historyReasonValue != "NonHistory"){
                  newHistoryId = await CreateReqHistoryAsync(reqInfo, historyReasonValue, transaction);
                }

                // Handle files without creating new history record
                var latestHistoryId = await _dbConnection.ExecuteScalarAsync<int?>(
                    "SELECT MAX(HISTORYID) FROM TB_REQ_HIST WHERE REQID = :REQID",
                    new { reqInfo.REQID },
                    transaction);

                if( newHistoryId == 0 ){
                    newHistoryId = latestHistoryId.Value;
                }

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
                _logger.LogError(ex, "Error updating requirement with ID {ReqId} via stored procedure.", reqInfo.REQID);
                throw;
            }
        }

        public async Task<bool> SaveDraftAsync(ReqInfo reqInfo, IEnumerable<IFormFile> newFiles, List<int> deletedFiles)
        {
            // 접수상태이면 진행중으로 변경
            if(reqInfo.PROC_STATUS == "62" || reqInfo.PROC_STATUS == "83") {
                reqInfo.PROC_STATUS = "64";
            }

            using var transaction = _dbConnection.BeginTransaction();
            try
            {
                var parameters = new OracleDynamicParameters();
                parameters.BindByName = true;

                // Add parameters for the stored procedure
                parameters.Add("P_REQID", reqInfo.REQID);
                parameters.Add("P_BXTID", reqInfo.BXTID);
                parameters.Add("P_IMPTCD", reqInfo.IMPTCD);
                parameters.Add("P_DFCLTCD", reqInfo.DFCLTCD);
                parameters.Add("P_MAN_DAY", reqInfo.MAN_DAY);
                parameters.Add("P_PROC_STATUS", reqInfo.PROC_STATUS);
                parameters.Add("P_RESTCODE", reqInfo.RESTCODE);
                parameters.Add("P_UPDATE_USERID", reqInfo.UPDATE_USERID);

                // Explicitly specify CLOB type for large string parameters
                parameters.Add("P_ANSWER_HTML", reqInfo.ANSWER_HTML, OracleMappingType.Clob);
                parameters.Add("P_NOTE_HTML", reqInfo.NOTE_HTML, OracleMappingType.Clob);

                // Execute the stored procedure
                await _dbConnection.ExecuteAsync(
                    "SP_REQ_SAVE_DRAFT", 
                    parameters, 
                    transaction: transaction, 
                    commandType: CommandType.StoredProcedure
                );

                // Handle files without creating new history record
                var latestHistoryId = await _dbConnection.ExecuteScalarAsync<int?>(
                    "SELECT MAX(HISTORYID) FROM TB_REQ_HIST WHERE REQID = :REQID",
                    new { reqInfo.REQID },
                    transaction);

                if (!latestHistoryId.HasValue)
                {
                    throw new InvalidOperationException($"Cannot save draft for request {reqInfo.REQID} because no history record exists.");
                }

                if (newFiles != null && newFiles.Any())
                {
                    await AddReqFilesAsync(reqInfo.REQID, latestHistoryId.Value, newFiles, reqInfo.UPDATE_USERID, transaction);
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
                _logger.LogError(ex, "Error saving draft for requirement with ID {ReqId}", reqInfo.REQID);
                throw;
            }
        }
        
        
        public async Task<bool> DeleteReqInfoAsync(int id, string userId)
        {
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
                await CreateReqHistoryAsync(reqInfo, "82", transaction);  // 요구사항 히스토리 삭제 : 82
                
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

        public async Task<PagedResult<ReqInfo>> GetReqInfosAsync(
            int page, 
            int pageSize, 
            string? reqTypeCd,
            string? procStatusCd,
            string? priorityCd,
            string? reqDate,
            string? dueDate,
            string? expectDate,
            string? regId,
            string? reqUserName,
            string? resUserName,
            string? searchValue,
            string? corCd,
            string? deptCd,
            string? officeCd,
            string? teamCd,
            List<string>? assignedResponsibilities)
        {
            
            var baseQuery = new System.Text.StringBuilder(@"
                FROM TB_REQ_INFO R
                LEFT JOIN TB_USER_INFO U_REQ ON R.REQUSERID = U_REQ.USERID
                LEFT JOIN TB_USER_INFO U_RES ON R.RESUSERID = U_RES.USERID
                LEFT JOIN TB_COMM_CODE S ON R.SYSTEMCD = S.CODEID AND S.PARENTID = 19
                LEFT JOIN TB_COMM_CODE RT ON R.REQTYPE = RT.CODEID AND RT.PARENTID = 13
                LEFT JOIN TB_COMM_CODE P ON R.PRIORITYCD = P.CODEID AND P.PARENTID = 1
                LEFT JOIN TB_COMM_CODE PS ON R.PROC_STATUS = PS.CODEID AND PS.PARENTID = 61
                LEFT JOIN TB_COMM_CODE M ON R.REQMENU  = M.CODEID 
                LEFT JOIN TB_COR_INFO CO ON R.CORCD = CO.CORCD
                LEFT JOIN TB_DEPT_INFO D ON R.DEPTCD = D.DEPTCD
                LEFT JOIN TB_DEPT_INFO OFI ON R.OFFICECD = OFI.DEPTCD
                LEFT JOIN TB_DEPT_INFO TM ON R.TEAMCD = TM.DEPTCD
                WHERE R.USEYN = 'Y'");
            
            var parameters = new DynamicParameters();

            if (!string.IsNullOrEmpty(reqTypeCd))
            {
                baseQuery.Append(" AND R.REQTYPE = :ReqTypeCd");
                parameters.Add("ReqTypeCd", reqTypeCd);
            }
            if (!string.IsNullOrEmpty(procStatusCd))
            {
                baseQuery.Append(" AND R.PROC_STATUS = :ProcStatusCd");
                parameters.Add("ProcStatusCd", procStatusCd);
            }
            if (!string.IsNullOrEmpty(priorityCd))
            {
                baseQuery.Append(" AND R.PRIORITYCD = :PriorityCd");
                parameters.Add("PriorityCd", priorityCd);
            }
            if (!string.IsNullOrEmpty(reqDate))
            {
                baseQuery.Append(" AND TRUNC(R.REQDATE) >= TO_DATE(:ReqDate, 'YYYY-MM-DD')");
                parameters.Add("ReqDate", reqDate);
            }
            if (!string.IsNullOrEmpty(dueDate))
            {
                baseQuery.Append(" AND TRUNC(R.DUEDATE) <= TO_DATE(:DueDate, 'YYYY-MM-DD')");
                parameters.Add("DueDate", dueDate);
            }
            if (!string.IsNullOrEmpty(expectDate))
            {
                baseQuery.Append(" AND TRUNC(R.EXPECTDATE) = TO_DATE(:ExpectDate, 'YYYY-MM-DD')");
                parameters.Add("ExpectDate", expectDate);
            }
            if (!string.IsNullOrEmpty(regId))
            {
                // Assuming RegId is an integer and directly corresponds to REQID
                if (int.TryParse(regId, out int parsedRegId))
                {
                    baseQuery.Append(" AND R.REQID = :RegId");
                    parameters.Add("RegId", parsedRegId);
                }
            }
            if (!string.IsNullOrEmpty(reqUserName))
            {
                baseQuery.Append(" AND U_REQ.USERNAME LIKE :ReqUserName");
                parameters.Add("ReqUserName", "%" + reqUserName + "%");
            }
            if (!string.IsNullOrEmpty(resUserName))
            {
                baseQuery.Append(" AND U_RES.USERNAME LIKE :ResUserName");
                parameters.Add("ResUserName", "%" + resUserName + "%");
            }
            if (!string.IsNullOrEmpty(searchValue))
            {
                baseQuery.Append(" AND R.TITLE LIKE :SearchValue");
                parameters.Add("SearchValue", "%" + searchValue + "%");
            }
            if (!string.IsNullOrEmpty(corCd))
            {
                baseQuery.Append(" AND R.CORCD = :CorCd");
                parameters.Add("CorCd", corCd);
            }
            if (!string.IsNullOrEmpty(deptCd))
            {
                baseQuery.Append(" AND R.DEPTCD = :DeptCd");
                parameters.Add("DeptCd", deptCd);
            }
            if (!string.IsNullOrEmpty(officeCd))
            {
                baseQuery.Append(" AND R.OFFICECD = :OfficeCd");
                parameters.Add("OfficeCd", officeCd);
            }
            if (!string.IsNullOrEmpty(teamCd))
            {
                baseQuery.Append(" AND R.TEAMCD = :TeamCd");
                parameters.Add("TeamCd", teamCd);
            }
            if (assignedResponsibilities != null && assignedResponsibilities.Any())
            {
                baseQuery.Append(" AND R.REQMENU IN :AssignedResponsibilities");
                parameters.Add("AssignedResponsibilities", assignedResponsibilities);
            }
            
            var countSql = "SELECT COUNT(*) " + baseQuery.ToString();
            var totalCount = await _dbConnection.ExecuteScalarAsync<int>(countSql, parameters);

            var offset = (page - 1) * pageSize;
            var dataSql = $@"
                SELECT * FROM (
                    SELECT a.*, ROWNUM rnum FROM (
                        SELECT 
                            R.*,
                            U_REQ.USERNAME as ReqUserName,
                            U_RES.USERNAME as ResUserName,
                            S.CODENM as SystemName,
                            RT.CODENM as ReqTypeName,
                            P.CODENM as PriorityName,
                            PS.CODENM as ProcStatusName,
                            M.CODENM as ReqMenuName,
                            CO.CORNM as CorpName,
                            D.DEPTNAME as DeptName,
                            OFI.DEPTNAME as OfficeName,
                            TM.DEPTNAME as TeamName
                        {baseQuery.ToString()}
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
            var sql = @"
                SELECT 
                    R.*,
                    U_REQ.USERNAME as ReqUserName,
                    NVL(U_REQ.MOB_PHONE_NO, U_REQ.TELNO) as ReqUserTel,
                    U_REQ.EMAIL_ADDR as ReqUserEmail,                    
                    U_RES.USERNAME as ResUserName,
                    S.CODENM as SystemName,
                    RT.CODENM as ReqTypeName,
                    P.CODENM as PriorityName,
                    PS.CODENM as ProcStatusName,
                    M.CODENM AS ReqMenuName,
                    CO.CORNM as CorpName,
                    D.DEPTNAME as DeptName,
                    OFI.DEPTNAME as OfficeName,
                    TM.DEPTNAME as TeamName
                FROM TB_REQ_INFO R
                LEFT JOIN TB_USER_INFO U_REQ ON R.REQUSERID = U_REQ.USERID
                LEFT JOIN TB_USER_INFO U_RES ON R.RESUSERID = U_RES.USERID
                LEFT JOIN TB_COMM_CODE S ON R.SYSTEMCD = S.CODEID AND S.PARENTID = 19 
                LEFT JOIN TB_COMM_CODE RT ON R.REQTYPE = RT.CODEID AND RT.PARENTID = 13 
                LEFT JOIN TB_COMM_CODE P ON R.PRIORITYCD = P.CODEID AND P.PARENTID = 1 
                LEFT JOIN TB_COMM_CODE PS ON R.PROC_STATUS = PS.CODEID AND PS.PARENTID = 61
                LEFT JOIN TB_COMM_CODE M ON R.REQMENU  = M.CODEID 
                LEFT JOIN TB_COR_INFO CO ON R.CORCD = CO.CORCD
                LEFT JOIN TB_DEPT_INFO D ON R.DEPTCD = D.DEPTCD
                LEFT JOIN TB_DEPT_INFO OFI ON R.OFFICECD = OFI.DEPTCD
                LEFT JOIN TB_DEPT_INFO TM ON R.TEAMCD = TM.DEPTCD
                WHERE R.REQID = :ID AND R.USEYN = 'Y'";
            
            var reqInfo = await _dbConnection.QueryFirstOrDefaultAsync<ReqInfo>(sql, new { ID = id });

            if (reqInfo != null)
            {
                var fileSql = "SELECT * FROM TB_REQ_FILE WHERE REQID = :REQID AND USEYN = 'Y'";
                reqInfo.AttachFiles = (await _dbConnection.QueryAsync<ReqFile>(fileSql, new { REQID = id })).ToList();
                
                var histSql = @" SELECT H.*, PS.CODENM AS HISTORY_REASON_STR, U.USERNAME AS ResUserName
                                 FROM TB_REQ_HIST H 
                                 LEFT JOIN TB_COMM_CODE PS ON H.HISTORY_REASON = PS.CODEID 
                                 LEFT JOIN TB_USER_INFO U ON H.RESUSERID = U.USERID
                                 WHERE H.REQID = :REQID ORDER BY H.HISTORYID DESC";
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
        private async Task<int> CreateReqHistoryAsync(ReqInfo reqInfo, string historyReason, IDbTransaction transaction)
        {
            var parameters = new OracleDynamicParameters();
            parameters.BindByName = true;

            // Add IN parameters for the stored procedure
            parameters.Add("P_REQID", reqInfo.REQID);
            parameters.Add("P_PARENTID", reqInfo.PARENTID);
            parameters.Add("P_TITLE", reqInfo.TITLE);
            parameters.Add("P_REQDATE", reqInfo.REQDATE);
            parameters.Add("P_DUEDATE", reqInfo.DUEDATE);
            parameters.Add("P_EXPECTDATE", reqInfo.EXPECTDATE);
            parameters.Add("P_STARTDATE", reqInfo.STARTDATE);
            parameters.Add("P_ENDDATE", reqInfo.ENDDATE);
            parameters.Add("P_REQTYPE", reqInfo.REQTYPE);
            parameters.Add("P_SYSTEMCD", reqInfo.SYSTEMCD);
            parameters.Add("P_REQMENU", reqInfo.REQMENU);
            parameters.Add("P_REQMENU_ETC", reqInfo.REQMENU_ETC);
            parameters.Add("P_REQTCODE", reqInfo.REQTCODE);
            parameters.Add("P_RESTCODE", reqInfo.RESTCODE);
            parameters.Add("P_BXTID", reqInfo.BXTID);
            parameters.Add("P_REQUSERID", reqInfo.REQUSERID);
            parameters.Add("P_RESUSERID", reqInfo.RESUSERID);
            parameters.Add("P_IMPTCD", reqInfo.IMPTCD);
            parameters.Add("P_DFCLTCD", reqInfo.DFCLTCD);
            parameters.Add("P_PRIORITYCD", reqInfo.PRIORITYCD);
            parameters.Add("P_MAN_DAY", reqInfo.MAN_DAY);
            parameters.Add("P_PROC_STATUS", reqInfo.PROC_STATUS);
            parameters.Add("P_PROC_RATE", reqInfo.PROC_RATE);
            parameters.Add("P_CORCD", reqInfo.CORCD);
            parameters.Add("P_DEPTCD", reqInfo.DEPTCD);
            parameters.Add("P_OFFICECD", reqInfo.OFFICECD);
            parameters.Add("P_TEAMCD", reqInfo.TEAMCD);
            parameters.Add("P_REG_USERID", reqInfo.REG_USERID);
            parameters.Add("P_HISTORY_REASON", historyReason);

            // CLOB Parameters
            parameters.Add("P_CONTENTS_HTML", reqInfo.CONTENTS_HTML, OracleMappingType.Clob);
            parameters.Add("P_CONTENTS_TEXT", reqInfo.CONTENTS_TEXT, OracleMappingType.Clob);
            parameters.Add("P_ANSWER_HTML", reqInfo.ANSWER_HTML, OracleMappingType.Clob);
            parameters.Add("P_ANSWER_TEXT", reqInfo.ANSWER_TEXT, OracleMappingType.Clob);
            parameters.Add("P_DELAYREASON_HTML", reqInfo.DELAYREASON_HTML, OracleMappingType.Clob);
            parameters.Add("P_DELAYREASON_TEXT", reqInfo.DELAYREASON_TEXT, OracleMappingType.Clob);
            parameters.Add("P_NOTE_HTML", reqInfo.NOTE_HTML, OracleMappingType.Clob);
            parameters.Add("P_NOTE_TEXT", reqInfo.NOTE_TEXT, OracleMappingType.Clob);
            
            parameters.Add("P_NEW_HISTORYID", dbType: OracleMappingType.Int32, direction: ParameterDirection.Output);

            // Execute the stored procedure
            await _dbConnection.ExecuteAsync(
                "SP_REQ_CREATE_HISTORY",
                parameters,
                transaction: transaction,
                commandType: CommandType.StoredProcedure
            );
            
            return parameters.Get<int>("P_NEW_HISTORYID");
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
                        VALUES (SEQ_REQ_FILE.NEXTVAL, :REQID, :HISTORYID, :REQTYPE, :UPLOAD_FILENAME, :REAL_FILENAME, :FILEPATH, :REG_USERID, 'Y')";
                    
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

        
        public async Task<bool> UpdateProcStatusAsync(int reqid, string statusVal, string userId)
        {
            // Get the current request info to create a history entry
            var reqInfo = await GetReqInfoByIdAsync(reqid);
            if (reqInfo == null)
            {
                _logger.LogWarning("UpdateProcStatusAsync: ReqInfo with ID {ReqId} not found.", reqid);
                return false;
            }

            var historyReason = "72"; // 접수 CODEID
            // 종결상태
            if (statusVal == "68"){
                historyReason = "76";                
            }else if( statusVal == "63"){
                historyReason = "84";
            }

            using var transaction = _dbConnection.BeginTransaction();
            try
            {
                // Update the fields for the history log
                reqInfo.PROC_STATUS = statusVal;
                reqInfo.UPDATE_USERID = userId;
                reqInfo.UPDATE_DATE = DateTime.Now;

                // Define the SQL to update the process status
                var sql = @"
                    UPDATE TB_REQ_INFO SET 
                        PROC_STATUS = :PROC_STATUS,
                        UPDATE_USERID = :UPDATE_USERID,
                        UPDATE_DATE = :UPDATE_DATE
                    WHERE REQID = :REQID";
                
                // Execute the update
                var affectedRows = await _dbConnection.ExecuteAsync(sql, new 
                { 
                    PROC_STATUS = statusVal, 
                    UPDATE_USERID = userId,
                    UPDATE_DATE = reqInfo.UPDATE_DATE,
                    REQID = reqid 
                }, transaction);

                // If the update was successful, create a history record
                if (affectedRows > 0)
                {
                    await CreateReqHistoryAsync(reqInfo, historyReason, transaction);
                    transaction.Commit();
                    return true;
                }
                else
                {
                    // If no rows were affected, it means the REQID was not found
                    transaction.Rollback();
                    return false;
                }
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                _logger.LogError(ex, "Error updating process status for ReqId {ReqId}", reqid);
                throw; // Re-throw the exception to be handled by the controller
            }
        }

        public async Task<bool> UpdateExpectDateAsync(int reqId, DateTime expectDate, string userId)
        {
            using var transaction = _dbConnection.BeginTransaction();
            try
            {
                // First, get the current state of the request for history logging
                var reqInfo = await GetReqInfoByIdAsync(reqId);
                if (reqInfo == null)
                {
                    _logger.LogWarning("UpdateExpectDateAsync: ReqInfo with ID {ReqId} not found.", reqId);
                    return false;
                }

                // Update the properties for the history log and the update itself
                reqInfo.EXPECTDATE = expectDate;
                reqInfo.UPDATE_USERID = userId;
                reqInfo.UPDATE_DATE = DateTime.Now;

                var sql = "UPDATE TB_REQ_INFO SET EXPECTDATE = :ExpectDate, UPDATE_USERID = :UpdateUserId, UPDATE_DATE = :UpdateDate WHERE REQID = :ReqId";
                var affectedRows = await _dbConnection.ExecuteAsync(sql, new { ExpectDate = expectDate, UpdateUserId = userId, UpdateDate = reqInfo.UPDATE_DATE, ReqId = reqId }, transaction);

                if (affectedRows > 0)
                {                    
                    await CreateReqHistoryAsync(reqInfo, "67", transaction); 
                    transaction.Commit();
                    return true;
                }
                else
                {
                    // If no rows were affected, it means the REQID was not found.
                    transaction.Rollback();
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating expect date for ReqId {ReqId}", reqId);
                transaction.Rollback();
                throw;
            }
        }

        public async Task<bool> UpdateResUserAsync(int reqId, string newResUserId, string updateUserId)
        {
            using var transaction = _dbConnection.BeginTransaction();
            try
            {
                var reqInfo = await GetReqInfoByIdAsync(reqId);
                if (reqInfo == null)
                {
                    _logger.LogWarning("UpdateResUserAsync: ReqInfo with ID {ReqId} not found.", reqId);
                    return false;
                }

                // Update the properties for the history log and the update itself
                reqInfo.RESUSERID = newResUserId;
                reqInfo.UPDATE_USERID = updateUserId;
                reqInfo.UPDATE_DATE = DateTime.Now;

                var sql = "UPDATE TB_REQ_INFO SET RESUSERID = :NewResUserId, UPDATE_USERID = :UpdateUserId, UPDATE_DATE = :UpdateDate WHERE REQID = :ReqId";
                var affectedRows = await _dbConnection.ExecuteAsync(sql, new { NewResUserId = newResUserId, UpdateUserId = updateUserId, UpdateDate = reqInfo.UPDATE_DATE, ReqId = reqId }, transaction);

                if (affectedRows > 0)
                {
                    await CreateReqHistoryAsync(reqInfo, "75", transaction); // Using 75 as a placeholder for "Assignee Changed"
                    transaction.Commit();
                    return true;
                }
                else
                {
                    transaction.Rollback();
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating assignee for ReqId {ReqId}", reqId);
                transaction.Rollback();
                throw;
            }
        }
    }
}

