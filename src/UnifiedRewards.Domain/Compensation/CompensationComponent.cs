using UnifiedRewards.Domain.Common;
using UnifiedRewards.Domain.Enums;

namespace UnifiedRewards.Domain.Compensation;

/// <summary>
/// A single line of a compensation structure (e.g. Basic, HRA, Provident Fund),
/// produced by the rules engine. Child of <see cref="CompensationStructure"/>.
/// </summary>
public class CompensationComponent : BaseEntity
{
    public Guid CompensationStructureId { get; set; }

    public string Name { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    public ComponentType Type { get; set; }
}
