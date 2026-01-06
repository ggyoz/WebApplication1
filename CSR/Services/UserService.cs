using CSR.Models;
using System.Data;
using Dapper;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;

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
            TELNO AS TelNo, MOB_PHONE_NO AS MobPhoneNo, EMAIL_ADDR AS EmailAddr, USERSTAT AS UserStat, 
            RETIRE_DATE AS RetireDate, ADMIN_FLAG AS AdminFlag, CUSTCD AS CustCd, VENDCD AS VendCd, 
            AUTH_FLAG AS AuthFlag, USER_DIV AS UserDiv, PW_MISS_COUNT AS PwMissCount, 
            REG_DATE AS RegDate, REG_USERID AS RegUserId, UPDATE_DATE AS UpdateDate, 
            UPDATE_USERID AS UpdateUserId, USEYN AS UseYn
        ";

        public async Task<User?> AuthenticateAsync(string userId, string password)
        {
            var user = await GetUserByIdAsync(userId);

            if (user == null || string.IsNullOrEmpty(user.UserPwd))
            {
                return null;
            }

            if (VerifyPasswordHash(password, user.UserPwd))
            {
                return user;
            }

            return null;
        }

        /// <summary>
        /// Password Hashing
        /// </summary>
        /// <param name="password">The password to hash</param>
        /// <returns>The password hash</returns>
        private string CreatePasswordHash(string password)
        {
            // Generate a salt
            byte[] salt = new byte[16];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }

            // Hash the password
            using (var sha256 = SHA256.Create())
            {
                var combined = salt.Concat(Encoding.UTF8.GetBytes(password)).ToArray();
                byte[] hash = sha256.ComputeHash(combined);
                
                // Combine salt and hash for storage
                return $"{Convert.ToHexString(salt)}:{Convert.ToHexString(hash)}";
            }
        }

        private bool VerifyPasswordHash(string password, string storedHashString)
        {
            try
            {
                // Split the stored string into salt and hash
                var parts = storedHashString.Split(':');
                if (parts.Length != 2) return false; // Invalid format

                byte[] salt = Convert.FromHexString(parts[0]);
                byte[] storedHash = Convert.FromHexString(parts[1]);
                
                // Hash the incoming password with the stored salt
                using (var sha256 = SHA256.Create())
                {
                    var combined = salt.Concat(Encoding.UTF8.GetBytes(password)).ToArray();
                    byte[] computedHash = sha256.ComputeHash(combined);

                    // Compare the computed hash with the stored hash
                    return computedHash.SequenceEqual(storedHash);
                }
            }
            catch
            {
                // Handle exceptions for invalid hex strings etc.
                return false;
            }
        }

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
            
            var pagedSql = $@"
                WITH PagedUsers AS (
                    SELECT 
                        {SelectColumns},
                        ROW_NUMBER() OVER (ORDER BY REG_DATE DESC) AS RN
                    FROM TB_USER_INFO
                    {whereSql}
                )
                SELECT * FROM PagedUsers
                WHERE RN > {offset} AND RN <= {offset + pageSize}
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
            // Hash the password before saving
            if (!string.IsNullOrEmpty(user.UserPwd))
            {
                user.UserPwd = CreatePasswordHash(user.UserPwd);
            }

            var sql = @"
                INSERT INTO TB_USER_INFO (
                    USERID, USERPWD, USERNAME, EMPNO, CORCD, DEPTCD, OFFICECD, TEAMCD, SYSCD, BIZCD, 
                    TELNO, MOB_PHONE_NO, EMAIL_ADDR, USERSTAT, CUSTCD, VENDCD, 
                    AUTH_FLAG, USER_DIV, REG_DATE, REG_USERID, USEYN
                ) VALUES (
                    :UserId, :UserPwd, :UserName, :EmpNo, :CorCd, :DeptCd, :OfficeCd, :TeamCd, :SysCd, :BizCd,
                    :TelNo, :MobPhoneNo, :EmailAddr, :UserStat, :CustCd, :VendCd,
                    :AuthFlag, :UserDiv, SYSDATE, :RegUserId, 'Y'
                )";

            // --- 여기부터 추가되는 코드 ---
    Console.WriteLine("Executing CreateUserAsync Query:");
    Console.WriteLine(sql);
    Console.WriteLine("Parameters: " + JsonConvert.SerializeObject(user, Formatting.Indented));
    // --- 여기까지 추가되는 코드 ---

    await _connection.ExecuteAsync(sql, user);
        }

        public async Task UpdateUserAsync(User user)
        {
            var parameters = new DynamicParameters(user);
            var setClauses = new List<string>
            {
                "USERNAME = :UserName",
                "EMPNO = :EmpNo",
                "CORCD = :CorCd",
                "DEPTCD = :DeptCd",
                "OFFICECD = :OfficeCd",
                "TEAMCD = :TeamCd",
                "SYSCD = :SysCd",
                "BIZCD = :BizCd",
                "TELNO = :TelNo",
                "MOB_PHONE_NO = :MobPhoneNo",
                "EMAIL_ADDR = :EmailAddr",
                "USERSTAT = :UserStat",
                "RETIRE_DATE = :RetireDate",
                "ADMIN_FLAG = :AdminFlag",
                "CUSTCD = :CustCd",
                "VENDCD = :VendCd",
                "AUTH_FLAG = :AuthFlag",
                "USER_DIV = :UserDiv",
                "PW_MISS_COUNT = :PwMissCount",
                "UPDATE_DATE = SYSDATE",
                "UPDATE_USERID = :UpdateUserId",
                "USEYN = :UseYn"
            };

            // Only update password if a new one is provided
            if (!string.IsNullOrWhiteSpace(user.UserPwd))
            {
                parameters.Add("UserPwd", CreatePasswordHash(user.UserPwd));
                setClauses.Add("USERPWD = :UserPwd");
            }

            var sql = $@"
                UPDATE TB_USER_INFO SET
                    {string.Join(", \n", setClauses)}
                WHERE USERID = :UserId";

            await _connection.ExecuteAsync(sql, parameters);
        }

        public async Task<string> ResetPasswordAsync(string userId)
        {
            // 1. Get the user's EmpNo
            var user = await GetUserByIdAsync(userId); // Assuming GetUserByIdAsync fetches EmpNo

            if (user == null || string.IsNullOrWhiteSpace(user.EmpNo))
            {
                throw new InvalidOperationException($"User with ID {userId} not found or has no employee number.");
            }

            string newPlainTextPassword = user.EmpNo; // New requirement: password is EmpNo

            // 2. Hash the new password (EmpNo)
            var hashedPassword = CreatePasswordHash(newPlainTextPassword);

            // 3. Update the user's password in the database
            var sql = @"
                UPDATE TB_USER_INFO
                SET USERPWD = :HashedPassword,
                    PW_MISS_COUNT = 0, -- Reset password failure count
                    UPDATE_DATE = SYSDATE
                WHERE USERID = :UserId";

            await _connection.ExecuteAsync(sql, new { HashedPassword = hashedPassword, UserId = userId });

            return newPlainTextPassword; // Return the EmpNo as the new password
        }

        public async Task DeleteUserAsync(string userId)
        {
            var sql = "DELETE FROM TB_USER_INFO WHERE USERID = :UserId";
            await _connection.ExecuteAsync(sql, new { UserId = userId });
        }
    }
}
