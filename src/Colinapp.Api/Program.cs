using System.Text;
using Colinapp.Api.Authorization;
using Colinapp.Api.Common;
using Colinapp.Api.Filters;
using Colinapp.Api.Middleware;
using Colinapp.Application;
using Colinapp.Application.Auth;
using Colinapp.Application.Common;
using Colinapp.Application.Storage;
using Colinapp.Infrastructure;
using Colinapp.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// ---- Serilog ----
builder.Host.UseSerilog((context, config) =>
    config.ReadFrom.Configuration(context.Configuration)
          .Enrich.FromLogContext()
          .WriteTo.Console()
          .WriteTo.File("logs/app-.log", rollingInterval: RollingInterval.Day));

// ---- 分层服务注册 ----
builder.Services.AddApplication(builder.Configuration);
builder.Services.AddInfrastructure(builder.Configuration);

// 当前用户上下文（基于 HttpContext）
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();

builder.Services.AddMemoryCache();

// 全局操作日志过滤器
builder.Services.AddScoped<OperationLogFilter>();
builder.Services.AddControllers(options => options.Filters.AddService<OperationLogFilter>());

// ---- JWT 认证 ----
var jwt = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()!;
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt.Issuer,
            ValidateAudience = true,
            ValidAudience = jwt.Audience,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SecretKey)),
            ClockSkew = TimeSpan.FromSeconds(30),
        };
    });
builder.Services.AddAuthorization();
// 动态权限策略（[HasPermission("xxx")]）
builder.Services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
builder.Services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();

// ---- Swagger ----
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "Colinapp API", Version = "v1" });

    var scheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Description = "请输入 JWT：Bearer {token}",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
    };
    options.AddSecurityDefinition("Bearer", scheme);
    options.AddSecurityRequirement(new OpenApiSecurityRequirement { [scheme] = [] });
});

// ---- CORS（开发期放开，便于前端联调）----
builder.Services.AddCors(options =>
    options.AddDefaultPolicy(p => p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// ---- 上传文件静态访问（默认 /uploads）----
var fileStorage = app.Services.GetRequiredService<IOptions<FileStorageOptions>>().Value;
var uploadRoot = fileStorage.ResolveRootPath();
Directory.CreateDirectory(uploadRoot);
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(uploadRoot),
    RequestPath = fileStorage.RequestPath,
});

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// ---- 启动时迁移并初始化种子数据 ----
await DbInitializer.InitializeAsync(app.Services);

app.Run();
