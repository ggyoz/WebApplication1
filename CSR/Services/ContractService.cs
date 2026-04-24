using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using CSR.Models;
using Dapper;

namespace CSR.Services
{
    public class ContractService
    {
        private readonly IDbConnection _db;

        public ContractService(IDbConnection db)
        {
            _db = db;
        }

        public async Task<IEnumerable<Contract>> GetContractsAsync(string vendorName = null, int? contractYear = null, string contractType = null)
        {
            var parameters = new DynamicParameters();
            string sql = @"
                SELECT c.*, v.VENDOR_NAME, v.VENDOR_CODE
                FROM TB_CONTRACT_INFO c
                LEFT JOIN TB_VENDOR_INFO v ON c.VENDORID = v.VENDORID
                WHERE c.USEYN = 'Y'";

            if (!string.IsNullOrEmpty(vendorName))
            {
                sql += " AND v.VENDOR_NAME LIKE '%' || :vendorName || '%'";
                parameters.Add("vendorName", vendorName);
            }
            if (contractYear.HasValue)
            {
                sql += " AND c.CONTRACT_YEAR = :contractYear";
                parameters.Add("contractYear", contractYear.Value);
            }
            if (!string.IsNullOrEmpty(contractType))
            {
                sql += " AND c.CONTRACT_TYPE = :contractType";
                parameters.Add("contractType", contractType);
            }

            sql += " ORDER BY c.CONTRACT_ID DESC";
            return await _db.QueryAsync<Contract>(sql, parameters);
        }

        public async Task<Contract> GetContractByIdAsync(int id)
        {
            string sql = @"
                SELECT c.*, v.VENDOR_NAME, v.VENDOR_CODE
                FROM TB_CONTRACT_INFO c
                LEFT JOIN TB_VENDOR_INFO v ON c.VENDORID = v.VENDORID
                WHERE c.CONTRACT_ID = :id";
            return await _db.QueryFirstOrDefaultAsync<Contract>(sql, new { id });
        }

        public async Task<int> CreateContractAsync(Contract contract)
        {
            string sql = @"
                INSERT INTO TB_CONTRACT_INFO (
                    CONTRACT_ID, VENDORID, CONTRACT_YEAR, CONTRACT_START, CONTRACT_END,
                    CONTRACT_YEARS, CONTRACT_MONTHS, CONTRACT_TYPE, CONTRACT_DESC,
                    USEYN, REG_DATE, REG_USERID
                ) VALUES (
                    SEQ_TB_CONTRACT_INFO.NEXTVAL, :VENDORID, :CONTRACT_YEAR, :CONTRACT_START, :CONTRACT_END,
                    :CONTRACT_YEARS, :CONTRACT_MONTHS, :CONTRACT_TYPE, :CONTRACT_DESC,
                    :USEYN, SYSDATE, :REG_USERID
                )";
            return await _db.ExecuteAsync(sql, contract);
        }

        public async Task<int> UpdateContractAsync(Contract contract)
        {
            string sql = @"
                UPDATE TB_CONTRACT_INFO SET
                    VENDORID       = :VENDORID,
                    CONTRACT_YEAR  = :CONTRACT_YEAR,
                    CONTRACT_START = :CONTRACT_START,
                    CONTRACT_END   = :CONTRACT_END,
                    CONTRACT_YEARS = :CONTRACT_YEARS,
                    CONTRACT_MONTHS = :CONTRACT_MONTHS,
                    CONTRACT_TYPE  = :CONTRACT_TYPE,
                    CONTRACT_DESC  = :CONTRACT_DESC,
                    USEYN          = :USEYN,
                    UPDATE_DATE    = SYSDATE,
                    UPDATE_USERID  = :UPDATE_USERID
                WHERE CONTRACT_ID = :CONTRACT_ID";
            return await _db.ExecuteAsync(sql, contract);
        }

        public async Task<int> DeleteContractAsync(int id)
        {
            string sql = "UPDATE TB_CONTRACT_INFO SET USEYN = 'N' WHERE CONTRACT_ID = :id";
            return await _db.ExecuteAsync(sql, new { id });
        }
    }
}
