using CodeFlowMvc.Configuration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CodeFlowMvc.Controllers
{
    [Authorize(AuthenticationSchemes = IndiceDefaults.AuthenticationScheme)]
    public class ProtectedController : Controller
    {
        [HttpGet]
        public ViewResult GetMyClaims() {
            var claims = User.Claims;
            return View(claims);
        }
    }
}
