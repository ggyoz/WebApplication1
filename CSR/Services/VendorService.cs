using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using CSR.Models;
using Dapper;

namespace CSR.Services
{
    public class VendorService
    {
        private readonly IDbConnection _db;

        public VendorService(IDbConnection db)
        {
            _db = db;
        }

        public async Task<IEnumerable<Vendor>> GetVendorsAsync(string vendorName = null, string vendorCode = null, string vendorType = null, string status = null)
        {
            var parameters = new DynamicParameters();
            string sql = "SELECT * FROM TB_VENDOR_INFO WHERE 1=1";

            if (!string.IsNullOrEmpty(vendorName))
            {
                sql += " AND VENDOR_NAME LIKE '%' || :vendorName || '%'";
                parameters.Add("vendorName", vendorName);
            }
            if (!string.IsNullOrEmpty(vendorCode))
            {
                sql += " AND VENDOR_CODE LIKE '%' || :vendorCode || '%'";
                parameters.Add("vendorCode", vendorCode);
            }
            if (!string.IsNullOrEmpty(vendorType))
            {
                sql += " AND VENDOR_TYPE = :vendorType";
                parameters.Add("vendorType", vendorType);
            }
            if (!string.IsNullOrEmpty(status))
            {
                sql += " AND STATUS = :status";
                parameters.Add("status", status);
            }

            sql += " ORDER BY VENDORID DESC";
            return await _db.QueryAsync<Vendor>(sql, parameters);
        }

        public async Task<Vendor> GetVendorByIdAsync(int id)
        {
            string sql = "SELECT * FROM TB_VENDOR_INFO WHERE VENDORID = :id";
            return await _db.QueryFirstOrDefaultAsync<Vendor>(sql, new { id });
        }

        public async Task<int> CreateVendorAsync(Vendor vendor)
        {
            string sql = @"
                INSERT INTO TB_VENDOR_INFO (
                    VENDORID, VENDOR_CODE, VENDOR_NAME, VENDOR_TYPE, BIZ_REG_NO, 
                    BIZ_TYPE, BIZ_ITEM, CEO_NAME, TEL_NO, EMAIL, 
                    ADDRESS, MANAGER_NAME, MANAGER_PHONE, MANAGER_EMAIL, STATUS, 
                    REMARKS, USEYN, REG_DATE, REG_USERID
                ) VALUES (
                    SEQ_TB_VENDOR_INFO.NEXTVAL, :VENDOR_CODE, :VENDOR_NAME, :VENDOR_TYPE, :BIZ_REG_NO, 
                    :BIZ_TYPE, :BIZ_ITEM, :CEO_NAME, :TEL_NO, :EMAIL, 
                    :ADDRESS, :MANAGER_NAME, :MANAGER_PHONE, :MANAGER_EMAIL, :STATUS, 
                    :REMARKS, :USEYN, SYSDATE, :REG_USERID
                )";
            return await _db.ExecuteAsync(sql, vendor);
        }

        public async Task<int> UpdateVendorAsync(Vendor vendor)
        {
            string sql = @"
                UPDATE TB_VENDOR_INFO SET 
                    VENDOR_CODE = :VENDOR_CODE,
                    VENDOR_NAME = :VENDOR_NAME,
                    VENDOR_TYPE = :VENDOR_TYPE,
                    BIZ_REG_NO = :BIZ_REG_NO,
                    BIZ_TYPE = :BIZ_TYPE,
                    BIZ_ITEM = :BIZ_ITEM,
                    CEO_NAME = :CEO_NAME,
                    TEL_NO = :TEL_NO,
                    EMAIL = :EMAIL,
                    ADDRESS = :ADDRESS,
                    MANAGER_NAME = :MANAGER_NAME,
                    MANAGER_PHONE = :MANAGER_PHONE,
                    MANAGER_EMAIL = :MANAGER_EMAIL,
                    STATUS = :STATUS,
                    REMARKS = :REMARKS,
                    USEYN = :USEYN,
                    UPDATE_DATE = SYSDATE,
                    UPDATE_USERID = :UPDATE_USERID
                WHERE VENDORID = :VENDORID";
            return await _db.ExecuteAsync(sql, vendor);
        }

        public async Task<int> DeleteVendorAsync(int id)
        {
            string sql = "DELETE FROM TB_VENDOR_INFO WHERE VENDORID = :id";
            return await _db.ExecuteAsync(sql, new { id });
        }
    }
}
