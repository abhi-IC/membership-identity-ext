using MembershipIdentityProvider.Code;
using MembershipIdentityProvider.Code.Identity;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using SampleWebsite.Code.Models;

namespace SampleWebsite.Code.Controllers
{
    [Authorize]
    public class AccountController(
        UserManager<MembershipUser> userManager, 
        SignInManager<MembershipUser> signInManager,
        RoleManager<MembershipRole> roleManager,
        IOptions<MembershipSettings> membershipSettings) : Controller
    {
        private MembershipSettings MembershipSettings { get; } = membershipSettings.Value;

		[HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Login(string returnUrl = null)
        {
            // Clear the existing external cookie to ensure a clean login process
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            if (ModelState.IsValid)
            {
                // This doesn't count login failures towards account lockout
                // To enable password failures to trigger account lockout, set lockoutOnFailure: true
                var result = await signInManager.PasswordSignInAsync(model.UserLogin, model.Password, model.RememberMe, lockoutOnFailure: false);
                if (result.Succeeded)
                {
                    //_logger.LogInformation("User logged in.");
                    return RedirectToLocal(returnUrl);
                }

                if (result.IsLockedOut)
                {
                    //_logger.LogWarning("User account locked out.");
                    return RedirectToAction(nameof(Lockout));
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                    return View(model);
                }
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Lockout()
        {
            return View();
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Register(string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model, string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            if (ModelState.IsValid)
            {
                var user = new MembershipUser { 
                    UserName = model.UserLogin, 
                    Email = model.Email,
                    Password = model.Password,
                    PasswordFormat = MembershipSettings.PasswordFormat,
					PasswordQuestion = model.PasswordQuestion,
					PasswordAnswer = model.PasswordAnswer,
				};

                var result = await userManager.CreateAsync(user, model.Password);
                if (result.Succeeded)
                {
                    //_logger.LogInformation("User created a new account with password.");

                    //var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                    //var callbackUrl = Url.EmailConfirmationLink(user.Id, code, Request.Scheme);
                    //await _emailSender.SendEmailConfirmationAsync(model.Email, callbackUrl);

                    await signInManager.SignInAsync(user, isPersistent: false);
                    //_logger.LogInformation("User created a new account with password.");
                    return RedirectToLocal(returnUrl);
                }
                AddErrors(result);
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

		//[HttpPost]
		//[ValidateAntiForgeryToken]
		public async Task<IActionResult> Logout()
        {
            await signInManager.SignOutAsync();
            //_logger.LogInformation("User logged out.");
            return RedirectToAction(nameof(HomeController.Index), "Home");
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }

        #region Helpers

        private void AddErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }

        private IActionResult RedirectToLocal(string returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            else
            {
                return RedirectToAction(nameof(HomeController.Index), "Home");
            }
        }

		#endregion

		#region Some methods to exemplify the use of the MembershipIdentityProvider
		public async Task<IActionResult> DeleteUser()
		{
			var user = await userManager.FindByNameAsync(User.Identity!.Name!);
			var result = await userManager.DeleteAsync(user!);

			if (result.Succeeded)
			{
				return Content("user is deleted");
			}
			AddErrors(result);

			return View();
		}

		/// <summary>
		/// Action for Testing
		/// </summary>
		/// <returns></returns>
		public async Task<IActionResult> CreateRole()
		{
			var role = new MembershipRole
			{
				Name = "TestingRole",
				Description = "A new role"
			};
			var result = await roleManager.CreateAsync(role);
			if (result.Succeeded)
			{
				//_logger.LogInformation("User created a new account with password.");
				return RedirectToAction(nameof(HomeController.Index), "Home");
			}
			AddErrors(result);

			return View();
		}

		public async Task<IActionResult> DeleteRole()
		{
			var role = await roleManager.FindByNameAsync("TestingRole");
			var result = await roleManager.DeleteAsync(role!);

			if (result.Succeeded)
			{
				return RedirectToAction(nameof(HomeController.Index), "Home");
			}
			AddErrors(result);

			return View();
		}

		public async Task<IActionResult> AssignRoleToUser()
		{
			var user = await userManager.FindByNameAsync(User.Identity!.Name!);
			await userManager.AddToRoleAsync(user!, "TestingRole");

			if (await userManager.IsInRoleAsync(user!, "TestingRole"))
			{
				return Content("user is in role");
			}

			return View();
		}

		public async Task<IActionResult> RemoveUserFromRole()
		{
			var user = await userManager.FindByNameAsync(User.Identity!.Name!);
			await userManager.RemoveFromRoleAsync(user!, "TestingRole");

			if (!await userManager.IsInRoleAsync(user!, "TestingRole"))
			{
				return Content("user isn't in role");
			}

			return View();
		}

		#endregion
	}
}
