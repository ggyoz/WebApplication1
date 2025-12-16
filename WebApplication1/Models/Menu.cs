using Newtonsoft.Json;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace WebApplication1.Models
{
    [Table("menus")]
    public class Menu : BaseModel
    {
        [PrimaryKey("id")]
        public int Id { get; set; }

        [Column("name")]
        public string Name { get; set; } = string.Empty;

        [Column("url")]
        public string? Url { get; set; }

        [Column("controller")]
        public string? Controller { get; set; }

        [Column("action")]
        public string? Action { get; set; }

        [Column("icon")]
        public string? Icon { get; set; }

        [Column("display_order")]
        public int DisplayOrder { get; set; }

        [Column("is_active")]
        public bool IsActive { get; set; } = true;

        [Column("parent_id")]
        public int? ParentId { get; set; }

        [Column("level")]
        public int Level { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // 자식 메뉴 목록 (데이터베이스 컬럼 아님, 코드에서 사용)
        [JsonIgnore]
        public List<Menu>? Children { get; set; }

        // 부모 메뉴 (데이터베이스 컬럼 아님, 코드에서 사용)
        [JsonIgnore]
        public Menu? Parent { get; set; }

        // 링크 URL 생성 (controller/action 또는 url 사용)
        public string GetLink()
        {
            if (!string.IsNullOrEmpty(Url))
            {
                return Url;
            }

            if (!string.IsNullOrEmpty(Controller) && !string.IsNullOrEmpty(Action))
            {
                return $"/{Controller}/{Action}";
            }

            return "#";
        }
    }
}

