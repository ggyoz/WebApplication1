using Dapper;
using CSR.Models;
using System.Data;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CSR.Services
{
    public class DeptService
    {
        private readonly IDbConnection _dbConnection;

        public DeptService(IDbConnection connection)
        {
            _dbConnection = connection;
        }

        private const string SelectColumns = @"
            DEPTID AS DeptId,
            DEPTCD AS DeptCd,
            PARENTID AS ParentId,
            DEPTNAME AS DeptName,
            CORCD AS CorCd,
            SORTORDER AS SortOrder,
            NOTE AS Note,
            USEYN AS UseYn
        ";

        public async Task<List<Dept>> GetAllDeptsAsync()
        {
            var sql = $"SELECT {SelectColumns} FROM TB_DEPT_INFO ORDER BY SORTORDER ASC";
            var result = await _dbConnection.QueryAsync<Dept>(sql);
            return result.ToList();
        }

        private void SetDeptLevels(Dept dept, int level)
        {
            dept.DeptLevel = level;
            if (dept.Children != null)
            {
                foreach (var child in dept.Children)
                {
                    SetDeptLevels(child, level + 1);
                }
            }
        }

        public async Task<List<Dept>> GetDeptTreeAsync()
        {
            var allDepts = await GetAllDeptsAsync();
            var deptDict = allDepts.ToDictionary(d => d.DeptId);
            var rootDepts = new List<Dept>();

            foreach (var dept in allDepts)
            {
                if (dept.ParentId == null || !deptDict.ContainsKey(dept.ParentId.Value))
                {
                    rootDepts.Add(dept);
                }
                else
                {
                    if (deptDict.TryGetValue(dept.ParentId.Value, out var parent))
                    {
                        parent.Children ??= new List<Dept>();
                        parent.Children.Add(dept);
                        dept.Parent = parent;
                    }
                }
            }

            foreach (var dept in allDepts.Where(d => d.Children != null))
            {
                dept.Children = dept.Children.OrderBy(d => d.SortOrder).ToList();
            }

            foreach (var rootDept in rootDepts)
            {
                SetDeptLevels(rootDept, 1);
            }

            return rootDepts.OrderBy(d => d.SortOrder).ToList();
        }

        public async Task<Dept?> GetDeptByIdAsync(long id)
        {
            var sql = $@"SELECT {SelectColumns}, 
                                (SELECT COUNT(*) FROM TB_DEPT_INFO WHERE PARENTID = d.DEPTID) as ChildCount
                         FROM TB_DEPT_INFO d WHERE DEPTID = :Id";
            return await _dbConnection.QueryFirstOrDefaultAsync<Dept>(sql, new { Id = id });
        }
        
        public async Task<List<Dept>> GetAllDeptsForDropdownAsync()
        {
            var sql = $"SELECT DEPTID as DeptId, DEPTNAME as DeptName FROM TB_DEPT_INFO ORDER BY DEPTNAME ASC";
            var result = await _dbConnection.QueryAsync<Dept>(sql);
            return result.ToList();
        }

        public async Task<long> CreateDeptAsync(Dept dept)
        {
            // Note: Assumes a sequence named SEQ_DEPT_INFO exists for Oracle.
            var sql = @"
                INSERT INTO TB_DEPT_INFO (
                    DEPTID, DEPTCD, PARENTID, DEPTNAME, CORCD, SORTORDER, NOTE, USEYN
                ) VALUES (
                    SEQ_DEPT_INFO.NEXTVAL, :DeptCd, :ParentId, :DeptName, :CorCd, :SortOrder, :Note, :UseYn
                ) RETURNING DEPTID INTO :NewDeptId";

            var parameters = new DynamicParameters();
            parameters.Add("DeptCd", dept.DeptCd);
            parameters.Add("ParentId", dept.ParentId);
            parameters.Add("DeptName", dept.DeptName);
            parameters.Add("CorCd", dept.CorCd);
            parameters.Add("SortOrder", dept.SortOrder);
            parameters.Add("Note", dept.Note);
            parameters.Add("UseYn", dept.UseYn);
            parameters.Add("NewDeptId", dbType: DbType.Int64, direction: ParameterDirection.Output);

            await _dbConnection.ExecuteAsync(sql, parameters);
            return parameters.Get<long>("NewDeptId");
        }

        public async Task<int> UpdateDeptAsync(Dept dept)
        {
            var sql = @"
                UPDATE TB_DEPT_INFO SET
                    DEPTCD = :DeptCd,
                    PARENTID = :ParentId,
                    DEPTNAME = :DeptName,
                    CORCD = :CorCd,
                    SORTORDER = :SortOrder,
                    NOTE = :Note,
                    USEYN = :UseYn
                WHERE DEPTID = :DeptId";
            return await _dbConnection.ExecuteAsync(sql, dept);
        }

        public async Task<int> DeleteDeptAsync(long id)
        {
            // Check for children first
            var childCountSql = "SELECT COUNT(*) FROM TB_DEPT_INFO WHERE PARENTID = :Id";
            var childCount = await _dbConnection.ExecuteScalarAsync<int>(childCountSql, new { Id = id });
            if (childCount > 0)
            {
                throw new InvalidOperationException("This department has sub-departments and cannot be deleted.");
            }

            var sql = "DELETE FROM TB_DEPT_INFO WHERE DEPTID = :Id";
            return await _dbConnection.ExecuteAsync(sql, new { Id = id });
        }
        
        private async Task<bool> MoveOrderAsync(long id, bool moveUp)
        {
            if (_dbConnection.State == ConnectionState.Closed) _dbConnection.Open();

            using var transaction = _dbConnection.BeginTransaction();
            try
            {
                var deptToMove = await _dbConnection.QueryFirstOrDefaultAsync<Dept>($"SELECT {SelectColumns} FROM TB_DEPT_INFO WHERE DEPTID = :Id", new {Id = id}, transaction);
                if (deptToMove == null) return false;

                var sibling = await _dbConnection.QueryFirstOrDefaultAsync<Dept>(
                    $@"SELECT {SelectColumns} FROM TB_DEPT_INFO
                       WHERE (PARENTID = :ParentId OR (PARENTID IS NULL AND :ParentId IS NULL)) 
                         AND SORTORDER {(moveUp ? "<" : ">")} :SortOrder
                       ORDER BY SORTORDER {(moveUp ? "DESC" : "ASC")}
                       FETCH FIRST 1 ROWS ONLY",
                    new { deptToMove.ParentId, deptToMove.SortOrder },
                    transaction);

                if (sibling == null) return false;

                var tempOrder = deptToMove.SortOrder;
                var updateSql = "UPDATE TB_DEPT_INFO SET SORTORDER = :SortOrder WHERE DEPTID = :DeptId";

                await _dbConnection.ExecuteAsync(updateSql, new { SortOrder = sibling.SortOrder, DeptId = deptToMove.DeptId }, transaction);
                await _dbConnection.ExecuteAsync(updateSql, new { SortOrder = tempOrder, DeptId = sibling.DeptId }, transaction);
                
                transaction.Commit();
                return true;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public Task<bool> MoveUpAsync(long id) => MoveOrderAsync(id, true);
        public Task<bool> MoveDownAsync(long id) => MoveOrderAsync(id, false);

        public async Task<List<Dept>> GetAutoCompleteDeptAsync(string searchString, string corCd)
        {

            var whereClauses = new List<string>();
            var parameters = new DynamicParameters();

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                whereClauses.Add("DeptCd LIKE :DeptCd");
                parameters.Add("DeptCd", "%" + searchString + "%");
            }

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                whereClauses.Add("DEPTNAME LIKE :DeptName");
                parameters.Add("DeptName", "%" + searchString + "%" );
            }

            var whereSql = whereClauses.Any() ? " AND " + string.Join(" OR ", whereClauses) : "";

            var sql = $"SELECT {SelectColumns} FROM TB_DEPT_INFO WHERE 1 = 1 AND CorCd = :corCd {whereSql}";
            parameters.Add("corCd", corCd);

            var result = await _dbConnection.QueryAsync<Dept>(sql, parameters);

            return result.ToList();
            
        }

    }

    

}
