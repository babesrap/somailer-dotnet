using dotnetprojekt.Authentication;
using dotnetprojekt.Context;
using dotnetprojekt.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace dotnetprojekt.Controllers
{
    public class LoginController : Controller
    {
        private readonly WineLoversContext _context;
        private readonly JwtProvider _jwtProvider;

        // Wstrzyknięcie kontekstu przez konstruktor
        public LoginController(WineLoversContext context,JwtProvider jwtProvider)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _jwtProvider = jwtProvider ?? throw new ArgumentNullException(nameof(jwtProvider));
        }

        [HttpPost]
        public async Task<IActionResult> Verify(User user)
        {
            string message;

            if(string.IsNullOrWhiteSpace(user.Username) || string.IsNullOrWhiteSpace(user.Password))
            {
                message = "Please enter correct username and password";

                ViewBag.message = message;
                return View("/Views/Auth/Login.cshtml");
            }

            var reg = await _context.Users.FirstOrDefaultAsync(x => x.Username == user.Username && x.Password == user.Password);

            if( reg != null)
            {
                // Generate JWT token
                var token = _jwtProvider.GenerateJwtToken(reg);
                
                // Better cookie configuration that works with both HTTP and HTTPS
                Response.Cookies.Append("jwt", token, new CookieOptions
                {
                    HttpOnly = true,
                    SameSite = SameSiteMode.Lax, // Changed from None to Lax for better browser compatibility
                    Secure = Request.IsHttps,     // Only set Secure flag when using HTTPS
                    Expires = DateTime.UtcNow.AddMinutes(60),
                    Path = "/"                    // Ensure cookie is sent with all requests to the domain
                });
                
                Console.WriteLine($"[Login Debug] Token generated for user: {reg.Username}");
                Console.WriteLine($"[Login Debug] Token: {token.Substring(0, 20)}...");

                return RedirectToAction("Index", "Home");
            }

            message = "Invalid username or password, try again.";
            ViewBag.message = message;

            return View("Views/Auth/Login.cshtml");
            
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            Response.Cookies.Delete("jwt");

            return RedirectToAction("Index", "Home");

        }
    }
}
