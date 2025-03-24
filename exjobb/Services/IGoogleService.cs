using exjobb.Models;

namespace exjobb.Services
{
    public interface IGoogleService
    {
        Task<GoogleUserInfo> ExchangeCodeForAccessToken(string accessToken);
        //Task<GoogleJsonWebSignature.Payload> VerifyGoogleToken(string idToken);
    }
}
