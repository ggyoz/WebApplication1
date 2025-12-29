using Dapper;
using Oracle.ManagedDataAccess.Client;
using CSR.Models;
using System.Data;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CSR.Services
{
    public class MenuService
    {
        private readonly IDbConnection _dbConnection;

        public MenuService(IDbConnection connection)
        {
            _dbConnection = connection;
        }

        private const string SelectColumns = @"
            MENUID AS MenuId, SYSTEMCODE AS SystemCode, MENUNAME AS MenuName, CONTROLLER, ACTION, URL, 
            PARENTID AS ParentId, INFO, SORT_ORDER AS SortOrder, USEYN AS UseYn, 
            TO_CHAR(CREATE_DATE, 'YYYY-MM-DD HH24:MI:SS') AS CreateDate, 
            CREATE_USERID AS CreateUserId, 
            TO_CHAR(UPDATE_DATE, 'YYYY-MM-DD HH24:MI:SS') AS UpdateDate, 
            UPDATE_USERID AS UpdateUserId,
            CAST(NULL AS VARCHAR2(100)) AS Icon
        ";

        // 모든 메뉴 조회 (활성화된 것만)
        public async Task<List<Menu>> GetAllActiveMenusAsync()
        {
            var sql = $"SELECT {SelectColumns} FROM TB_MENU_INFO WHERE USEYN = 'Y' ORDER BY SORT_ORDER ASC";
            var result = await _dbConnection.QueryAsync<Menu>(sql);
            return result.ToList();
        }

        // 모든 메뉴 조회 (관리자용)
        public async Task<List<Menu>> GetAllMenusAsync()
        {
            var sql = $"SELECT {SelectColumns} FROM TB_MENU_INFO ORDER BY SORT_ORDER ASC";
            var result = await _dbConnection.QueryAsync<Menu>(sql);
            return result.ToList();
        }
        
        private void SetMenuLevels(Menu menu, int level)
        {
            menu.MenuLevel = level;
            if (menu.Children != null)
            {
                foreach (var child in menu.Children)
                {
                    SetMenuLevels(child, level + 1);
                }
            }
        }

        // 계층 구조로 메뉴 조회 (1단계 메뉴만, 자식 포함)
        public async Task<List<Menu>> GetMenuTreeAsync(bool activeOnly = true)
        {
            var allMenus = activeOnly 
                ? await GetAllActiveMenusAsync() 
                : await GetAllMenusAsync();

            var menuDict = allMenus.ToDictionary(m => m.MenuId);
            var rootMenus = new List<Menu>();

            foreach (var menu in allMenus)
            {
                if (string.IsNullOrEmpty(menu.ParentId) || !menuDict.ContainsKey(menu.ParentId))
                {
                    rootMenus.Add(menu);
                }
                else
                {
                    if (menuDict.TryGetValue(menu.ParentId, out var parent))
                    {
                        parent.Children ??= new List<Menu>();
                        parent.Children.Add(menu);
                    }
                }
            }
            
            // Ensure children are sorted
            foreach(var menu in allMenus.Where(m => m.Children != null))
            {
                menu.Children = menu.Children.OrderBy(m => m.SortOrder).ToList();
            }

            foreach (var rootMenu in rootMenus)
            {
                SetMenuLevels(rootMenu, 1);
            }

            return rootMenus.OrderBy(m => m.SortOrder).ToList();
        }

        // ID로 메뉴 조회
        public async Task<Menu?> GetMenuByIdAsync(string id)
        {
            var sql = @"SELECT
                        q.MenuId,
                        q.SystemCode,
                        q.MenuName,
                        q.Controller,
                        q.Action,
                        q.Url,
                        q.ParentId,
                        q.Info,
                        q.SortOrder,
                        q.UseYn,
                        q.CreateDate,
                        q.CreateUserId,
                        q.UpdateDate,
                        q.UpdateUserId,
                        q.Icon,
                        q.MenuLevel,
                        NVL(c.ChildCount, 0) AS ChildCount
                    FROM (
                        SELECT 
                            MENUID AS MenuId,
                            SYSTEMCODE AS SystemCode,
                            MENUNAME AS MenuName,
                            CONTROLLER,
                            ACTION,
                            URL,
                            PARENTID AS ParentId,
                            INFO,
                            SORT_ORDER AS SortOrder,
                            USEYN AS UseYn,
                            TO_CHAR(CREATE_DATE, 'YYYY-MM-DD HH24:MI:SS') AS CreateDate,
                            CREATE_USERID AS CreateUserId,
                            TO_CHAR(UPDATE_DATE, 'YYYY-MM-DD HH24:MI:SS') AS UpdateDate,
                            UPDATE_USERID AS UpdateUserId,
                            CAST(NULL AS VARCHAR2(100)) AS Icon,
                            LEVEL AS MenuLevel
                        FROM TB_MENU_INFO
                        START WITH PARENTID IS NULL
                        CONNECT BY PRIOR MENUID = PARENTID
                    ) q
                    LEFT JOIN (
                        SELECT PARENTID, COUNT(*) AS ChildCount
                        FROM TB_MENU_INFO
                        GROUP BY PARENTID
                    ) c
                        ON c.PARENTID = q.MenuId
                    WHERE q.MenuId = :Id";

            var result = await _dbConnection.QueryAsync<Menu>(sql, new { Id = id });
            return result.FirstOrDefault();
        }

        // 부모 메뉴 목록 조회 (드롭다운용)
        public async Task<List<Menu>> GetParentMenusAsync(string? excludeId = null)
        {
            var sql = $"SELECT {SelectColumns} FROM TB_MENU_INFO";
            var parameters = new DynamicParameters();
            
            if (excludeId != null)
            {
                sql += " WHERE MENUID != :ExcludeId";
                parameters.Add("ExcludeId", excludeId);
            }
            
            sql += " ORDER BY SORT_ORDER ASC";
            
            var result = await _dbConnection.QueryAsync<Menu>(sql, parameters);
            return result.ToList();
        }

        // 메뉴 생성
        public async Task<string> CreateMenuAsync(Menu menu)
        {
            var sql = @"
                INSERT INTO TB_MENU_INFO (
                    MENUID, MENUNAME, URL, CONTROLLER, ACTION, SORT_ORDER, USEYN, PARENTID, SYSTEMCODE, INFO, CREATE_USERID, UPDATE_USERID, CREATE_DATE, UPDATE_DATE
                ) VALUES (
                    SEQ_MENU_INFO.NEXTVAL, :MenuName, :Url, :Controller, :Action, :SortOrder, :UseYn, :ParentId, :SystemCode, :Info, :CreateUserId, :UpdateUserId, SYSDATE, SYSDATE
                ) RETURNING MENUID INTO :NewMenuId";

            var parameters = new DynamicParameters();
            parameters.Add("MenuName", menu.MenuName);
            parameters.Add("Url", menu.Url);
            parameters.Add("Controller", menu.Controller);
            parameters.Add("Action", menu.Action);
            parameters.Add("SortOrder", menu.SortOrder);
            parameters.Add("UseYn", menu.UseYn);
            parameters.Add("ParentId", menu.ParentId);
            parameters.Add("SystemCode", menu.SystemCode);
            parameters.Add("Info", menu.Info);
            parameters.Add("CreateUserId", menu.CreateUserId);
            parameters.Add("UpdateUserId", menu.UpdateUserId);
            parameters.Add("NewMenuId", dbType: DbType.String, direction: ParameterDirection.Output, size: 50);

            await _dbConnection.ExecuteAsync(sql, parameters);
            
            return parameters.Get<string>("NewMenuId");
        }

        // 메뉴 수정
        public async Task UpdateMenuAsync(Menu menu)
        {
            var sql = @"
                UPDATE TB_MENU_INFO SET
                    MENUNAME = :MenuName,
                    URL = :Url,
                    CONTROLLER = :Controller,
                    ACTION = :Action,
                    SORT_ORDER = :SortOrder,
                    USEYN = :UseYn,
                    PARENTID = :ParentId,
                    SYSTEMCODE = :SystemCode,
                    INFO = :Info,
                    UPDATE_USERID = :UpdateUserId,
                    UPDATE_DATE = SYSDATE
                WHERE MENUID = :MenuId";
            await _dbConnection.ExecuteAsync(sql, new {
                menu.MenuName,
                menu.Url,
                menu.Controller,
                menu.Action,
                menu.SortOrder,
                menu.UseYn,
                menu.ParentId,
                menu.SystemCode,
                menu.Info,
                menu.UpdateUserId,
                menu.MenuId
            });
        }

        // 메뉴 삭제
        public async Task DeleteMenuAsync(string id)
        {
            var sql = "DELETE FROM TB_MENU_INFO WHERE MENUID = :Id";
            await _dbConnection.ExecuteAsync(sql, new { Id = id });
        }

        private async Task<Menu?> GetSiblingAsync(Menu menu, bool findPrevious)
        {
            var op = findPrevious ? "<" : ">";
            var orderBy = findPrevious ? "DESC" : "ASC";
            
            var sql = $@"
                SELECT {SelectColumns} FROM TB_MENU_INFO
                WHERE COALESCE(PARENTID, 'ROOT') = COALESCE(:ParentId, 'ROOT')
                  AND SORT_ORDER {op} :SortOrder
                ORDER BY SORT_ORDER {orderBy}
                FETCH FIRST 1 ROWS ONLY";
            
            return await _dbConnection.QueryFirstOrDefaultAsync<Menu>(sql, new { menu.ParentId, menu.SortOrder });
        }

        // 순서 변경 (위로 또는 아래로)
        private async Task<bool> MoveOrderAsync(string id, bool moveUp)
        {
            if (_dbConnection.State == ConnectionState.Closed)
            {
                _dbConnection.Open();
            }

            using var transaction = _dbConnection.BeginTransaction();
            try
            {
                var menuToMove = await _dbConnection.QueryFirstOrDefaultAsync<Menu>($"SELECT {SelectColumns} FROM TB_MENU_INFO WHERE MENUID = :Id", new {Id = id}, transaction);
                if (menuToMove == null) return false;

                var sibling = await _dbConnection.QueryFirstOrDefaultAsync<Menu>(
                    $@"SELECT {SelectColumns} FROM TB_MENU_INFO
                       WHERE COALESCE(PARENTID, 'ROOT') = COALESCE(:ParentId, 'ROOT') AND SORT_ORDER {(moveUp ? "<" : ">")} :SortOrder
                       ORDER BY SORT_ORDER {(moveUp ? "DESC" : "ASC")}
                       FETCH FIRST 1 ROWS ONLY",
                    new { menuToMove.ParentId, menuToMove.SortOrder },
                    transaction);

                if (sibling == null) return false;

                // Swap SORT_ORDER
                var tempOrder = menuToMove.SortOrder;
                
                var updateSql = "UPDATE TB_MENU_INFO SET SORT_ORDER = :SortOrder WHERE MENUID = :MenuId";

                await _dbConnection.ExecuteAsync(updateSql, new { SortOrder = sibling.SortOrder, MenuId = menuToMove.MenuId }, transaction);
                await _dbConnection.ExecuteAsync(updateSql, new { SortOrder = tempOrder, MenuId = sibling.MenuId }, transaction);
                
                transaction.Commit();
                return true;
            }
            catch
            {
                transaction.Rollback();
                throw; // re-throw the exception to see what went wrong
            }
        }

        public Task<bool> MoveUpAsync(string id) => MoveOrderAsync(id, true);
        public Task<bool> MoveDownAsync(string id) => MoveOrderAsync(id, false);

        public async Task<List<Menu>> GetMenusByLevelAsync(int level)
        {
            // LEVEL 가상 컬럼을 사용하여 특정 레벨의 메뉴만 조회합니다.
            var sql = @"
                SELECT * FROM (
                    SELECT
                        MENUID AS MenuId,
                        MENUNAME AS MenuName,
                        SORT_ORDER AS SortOrder,
                        URL as Url,
                        LEVEL AS MenuLevel
                    FROM TB_MENU_INFO
                    START WITH PARENTID IS NULL
                    CONNECT BY PRIOR MENUID = PARENTID
                    ORDER SIBLINGS BY SORT_ORDER
                )
                WHERE MenuLevel = :MenuLevel";

            var result = await _dbConnection.QueryAsync<Menu>(sql, new { MenuLevel = level });
            return result.ToList();
        }
    }
}



