namespace ClimateExplorer.UnitTests;

using System;
using System.Threading;
using System.Threading.Tasks;
using Blazored.LocalStorage;
using ClimateExplorer.Web.Client.Units;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

[TestClass]
public class UnitPreferenceTests
{
    [TestMethod]
    public async Task LoadAsync_StoredUsCustomary_SwitchesFormatterAndRaisesChanged()
    {
        var preference = new UnitPreference(CreateStorage("UsCustomary").Object);
        var changes = 0;
        preference.Changed += () => changes++;

        await preference.LoadAsync();

        Assert.AreSame(UnitFormatter.UsCustomary, preference.Formatter);
        Assert.AreEqual(1, changes);
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("Imperial")]
    [DataRow("7")]
    public async Task LoadAsync_NothingOrUnknownStored_StaysMetric(string? stored)
    {
        var preference = new UnitPreference(CreateStorage(stored).Object);

        await preference.LoadAsync();

        Assert.AreSame(UnitFormatter.Metric, preference.Formatter);
    }

    [TestMethod]
    public async Task LoadAsync_CalledTwice_ReadsStorageOnce()
    {
        var storage = CreateStorage("UsCustomary");
        var preference = new UnitPreference(storage.Object);

        await preference.LoadAsync();
        await preference.LoadAsync();

        storage.Verify(x => x.GetItemAsStringAsync("unitSystem", It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task LoadAsync_Prerendering_ReadsOnNextCall()
    {
        var storage = new Mock<ILocalStorageService>();
        storage
            .SetupSequence(x => x.GetItemAsStringAsync("unitSystem", It.IsAny<CancellationToken>()))
            .Throws(new InvalidOperationException("JavaScript interop calls cannot be issued at this time."))
            .Returns(new ValueTask<string?>("UsCustomary"));
        var preference = new UnitPreference(storage.Object);

        await preference.LoadAsync();
        Assert.AreSame(UnitFormatter.Metric, preference.Formatter);

        await preference.LoadAsync();
        Assert.AreSame(UnitFormatter.UsCustomary, preference.Formatter);
    }

    [TestMethod]
    public async Task SetAsync_UsCustomary_StoresChoiceAndRaisesChanged()
    {
        var storage = CreateStorage(null);
        var preference = new UnitPreference(storage.Object);
        var changes = 0;
        preference.Changed += () => changes++;

        await preference.SetAsync(UnitSystem.UsCustomary);
        await preference.SetAsync(UnitSystem.UsCustomary);

        Assert.AreSame(UnitFormatter.UsCustomary, preference.Formatter);
        Assert.AreEqual(1, changes, "Choosing the current system again isn't a change.");
        storage.Verify(x => x.SetItemAsStringAsync("unitSystem", "UsCustomary", It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    private static Mock<ILocalStorageService> CreateStorage(string? stored)
    {
        var storage = new Mock<ILocalStorageService>();
        storage
            .Setup(x => x.GetItemAsStringAsync("unitSystem", It.IsAny<CancellationToken>()))
            .Returns(new ValueTask<string?>(stored));
        storage
            .Setup(x => x.SetItemAsStringAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);
        return storage;
    }
}
