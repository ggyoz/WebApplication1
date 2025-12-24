using Oracle.ManagedDataAccess.Client;
using CSR.Models;
using System.Data;
using Dapper;

namespace CSR.Services
{
    public class UserService
    {
        private readonly OracleConnection _connection;

        public UserService(OracleConnection connection)
        {
            _connection = connection;
        }

        // User 전체 조회
        public async Task<List<User>> GetAllUsersAsync()
        {
            var sql = @"
                SELECT 
                    USERID, USERPWD, USERNAME, EMPNO, DEPTCD, DEPTNAME, 
                    TEAMCD, TEAMNAME, CORCD, SYSCD, BIZCD, TELNO, MOB_PHONE_NO, 
                    EMAIL_ADDR, RETIRE_DATE, CORNAME, SYSNAME, BIZNAME, ADMIN_FLAG, 
                    CUSTCD, VENDCD, AUTH_FLAG, USER_DIV, PW_MISS_COUNT, REG_DATE, 
                    REG_USERID, UPDATE_DATE, UPDATE_USERID
                FROM TB_USER_INFO";
            
            var users = await _connection.QueryAsync<User>(sql);
            return users.ToList();
        }

        // User 개별 조회
        public async Task<User?> GetUserByIdAsync(string userId)
        {
            var sql = @"
                    SELECT 
                        USERID, USERPWD, USERNAME, EMPNO, DEPTCD, DEPTNAME, 
                        TEAMCD, TEAMNAME, CORCD, SYSCD, BIZCD, TELNO, MOB_PHONE_NO, 
                        EMAIL_ADDR, RETIRE_DATE, CORNAME, SYSNAME, BIZNAME, ADMIN_FLAG, 
                        CUSTCD, VENDCD, AUTH_FLAG, USER_DIV, PW_MISS_COUNT, REG_DATE, 
                        REG_USERID, UPDATE_DATE, UPDATE_USERID
                    FROM TB_USER_INFO
                    WHERE USERID = :UserId";

            var user = await _connection.QueryFirstOrDefaultAsync<User>(sql, new { UserId = userId });
            return user;
        }

        public async Task CreateUserAsync(User user)
        {
            var sql = @"
                INSERT INTO TB_USER_INFO (
                    USERID, USERPWD, USERNAME, EMPNO, DEPTCD, DEPTNAME, 
                    TEAMCD, TEAMNAME, CORCD, SYSCD, BIZCD, TELNO, MOB_PHONE_NO, 
                    EMAIL_ADDR, RETIRE_DATE, CORNAME, SYSNAME, BIZNAME, ADMIN_FLAG, 
                    CUSTCD, VENDCD, AUTH_FLAG, USER_DIV, PW_MISS_COUNT, REG_DATE, 
                    REG_USERID, UPDATE_DATE, UPDATE_USERID
                ) VALUES (
                    :UserId, :UserPwd, :UserName, :EmpNo, :DeptCd, :DeptName, 
                    :TeamCd, :TeamName, :CorCd, :SysCd, :BizCd, :TelNo, :MobPhoneNo, 
                    :EmailAddr, :RetireDate, :CorName, :SysName, :BizName, :AdminFlag, 
                    :CustCd, :VendCd, :AuthFlag, :UserDiv, :PwMissCount, :RegDate, 
                    :RegUserId, :UpdateDate, :UpdateUserId
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
                    DEPTCD = :DeptCd, 
                    DEPTNAME = :DeptName, 
                    TEAMCD = :TeamCd, 
                    TEAMNAME = :TeamName, 
                    CORCD = :CorCd, 
                    SYSCD = :SysCd, 
                    BIZCD = :BizCd, 
                    TELNO = :TelNo, 
                    MOB_PHONE_NO = :MobPhoneNo, 
                    EMAIL_ADDR = :EmailAddr, 
                    RETIRE_DATE = :RetireDate, 
                    CORNAME = :CorName, 
                    SYSNAME = :SysName, 
                    BIZNAME = :BizName, 
                    ADMIN_FLAG = :AdminFlag, 
                    CUSTCD = :CustCd, 
                    VENDCD = :VendCd, 
                    AUTH_FLAG = :AuthFlag, 
                    USER_DIV = :UserDiv, 
                    PW_MISS_COUNT = :PwMissCount, 
                    REG_DATE = :RegDate, 
                    REG_USERID = :RegUserId, 
                    UPDATE_DATE = :UpdateDate, 
                    UPDATE_USERID = :UpdateUserId
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
