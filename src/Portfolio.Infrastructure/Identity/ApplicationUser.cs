using Microsoft.AspNetCore.Identity;

namespace Portfolio.Infrastructure.Identity;

public class ApplicationUser : IdentityUser<Guid>
{
    public DateTimeOffset CreatedAt { get; set; }
}
