namespace AutonomusCRM.Application.KnowledgeGraph;

/// <summary>Tipos de nodo del grafo empresarial ABOS (Phase C).</summary>
public static class KnowledgeGraphNodeTypes
{
    public const string Customer = "CustomerNode";
    public const string Company = "CompanyNode";
    public const string Contact = "ContactNode";
    public const string Deal = "DealNode";
    public const string Revenue = "RevenueNode";
    public const string Invoice = "InvoiceNode";
    public const string Payment = "PaymentNode";
    public const string Campaign = "CampaignNode";
    public const string Product = "ProductNode";
    public const string Agent = "AgentNode";
    public const string Decision = "DecisionNode";
    public const string Outcome = "OutcomeNode";
    public const string Memory = "MemoryNode";
    public const string Learning = "LearningNode";

    // Phase D — operational nodes
    public const string TrustDecision = "TrustDecisionNode";
    public const string Approval = "ApprovalNode";
    public const string Rejection = "RejectionNode";
    public const string Rollback = "RollbackNode";
    public const string TrustPolicy = "TrustPolicyNode";
    public const string TrustRisk = "TrustRiskNode";
    public const string Communication = "CommunicationNode";
    public const string VoiceCall = "VoiceCallNode";
    public const string Transcript = "TranscriptNode";
    public const string Sentiment = "SentimentNode";
}

/// <summary>Tipos de relación del grafo empresarial.</summary>
public static class KnowledgeGraphRelations
{
    public const string HasContact = "HAS_CONTACT";
    public const string BelongsToCompany = "BELONGS_TO_COMPANY";
    public const string BoughtProduct = "BOUGHT_PRODUCT";
    public const string HasDeal = "HAS_DEAL";
    public const string GeneratedRevenue = "GENERATED_REVENUE";
    public const string ProducedOutcome = "PRODUCED_OUTCOME";
    public const string ExecutedDecision = "EXECUTED_DECISION";
    public const string InfluencedByAgent = "INFLUENCED_BY_AGENT";
    public const string SupportsDecision = "SUPPORTS_DECISION";
    public const string DerivedFromOutcome = "DERIVED_FROM_OUTCOME";
    public const string LinkedToMemory = "LINKED_TO_MEMORY";
    public const string RanCampaign = "RAN_CAMPAIGN";
    public const string AtRisk = "AT_RISK";
    public const string ExpansionReady = "EXPANSION_READY";
    public const string ContextFor = "CONTEXT_FOR";

    // Phase D — Trust
    public const string RequiredApproval = "REQUIRED_APPROVAL";
    public const string EnabledExecution = "ENABLED_EXECUTION";
    public const string BlockedExecution = "BLOCKED_EXECUTION";
    public const string ReversedOutcome = "REVERSED_OUTCOME";
    public const string TrustPolicyApplies = "TRUST_POLICY_APPLIES";

    // Phase D — Comms
    public const string ReceivedCommunication = "RECEIVED_COMMUNICATION";
    public const string SentCommunication = "SENT_COMMUNICATION";
    public const string InfluencedOutcome = "INFLUENCED_OUTCOME";

    // Phase D — Voice
    public const string GeneratedMemory = "GENERATED_MEMORY";
    public const string InfluencedDecision = "INFLUENCED_DECISION";
    public const string CreatedRiskSignal = "CREATED_RISK_SIGNAL";
    public const string CreatedExpansionSignal = "CREATED_EXPANSION_SIGNAL";
}
