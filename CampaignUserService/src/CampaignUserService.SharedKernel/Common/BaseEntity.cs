namespace CampaignUserService.SharedKernel.Common;

/// <summary>
/// Base class for all entities in the domain model. Provides a strongly typed
/// identity and auditing metadata shared across the whole system.
/// </summary>
public abstract class BaseEntity
{
    public Guid Id { get; protected set; } = Guid.NewGuid();

    public DateTime CreatedAtUtc { get; protected set; } = DateTime.UtcNow;

    public DateTime? UpdatedAtUtc { get; protected set; }

    public DateTime? DeletedAtUtc { get; protected set; }

    public bool IsDeleted { get; protected set; }

    public void MarkUpdated(DateTime utcNow) => UpdatedAtUtc = utcNow;

    public void MarkDeleted(DateTime utcNow)
    {
        IsDeleted = true;
        DeletedAtUtc = utcNow;
    }

    public override bool Equals(object? obj)
    {
        if (obj is not BaseEntity other)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        if (GetType() != other.GetType())
        {
            return false;
        }

        return Id == other.Id;
    }

    public override int GetHashCode() => HashCode.Combine(GetType(), Id);

    public static bool operator ==(BaseEntity? left, BaseEntity? right) =>
        left is null ? right is null : left.Equals(right);

    public static bool operator !=(BaseEntity? left, BaseEntity? right) => !(left == right);
}
