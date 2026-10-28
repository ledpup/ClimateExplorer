namespace ClimateExplorer.WebApi.UnitTests;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ClimateExplorer.Data.Ghcnm;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public sealed class FipsToIsoMapperTests
{
    [TestMethod]
    public void TryMap_PlainFipsCode_ReturnsIsoCode()
    {
        var mapper = CreateMapper();

        var mapped = mapper.TryMap("ASN00070351", out var isoCode);

        Assert.IsTrue(mapped);
        Assert.AreEqual("AU", isoCode);
    }

    [TestMethod]
    public void TryMap_StationWithOverride_ReturnsOverrideBeforePrefix()
    {
        var mapper = CreateMapper();

        Assert.IsTrue(mapper.TryMap("NTM00078866", out var princessJuliana));
        Assert.IsTrue(mapper.TryMap("NTM00078988", out var hato));
        Assert.AreEqual("SX", princessJuliana);
        Assert.AreEqual("CW", hato);
    }

    [TestMethod]
    public void TryMap_UnknownFipsCode_ReturnsFalse()
    {
        var mapper = CreateMapper();

        Assert.IsFalse(mapper.TryMap("QQ000000001", out _));
    }

    [TestMethod]
    public void TryMap_UnmappableCodeWithoutOverride_ReturnsFalse()
    {
        var mapper = CreateMapper();

        Assert.IsFalse(mapper.TryMap("NTM00000001", out _));
    }

    [TestMethod]
    public void IsoCodes_MapperWithUnmappableCodeAndOverrides_ReturnsDistinctIsoCodesOnly()
    {
        var mapper = CreateMapper();

        CollectionAssert.AreEquivalent(new[] { "AU", "AS", "CH", "SX", "CW" }, mapper.IsoCodes.ToList());
    }

    [TestMethod]
    public async Task ReadCodes_DuplicateKey_Throws()
    {
        var path = Path.GetTempFileName();
        try
        {
            await File.WriteAllLinesAsync(path, ["# comment", "AS AU Australia", "AS AU Australia"]);

            await Assert.ThrowsExactlyAsync<FormatException>(() => FipsToIsoMapper.ReadCodes(path));
        }
        finally
        {
            File.Delete(path);
        }
    }

    private static FipsToIsoMapper CreateMapper()
    {
        return new FipsToIsoMapper(
            new Dictionary<string, string>
            {
                ["AS"] = "AU",
                ["AQ"] = "AS",
                ["SZ"] = "CH",
                ["NT"] = "--",
            },
            new Dictionary<string, string>
            {
                ["NTM00078866"] = "SX",
                ["NTM00078988"] = "CW",
            });
    }
}
