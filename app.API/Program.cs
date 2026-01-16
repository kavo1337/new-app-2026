using app.API.Contracts;
using app.API.Data;
using app.API.Data.Models;
using app.API.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using VWSR.Api.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSwaggerGen();
builder.Services.AddDbContext<AppDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("Default");
    options.UseSqlServer(connectionString);
});

builder.Services.AddSingleton<JwtTokenService>();
builder.Services.AddSingleton<RefreshTokenStore>();
builder.Services.AddSingleton<MonitoringStatusGenerator>();

var jwtKey = builder.Configuration["Jwt:Key"] ?? "ChangeThis";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "VendingService.API";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "VendingService.CLIENT";
var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = signingKey,
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });

builder.Services.AddAuthorization();
var app = builder.Build();
app.UseAuthentication();
app.UseAuthorization();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}


var authGroup = app.MapGroup("/api/authControllers");

authGroup.MapPost("/login", async (
    LoginRequest request,
    AppDbContext db,
    JwtTokenService tokenService,
    RefreshTokenStore refreshTokenStore) =>
{
    var user = await db.UserAccount
        .Include(u => u.UserRole)
        .FirstOrDefaultAsync(u => u.Email == request.Email);

    if (user is null || !user.IsActive)
    {
        return Results.Unauthorized();
    }

    if (!PasswordHasher.Verify(request.Password, user.PasswordHash, user.PasswordSalt))
    {
        return Results.Unauthorized();
    }

    var accessToken = tokenService.CreateAccessToken(user);
    var refreshToken = tokenService.CreateRefreshToken();

    refreshTokenStore.Store(
        refreshToken,
        new RefreshTokenEntry(user.UserAccountId, tokenService.GetRefreshTokenExpiry()));

    var profile = new UserProfile(
        user.UserAccountId,
        user.Email,
        BuildFullName(user),
        user.UserRole.Name,
        user.PhotoUrl);

    return Results.Ok(new LoginResponse(accessToken, refreshToken, profile));
});

authGroup.MapPost("/refresh-token", async (
    RefreshRequest request,
    AppDbContext db,
    JwtTokenService tokenService,
    RefreshTokenStore refreshTokenStore) =>
{
    if (!refreshTokenStore.TryGet(request.RefreshToken, out var entry))
    {
        return Results.Unauthorized();
    }

    var user = await db.UserAccount
        .Include(u => u.UserRole)
        .FirstOrDefaultAsync(u => u.UserAccountId == entry.UserAccountId);

    if (user is null || !user.IsActive)
    {
        return Results.Unauthorized();
    }

    var accessToken = tokenService.CreateAccessToken(user);
    var newRefreshToken = tokenService.CreateRefreshToken();

    refreshTokenStore.Remove(request.RefreshToken);
    refreshTokenStore.Store(
        newRefreshToken,
        new RefreshTokenEntry(user.UserAccountId, tokenService.GetRefreshTokenExpiry()));

    var profile = new UserProfile(
        user.UserAccountId,
        user.Email,
        BuildFullName(user),
        user.UserRole.Name,
        user.PhotoUrl);

    return Results.Ok(new LoginResponse(accessToken, newRefreshToken, profile));
});

authGroup.MapPost("/logout", (LogoutRequest request, RefreshTokenStore refreshTokenStore) =>
{
    refreshTokenStore.Remove(request.RefreshToken);
    return Results.Ok();
});

app.Run();


static string BuildFullName(UserAccount user)
{
    var parts = new[] { user.LastName, user.FirstName, user.Patronymic };
    return string.Join(" ", parts.Where(part => !string.IsNullOrWhiteSpace(part)));
}
