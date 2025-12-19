using Supabase;
using CSR.Models;

namespace CSR.Services
{
    public class PostService
    {
        private readonly Supabase.Client _supabase;

        public PostService(Supabase.Client supabase)
        {
            _supabase = supabase;
        }

        public async Task<List<Post>> GetAllPostsAsync()
        {
            var response = await _supabase
                .From<Post>()
                .Get();
            return response.Models;
        }

        public async Task<Post?> GetPostByIdAsync(int id)
        {
            var response = await _supabase
                .From<Post>()
                .Select("*")
                .Filter("id", Supabase.Postgrest.Constants.Operator.Equals, id)
                .Get();
            return response.Models.FirstOrDefault();
        }

        public async Task<Post> CreatePostAsync(Post post)
        {
            var response = await _supabase
                .From<Post>()
                .Insert(post);
            return response.Models.First();
        }

        public async Task<Post> UpdatePostAsync(Post post)
        {
            var response = await _supabase
                .From<Post>()
                .Filter("id", Supabase.Postgrest.Constants.Operator.Equals, post.Id)
                .Set(x => x.Title, post.Title)
                .Set(x => x.Content, post.Content)
                .Update();
            return response.Models.FirstOrDefault() ?? post;
        }

        public async Task DeletePostAsync(int id)
        {
            await _supabase
                .From<Post>()
                .Filter("id", Supabase.Postgrest.Constants.Operator.Equals, id)
                .Delete();
        }
    }
}

