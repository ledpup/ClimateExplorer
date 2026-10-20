namespace ClimateExplorer.UnitTests;

using System.Threading.Tasks;
using ClimateExplorer.WebApi;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public sealed class MetadataEndpointsTests
{
    [TestMethod]
    public async Task GetDataSetDefinitions_CalledTwice_ReturnsCachedInstance()
    {
        var first = await MetadataEndpoints.GetDataSetDefinitions();
        var second = await MetadataEndpoints.GetDataSetDefinitions();

        Assert.IsNotEmpty(first);
        Assert.AreSame(first, second);
    }
}
