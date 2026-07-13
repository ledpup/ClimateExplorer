namespace ClimateExplorer.Core.Interface;

public interface ICachedData
{
    public DateTimeOffset? RetrievedDate { get; set; }
}