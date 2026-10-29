namespace ClimateExplorer.Web.UnitTests;

using System;
using System.Globalization;
using System.Threading;
using ClimateExplorer.Web.Client.Components.Common;
using ClimateExplorer.Web.UiModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class SelectValueMapTests
{
    [TestMethod]
    public void KeyFor_EnumValue_ReturnsEnumName()
    {
        var key = SelectValueMap<SeriesDisplayStyle>.KeyFor(SeriesDisplayStyle.Scatter);

        Assert.AreEqual("Scatter", key);
    }

    [TestMethod]
    public void KeyFor_ShortValue_UsesInvariantCulture()
    {
        var originalCulture = Thread.CurrentThread.CurrentCulture;
        try
        {
            // fa-IR formats digits and the minus sign differently from the invariant culture
            Thread.CurrentThread.CurrentCulture = new CultureInfo("fa-IR");

            var key = SelectValueMap<short>.KeyFor(-182);

            Assert.AreEqual("-182", key);
        }
        finally
        {
            Thread.CurrentThread.CurrentCulture = originalCulture;
        }
    }

    [TestMethod]
    public void KeyFor_NullString_ReturnsEmpty()
    {
        var key = SelectValueMap<string?>.KeyFor(null);

        Assert.AreEqual(string.Empty, key);
    }

    [TestMethod]
    public void TryGetValue_RegisteredGuidKey_ReturnsOriginalGuid()
    {
        var map = new SelectValueMap<Guid>();
        var id = Guid.NewGuid();
        var key = map.Register(id);

        var found = map.TryGetValue(key, out var value);

        Assert.IsTrue(found);
        Assert.AreEqual(id, value);
    }

    [TestMethod]
    public void TryGetValue_UnknownKey_ReturnsFalse()
    {
        var map = new SelectValueMap<int>();
        map.Register(10);

        var found = map.TryGetValue("20", out _);

        Assert.IsFalse(found);
    }

    [TestMethod]
    public void TryGetValue_NullKey_ReturnsFalse()
    {
        var map = new SelectValueMap<int>();
        map.Register(10);

        var found = map.TryGetValue(null, out _);

        Assert.IsFalse(found);
    }

    [TestMethod]
    public void Unregister_RegisteredValue_RemovesKey()
    {
        var map = new SelectValueMap<SeriesDisplayStyle>();
        var key = map.Register(SeriesDisplayStyle.Bar);

        map.Unregister(SeriesDisplayStyle.Bar);

        Assert.IsFalse(map.TryGetValue(key, out _));
    }

    [TestMethod]
    public void Unregister_ValueRegisteredTwice_KeepsKeyUntilLastUnregister()
    {
        // Blazor registers a replacement item before it disposes the item being replaced
        var map = new SelectValueMap<int>();
        var key = map.Register(7);
        map.Register(7);

        map.Unregister(7);
        var foundAfterFirst = map.TryGetValue(key, out var value);
        map.Unregister(7);
        var foundAfterSecond = map.TryGetValue(key, out _);

        Assert.IsTrue(foundAfterFirst);
        Assert.AreEqual(7, value);
        Assert.IsFalse(foundAfterSecond);
    }
}
