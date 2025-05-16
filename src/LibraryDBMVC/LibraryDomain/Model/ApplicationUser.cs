using Microsoft.AspNetCore.Identity;

namespace LibraryDomain.Model
{
    public class ApplicationUser : IdentityUser
    {
        public string FullName { get; set; } = string.Empty;

        public override string Email { get; set; } = string.Empty;

        public Worker Worker { get; set; }
    }
}