using CSR.Models;
using System.Data;
using Dapper;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace CSR.Services
{
    public class UserService
    {
        private readonly IDbConnection _connection;

        public UserService(IDbConnection connection)
        {
            _connection = connection;
        }

        private const string SelectColumns = @"
            USERID AS UserId, USERPWD AS UserPwd, USERNAME AS UserName, EMPNO AS EmpNo, CORCD AS CorCd, 
            DEPTCD AS DeptCd, OFFICECD AS OfficeCd, TEAMCD AS TeamCd, SYSCD AS SysCd, BIZCD AS BizCd, 
            TELNO AS TelNo, MOB_PHONE_NO AS MobPhoneNo, EMAIL_ADDR AS EmailAddr, USERSTAT AS Status, 
            RETIRE_DATE AS RetireDate, ADMIN_FLAG AS AdminFlag, CUSTCD AS CustCd, VENDCD AS VendCd, 
            AUTH_FLAG AS AuthFlag, USER_DIV AS UserDiv, PW_MISS_COUNT AS PwMissCount, 
            REG_DATE AS RegDate, REG_USERID AS RegUserId, UPDATE_DATE AS UpdateDate, 
            UPDATE_USERID AS UpdateUserId, USEYN AS UseYn
        ";

        // Paged User list
        public async Task<PagedResult<User>> GetUsersAsync(UserSearchViewModel search, int pageNumber, int pageSize)
        {
            var whereClauses = new List<string>();
            var parameters = new DynamicParameters();

            if (!string.IsNullOrWhiteSpace(search.UserId))
            {
                whereClauses.Add("UPPER(USERID) LIKE '%' || UPPER(:UserId) || '%'");
                parameters.Add("UserId", search.UserId);
            }
            if (!string.IsNullOrWhiteSpace(search.UserName))
            {
                whereClauses.Add("UPPER(USERNAME) LIKE '%' || UPPER(:UserName) || '%'");
                parameters.Add("UserName", search.UserName);
            }
            if (!string.IsNullOrWhiteSpace(search.CorCd))
            {
                whereClauses.Add("CORCD = :CorCd");
                parameters.Add("CorCd", search.CorCd);
            }
            if (!string.IsNullOrWhiteSpace(search.BizCd))
            {
                whereClauses.Add("BIZCD = :BizCd");
                parameters.Add("BizCd", search.BizCd);
            }
            if (!string.IsNullOrWhiteSpace(search.DeptCd))
            {
                whereClauses.Add("DEPTCD = :DeptCd");
                parameters.Add("DeptCd", search.DeptCd);
            }
            if (!string.IsNullOrWhiteSpace(search.TeamCd))
            {
                whereClauses.Add("TEAMCD = :TeamCd");
                parameters.Add("TeamCd", search.TeamCd);
            }
            
            var whereSql = whereClauses.Any() ? " WHERE " + string.Join(" AND ", whereClauses) : "";

            var countSql = $"SELECT COUNT(*) FROM TB_USER_INFO {whereSql}";
            var totalCount = await _connection.ExecuteScalarAsync<int>(countSql, parameters);

            var offset = (pageNumber - 1) * pageSize;
            parameters.Add("startnum", offset);
            parameters.Add("endnum", offset + pageSize);
            
            var pagedSql = $@"
                SELECT * FROM (
                    SELECT 
                        ROW_NUMBER() OVER (ORDER BY REG_DATE DESC) AS RN,
                        {SelectColumns}
                    FROM TB_USER_INFO
                    {whereSql}
                ) WHERE RN > :startnum AND RN <= :endnum 
                ORDER BY RN";
            
            var users = await _connection.QueryAsync<User>(pagedSql, parameters);
            
            return new PagedResult<User>(users.ToList(), totalCount, pageNumber, pageSize);
        }

        // User 개별 조회
        public async Task<User?> GetUserByIdAsync(string userId)
        {
            var sql = $@"
                    SELECT {SelectColumns}
                    FROM TB_USER_INFO
                    WHERE USERID = :UserId";

            var user = await _connection.QueryFirstOrDefaultAsync<User>(sql, new { UserId = userId });
            return user;
        }

        public async Task CreateUserAsync(User user)
        {
            // The `User` model now reflects the latest `TB_USER_INFO` schema.
            var sql = @"
                INSERT INTO TB_USER_INFO (
                    USERID, USERPWD, USERNAME, EMPNO, CORCD, DEPTCD, OFFICECD, TEAMCD, SYSCD, BIZCD, 
                    TELNO, MOB_PHONE_NO, EMAIL_ADDR, STATUS, RETIRE_DATE, ADMIN_FLAG, CUSTCD, VENDCD, 
                    AUTH_FLAG, USER_DIV, PW_MISS_COUNT, REG_DATE, REG_USERID, UPDATE_DATE, UPDATE_USERID, USEYN
                ) VALUES (
                    :UserId, :UserPwd, :UserName, :EmpNo, :CorCd, :DeptCd, :OfficeCd, :TeamCd, :SysCd, :BizCd,
                    :TelNo, :MobPhoneNo, :EmailAddr, :Status, :RetireDate, :AdminFlag, :CustCd, :VendCd,
                    :AuthFlag, :UserDiv, :PwMissCount, SYSDATE, :RegUserId, SYSDATE, :UpdateUserId, :UseYn
                )";

            await _connection.ExecuteAsync(sql, user);
        }

        public async Task UpdateUserAsync(User user)
        {
            var sql = @"
                UPDATE TB_USER_INFO SET
                    USERPWD = :UserPwd, 
                    USERNAME = :UserName, 
                    EMPNO = :EmpNo, 
                    CORCD = :CorCd,
                    DEPTCD = :DeptCd, 
                    OFFICECD = :OfficeCd,
                    TEAMCD = :TeamCd, 
                    SYSCD = :SysCd, 
                    BIZCD = :BizCd, 
                    TELNO = :TelNo, 
                    MOB_PHONE_NO = :MobPhoneNo, 
                    EMAIL_ADDR = :EmailAddr, 
                    STATUS = :Status,
                    RETIRE_DATE = :RetireDate, 
                    ADMIN_FLAG = :AdminFlag, 
                    CUSTCD = :CustCd, 
                    VENDCD = :VendCd, 
                    AUTH_FLAG = :AuthFlag, 
                    USER_DIV = :UserDiv, 
                    PW_MISS_COUNT = :PwMissCount, 
                    UPDATE_DATE = SYSDATE, 
                    UPDATE_USERID = :UpdateUserId,
                    USEYN = :UseYn
                WHERE USERID = :UserId";

            await _connection.ExecuteAsync(sql, user);
        }

        public async Task DeleteUserAsync(string userId)
        {
            var sql = "DELETE FROM TB_USER_INFO WHERE USERID = :UserId";
            await _connection.ExecuteAsync(sql, new { UserId = userId });
        }
    }
}
