namespace ClimateExplorer.Web.Client.CarbonSim.Common;

using Microsoft.AspNetCore.Components;

/// <summary>
/// Shows one of <see cref="Items"/> at a time, in order from left to right, wrapping at the ends. Each arrow brings in
/// the item on its side. On fullscreen the active item fills the carousel's width, and the arrows and the faded
/// neighbouring items sit outside it, out to the edges of the browser. On tablet and mobile only the active item
/// shows, with the arrows inside it, like an image carousel.
/// </summary>
public partial class Carousel<TItem>
{
    // Slides either side of the active one: its neighbours, and one more each side for a move to slide in from
    private const int Radius = 2;

    private int index;
    private int? receivedIndex;

    // Each move renders a new track, so its slide-in animation starts afresh
    private int move;
    private bool? fromLeft;
    private int? leavingOffset;
    private string? announcement;

    [Parameter]
    [EditorRequired]
    public IReadOnlyList<TItem> Items { get; set; } = [];

    [Parameter]
    public int Index { get; set; }

    [Parameter]
    public EventCallback<int> IndexChanged { get; set; }

    [Parameter]
    [EditorRequired]
    public string AriaLabel { get; set; } = string.Empty;

    /// <summary>Names an item in the arrows' and dots' accessible names, and in the announcement of a move.</summary>
    [Parameter]
    [EditorRequired]
    public Func<TItem, string> ItemLabel { get; set; } = _ => string.Empty;

    [Parameter]
    public RenderFragment<CarouselSlide<TItem>>? ItemTemplate { get; set; }

    private int SlidesBefore => Items.Count > 1 ? Radius : 0;

    private IEnumerable<int> Offsets => Enumerable.Range(-SlidesBefore, (2 * SlidesBefore) + 1);

    private string TrackClass => fromLeft switch
    {
        true => "carousel-track carousel-track-from-left",
        false => "carousel-track carousel-track-from-right",
        null => "carousel-track",
    };

    // An index from outside replaces the active item without a slide. One the carousel raised comes back unchanged.
    protected override void OnParametersSet()
    {
        if (Index == receivedIndex)
        {
            return;
        }

        receivedIndex = Index;
        index = Wrap(Index);
        fromLeft = null;
        leavingOffset = null;
    }

    private string SlideClass(int offset) =>
        offset == 0 ? "carousel-slide carousel-slide-active"
        : offset == leavingOffset ? "carousel-slide carousel-slide-leaving"
        : "carousel-slide";

    private TItem ItemAt(int offset) => Items[Wrap(index + offset)];

    private string PositionText(int offset) => $"{Wrap(index + offset) + 1} of {Items.Count}";

    private string ArrowLabel(int offset) => $"Show {ItemLabel(ItemAt(offset))}";

    private int Wrap(int i) => Items.Count == 0 ? 0 : ((i % Items.Count) + Items.Count) % Items.Count;

    private Task ShowLeftNeighbourAsync() => MoveAsync(Wrap(index - 1), fromLeft: true);

    private Task ShowRightNeighbourAsync() => MoveAsync(Wrap(index + 1), fromLeft: false);

    // A dot slides its item in from the nearer side, going round the end if that's shorter
    private Task ShowAsync(int target) =>
        MoveAsync(target, fromLeft: Wrap(target - index) > Items.Count / 2);

    private async Task MoveAsync(int target, bool fromLeft)
    {
        if (target == index)
        {
            return;
        }

        // The item that was active is now beside the new one, unless a dot jumped further
        var side = fromLeft ? 1 : -1;
        leavingOffset = Wrap(target + side) == index ? side : null;

        this.fromLeft = fromLeft;
        index = target;
        move++;
        announcement = $"{ItemLabel(Items[target])}, {target + 1} of {Items.Count}";

        await IndexChanged.InvokeAsync(target);
    }
}
