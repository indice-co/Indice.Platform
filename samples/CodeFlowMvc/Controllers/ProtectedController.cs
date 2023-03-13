using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace CodeFlowMvc.Controllers;

[Authorize]
public class ProtectedController : Controller
{
    public const string Name = "Protected";

    [HttpGet]
    public async Task<ViewResult> GetMyDetaiils() {
        var claims = User.Claims;
        var accessToken = await HttpContext.GetTokenAsync(".Token.access_token");
        var idToken = await HttpContext.GetTokenAsync(OpenIdConnectParameterNames.IdToken);
        var tokenType = await HttpContext.GetTokenAsync(OpenIdConnectParameterNames.TokenType);
        var refreshToken = await HttpContext.GetTokenAsync(OpenIdConnectParameterNames.RefreshToken);
        return View();
    }
}
