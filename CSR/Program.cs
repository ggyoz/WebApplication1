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

builder.Services.AddControllersWithViews()
    .AddViewLocalization() // 뷰에서 다국어를 지원하도록 설정
    .AddDataAnnotationsLocalization(); // 데이터 유효성 검사 메시지에서 다국어를 지원하도록 설정

// FluentValidation 등록
// builder.Services.AddFluentValidationAutoValidation(); // 비동기 검증을 위해 자동 유효성 검사는 비활성화
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

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

}
else
{
    // 개발 환경에서 Supabase 설정이 없을 때 경고만 출력
    if (builder.Environment.IsDevelopment())
    {
        Console.WriteLine("⚠️  경고: Supabase 설정이 없습니다. appsettings.json에 Supabase URL과 AnonKey를 설정해주세요.");
    }
}

// Oracle DB 연결을 위한 IDbConnection 등록
builder.Services.AddScoped<IDbConnection>(sp =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    var connectionString = configuration.GetConnectionString("OracleConnection");

    if (string.IsNullOrEmpty(connectionString))
    {
        throw new InvalidOperationException("OracleConnection 연결 문자열이 설정되지 않았습니다. appsettings.json을 확인해주세요.");
    }
    
    return new OracleConnection(connectionString);
});

// CSR.Services 네임스페이스의 모든 서비스를 자동으로 등록합니다.
var serviceTypes = typeof(Program).Assembly.GetTypes()
    .Where(t => t.IsClass && !t.IsAbstract && t.Namespace == "CSR.Services");

foreach (var service in serviceTypes)
{
    builder.Services.AddScoped(service);
}

// 다국어 옵션 설정
builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    var supportedCultures = new[] { "ko-KR", "en-US", "hi-IN", "zh-CN" };
    options.SetDefaultCulture(supportedCultures[1]); // en-US
    options.AddSupportedCultures(supportedCultures);
    options.AddSupportedUICultures(supportedCultures);
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

app.UseRouting();

app.UseAuthentication(); // 인증 미들웨어 추가
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
