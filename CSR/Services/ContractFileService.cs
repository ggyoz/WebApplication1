using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using CSR.Models;
using Dapper;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace CSR.Services
{
    public class ContractFileService
    {
        private readonly IDbConnection _db;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<ContractFileService> _logger;

        public ContractFileService(IDbConnection db, IWebHostEnvironment env, ILogger<ContractFileService> logger)
        {
            _db = db;
            _env = env;
            _logger = logger;

            var uploadFolder = Path.Combine(_env.WebRootPath, "uploads", "contract");
            if (!Directory.Exists(uploadFolder))
                Directory.CreateDirectory(uploadFolder);
        }

        public async Task<IEnumerable<ContractFile>> GetContractFilesAsync(int contractId)
        {
            var sql = "SELECT * FROM TB_CONTRACT_FILE WHERE CONTRACT_ID = :contractId AND USEYN = 'Y' ORDER BY FILEID";
            return await _db.QueryAsync<ContractFile>(sql, new { contractId });
        }

        public async Task<ContractFile> GetContractFileByIdAsync(int fileId)
        {
            var sql = "SELECT * FROM TB_CONTRACT_FILE WHERE FILEID = :fileId";
            return await _db.QueryFirstOrDefaultAsync<ContractFile>(sql, new { fileId });
        }

        public async Task AddContractFilesAsync(int contractId, IEnumerable<IFormFile> files, string userId)
        {
            if (files == null) return;

            var uploadFolder = Path.Combine(_env.WebRootPath, "uploads", "contract");

            foreach (var file in files)
            {
                if (file.Length <= 0) continue;

                try
                {
                    var ext = Path.GetExtension(file.FileName);
                    var uploadFileName = $"{Guid.NewGuid()}{ext}";
                    var filePath = Path.Combine("uploads", "contract", uploadFileName);
                    var absolutePath = Path.Combine(_env.WebRootPath, filePath);

                    using (var stream = new FileStream(absolutePath, FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }

                    var sql = @"
                        INSERT INTO TB_CONTRACT_FILE (FILEID, CONTRACT_ID, UPLOAD_FILENAME, REAL_FILENAME, FILEPATH, REG_DATE, REG_USERID, USEYN)
                        VALUES (SEQ_CONTRACT_FILE.NEXTVAL, :CONTRACT_ID, :UPLOAD_FILENAME, :REAL_FILENAME, :FILEPATH, SYSDATE, :REG_USERID, 'Y')";

                    await _db.ExecuteAsync(sql, new
                    {
                        CONTRACT_ID = contractId,
                        UPLOAD_FILENAME = uploadFileName,
                        REAL_FILENAME = file.FileName,
                        FILEPATH = filePath,
                        REG_USERID = userId
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "파일 저장 오류: {FileName}, ContractId: {ContractId}", file.FileName, contractId);
                    throw;
                }
            }
        }

        public async Task DeleteContractFileAsync(int fileId)
        {
            var sql = "UPDATE TB_CONTRACT_FILE SET USEYN = 'N' WHERE FILEID = :fileId";
            await _db.ExecuteAsync(sql, new { fileId });
        }
    }
}
