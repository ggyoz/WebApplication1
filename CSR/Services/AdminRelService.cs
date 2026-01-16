using CSR.Models;
using Dapper;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Mvc.Rendering;


namespace CSR.Services
{
    public interface IAdminRelService
    {
        Task<List<string>> GetAssignedMenuIdsForUserAsync(string userId);
        Task<List<string>> GetAssignedUserForMenuId(int menuId);
        Task<IEnumerable<SelectListItem>> GetSelectListByAssignedUserAsync(int menuId);
        Task UpdateResponsibilitiesForUserAsync(string userId, List<string> assignedMenuIds, string adminUserId);
    }

    public class AdminRelService : IAdminRelService
    {
        private readonly IDbConnection _dbConnection;

        public AdminRelService(IDbConnection dbConnection)
        {
            _dbConnection = dbConnection;
        }

        public async Task<IEnumerable<SelectListItem>> GetSelectListByAssignedUserAsync(int menuId)
        {
            const string sql = @"SELECT AR.USERID as userId, U.USERNAME as userName
                                FROM TB_ADMIN_REL AR
                                LEFT JOIN TB_USER_INFO U
                                    ON AR.USERID = U.USERID 
                                WHERE AR.MENUID = :MenuId";
            var assignedUsers = await _dbConnection.QueryAsync(sql, new { MenuId = menuId });
            var items = assignedUsers.Select(c => new SelectListItem
                                {
                                    Value = c.USERID,
                                    Text = c.USERNAME
                                });
            return items;
        }

        public async Task<List<string>> GetAssignedUserForMenuId(int menuId)
        {
            const string sql = "SELECT MENUID, USERID FROM TB_ADMIN_REL WHERE MENUID = :MenuId";
            var assignedIds = await _dbConnection.QueryAsync<string>(sql, new { MenuId = menuId });
            return assignedIds.ToList();
        }

        public async Task<List<string>> GetAssignedMenuIdsForUserAsync(string userId)
        {
            const string sql = "SELECT MENUID FROM TB_ADMIN_REL WHERE USERID = :UserId";
            var assignedIds = await _dbConnection.QueryAsync<string>(sql, new { UserId = userId });
            return assignedIds.ToList();
        }

        public async Task UpdateResponsibilitiesForUserAsync(string userId, List<string> assignedMenuIds, string adminUserId)
        {

            Console.WriteLine("UpdateResponsibilitiesForUserAsync: " + JsonConvert.SerializeObject(assignedMenuIds, Formatting.Indented));

            using var transaction = _dbConnection.BeginTransaction();
            
            try
            {
                // 1. Delete existing responsibilities for the user
                const string deleteSql = "DELETE FROM TB_ADMIN_REL WHERE USERID = :UserId";
                await _dbConnection.ExecuteAsync(deleteSql, new { UserId = userId }, transaction);

                // 2. Insert the new responsibilities
                if (assignedMenuIds != null && assignedMenuIds.Any())
                {
                    const string insertSql = @"
                        INSERT INTO TB_ADMIN_REL (ID, USERID, MENUID, ROLE_TYPE, REG_DATE, REG_USERID)
                        VALUES (SEQ_ADMIN_REL.NEXTVAL, :UserId, :MenuId, 'MAIN', SYSDATE, :AdminUserId)";

                    foreach (var menuId in assignedMenuIds)
                    {
                        await _dbConnection.ExecuteAsync(insertSql, new
                        {
                            UserId = userId,
                            MenuId = menuId,
                            AdminUserId = adminUserId
                        }, transaction);
                    }
                }

                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }
    }
}
