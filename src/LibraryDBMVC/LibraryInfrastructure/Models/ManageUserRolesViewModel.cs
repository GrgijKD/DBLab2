namespace LibraryInfrastructure.Models
{
    public class ManageUserRolesViewModel
    {
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public List<string> UserRoles { get; set; } = new();
        public List<string> AllRoles { get; set; } = new();
    }
}