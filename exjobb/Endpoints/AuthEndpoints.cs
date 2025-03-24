using exjobb.Models;
using exjobb.Services;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;

namespace exjobb.Endpoints
{
    public static class AuthEndpoints
    {
        public static RouteGroupBuilder MapAuth(this RouteGroupBuilder group)
        {
            group.MapPost("/register", Register);
            group.MapPost("/login", Login);
            group.MapPost("/refresh", Refresh);
            group.MapPost("/makeadmin", MakeAdmin).RequireAuthorization("Admin");
            group.MapPost("/removeadmin", RemoveAdmin).RequireAuthorization("Admin");
            group.MapPost("/logout", Logout);
            group.MapGet("/users", GetAllUsers).RequireAuthorization("Admin");
            group.MapGet("/user/{id}", GetUser);
            group.MapPost("/google-login", GoogleLogin);
            group.MapPost("/google-register", GoogleRegister);

            return group;
        }

        static async Task<IResult> Register(CreateUserDto user, IAuthService authService, HttpContext httpContext)
        {
            var result = await authService.RegisterUser(user);

            if (!result.Success)
            {
                return TypedResults.BadRequest(new { errors = result.Errors });
            }

            SetCookie(httpContext, result.Data!.RefreshToken);

            return TypedResults.Ok(new { result.Data.User, result.Data.AccessToken });
        }

        static async Task<IResult> Login(LoginUserDto user, IAuthService authService, HttpContext httpContext)
        {
            var result = await authService.LoginUser(user);
            if (!result.Success)
            {
                return TypedResults.BadRequest(new { errors = result.Errors });
            }

            SetCookie(httpContext, result.Data!.RefreshToken);
            return TypedResults.Ok(new { result.Data.User, result.Data.AccessToken });
        }

        static async Task<IResult> GoogleLogin(GoogleLoginRequest request, IAuthService authService,
            IGoogleService googleService, HttpContext httpContext)
        {

            if (string.IsNullOrEmpty(request?.Code))
            {
                return Results.BadRequest("Authorization code is required.");
            }

            var googleResponse = await googleService.ExchangeCodeForAccessToken(request.Code);
            if (googleResponse == null)
            {
                return Results.Unauthorized();
            }

            if (string.IsNullOrEmpty(googleResponse.Email))
            {
                return TypedResults.NotFound("No email from Google");
            }

            var result = await authService.LoginGoogleUser(googleResponse.Email);
            if (!result.Success)
            {
                return TypedResults.BadRequest(new { errors = result.Errors });
            }

            SetCookie(httpContext, result.Data!.RefreshToken);

            return TypedResults.Ok(new { result.Data.User, result.Data.AccessToken });

        }


        static async Task<IResult> GoogleRegister(GoogleLoginRequest request, IAuthService authService,
            IGoogleService googleService, HttpContext httpContext)
        {
            if (string.IsNullOrEmpty(request?.Code))
            {
                return Results.BadRequest("Authorization code is required.");
            }

            var googleResponse = await googleService.ExchangeCodeForAccessToken(request.Code);
            if (googleResponse == null)
            {
                return Results.Unauthorized();
            }

            var dto = new CreateUserDto
            {
                Email = googleResponse.Email,
                FirstName = googleResponse.GivenName,
                LastName = googleResponse.FamilyName
            };
            var result = await authService.RegisterUser(dto);
            if (!result.Success)
            {
                return TypedResults.BadRequest(new { errors = result.Errors });
            }

            SetCookie(httpContext, result.Data!.RefreshToken);

            return TypedResults.Ok(new { result.Data.User, result.Data.AccessToken });

        }

        static async Task<IResult> Refresh(IAuthService authService, HttpContext httpContext)
        {
            if (!httpContext.Request.Cookies.TryGetValue("refresh", out string? refreshToken))
            {
                return Results.Unauthorized();
            }


            var tokens = await authService.Refresh(refreshToken);
            if (!tokens.Success)
            {
                return TypedResults.Unauthorized();
            }
            SetCookie(httpContext, tokens.Data!.RefreshToken);

            return TypedResults.Ok(new { tokens.Data.User, tokens.Data.AccessToken });
        }

        static async Task<IResult> MakeAdmin([FromBody] string email, IAuthService authService)
        {
            var result = await authService.MakeUserAdmin(email);
            if (!result.Success)
            {
                return TypedResults.BadRequest(new { errors = result.Errors });
            }
            return TypedResults.NoContent();
        }

        static async Task<IResult> RemoveAdmin([FromBody] string email, IAuthService authService)
        {
            var result = await authService.RemoveAdminRights(email);
            if (!result.Success)
            {
                return TypedResults.BadRequest(new { errors = result.Errors });
            }
            return TypedResults.NoContent();
        }

        static async Task<IResult> GetAllUsers(IAuthService authService)
        {
            var users = await authService.GetAllUsersAsync();

            if (users is null)
            {
                return TypedResults.NotFound($"No users found");
            }

            return TypedResults.Ok(users);
        }
        static async Task<IResult> GetUser(string id, IAuthService authService)
        {
            var user = await authService.GetUserAsync(id);
            if (user is null)
            {
                return TypedResults.NotFound($"User with id {id} could not be found");
            }
            return TypedResults.Ok(user);
        }

        static async Task<IResult> Logout(IAuthService authService, HttpContext httpContext)
        {
            var authHeader = httpContext.Request.Headers["Authorization"].FirstOrDefault();
            var token = authHeader?.Split(" ").Last();

            if (!string.IsNullOrEmpty(token))
            {
                var handler = new JwtSecurityTokenHandler();
                var jwtToken = handler.ReadJwtToken(token);
                var userId = jwtToken.Claims.FirstOrDefault(c => c.Type == "sub")?.Value;

                if (string.IsNullOrEmpty(userId))
                {
                    return TypedResults.BadRequest("Missing bearer token");
                }
                var result = await authService.LogoutUser(userId);
                if (!result.Success)
                {
                    return TypedResults.NotFound(new { errors = result.Errors });
                }

            }
            //radera cookie
            httpContext.Response.Cookies.Append("refresh", "",
            new CookieOptions
            {
                Expires = DateTime.UtcNow.AddDays(-1),
                Path = "/",
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None
            });
            return Results.NoContent();
        }

        private static void SetCookie(HttpContext httpContext, string refreshToken)
        {
            httpContext.Response.Cookies.Append("refresh", refreshToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None,
                Expires = DateTime.UtcNow.AddDays(1),

            });
        }
    }
}
