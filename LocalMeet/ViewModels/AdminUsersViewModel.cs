using LocalMeet.Models;

namespace LocalMeet.ViewModels
{
    public class AdminUsersViewModel
    {
        public List<ApplicationUser> Users { get; set; }
            = new List<ApplicationUser>();

        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
    }
}