using Microsoft.EntityFrameworkCore;

namespace Api.Infrastructure.Entities;

// Lazy-create these when we get a Clerk auth token
// We could use the webhooks, but that's a bit more work
// Normally, this is where the user profile info would go
[Index(nameof(AuthId), IsUnique = true)]
public class UserEntity : Entity
{
    public required string AuthId { get; set; } // Clerk's user Id
    public required string Name { get; set; }

    // Navigation properties
    public virtual List<ListEntity> Lists { get; set; } = new();

}
