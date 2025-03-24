using exjobb.Models;

namespace exjobb.Services
{
    public interface IAuthService
    {
        Task<ServiceResult<AuthTokensDto>> RegisterUser(CreateUserDto userDto);
        Task<ServiceResult<AuthTokensDto>> LoginUser(LoginUserDto userDto);
        Task<ServiceResult<AuthTokensDto>> Refresh(string refreshToken);
        Task<ServiceResult<string>> LogoutUser(string id);
        Task<ServiceResult<string>> MakeUserAdmin(string email);
        Task<ServiceResult<string>> RemoveAdminRights(string email);
        Task<ServiceResult<AuthTokensDto>> LoginGoogleUser(string email);
        Task<UserResponse> GetUserAsync(string id);
        Task<List<UserResponse>> GetAllUsersAsync();
    }
}
