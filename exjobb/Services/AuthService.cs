using exjobb.Entities;
using exjobb.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;

namespace exjobb.Services
{
    public class AuthService : IAuthService
    {
        private readonly IConfiguration _configuration;
        private readonly UserManager<AppUser> _userManager;
        private readonly SignInManager<AppUser> _signInManager;

        public AuthService(IConfiguration configuration, UserManager<AppUser> userManager,
            SignInManager<AppUser> signInManager)
        {
            _configuration = configuration;
            _userManager = userManager;
            _signInManager = signInManager;

        }
        public async Task<ServiceResult<AuthTokensDto>> RegisterUser(CreateUserDto request)
        {
            try
            {
                var newUser = new AppUser
                {
                    FirstName = request.FirstName,
                    LastName = request.LastName,
                    Email = request.Email,
                    UserName = request.Email
                };

                IdentityResult result;
                if (string.IsNullOrWhiteSpace(request.Password))
                {
                    result = await _userManager.CreateAsync(newUser);
                }
                else
                {
                    result = await _userManager.CreateAsync(newUser, request.Password);
                }


                if (!result.Succeeded)
                {
                    var errors = result.Errors.Select(e => e.Description).ToList();

                    return ServiceResult<AuthTokensDto>.Fail(result.Errors.Select(e => e.Description).ToList());
                }

                var token = await GenerateToken(newUser.Email);
                var refreshToken = await GenerateAndStoreRefreshToken(newUser);

                var responseUser = new UserResponse()
                {
                    Id = newUser.Id,
                    FirstName = newUser.FirstName,
                    LastName = newUser.LastName,
                    Email = newUser.Email
                };

                AuthTokensDto authTokens = new()
                {
                    User = responseUser,
                    AccessToken = token,
                    RefreshToken = refreshToken
                };
                return ServiceResult<AuthTokensDto>.Ok(authTokens);
            }
            catch (Exception)
            {
                return ServiceResult<AuthTokensDto>.Fail(new List<string> { "Internal Server Error" });
            }
        }

        public async Task<ServiceResult<AuthTokensDto>> LoginUser(LoginUserDto userDto)
        {
            try
            {
                var user = await _userManager.FindByEmailAsync(userDto.Email);
                if (user is null)
                {
                    return ServiceResult<AuthTokensDto>.Fail(new List<string> { "Email or Password is invalid" });
                }
                var result = await _signInManager.CheckPasswordSignInAsync(user, userDto.Password,
                   lockoutOnFailure: false);

                if (!result.Succeeded)
                {
                    return ServiceResult<AuthTokensDto>.Fail(new List<string> { "Email or Password is invalid" });
                }

                var token = await GenerateToken(user.Email!);
                var refreshToken = await GenerateAndStoreRefreshToken(user);

                var responseUser = new UserResponse()
                {
                    Id = user.Id,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Email = user.Email!
                };

                AuthTokensDto authTokens = new()
                {
                    User = responseUser,
                    AccessToken = token,
                    RefreshToken = refreshToken
                };

                return ServiceResult<AuthTokensDto>.Ok(authTokens);
            }
            catch (Exception)
            {
                return ServiceResult<AuthTokensDto>.Fail(new List<string> { "Internal Server Error" });
            }
        }

        public async Task<ServiceResult<AuthTokensDto>> Refresh(string refreshToken)
        {
            var user = await _userManager.Users.FirstOrDefaultAsync(u => u.RefreshToken == refreshToken);
            if (user is null)
            {
                return ServiceResult<AuthTokensDto>.Fail(new List<string>());
            }
            Console.WriteLine($"inkommande refresh: {refreshToken}, users refresh: {user.RefreshToken} ");
            if (user.RefreshTokenExpiration < DateTime.UtcNow)
            {
                return ServiceResult<AuthTokensDto>.Fail(new List<string>());
            }

            var newAccessToken = await GenerateToken(user.Email!);
            var newRefreshToken = await GenerateAndStoreRefreshToken(user);

            var responseUser = new UserResponse() { Id = user.Id, FirstName = user.FirstName, LastName = user.LastName, Email = user.Email! };
            AuthTokensDto authTokens = new()
            {
                User = responseUser,
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken
            };

            return ServiceResult<AuthTokensDto>.Ok(authTokens);
        }

        public async Task<ServiceResult<string>> MakeUserAdmin(string email)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user is null)
            {
                return ServiceResult<string>.Fail("User not found");
            }
            await _userManager.AddClaimAsync(user, new Claim("role", "admin"));

            return ServiceResult<string>.Ok(string.Empty);
        }

        public async Task<ServiceResult<string>> RemoveAdminRights(string email)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user is null)
            {
                return ServiceResult<string>.Fail("User not found");
            }
            await _userManager.RemoveClaimAsync(user, new Claim("role", "admin"));

            return ServiceResult<string>.Ok(string.Empty);
        }

        public async Task<List<UserResponse>> GetAllUsersAsync()
        {
            var users = await _userManager.Users.AsNoTracking().ToListAsync();
            if (users is null)
            {
                return null;
            }
            var responseUsers = users.Select(u => new UserResponse
            {
                Id = u.Id,
                FirstName = u.FirstName,
                LastName = u.LastName,
                Email = u.Email!
            }).ToList();

            return responseUsers;
        }

        public async Task<UserResponse> GetUserAsync(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user is null)
            {
                return null;
            }
            var responseUser = new UserResponse
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email!
            };
            return responseUser;
        }

        public async Task<ServiceResult<string>> LogoutUser(string id)
        {
            var user = await _userManager.FindByIdAsync(id);

            if (user is null)
            {
                return ServiceResult<string>.Fail("User not found");
            }

            user.RefreshToken = null;
            user.RefreshTokenExpiration = null;
            var results = await _userManager.UpdateAsync(user);

            if (!results.Succeeded)
            {
                return ServiceResult<string>.Fail("Failed to log out");
            }

            return ServiceResult<string>.Ok(string.Empty);
        }

        public async Task<ServiceResult<AuthTokensDto>> LoginGoogleUser(string email)
        {
            try
            {
                var user = await _userManager.FindByEmailAsync(email);
                if (user is null)
                {
                    return ServiceResult<AuthTokensDto>.Fail("Could not find user");
                }

                var token = await GenerateToken(user.Email!);
                var refreshToken = await GenerateAndStoreRefreshToken(user);

                var responseUser = new UserResponse
                {
                    Id = user.Id,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Email = user.Email!
                };

                AuthTokensDto authTokens = new()
                {
                    User = responseUser,
                    AccessToken = token,
                    RefreshToken = refreshToken
                };

                return ServiceResult<AuthTokensDto>.Ok(authTokens);
            }
            catch (Exception)
            {
                return ServiceResult<AuthTokensDto>.Fail(new List<string> { "Internal Server Error" });
            }
        }

        private async Task<string> GenerateToken(string email)
        {
            var user = await _userManager.FindByNameAsync(email);
            if (user is null)
            {
                throw new Exception("User not found");
            }

            var claimsList = new List<Claim>
            {
                new Claim("email", email),
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Iat,  ((DateTimeOffset)DateTime.UtcNow).ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
                new Claim(JwtRegisteredClaimNames.Exp, ((DateTimeOffset)DateTime.UtcNow).AddMinutes(10).ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
                new Claim(JwtRegisteredClaimNames.Iss, _configuration["JwtSettings:Issuer"]!),
                new Claim(JwtRegisteredClaimNames.Aud, _configuration["JwtSettings:Audience"]!)
            };

            var claimsFromDb = await _userManager.GetClaimsAsync(user!);

            claimsList.AddRange(claimsFromDb);

            var key = new SymmetricSecurityKey(Convert.FromBase64String(_configuration.GetValue<string>("jwtSettings:SigningKey:Value")!));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);


            var securityToken = new JwtSecurityToken(
                claims: claimsList,
                signingCredentials: credentials);

            var token = new JwtSecurityTokenHandler().WriteToken(securityToken);

            return token;
        }

        private string GenerateRefreshToken()
        {
            var randomNumber = new byte[32];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);

            return Convert.ToBase64String(randomNumber);
        }

        private async Task<string> GenerateAndStoreRefreshToken(AppUser user)
        {
            var refreshToken = GenerateRefreshToken();
            var expiration = DateTime.UtcNow.AddDays(1);

            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiration = expiration;

            await _userManager.UpdateAsync(user);

            return refreshToken;
        }

    }
}
