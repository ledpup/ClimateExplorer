namespace ClimateExplorer.Web.Client.Components.Co2;

using System.Globalization;
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
    private short? MeasurementYear { get; set; }
    private short? MeasurementMonth { get; set; }
    private bool IsVisible { get; set; }

    private string? TooltipText =>
        Value.HasValue && MeasurementMonth.HasValue
            ? $"<p>Atmospheric CO₂ (deseasonalised)</p><p>{Value.Value.ToString("F2")} parts per million</p><p>Recorded {CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(MeasurementMonth.Value)} {MeasurementYear}</p><p>From Mauna Loa Observatory</p>"
            : null;

    [Inject]
    private IDataService DataService { get; set; } = default!;

    public void Dispose()
    {
        cancellationTokenSource?.Cancel();
        cancellationTokenSource?.Dispose();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (Value is not null)
        {
            return;
        }

        cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;

        ClimateRecordsResponse? response;

        try
        {
            response = await GetCo2(true);

            if (response is null)
            {
                await Task.Delay(LoadDelayMs, cancellationToken);
                response = await GetCo2(false);
            }
        }
        catch (OperationCanceledException)
        {
            return;
        }

        var record = response?.Records.FirstOrDefault();
        Value = record?.Value;
        MeasurementYear = record?.Year;
        MeasurementMonth = record?.Month;

        if (Value.HasValue)
        {
            IsVisible = true;
            await InvokeAsync(StateHasChanged);
        }
    }

    private async Task<ClimateRecordsResponse?> GetCo2(bool fromCacheOnly)
    {
        return await DataService.GetClimateRecords(
            AtmosphereLocationId,
            DataType.CO2Deseasoned,
            ascending: false,
            take: 1,
            monthly: true,
            fromCacheOnly: fromCacheOnly);
    }
}
