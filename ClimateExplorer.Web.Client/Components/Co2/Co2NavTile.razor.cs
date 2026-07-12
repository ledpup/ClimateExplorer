namespace ClimateExplorer.Web.Client.Components.Co2;

using ClimateExplorer.Core.Model;
using ClimateExplorer.WebApiClient.Services;
using Microsoft.AspNetCore.Components;
using static ClimateExplorer.Core.Enums;

public partial class Co2NavTile : IDisposable
{
    private const int LoadDelayMs = 2000;
    private static readonly Guid AtmosphereLocationId = Region.RegionId(Region.Atmosphere);

    private CancellationTokenSource? cancellationTokenSource;
    private double? Value { get; set; }
    private bool IsVisible { get; set; }

    [Inject]
    private IDataService DataService { get; set; } = default!;

    public void Dispose()
    {
        cancellationTokenSource?.Cancel();
        cancellationTokenSource?.Dispose();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
        {
            return;
        }

        cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;

        try
        {
            await Task.Delay(LoadDelayMs, cancellationToken);

            var response = await DataService.GetClimateRecords(
                AtmosphereLocationId,
                DataType.CO2Deseasoned,
                ascending: false,
                take: 1,
                monthly: true);

            Value = response.Records.FirstOrDefault()?.Value;
        }
        catch (OperationCanceledException)
        {
            return;
        }

        if (Value.HasValue)
        {
            IsVisible = true;
            await InvokeAsync(StateHasChanged);
        }
    }
}
