using System.Text;
using System.Threading.Tasks;
using LibraryDomain.Model;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;

namespace LibraryInfrastructure.Areas.Identity.Pages.Account
{
    public class ConfirmEmailModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public ConfirmEmailModel(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        public string StatusMessage { get; set; }

        public async Task<IActionResult> OnGetAsync(string userId, string code)
        {
            if (userId == null || code == null)
            {
                return RedirectToPage("/Index");
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound($"Не вдалося знайти користувача з ID: {userId}");
            }

            var decodedCode = WebEncoders.Base64UrlDecode(code);
            var result = await _userManager.ConfirmEmailAsync(user, Encoding.UTF8.GetString(decodedCode));

            if (result.Succeeded)
            {
                StatusMessage = "Вашу електронну пошту підтверджено!";
            }
            else
            {
                StatusMessage = "Не вдалося підтвердити електронну пошту.";
            }

            return Page();
        }
    }
}