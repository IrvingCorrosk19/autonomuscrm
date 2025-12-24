using AutonomusCRM.Domain.Events;

namespace AutonomusCRM.Domain.Deals.Events;

public class DealStageChangedEvent : DomainEventBase
{
    public override string EventType => "Deal.StageChanged";
    public Guid DealId { get; }
    public DealStage OldStage { get; }
    public DealStage NewStage { get; }
    public int Probability { get; }

    public DealStageChangedEvent(Guid dealId, Guid tenantId, DealStage oldStage, DealStage newStage, int probability) : base(tenantId)
    {
        DealId = dealId;
        OldStage = oldStage;
        NewStage = newStage;
        Probability = probability;
    }
}

