using Microsoft.AspNetCore.Identity;

namespace RealEstateWebApp.Models;

public class ApplicationUser : IdentityUser
{
    public string? FullName { get; set; }

    public ICollection<Property> Properties { get; set; } = new List<Property>();
    public ICollection<Favorite> Favorites { get; set; } = new List<Favorite>();
}