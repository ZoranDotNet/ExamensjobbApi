namespace exjobb.Models
{
    public class AuthTokensDto
    {
        public UserResponse User { get; set; } = null!;
        public string AccessToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
    }
}
