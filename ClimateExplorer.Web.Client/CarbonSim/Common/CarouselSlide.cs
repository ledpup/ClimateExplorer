namespace ClimateExplorer.Web.Client.CarbonSim.Common;

/// <summary>An item as a <see cref="Carousel{TItem}"/> slide: the active one, or one beside it.</summary>
public readonly record struct CarouselSlide<TItem>(TItem Item, bool IsActive);
