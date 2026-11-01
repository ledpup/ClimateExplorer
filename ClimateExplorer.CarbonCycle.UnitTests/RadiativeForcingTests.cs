using ClimateExplorer.CarbonCycle.Simulation;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ClimateExplorer.CarbonCycle.UnitTests;

[TestClass]
public class RadiativeForcingTests
{
    [TestMethod]
    public void CarbonDioxide_Doubled_AddsAr6ForcingPerDoubling()
    {
        Assert.AreEqual(3.93, RadiativeForcing.CarbonDioxide(840e-6, 420e-6), 1e-12);
        Assert.AreEqual(2 * 3.93, RadiativeForcing.CarbonDioxide(1680e-6, 420e-6), 1e-12);
    }

    [TestMethod]
    public void CarbonDioxide_AtReference_Zero()
    {
        Assert.AreEqual(0, RadiativeForcing.CarbonDioxide(420e-6, 420e-6));
    }

    [TestMethod]
    public void CarbonDioxide_BelowPreindustrial_TakenAsPreindustrial()
    {
        Assert.AreEqual(0, RadiativeForcing.CarbonDioxide(100e-6, 280e-6));
        Assert.AreEqual(RadiativeForcing.CarbonDioxide(280e-6, 420e-6), RadiativeForcing.CarbonDioxide(0, 420e-6), 1e-12);
    }

    [TestMethod]
    public void Solar_ConstantSunlight_Zero()
    {
        Assert.AreEqual(0, RadiativeForcing.Solar(1361, 1361, 0.3));
    }

    [TestMethod]
    public void Solar_BrighterSun_QuarterOfTheChangeLessWhatsReflected()
    {
        Assert.AreEqual(10.0 / 4 * 0.7, RadiativeForcing.Solar(1371, 1361, 0.3), 1e-12);
    }

    [TestMethod]
    public void Albedo_AtReference_Zero()
    {
        Assert.AreEqual(0, RadiativeForcing.Albedo(1361, 0.3, 0.3));
    }

    [TestMethod]
    public void Albedo_Brighter_Cools()
    {
        Assert.IsLessThan(0, RadiativeForcing.Albedo(1361, 0.31, 0.3));
    }

    [TestMethod]
    public void SolarPlusAlbedo_BothChanged_EqualsChangeInAbsorbedSunlight()
    {
        double s0 = 1361, alpha0 = 0.30, s = 1380, alpha = 0.25;

        var forcing = RadiativeForcing.Solar(s, s0, alpha) + RadiativeForcing.Albedo(s0, alpha, alpha0);

        Assert.AreEqual((s / 4 * (1 - alpha)) - (s0 / 4 * (1 - alpha0)), forcing, 1e-12);
    }
}
