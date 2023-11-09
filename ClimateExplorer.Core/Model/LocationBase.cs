namespace ClimateExplorer.Core.Model;

public class LocationBase
{
    public required Guid Id { get; set; }
    public required string Name { get; set; }

    public LocationBase() { }

    public override string ToString()
    {
        return Name;
    }
}
