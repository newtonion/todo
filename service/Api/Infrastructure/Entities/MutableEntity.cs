using System;

namespace Api.Infrastructure.Entities;

public class MutableEntity: Entity
{
    public DateTime CreatedOn { get; set; }
    public DateTime UpdatedOn { get; set; }
}
