using Dapper;
using CSR.Models;
using System.Data;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace CSR.Services
{
    public interface ICommCodeService
    {
        Task<List<CommCode>> GetCommCodeTreeAsync();
        Task<CommCode?> GetCommCodeByIdAsync(int id);
        Task<int> CreateCommCodeAsync(CommCode commCode);
        Task UpdateCommCodeAsync(CommCode commCode);
        Task<List<CommCode>> GetAllCommCodesAsync();
        Task<IEnumerable<SelectListItem>> GetNoticeTypeSelectListAsync(); // New method
    }

    public class CommCodeService : ICommCodeService
    {
        private readonly IDbConnection _dbConnection;

        public CommCodeService(IDbConnection connection)
        {
            _dbConnection = connection;
        }

        public async Task<List<CommCode>> GetAllCommCodesAsync()
        {
            const string sql = "SELECT * FROM TB_COMM_CODE ORDER BY PARENTID, SORTORDER";
            var result = await _dbConnection.QueryAsync<CommCode>(sql);
            return result.ToList();
        }

        public async Task<List<CommCode>> GetCommCodeTreeAsync()
        {
            var allCodes = await GetAllCommCodesAsync();
            var codeDict = allCodes.ToDictionary(c => c.CODEID);
            var rootCodes = new List<CommCode>();

            foreach (var code in allCodes)
            {
                if (code.PARENTID == 0 || !codeDict.ContainsKey(code.PARENTID))
                {
                    rootCodes.Add(code);
                }
                else
                {
                    if (codeDict.TryGetValue(code.PARENTID, out var parent))
                    {
                        parent.Children ??= new List<CommCode>();
                        parent.Children.Add(code);
                        code.Parent = parent;
                    }
                }
            }
            
            foreach(var code in allCodes.Where(c => c.Children != null))
            {
                code.Children = code.Children.OrderBy(c => c.SORTORDER).ToList();
            }

            return rootCodes.OrderBy(c => c.SORTORDER).ToList();
        }

        public async Task<CommCode?> GetCommCodeByIdAsync(int id)
        {
            const string sql = "SELECT * FROM TB_COMM_CODE WHERE CODEID = :Id";
            return await _dbConnection.QueryFirstOrDefaultAsync<CommCode>(sql, new { Id = id });
        }

        public async Task<int> CreateCommCodeAsync(CommCode commCode)
        {
            var sql = @"
                INSERT INTO TB_COMM_CODE (
                    CODEID, PARENTID, CODENM, CODE, SORTORDER, NOTE, USEYN, REG_USERID, REG_DATE
                ) VALUES (
                   SEQ_COMM_CODE.NEXTVAL, :PARENTID, :CODENM, :CODE, :SORTORDER, :NOTE, :USEYN, :REG_USERID, SYSDATE
                ) RETURNING CODEID INTO :NewCodeId";

            var parameters = new DynamicParameters(commCode);
            parameters.Add("NewCodeId", dbType: DbType.Int32, direction: ParameterDirection.Output);

            await _dbConnection.ExecuteAsync(sql, parameters);

            // --- 쿼리디버킹코드 ---
            Console.WriteLine("Executing CreateUserAsync Query:");
            Console.WriteLine(sql);
            Console.WriteLine("Parameters: " + JsonConvert.SerializeObject(commCode, Formatting.Indented));
            // --- 쿼리디버킹코드 ---
            
            return parameters.Get<int>("NewCodeId");
        }

        public async Task UpdateCommCodeAsync(CommCode commCode)
        {
            var sql = @"
                UPDATE TB_COMM_CODE SET
                    PARENTID = :PARENTID,
                    CODENM = :CODENM,
                    CODE = :CODE,
                    SORTORDER = :SORTORDER,
                    NOTE = :NOTE,
                    USEYN = :USEYN,
                    UPDATE_USERID = :UPDATE_USERID,
                    UPDATE_DATE = SYSDATE
                WHERE CODEID = :CODEID";
            await _dbConnection.ExecuteAsync(sql, commCode);
        }

        // New method implementation
        public async Task<IEnumerable<SelectListItem>> GetNoticeTypeSelectListAsync()
        {
            var allCodes = await GetAllCommCodesAsync();
            var noticeTypes = allCodes.Where(c => c.PARENTID == 54 && c.USEYN == "Y") // Filter for notice types and active ones
                                      .OrderBy(c => c.SORTORDER)
                                      .Select(c => new SelectListItem
                                      {
                                          Value = c.CODE, // Assuming CODE is the value for SelectList
                                          Text = c.CODENM // CODENM is the display text for SelectList
                                      });
            return noticeTypes;
        }
    }
}
