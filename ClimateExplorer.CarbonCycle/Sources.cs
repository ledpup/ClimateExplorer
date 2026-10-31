namespace ClimateExplorer.CarbonCycle;

// Where each constant and default comes from. Tab 1 step 2 replaces the provisional ones with citations.
public static class Sources
{
    public const string AtomicWeights = "IUPAC abridged standard atomic weights";

    public const string ProvisionalEnthalpy = "Standard enthalpy of formation at 25 °C; provisional, to verify against NIST-JANAF / CODATA (Tab 1 step 2)";

    public const string EnthalpyToSource = "Enthalpy of formation not yet sourced (Tab 1 step 2)";

    public const string DisplayOnly = "Display only; no enthalpy of formation needed";

    public const string ProvisionalFuel = "Provisional default from the Tab 1 plan §5; to verify against IPCC 2006 Vol 2 Ch 1 (Tab 1 step 2)";

    public const string ProvisionalCalcination = "Provisional default from the Tab 1 plan §2.1 (IPCC Tier 1); to verify (Tab 1 step 2)";

    public const string Generated = "Generated from the fuel's composition";
}
