using Dapper;
using CSR.Models;
using System.Data;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace CSR.Services
{
    public class CorpService
    {
        private readonly IDbConnection _dbConnection;

        public CorpService(IDbConnection connection)
        {
            _dbConnection = connection;
        }

        private const string SelectColumns = @"
            CORCD AS CorCd, CORNM AS CorNm, NATIONCD AS NationCd, COINCD AS CoinCd, 
            ""LANGUAGE"" AS Language, ACC_TITLE AS AccTitle
        ";

        public async Task<List<Corp>> GetAllCorpsAsync()
        {
            var sql = $"SELECT {SelectColumns} FROM TB_COR_INFO ORDER BY CORCD";
            var result = await _dbConnection.QueryAsync<Corp>(sql);
            return result.ToList();
        }

        public async Task<Corp?> GetCorpByIdAsync(string id)
        {
            var sql = $"SELECT {SelectColumns} FROM TB_COR_INFO WHERE CORCD = :Id";
            return await _dbConnection.QueryFirstOrDefaultAsync<Corp>(sql, new { Id = id });
        }

        public async Task CreateCorpAsync(Corp corp)
        {
            var sql = @"
                INSERT INTO TB_COR_INFO (CORCD, CORNM, NATIONCD, COINCD, ""LANGUAGE"", ACC_TITLE)
                VALUES (:CorCd, :CorNm, :NationCd, :CoinCd, :Language, :AccTitle)";
            await _dbConnection.ExecuteAsync(sql, corp);
        }

        public async Task UpdateCorpAsync(Corp corp)
        {
            var sql = @"
                UPDATE TB_COR_INFO SET
                    CORNM = :CorNm,
                    NATIONCD = :NationCd,
                    COINCD = :CoinCd,
                    ""LANGUAGE"" = :Language,
                    ACC_TITLE = :AccTitle
                WHERE CORCD = :CorCd";
            await _dbConnection.ExecuteAsync(sql, corp);
        }

        public async Task DeleteCorpAsync(string id)
        {
            var sql = "DELETE FROM TB_COR_INFO WHERE CORCD = :Id";
            await _dbConnection.ExecuteAsync(sql, new { Id = id });
        }
        
        
        public async Task<List<Corp>> GetAutoCompleteCorpAsync(string searchString)
        {

            var whereClauses = new List<string>();
            var parameters = new DynamicParameters();

            Console.WriteLine("searchString : " + searchString);

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                whereClauses.Add("CORCD LIKE :CorCd");
                parameters.Add("CorCd", "%" + searchString + "%");
            }

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                whereClauses.Add("CORNM LIKE :CorNm");
                parameters.Add("CorNm", "%" + searchString + "%" );
            }

            var whereSql = whereClauses.Any() ? " WHERE " + string.Join(" OR ", whereClauses) : "";
            var sql = $"SELECT {SelectColumns} FROM TB_COR_INFO {whereSql}";
            var result = await _dbConnection.QueryAsync<Corp>(sql, parameters);

           
            return result.ToList();
            
        }
    }
}
