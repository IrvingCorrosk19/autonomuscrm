using AutonomusCRM.Domain.Common;

namespace AutonomusCRM.Application.CustomerSuccess;

public class CustomerContract : Entity
{
    public Guid TenantId { get; private set; }
    public Guid CustomerId { get; private set; }
    public Guid? SourceDealId { get; private set; }
    public DateTime StartDate { get; private set; }
    public DateTime RenewalDate { get; private set; }
    public decimal AnnualValue { get; private set; }
    public string Status { get; private set; }
    public int TermMonths { get; private set; }

    private CustomerContract() : base()
    {
        Status = CustomerSuccessConstants.ContractActive;
        TermMonths = 12;
    }

    public static CustomerContract Create(
        Guid tenantId,
        Guid customerId,
        Guid? sourceDealId,
        DateTime startDate,
        decimal annualValue,
        int termMonths = 12)
    {
        if (annualValue < 0)
            throw new ArgumentException("Validation_Contract_AnnualValue", nameof(annualValue));
        if (termMonths < 1)
            throw new ArgumentException("Validation_Contract_TermMonths", nameof(termMonths));

        var renewal = startDate.AddMonths(termMonths);
        return new CustomerContract
        {
            TenantId = tenantId,
            CustomerId = customerId,
            SourceDealId = sourceDealId,
            StartDate = startDate,
            RenewalDate = renewal,
            AnnualValue = annualValue,
            TermMonths = termMonths,
            Status = CustomerSuccessConstants.ContractActive
        };
    }

    public void MarkPendingRenewal()
    {
        Status = CustomerSuccessConstants.ContractPendingRenewal;
        MarkAsUpdated();
    }

    public void Renew(DateTime newRenewalDate, decimal? newAnnualValue = null)
    {
        RenewalDate = newRenewalDate;
        if (newAnnualValue.HasValue)
            AnnualValue = newAnnualValue.Value;
        Status = CustomerSuccessConstants.ContractActive;
        MarkAsUpdated();
    }

    public void MarkChurned()
    {
        Status = CustomerSuccessConstants.ContractChurned;
        MarkAsUpdated();
    }

    public int DaysUntilRenewal(DateTime asOfUtc)
        => (int)Math.Ceiling((RenewalDate.Date - asOfUtc.Date).TotalDays);
}
