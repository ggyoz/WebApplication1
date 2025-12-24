using Supabase;
using Oracle.ManagedDataAccess.Client;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Supabase 클라이언트 등록
var supabaseUrl = builder.Configuration["Supabase:Url"];
var supabaseKey = builder.Configuration["Supabase:AnonKey"];

if (!string.IsNullOrEmpty(supabaseUrl) && !string.IsNullOrEmpty(supabaseKey))
{
    builder.Services.AddSingleton(provider =>
    {
        var client = new Supabase.Client(supabaseUrl, supabaseKey, new SupabaseOptions
        {
            AutoRefreshToken = true,
            AutoConnectRealtime = false
        });
        
        if (builder.Environment.IsDevelopment())
        {
            Console.WriteLine($"✅ Supabase 클라이언트 생성 완료: {supabaseUrl}");
            Console.WriteLine("📊 DB 쿼리 로그가 활성화되었습니다. (appsettings.json의 로깅 설정 확인)");
        }
        
        return client;
    });

    // PostService 등록
    builder.Services.AddScoped<CSR.Services.PostService>();
    
    // MenuService 등록
    builder.Services.AddScoped<CSR.Services.MenuService>();
}
else
{
    // 개발 환경에서 Supabase 설정이 없을 때 경고만 출력
    if (builder.Environment.IsDevelopment())
    {
        Console.WriteLine("⚠️  경고: Supabase 설정이 없습니다. appsettings.json에 Supabase URL과 AnonKey를 설정해주세요.");
    }
}

builder.Services.AddScoped<OracleConnection>(sp =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    var connectionString = configuration.GetConnectionString("OracleConnection");

    if (string.IsNullOrEmpty(connectionString))
    {
        // 연결 문자열이 없으면
        throw new InvalidOperationException("OracleConnection 연결 문자열이 설정되지 않았습니다. appsettings.json을 확인해주세요.");
    }

    // 주입된 OracleConnection은 사용하는 서비스 내에서 using 블록을 통해 관리(Open/Close)되어야 합니다.
    return new OracleConnection(connectionString);
});

// UserService 등록 (Oracle DB 사용)
builder.Services.AddScoped<CSR.Services.UserService>();

var app = builder.Build();

// Configure the HTTP request pipeline.

if(app.Environment.IsDevelopment()){
    
     app.UseDeveloperExceptionPage();

} else {
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
