using Microsoft.AspNetCore.Mvc;
using WebApplication1.Models;
using WebApplication1.Services;

namespace WebApplication1.Controllers
{
    public class PostController : Controller
    {
        private readonly PostService? _postService;
        private readonly ILogger<PostController> _logger;

        public PostController(PostService? postService, ILogger<PostController> logger)
        {
            _postService = postService;
            _logger = logger;
        }

        // GET: Post
        public async Task<IActionResult> Index()
        {
            if (_postService == null)
            {
                ViewBag.Error = "Supabase가 설정되지 않았습니다. appsettings.json에 Supabase URL과 AnonKey를 설정해주세요.";
                return View(new List<Post>());
            }

            try
            {
                var posts = await _postService.GetAllPostsAsync();
                _logger.LogInformation($"게시물 {posts.Count}개를 성공적으로 가져왔습니다.");
                return View(posts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "게시물 목록을 가져오는 중 오류 발생: {Message}", ex.Message);
                ViewBag.Error = $"데이터베이스 연결에 실패했습니다: {ex.Message}";
                ViewBag.DetailedError = ex.ToString();
                return View(new List<Post>());
            }
        }

        // GET: Post/Details/5
        public async Task<IActionResult> Details(int id)
        {
            if (_postService == null)
            {
                ViewBag.Error = "Supabase가 설정되지 않았습니다.";
                return View();
            }

            var post = await _postService.GetPostByIdAsync(id);
            if (post == null)
            {
                return NotFound();
            }
            return View(post);
        }

        // GET: Post/Create
        public IActionResult Create()
        {
            if (_postService == null)
            {
                ViewBag.Error = "Supabase가 설정되지 않았습니다.";
            }
            return View();
        }

        // POST: Post/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Title,Content")] Post post)
        {
            if (_postService == null)
            {
                ModelState.AddModelError("", "Supabase가 설정되지 않았습니다.");
                return View(post);
            }

            if (ModelState.IsValid)
            {
                try
                {
                    await _postService.CreatePostAsync(post);
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "게시물 생성 중 오류 발생");
                    ModelState.AddModelError("", "게시물 생성에 실패했습니다.");
                }
            }
            return View(post);
        }
    }
}

