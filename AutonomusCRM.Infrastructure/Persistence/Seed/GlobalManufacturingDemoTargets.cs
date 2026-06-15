namespace AutonomusCRM.Infrastructure.Persistence.Seed;

/// <summary>Target volumes for Global Manufacturing Group demo tenant.</summary>
public static class GlobalManufacturingDemoTargets
{
    public const string TenantName = "Global Manufacturing Group";
    public const string TenantDescription = "Enterprise manufacturing demo — sales, DIP, and executive dashboards";

    public const int Customers = 50_000;
    public const int Leads = 5_000;
    public const int Deals = 2_500;
    public const int Tasks = 1_000;
    public const int ProductEvents = 3_200;
    public const int Companies = 200;

    public const int ErpInvoices = 500_000;
    public const int ErpPayments = 2_000_000;
    public const int ErpContacts = 75_000;
    public const int ErpProducts = 320;
    public const int ErpActivities = 15_000;
    public const int ErpDeals = 2_500;
    public const int ErpTasks = 1_000;

    public const int LiteCustomers = 500;
    public const int LiteLeads = 250;
    public const int LiteDeals = 120;
}
