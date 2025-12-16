using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace WebApplication1.Models
{
    [Table("posts")]
    public class Post : BaseModel
    {
        [PrimaryKey("id")]
        public int Id { get; set; }

        [Column("title")]
        public string Title { get; set; } = string.Empty;

        [Column("content")]
        public string Content { get; set; } = string.Empty;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}

