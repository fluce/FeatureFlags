using System;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace TestWebApp.Models
{
    // Add profile data for application users by adding properties to the ApplicationUser class
    public class ApplicationUser : IdentityUser
    {
        public Guid UID { get; set; }
    }
}
