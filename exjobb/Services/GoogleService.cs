using exjobb.Models;
using Google.Apis.Auth;
using Newtonsoft.Json;

namespace exjobb.Services
{
    public class GoogleService : IGoogleService
    {
        private readonly IHttpClientFactory _clientFactory;
        private readonly IConfiguration _configuration;

        public GoogleService(IHttpClientFactory clientFactory, IConfiguration configuration)
        {
            _clientFactory = clientFactory;
            _configuration = configuration;
        }

        public async Task<GoogleUserInfo> ExchangeCodeForAccessToken(string code)
        {
            var clientId = _configuration.GetValue<string>("GoogleSettings:ClientId");
            var clientSecret = _configuration.GetValue<string>("GoogleSettings:ClientSecret");
            var url = _configuration.GetValue<string>("GoogleSettings:Url");
            var client = _clientFactory.CreateClient();
            //Console.WriteLine("Logg values" + clientId + " " + clientSecret + " " + url);
            var values = new Dictionary<string, string>
            {
                { "code", code },
                { "client_id", clientId },
                { "client_secret", clientSecret },
                { "redirect_uri", "http://localhost:5173" },
                { "grant_type", "authorization_code" }
            };

            var content = new FormUrlEncodedContent(values);

            try
            {
                var response = await client.PostAsync(url, content);

                if (response.IsSuccessStatusCode)
                {
                    var responseBody = await response.Content.ReadAsStringAsync();
                    var tokenResponse = JsonConvert.DeserializeObject<GoogleTokenResponse>(responseBody);

                    //verify Google ID-token
                    var res = await VerifyGoogleToken(tokenResponse!.IdToken);

                    if (!string.IsNullOrWhiteSpace(res.Name))
                    {
                        var nameParts = res.Name.Split(' ');
                        res.GivenName = nameParts[0];
                        res.FamilyName = nameParts.Length > 1 ? nameParts[1] : ""; // if we have a lastname we set it else we use empty string
                    }
                    var userInfo = new GoogleUserInfo
                    {
                        Email = res.Email,
                        Name = res.Name,
                        GivenName = res.GivenName,
                        FamilyName = res.FamilyName
                    };
                    return userInfo;
                }
                else
                {
                    Console.WriteLine($"Failed to exchange code: {response.StatusCode}");
                    //should have logging here
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error occurred: {ex.Message}");
            }

            return null;
        }

        private async Task<GoogleJsonWebSignature.Payload> VerifyGoogleToken(string idToken)
        {
            var settings = new GoogleJsonWebSignature.ValidationSettings
            {
                Audience = new[] { _configuration.GetValue<string>("GoogleSettings:ClientId") }
            };

            var payload = await GoogleJsonWebSignature.ValidateAsync(idToken, settings);

            return payload;
        }

        //public async Task<GoogleUserInfo> ValidateGoogleAccessToken(string code)
        //{
        //    var client = _clientFactory.CreateClient();
        //    var url = $"https://oauth2.googleapis.com/tokeninfo?access_token={code}";

        //    var response = await client.GetAsync(url);
        //    if (response.IsSuccessStatusCode)
        //    {
        //        var result = await response.Content.ReadAsStringAsync();
        //        return JsonConvert.DeserializeObject<GoogleUserInfo>(result);
        //        //var googleResponse = JObject.Parse(result);
        //        //return response;
        //    }
        //    else
        //    {
        //        // Logga och hantera fel här
        //        Console.WriteLine("Error validating token");
        //        return null;
        //    }
        //}

        //public async Task<GoogleUserInfo> GetGoogleUserInfo(string accessToken)
        //{
        //    var client = _clientFactory.CreateClient();
        //    var url = "https://www.googleapis.com/oauth2/v3/userinfo";
        //    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        //    var response = await client.GetAsync(url);


        //    if (response.IsSuccessStatusCode)
        //    {
        //        var result = await response.Content.ReadAsStringAsync();
        //        //Console.WriteLine("res" + result);
        //        return JsonConvert.DeserializeObject<GoogleUserInfo>(result);
        //    }
        //    else
        //    {
        //        throw new Exception("Failed to fetch user-information info from Google");
        //    }
        //}


    }
}
