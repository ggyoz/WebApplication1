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
    public class MyPageService
    {
        private readonly IDbConnection _connection;

        public MyPageService(IDbConnection connection)
        {
            _connection = connection;
        }

        public async Task<User?> GetMyInfoAsync(string userId)
        {
            var sql = @"
                SELECT 
                    USERID AS UserId, USERPWD AS UserPwd, USERNAME AS UserName, EMPNO AS EmpNo, CORCD AS CorCd, 
                    DEPTCD AS DeptCd, OFFICECD AS OfficeCd, TEAMCD AS TeamCd, SYSCD AS SysCd, BIZCD AS BizCd, 
                    TELNO AS TelNo, MOB_PHONE_NO AS MobPhoneNo, EMAIL_ADDR AS EmailAddr, USERSTAT AS UserStat, 
                    RETIRE_DATE AS RetireDate, ADMIN_FLAG AS AdminFlag, CUSTCD AS CustCd, VENDCD AS VendCd, 
                    AUTH_FLAG AS AuthFlag, USER_DIV AS UserDiv, PW_MISS_COUNT AS PwMissCount, 
                    REG_DATE AS RegDate, REG_USERID AS RegUserId, UPDATE_DATE AS UpdateDate, 
                    UPDATE_USERID AS UpdateUserId, USEYN AS UseYn
                FROM TB_USER_INFO
                WHERE USERID = :UserId";

            var user = await _connection.QueryFirstOrDefaultAsync<User>(sql, new { UserId = userId });
            return user;
        }

        public async Task UpdateMyInfoAsync(User user, string updateUserId)
        {

            var parameters = new DynamicParameters();
            parameters.Add("UserId", user.UserId);
            parameters.Add("UserName", user.UserName);
            parameters.Add("TelNo", user.TelNo);
            parameters.Add("MobPhoneNo", user.MobPhoneNo);
            parameters.Add("EmailAddr", user.EmailAddr);
            // parameters.Add("CorCd", user.CorCd);
            // parameters.Add("DeptCd", user.DeptCd);
            // parameters.Add("OfficeCd", user.OfficeCd);
            // parameters.Add("TeamCd", user.TeamCd);
            parameters.Add("UpdateUserId", updateUserId);


            var setClauses = new List<string>
            {
                "USERNAME = :UserName",
                "TELNO = :TelNo",
                "MOB_PHONE_NO = :MobPhoneNo",
                "EMAIL_ADDR = :EmailAddr",
                "UPDATE_DATE = SYSDATE",
                // "CORCD = :CorCd",
                // "DEPTCD = :DeptCd",
                // "OFFICECD = :OfficeCd",
                // "TEAMCD = :TEAMCD",
                "UPDATE_USERID = :UpdateUserId"
            };

            // Only update password if a new one is provided
            if (!string.IsNullOrWhiteSpace(user.UserPwd))
            {
                parameters.Add("UserPwd", CreatePasswordHash(user.UserPwd));
                setClauses.Add("USERPWD = :UserPwd");
            }

            var sql = $@"
                UPDATE TB_USER_INFO SET
                    {string.Join(", ", setClauses)}
                WHERE USERID = :UserId";

            // // --- 쿼리디버깅코드 ---
            // Console.WriteLine("Executing CreateUserAsync Query:");
            // Console.WriteLine(sql);
            // Console.WriteLine("Parameters: " + JsonConvert.SerializeObject(user, Formatting.Indented));

            await _connection.ExecuteAsync(sql, parameters);

        }        
        
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
    }
}
