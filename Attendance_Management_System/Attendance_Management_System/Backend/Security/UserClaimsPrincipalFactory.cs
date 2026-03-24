using System.Security.Claims;
using Attendance_Management_System.Backend.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace Attendance_Management_System.Backend.Security;

// Adds the custom User.Role value as a ClaimTypes.Role claim for cookie auth
public class UserClaimsPrincipalFactory : UserClaimsPrincipalFactory<User, IdentityRole<int>>
{
    public UserClaimsPrincipalFactory(
        UserManager<User> userManager,
        RoleManager<IdentityRole<int>> roleManager,
        IOptions<IdentityOptions> optionsAccessor)
        : base(userManager, roleManager, optionsAccessor)
    {
    }

    // Generates claims identity including the custom role claim
    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(User user)
    {
        // Get base claims from Identity framework
        var identity = await base.GenerateClaimsAsync(user);

        // Add role claim if not already present
        if (!string.IsNullOrWhiteSpace(user.Role)
            && !identity.HasClaim(c => c.Type == ClaimTypes.Role && c.Value == user.Role))
        {
            identity.AddClaim(new Claim(ClaimTypes.Role, user.Role));
        }

        return identity;
    }
}
