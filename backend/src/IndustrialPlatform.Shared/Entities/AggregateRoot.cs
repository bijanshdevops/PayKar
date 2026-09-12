namespace IndustrialPlatform.Shared.Entities;

/// <summary>
/// ریشه تجمیع (Aggregate Root) طبق سند 01-Architecture.md — مرز تراکنشی و قوانین ثبات دامنه.
/// </summary>
public abstract class AggregateRoot<TId> : BaseEntity<TId> where TId : notnull
{
    protected AggregateRoot() { }
    protected AggregateRoot(TId id) : base(id) { }
}
