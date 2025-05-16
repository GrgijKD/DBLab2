using LibraryDomain.Model;
using LibraryInfrastructure.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Threading.Tasks;
using LibraryInfrastructure.Models;

[Authorize(Roles = "Admin")]
public class RolesController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;

    public RolesController(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
    {
        _userManager = userManager;
        _roleManager = roleManager;
    }

    public async Task<IActionResult> Index()
    {
        var users = _userManager.Users.ToList();

        var userRoles = new List<UserWithRolesViewModel>();

        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            userRoles.Add(new UserWithRolesViewModel
            {
                Id = user.Id,
                Email = user.Email,
                Roles = roles.ToList()
            });
        }

        var usersWithoutEmail = _userManager.Users.Where(u => u.Email == null).ToList();
        foreach (var user in usersWithoutEmail)
        {
            Console.WriteLine($"User {user.Id} має NULL у Email");
        }

        return View(userRoles);
    }

    public async Task<IActionResult> ManageRoles(string userId)
    {
        if (string.IsNullOrEmpty(userId))
        {
            return BadRequest("User ID не може бути пустим");
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return NotFound("Користувач не знайдений");
        }

        var roles = _roleManager.Roles.Select(r => r.Name).ToList();
        var userRoles = await _userManager.GetRolesAsync(user);

        var model = new ManageUserRolesViewModel
        {
            UserId = user.Id,
            UserName = user.UserName,
            UserRoles = userRoles.ToList(),
            AllRoles = roles
        };

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> AddRole(string userId, string role)
    {
        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(role))
        {
            return BadRequest("User ID і роль не можуть бути пустими");
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return NotFound("Користувач не знайдений");
        }

        var result = await _userManager.AddToRoleAsync(user, role);
        if (!result.Succeeded)
        {
            return BadRequest("Не вдалося додати роль");
        }

        return RedirectToAction("ManageRoles", new { userId });
    }

    [HttpPost]
    public async Task<IActionResult> RemoveRole(string userId, string role)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return NotFound();

        if (role == "Admin")
        {
            TempData["Error"] = "Роль Admin не може бути видалена!";
            return RedirectToAction("ManageRoles", new { userId });
        }

        var result = await _userManager.RemoveFromRoleAsync(user, role);
        if (!result.Succeeded)
        {
            TempData["Error"] = "Помилка видалення ролі!";
        }

        return RedirectToAction("ManageRoles", new { userId });
    }
}