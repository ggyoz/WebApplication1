namespace CSR.Models
{
    public class UserIndexViewModel
    {
        public PagedResult<User> Users { get; set; }
        public UserSearchViewModel Search { get; set; }

        public UserIndexViewModel()
        {
            Users = new PagedResult<User>(new List<User>(), 0, 1, 15);
            Search = new UserSearchViewModel();
        }
    }
}
