using System.Text;
using System.Text.Json.Serialization;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Portfolio.Api.Filters;
using Portfolio.Api.Middleware;
using Portfolio.Application.Auth;
using Portfolio.Application.Auth.Validators;
using Portfolio.Application.Holdings;
using Portfolio.Application.MarketData;
using Portfolio.Application.Portfolios;
using Portfolio.Application.Transactions;
using Portfolio.Infrastructure.Auth;
using Portfolio.Infrastructure.Holdings;
using Portfolio.Infrastructure.Identity;
using Portfolio.Infrastructure.Jobs;
using Portfolio.Infrastructure.MarketData;
using Portfolio.Infrastructure.Persistence;
using Portfolio.Infrastructure.Portfolios;
using Portfolio.Infrastructure.Transactions;
using Quartz;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers(options =>
{
    options.Filters.Add<FluentValidationActionFilter>();
}).AddJsonOptions(options =>
{
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" },
            },
            Array.Empty<string>()
        },
    });
});

builder.Services.AddValidatorsFromAssemblyContaining<RegisterRequestValidator>();

builder.Services.AddDbContext<PortfolioDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

builder.Services.AddDataProtection();

builder.Services
    .AddIdentityCore<ApplicationUser>(options =>
    {
        options.Password.RequiredLength = 8;
        options.User.RequireUniqueEmail = true;
    })
    .AddRoles<IdentityRole<Guid>>()
    .AddEntityFrameworkStores<PortfolioDbContext>()
    .AddDefaultTokenProviders()
    .AddSignInManager();

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));
builder.Services.AddSingleton<ITokenService, JwtTokenService>();

builder.Services.AddScoped<IPortfolioService, PortfolioService>();
builder.Services.AddScoped<ITransactionService, TransactionService>();
builder.Services.AddScoped<IHoldingsService, HoldingsService>();

builder.Services.Configure<AlphaVantageOptions>(builder.Configuration.GetSection(AlphaVantageOptions.SectionName));
var alphaVantageOptions = builder.Configuration.GetSection(AlphaVantageOptions.SectionName).Get<AlphaVantageOptions>()
    ?? throw new InvalidOperationException("AlphaVantage configuration section is missing.");
builder.Services.AddHttpClient<IMarketDataClient, AlphaVantageClient>(client =>
{
    client.BaseAddress = new Uri(alphaVantageOptions.BaseUrl);
});
builder.Services.AddScoped<PortfolioSnapshotService>();

builder.Services.AddQuartz(q =>
{
    var priceRefreshJobKey = new JobKey(nameof(PriceRefreshJob));
    q.AddJob<PriceRefreshJob>(opts => opts.WithIdentity(priceRefreshJobKey));
    q.AddTrigger(opts => opts
        .ForJob(priceRefreshJobKey)
        .WithIdentity($"{nameof(PriceRefreshJob)}-trigger")
        .WithCronSchedule("0 0 6 * * ?")); // Daily at 06:00 UTC.
});
builder.Services.AddQuartzHostedService(opts => opts.WaitForJobsToComplete = true);

var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
    ?? throw new InvalidOperationException("Jwt configuration section is missing.");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

const string FrontendCorsPolicy = "Frontend";
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? ["http://localhost:5173"];

builder.Services.AddCors(options =>
{
    options.AddPolicy(FrontendCorsPolicy, policy =>
    {
        policy.WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<PortfolioDbContext>();
    await db.Database.MigrateAsync();
}

app.UseExceptionHandler();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors(FrontendCorsPolicy);

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
