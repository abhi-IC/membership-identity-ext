using MembershipIdentityProvider.Code.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace SampleWebsite.Code.Controllers
{
    public class HomeController(UserManager<MembershipUser> userManager) : Controller
    {
		public async Task<IActionResult> Index()
        {
            //var users = await userManager.GetUsersInRoleAsync("Jornalistas");

            return View();
        }
    }
}
