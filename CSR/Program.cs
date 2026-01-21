using Supabase;
using Oracle.ManagedDataAccess.Client;
using System.Data;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Authentication.Cookies;
using System;
using FluentValidation;
using FluentValidation.AspNetCore;
using CSR.Data;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Dapper bool<->number(1/0) 타입 핸들러 등록
Dapper.SqlMapper.AddTypeHandler(new BooleanNumericTypeHandler());

// Add services to the container.
// 다국어 서비스 등록
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");

// 인증 서비스 등록
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
        options.SlidingExpiration = true;
    });


# region 권한 설정
// 권한 정책 정의
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireTeamLeaderOrHigher", policy => 
        policy.RequireRole("R2", "R3", "R4"));

    options.AddPolicy("RequireManagerOrHigher", policy => 
        policy.RequireRole("R3", "R4"));

    options.AddPolicy("RequireSuperAdmin", policy => 
        policy.RequireRole("R4"));
});

# endregion

builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add(new AuthorizeFilter());
})
    .AddViewLocalization()                                              // 뷰에서 다국어를 지원하도록 설정
    .AddDataAnnotationsLocalization();                                  // 데이터 유효성 검사 메시지에서 다국어를 지원하도록 설정

// FluentValidation 등록
// builder.Services.AddFluentValidationAutoValidation();                // 비동기 검증을 위해 자동 유효성 검사는 비활성화
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

# region Supabase 클라이언트 등록
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

}
else
{
    // 개발 환경에서 Supabase 설정이 없을 때 경고만 출력
    if (builder.Environment.IsDevelopment())
    {
        Console.WriteLine("⚠️  경고: Supabase 설정이 없습니다. appsettings.json에 Supabase URL과 AnonKey를 설정해주세요.");
    }
}
# endregion 

# region Oracle 클라이언트 등록
// Oracle DB 연결을 위한 IDbConnection 등록
builder.Services.AddScoped<IDbConnection>(sp =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    var connectionString = configuration.GetConnectionString("OracleConnection");

    if (string.IsNullOrEmpty(connectionString))
    {
        throw new InvalidOperationException("OracleConnection 연결 문자열이 설정되지 않았습니다. appsettings.json을 확인해주세요.");
    }
    
    var connection = new OracleConnection(connectionString);
    connection.Open();
    return connection;
});
# endregion



# region 서비스 자동등록 

// CSR.Services 네임스페이스의 모든 서비스를 자동으로 등록합니다.

var serviceTypes = typeof(Program).Assembly.GetTypes()
                    .Where(t => t.IsClass && !t.IsAbstract && t.Namespace == "CSR.Services");



foreach (var type in serviceTypes)
{
    // 'I' + 클래스 이름 규칙을 따르는 인터페이스를 찾습니다.
    var serviceInterface = type.GetInterfaces().FirstOrDefault(i => i.Name == "I" + type.Name);

    if (serviceInterface != null)
    {
        // 매칭되는 인터페이스가 있으면, <인터페이스, 구현체>로 등록합니다.
        builder.Services.AddScoped(serviceInterface, type);
    }
    else
    {
        // 매칭되는 인터페이스가 없으면, 클래스 자체를 등록합니다. (예: UserService)
        builder.Services.AddScoped(type);
    }
}

# endregion

# region 다국어 설정
// 다국어 옵션 설정
builder.Services.Configure<RequestLocalizationOptions>(options =>
{

    var supportedCultures = new[] { "ko-KR", "en-US" };
    // var supportedCultures = new[] { "ko-KR", "en-US", "hi-IN", "zh-CN" };
    options.SetDefaultCulture(supportedCultures[1]); // en-US
    options.AddSupportedCultures(supportedCultures);
    options.AddSupportedUICultures(supportedCultures);
});

# endregion

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped(sp => sp.GetRequiredService<IHttpContextAccessor>().HttpContext);

// 세션 서비스 등록
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Rate Limiter 서비스 등록 (imageUploadPolicy 정책)
builder.Services.AddRateLimiter(options =>
{

    // options.AddPolicy("imageUploadPolicy", context =>
    // {
    //     var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    //     return RateLimitPartition.GetFixedWindowLimiter(ip, _ =>
    //         new FixedWindowRateLimiterOptions
    //         {                
    //             PermitLimit = 2,
    //             Window = TimeSpan.FromMinutes(1),
    //             QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
    //             QueueLimit = 0
    //         });
    // });

    
    options.AddFixedWindowLimiter(policyName: "imageUploadPolicy", opt =>
    {
        opt.PermitLimit = 2; // 테스트를 위해 2개로 제한
        opt.Window = TimeSpan.FromMinutes(1);
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        opt.QueueLimit = 0; // 대기열 없이 즉시 거부
    });

    // 요청이 거부되었을 때의 응답을 설정
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = (context, token) =>
    {
        context.HttpContext.Response.WriteAsync("Too many requests. Please try again later.", cancellationToken: token);
        return new ValueTask();
    };
});

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

app.UseRequestLocalization(); // 요청 파이프라인에 미들웨어 추가

app.UseRateLimiter(); // Rate Limiter 미들웨어 추가

app.UseRouting();

app.UseSession(); // 세션 미들웨어 추가

app.UseAuthentication(); // 인증 미들웨어 추가
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();