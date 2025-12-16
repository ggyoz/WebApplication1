using Supabase;
using WebApplication1.Models;

namespace WebApplication1.Services
{
    public class MenuService
    {
        private readonly Supabase.Client _supabase;

        public MenuService(Supabase.Client supabase)
        {
            _supabase = supabase;
        }

        // 모든 메뉴 조회 (활성화된 것만)
        public async Task<List<Menu>> GetAllActiveMenusAsync()
        {
            var response = await _supabase
                .From<Menu>()
                .Select("id,name,url,controller,action,icon,display_order,is_active,parent_id,level,created_at,updated_at")
                .Filter("is_active", Supabase.Postgrest.Constants.Operator.Equals, true)
                .Order("display_order", Supabase.Postgrest.Constants.Ordering.Ascending)
                .Get();
            return response.Models;
        }

        // 모든 메뉴 조회 (관리자용)
        public async Task<List<Menu>> GetAllMenusAsync()
        {
            var response = await _supabase
                .From<Menu>()
                .Select("id,name,url,controller,action,icon,display_order,is_active,parent_id,level,created_at,updated_at")
                .Order("display_order", Supabase.Postgrest.Constants.Ordering.Ascending)
                .Get();
            return response.Models;
        }

        // 계층 구조로 메뉴 조회 (1단계 메뉴만, 자식 포함)
        public async Task<List<Menu>> GetMenuTreeAsync(bool activeOnly = true)
        {
            var allMenus = activeOnly 
                ? await GetAllActiveMenusAsync() 
                : await GetAllMenusAsync();

            // 1단계 메뉴만 필터링
            var rootMenus = allMenus.Where(m => m.Level == 1).ToList();

            // 각 메뉴에 자식 메뉴 추가
            foreach (var menu in rootMenus)
            {
                menu.Children = BuildMenuTree(menu, allMenus);
            }

            return rootMenus;
        }

        // 재귀적으로 메뉴 트리 구성
        private List<Menu> BuildMenuTree(Menu parent, List<Menu> allMenus)
        {
            var children = allMenus
                .Where(m => m.ParentId == parent.Id)
                .OrderBy(m => m.DisplayOrder)
                .ToList();

            foreach (var child in children)
            {
                child.Children = BuildMenuTree(child, allMenus);
            }

            return children;
        }

        // ID로 메뉴 조회
        public async Task<Menu?> GetMenuByIdAsync(int id)
        {
            var response = await _supabase
                .From<Menu>()
                .Select("id,name,url,controller,action,icon,display_order,is_active,parent_id,level,created_at,updated_at")
                .Filter("id", Supabase.Postgrest.Constants.Operator.Equals, id)
                .Get();
            return response.Models.FirstOrDefault();
        }

        // 부모 메뉴 목록 조회 (드롭다운용)
        public async Task<List<Menu>> GetParentMenusAsync(int? excludeId = null, int? maxLevel = null)
        {
            var query = _supabase
                .From<Menu>()
                .Select("id,name,url,controller,action,icon,display_order,is_active,parent_id,level,created_at,updated_at");

            if (maxLevel.HasValue)
            {
                query = query.Filter("level", Supabase.Postgrest.Constants.Operator.LessThan, maxLevel.Value);
            }

            if (excludeId.HasValue)
            {
                query = query.Filter("id", Supabase.Postgrest.Constants.Operator.NotEqual, excludeId.Value);
            }

            var response = await query
                .Order("display_order", Supabase.Postgrest.Constants.Ordering.Ascending)
                .Get();

            return response.Models;
        }

        // 메뉴 생성
        public async Task<Menu> CreateMenuAsync(Menu menu)
        {
            var response = await _supabase
                .From<Menu>()
                .Insert(menu);
            return response.Models.First();
        }

        // 메뉴 수정
        public async Task<Menu> UpdateMenuAsync(Menu menu)
        {
            var query = _supabase
                .From<Menu>()
                .Filter("id", Supabase.Postgrest.Constants.Operator.Equals, menu.Id)
                .Set(x => x.Name, menu.Name)
                .Set(x => x.DisplayOrder, menu.DisplayOrder)
                .Set(x => x.IsActive, menu.IsActive)
                .Set(x => x.Level, menu.Level);

            // Nullable 필드들은 조건부로 설정
            query = query.Set(x => x.Url, menu.Url ?? string.Empty);
            query = query.Set(x => x.Controller, menu.Controller ?? string.Empty);
            query = query.Set(x => x.Action, menu.Action ?? string.Empty);
            query = query.Set(x => x.Icon, menu.Icon ?? string.Empty);
            query = query.Set(x => x.ParentId, menu.ParentId);

            var response = await query.Update();
            return response.Models.FirstOrDefault() ?? menu;
        }

        // 메뉴 삭제
        public async Task DeleteMenuAsync(int id)
        {
            await _supabase
                .From<Menu>()
                .Filter("id", Supabase.Postgrest.Constants.Operator.Equals, id)
                .Delete();
        }

        // 순서 변경 (위로)
        public async Task<bool> MoveUpAsync(int id)
        {
            var menu = await GetMenuByIdAsync(id);
            if (menu == null) return false;

            // 같은 레벨, 같은 부모의 메뉴 중에서 현재보다 display_order가 작은 것 중 가장 큰 것 찾기
            var query = _supabase
                .From<Menu>()
                .Select("id,name,url,controller,action,icon,display_order,is_active,parent_id,level,created_at,updated_at")
                .Filter("level", Supabase.Postgrest.Constants.Operator.Equals, menu.Level)
                .Filter("display_order", Supabase.Postgrest.Constants.Operator.LessThan, menu.DisplayOrder);

            if (menu.ParentId.HasValue)
            {
                query = query.Filter("parent_id", Supabase.Postgrest.Constants.Operator.Equals, menu.ParentId.Value);
            }
            else
            {
                query = query.Filter<int?>("parent_id", Supabase.Postgrest.Constants.Operator.Is, null);
            }

            var response = await query
                .Order("display_order", Supabase.Postgrest.Constants.Ordering.Descending)
                .Limit(1)
                .Get();

            var previousMenu = response.Models.FirstOrDefault();
            if (previousMenu == null) return false;

            // 순서 교환
            var tempOrder = menu.DisplayOrder;
            menu.DisplayOrder = previousMenu.DisplayOrder;
            previousMenu.DisplayOrder = tempOrder;

            await UpdateMenuAsync(menu);
            await UpdateMenuAsync(previousMenu);

            return true;
        }

        // 순서 변경 (아래로)
        public async Task<bool> MoveDownAsync(int id)
        {
            var menu = await GetMenuByIdAsync(id);
            if (menu == null) return false;

            // 같은 레벨, 같은 부모의 메뉴 중에서 현재보다 display_order가 큰 것 중 가장 작은 것 찾기
            var query = _supabase
                .From<Menu>()
                .Select("id,name,url,controller,action,icon,display_order,is_active,parent_id,level,created_at,updated_at")
                .Filter("level", Supabase.Postgrest.Constants.Operator.Equals, menu.Level)
                .Filter("display_order", Supabase.Postgrest.Constants.Operator.GreaterThan, menu.DisplayOrder);

            if (menu.ParentId.HasValue)
            {
                query = query.Filter("parent_id", Supabase.Postgrest.Constants.Operator.Equals, menu.ParentId.Value);
            }
            else
            {
                query = query.Filter<int?>("parent_id", Supabase.Postgrest.Constants.Operator.Is, null);
            }

            var response = await query
                .Order("display_order", Supabase.Postgrest.Constants.Ordering.Ascending)
                .Limit(1)
                .Get();

            var nextMenu = response.Models.FirstOrDefault();
            if (nextMenu == null) return false;

            // 순서 교환
            var tempOrder = menu.DisplayOrder;
            menu.DisplayOrder = nextMenu.DisplayOrder;
            nextMenu.DisplayOrder = tempOrder;

            await UpdateMenuAsync(menu);
            await UpdateMenuAsync(nextMenu);

            return true;
        }
    }
}

