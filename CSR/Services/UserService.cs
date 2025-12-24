using Oracle.ManagedDataAccess.Client;
using CSR.Models;
using System.Data;

namespace CSR.Services
{
    public class UserService
    {
        private readonly OracleConnection _connection;

        public UserService(OracleConnection connection)
        {
            _connection = connection;
        }

        public async Task<List<User>> GetAllUsersAsync()
        {
            var users = new List<User>();
            if (_connection.State != ConnectionState.Open)
            {
                await _connection.OpenAsync();
            }

            using (var command = _connection.CreateCommand())
            {
                command.CommandText = @"
                    SELECT 
                        USERID, USERPWD, USERNAME, EMPNO, DEPTCD, DEPTNAME, 
                        TEAMCD, TEAMNAME, CORCD, SYSCD, BIZCD, TELNO, MOB_PHONE_NO, 
                        EMAIL_ADDR, RETIRE_DATE, CORNAME, SYSNAME, BIZNAME, ADMIN_FLAG, 
                        CUSTCD, VENDCD, AUTH_FLAG, USER_DIV, PW_MISS_COUNT, REG_DATE, 
                        REG_USERID, UPDATE_DATE, UPDATE_USERID
                    FROM TB_USER_INFO";
                command.CommandType = CommandType.Text;

                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        users.Add(MapToUser(reader));
                    }
                }
            }
            await _connection.CloseAsync(); // Close the connection after use.
            return users;
        }

        public async Task<User?> GetUserByIdAsync(string userId)
        {
            User? user = null;
            if (_connection.State != ConnectionState.Open)
            {
                await _connection.OpenAsync();
            }

            using (var command = _connection.CreateCommand())
            {
                command.CommandText = @"
                    SELECT 
                        USERID, USERPWD, USERNAME, EMPNO, DEPTCD, DEPTNAME, 
                        TEAMCD, TEAMNAME, CORCD, SYSCD, BIZCD, TELNO, MOB_PHONE_NO, 
                        EMAIL_ADDR, RETIRE_DATE, CORNAME, SYSNAME, BIZNAME, ADMIN_FLAG, 
                        CUSTCD, VENDCD, AUTH_FLAG, USER_DIV, PW_MISS_COUNT, REG_DATE, 
                        REG_USERID, UPDATE_DATE, UPDATE_USERID
                    FROM TB_USER_INFO
                    WHERE USERID = :UserId";
                command.CommandType = CommandType.Text;
                command.Parameters.Add(new OracleParameter("UserId", userId));

                using (var reader = await command.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        user = MapToUser(reader);
                    }
                }
            }
            await _connection.CloseAsync();
            return user;
        }

        public async Task CreateUserAsync(User user)
        {
            if (_connection.State != ConnectionState.Open)
            {
                await _connection.OpenAsync();
            }

            using (var command = _connection.CreateCommand())
            {
                command.CommandText = @"
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
                command.CommandType = CommandType.Text;
                AddUserParameters(command, user);
                await command.ExecuteNonQueryAsync();
            }
            await _connection.CloseAsync();
        }

        public async Task UpdateUserAsync(User user)
        {
            if (_connection.State != ConnectionState.Open)
            {
                await _connection.OpenAsync();
            }

            using (var command = _connection.CreateCommand())
            {
                command.CommandText = @"
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
                command.CommandType = CommandType.Text;
                AddUserParameters(command, user);
                await command.ExecuteNonQueryAsync();
            }
            await _connection.CloseAsync();
        }

        public async Task DeleteUserAsync(string userId)
        {
            if (_connection.State != ConnectionState.Open)
            {
                await _connection.OpenAsync();
            }

            using (var command = _connection.CreateCommand())
            {
                command.CommandText = "DELETE FROM TB_USER_INFO WHERE USERID = :UserId";
                command.CommandType = CommandType.Text;
                command.Parameters.Add(new OracleParameter("UserId", userId));
                await command.ExecuteNonQueryAsync();
            }
            await _connection.CloseAsync();
        }

        private User MapToUser(OracleDataReader reader)
        {
            return new User
            {
                UserId = reader["USERID"].ToString() ?? string.Empty,
                UserPwd = reader["USERPWD"].ToString() ?? string.Empty,
                UserName = reader["USERNAME"].ToString() ?? string.Empty,
                EmpNo = reader["EMPNO"].ToString() ?? string.Empty,
                DeptCd = reader["DEPTCD"].ToString() ?? string.Empty,
                DeptName = reader["DEPTNAME"].ToString() ?? string.Empty,
                TeamCd = reader["TEAMCD"].ToString() ?? string.Empty,
                TeamName = reader["TEAMNAME"].ToString() ?? string.Empty,
                CorCd = reader["CORCD"].ToString() ?? string.Empty,
                SysCd = reader["SYSCD"].ToString() ?? string.Empty,
                BizCd = reader["BIZCD"].ToString() ?? string.Empty,
                TelNo = reader["TELNO"].ToString() ?? string.Empty,
                MobPhoneNo = reader["MOB_PHONE_NO"].ToString() ?? string.Empty,
                EmailAddr = reader["EMAIL_ADDR"].ToString() ?? string.Empty,
                RetireDate = reader["RETIRE_DATE"].ToString() ?? string.Empty,
                CorName = reader["CORNAME"].ToString() ?? string.Empty,
                SysName = reader["SYSNAME"].ToString() ?? string.Empty,
                BizName = reader["BIZNAME"].ToString() ?? string.Empty,
                AdminFlag = reader["ADMIN_FLAG"] is DBNull ? false : Convert.ToInt32(reader["ADMIN_FLAG"]) == 1,
                CustCd = reader["CUSTCD"].ToString() ?? string.Empty,
                VendCd = reader["VENDCD"].ToString() ?? string.Empty,
                AuthFlag = reader["AUTH_FLAG"] is DBNull ? 0 : Convert.ToInt32(reader["AUTH_FLAG"]),
                UserDiv = reader["USER_DIV"].ToString() ?? string.Empty,
                PwMissCount = reader["PW_MISS_COUNT"] is DBNull ? 0 : Convert.ToInt32(reader["PW_MISS_COUNT"]),
                RegDate = reader["REG_DATE"] is DBNull ? null : Convert.ToDateTime(reader["REG_DATE"]),
                RegUserId = reader["REG_USERID"].ToString() ?? string.Empty,
                UpdateDate = reader["UPDATE_DATE"] is DBNull ? null : Convert.ToDateTime(reader["UPDATE_DATE"]),
                UpdateUserId = reader["UPDATE_USERID"].ToString() ?? string.Empty
            };
        }

        private void AddUserParameters(OracleCommand command, User user)
        {
            command.Parameters.Add(new OracleParameter("UserId", user.UserId));
            command.Parameters.Add(new OracleParameter("UserPwd", user.UserPwd));
            command.Parameters.Add(new OracleParameter("UserName", user.UserName));
            command.Parameters.Add(new OracleParameter("EmpNo", user.EmpNo));
            command.Parameters.Add(new OracleParameter("DeptCd", user.DeptCd));
            command.Parameters.Add(new OracleParameter("DeptName", user.DeptName));
            command.Parameters.Add(new OracleParameter("TeamCd", user.TeamCd));
            command.Parameters.Add(new OracleParameter("TeamName", user.TeamName));
            command.Parameters.Add(new OracleParameter("CorCd", user.CorCd));
            command.Parameters.Add(new OracleParameter("SysCd", user.SysCd));
            command.Parameters.Add(new OracleParameter("BizCd", user.BizCd));
            command.Parameters.Add(new OracleParameter("TelNo", user.TelNo));
            command.Parameters.Add(new OracleParameter("MobPhoneNo", user.MobPhoneNo));
            command.Parameters.Add(new OracleParameter("EmailAddr", user.EmailAddr));
            command.Parameters.Add(new OracleParameter("RetireDate", user.RetireDate));
            command.Parameters.Add(new OracleParameter("CorName", user.CorName));
            command.Parameters.Add(new OracleParameter("SysName", user.SysName));
            command.Parameters.Add(new OracleParameter("BizName", user.BizName));
            command.Parameters.Add(new OracleParameter("AdminFlag", user.AdminFlag ? 1 : 0));
            command.Parameters.Add(new OracleParameter("CustCd", user.CustCd));
            command.Parameters.Add(new OracleParameter("VendCd", user.VendCd));
            command.Parameters.Add(new OracleParameter("AuthFlag", user.AuthFlag));
            command.Parameters.Add(new OracleParameter("UserDiv", user.UserDiv));
            command.Parameters.Add(new OracleParameter("PwMissCount", user.PwMissCount));
            command.Parameters.Add(new OracleParameter("RegDate", (object?)user.RegDate ?? DBNull.Value));
            command.Parameters.Add(new OracleParameter("RegUserId", user.RegUserId));
            command.Parameters.Add(new OracleParameter("UpdateDate", (object?)user.UpdateDate ?? DBNull.Value));
            command.Parameters.Add(new OracleParameter("UpdateUserId", user.UpdateUserId));
        }
    }
}
