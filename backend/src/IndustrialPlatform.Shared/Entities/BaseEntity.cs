using IndustrialPlatform.Shared.Abstractions;

namespace IndustrialPlatform.Shared.Entities;

public abstract class BaseEntity<TId> : IAuditableEntity, ISoftDeletable where TId : notnull
{
    public TId Id { get; protected set; } = default!;

    public DateTime CreatedAtUtc { get; private set; }
    public Guid? CreatedBy { get; private set; }
    public DateTime? LastModifiedAtUtc { get; private set; }
    public Guid? LastModifiedBy { get; private set; }

    public bool IsDeleted { get; private set; }
    public DateTime? DeletedAtUtc { get; private set; }
    public Guid? DeletedBy { get; private set; }

    // یادداشت: توکن همروندی خوش‌بینانه (Optimistic Concurrency) به‌صورت shadow property
    // از طریق UseXminAsConcurrencyToken() در PersistenceHelpers.ConfigureAuditColumns تنظیم می‌شود
    // و نیازی به تعریف صریح یک پراپرتی CLR در اینجا نیست (سند 03-Database-Standards.md).

    private readonly List<IDomainEvent> _domainEvents = new();
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected BaseEntity() { }

    protected BaseEntity(TId id) => Id = id;

    protected void AddDomainEvent(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);

    public void ClearDomainEvents() => _domainEvents.Clear();

    public void SetCreated(Guid? userId, DateTime utcNow)
    {
        CreatedAtUtc = utcNow;
        CreatedBy = userId;
    }

    public void SetModified(Guid? userId, DateTime utcNow)
    {
        LastModifiedAtUtc = utcNow;
        LastModifiedBy = userId;
    }

    public void MarkAsDeleted(Guid? deletedBy, DateTime utcNow)
    {
        IsDeleted = true;
        DeletedAtUtc = utcNow;
        DeletedBy = deletedBy;
    }

    public override bool Equals(object? obj)
    {
        if (obj is not BaseEntity<TId> other) return false;
        if (ReferenceEquals(this, other)) return true;
        return Id.Equals(other.Id);
    }

    public override int GetHashCode() => Id.GetHashCode();
}
