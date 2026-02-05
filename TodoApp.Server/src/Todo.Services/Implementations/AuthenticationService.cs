using MayNghien.Infrastructures.Models.Responses;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Identity.Client;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Todo.Commons.Enums;
using Todo.DTOs.Authentication.Requests;
using Todo.DTOs.Authentication.Responses;
using Todo.Models.Entities;
using Todo.Repositories.Interfaces;
using Todo.Services.Interfaces;

namespace Todo.Services.Implementations
{
    public class AuthenticationService : IAuthenticationService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IConfiguration _configuration;
        private readonly IRefreshTokenRepository _refreshTokenRepository;

        public AuthenticationService(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, RoleManager<IdentityRole> roleManager, IConfiguration configuration, IRefreshTokenRepository refreshTokenRepository)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _configuration = configuration;
            _refreshTokenRepository = refreshTokenRepository;
        }

        public async Task<AppResponse<LoginResponse>> LoginAsync(LoginRequest request)
        {
            var result = new AppResponse<LoginResponse>();
            try
            {
                ApplicationUser? user = await _userManager.FindByNameAsync(request.UserName);
                if (user == null)
                {
                    user = await _userManager.FindByEmailAsync(request.UserName);
                    if (user == null && request.UserName == "tanchuonghuynh3@gmail.com")
                    {
                        await CreateAdminAsync(request.UserName);
                        user = await _userManager.FindByEmailAsync(request.UserName);
                    }

                    if (user == null)
                        return result.BuildError("User not found. Please check your User name or email.");

                    if (!await _userManager.CheckPasswordAsync(user, request.Password))
                        return result.BuildError("Invalid credentials. Please check your password.");

                    var roles = await _userManager.GetRolesAsync(user);
                    if (!roles.Any())
                        return result.BuildError("User has no role assigned.");

                    var claims = await GetClaims(new LoginRequest { UserName = user.Email! }, user);
                    var (accessToken, refreshToken) = await GenerateTokens(user, claims);
                }
            }
            catch (Exception ex)
            {
                result.BuildError(ex.Message + " " + ex.StackTrace);
            }
            return result;
        }

        private async Task<(string accessToken, string refreshToken)> GenerateTokens(ApplicationUser user, IEnumerable<Claim> claims)
        {
            var accessToken = GenerateAccessToken(claims);
            var refreshToken = GenerateRefreshToken();
            var refreshTokenModel = new RefreshTokenModel
            {
                UserId = Guid.Parse(user.Id),
                RefreshToken = refreshToken,
                RefreshTokenExpiryTime = DateTime.UtcNow.AddMinutes(int.Parse(_configuration["Jwt:RefreshTokenExpiresIn"] ?? "10080")),
                IsRevoked = false,
                CreatedOn = DateTime.UtcNow,
                CreatedBy = user.Email
            };

            await _refreshTokenRepository.AddAsync(refreshTokenModel);
            return (accessToken, refreshToken);
        }

        private string GenerateRefreshToken()
        {
            var randomNumber = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomNumber);
                return Convert.ToBase64String(randomNumber);
            }
        }

        private string GenerateAccessToken(IEnumerable<Claim> claims)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddSeconds(int.Parse(_configuration["Jwt:AccessTokenExpiresIn"] ?? "3600")),
                signingCredentials: credentials
            );
            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private async Task<List<Claim>> GetClaims(LoginRequest user, ApplicationUser identityUser)
        {
            var claims = new List<Claim>()
            {
                new Claim(ClaimTypes.Email, user.UserName)
            };

            var roles = await _userManager.GetRolesAsync(identityUser);
            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
                claims.Add(new Claim(ClaimTypes.Name, user.UserName));
            }
            return claims;
        }

        private async Task<ApplicationUser> CreateAdminAsync(string email)
        {
            var admin = new ApplicationUser
            {
                Email = email,
                EmailConfirmed = true,
                UserName = email,
                Role = Role.SuperAdmin
            };

            await _userManager.CreateAsync(admin);
            await _userManager.AddPasswordAsync(admin, "Admin@123");

            if (!await _roleManager.RoleExistsAsync("SuperAdmin"))
                await _roleManager.CreateAsync(new IdentityRole("SuperAdmin"));

            await _userManager.AddToRoleAsync(admin, "SuperAdmin");
            return admin;
        }

        public async Task LogoutAsync()
        {
            await _signInManager.SignOutAsync();
        }

        public async Task<AppResponse<RegisterResponse>> RegisterAsync(RegisterRequest request)
        {
            var result = new AppResponse<RegisterResponse>();
            try
            {
                if (await CheckUserExists(request.Email, request.PhoneNumber))
                    return result.BuildError("User already exists.");

                var createResult = await CreateUserAsync(request);
                if (!createResult.Succeeded)
                    return result.BuildError(string.Join(", ", createResult.Errors.Select(e => e.Description)));

                var user = await _userManager.FindByEmailAsync(request.Email);
                await AssignRole(user!, "User");

                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Email, user!.Email!),
                    new Claim(ClaimTypes.Role, "TenantAdmin"),
                };

                var (accessToken, refreshToken) = await GenerateTokens(user!, claims);
                var response = new RegisterResponse
                {
                    Email = request.Email,
                    AccessToken = accessToken,
                    PhoneNumber = request.PhoneNumber,
                    Name = request.Name,
                    RefreshToken = refreshToken,
                };

                await _signInManager.SignInAsync(user!, isPersistent: false);
                return result.BuildResult(response, "User registered successfully!");
            }
            catch (Exception ex)
            {
                result.BuildError(ex.Message + " " + ex.StackTrace);
            }
            return result;
        }

        private async Task<IdentityResult> CreateUserAsync(RegisterRequest request)
        {
            var user = new ApplicationUser
            {
                UserName = request.Email,
                Email = request.Email,
                EmailConfirmed = true,
                SecurityStamp = Guid.NewGuid().ToString(),
                PhoneNumber = request.PhoneNumber,
                Role = Role.User
            };
            return await _userManager.CreateAsync(user, request.Password);
        }

        private async Task AssignRole(ApplicationUser user, string role)
        {
            if (!await _roleManager.RoleExistsAsync(role))
                await _roleManager.CreateAsync(new IdentityRole(role));

            await _userManager.AddToRoleAsync(user, role);
        }

        private async Task<bool> CheckUserExists(string email, string phoneNumber)
        {
            var userByEmail = await _userManager.FindByEmailAsync(email);
            if (userByEmail != null) return true;

            return _userManager.Users.Any(p => p.PhoneNumber == phoneNumber);
        }
    }
}
