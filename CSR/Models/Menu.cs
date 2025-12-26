using Newtonsoft.Json;
using System.Collections.Generic;

namespace CSR.Models
{
    /// <summary>
    /// Represents an item in the menu. 
    /// The properties are mapped from the TB_MENU_INFO table in the Oracle database.
    /// </summary>
    public class Menu
    {
        // Columns from TB_MENU_INFO
        public string MenuId { get; set; } = string.Empty;
        public string? SystemCode { get; set; }
        public string? MenuName { get; set; }
        public string? Controller { get; set; }
        public string? Action { get; set; }
        public string? Url { get; set; }
        public string? ParentId { get; set; }
        public string? Info { get; set; }
        public int SortOrder { get; set; }
        public string UseYn { get; set; } = "Y";
        public DateTime? CreateDate { get; set; }
        public string? CreateUserId { get; set; }
        public DateTime? UpdateDate { get; set; }
        public string? UpdateUserId { get; set; }

        // --- Compatibility & Helper Properties ---

        // Properties from original Menu model that are not in TB_MENU_INFO.
        // Dapper will ignore them if they are not in the SELECT list.
        // Kept for compatibility with existing views.
        public string? Icon { get; set; }
        public int Level { get; set; }

        // Navigation properties, populated manually.
        [JsonIgnore]
        public List<Menu>? Children { get; set; }
        [JsonIgnore]
        public Menu? Parent { get; set; }

        // The original model used different property names.
        // These read-only properties are for compatibility with views and other code.
        [JsonIgnore]
        public string Id => MenuId;
        [JsonIgnore]
        public string Name => MenuName ?? string.Empty;
        [JsonIgnore]
        public int DisplayOrder => SortOrder;

        
        /// <summary>
        /// Generates the link for the menu item, using either a direct URL or a controller/action pair.
        /// </summary>
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

