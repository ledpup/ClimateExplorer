namespace ClimateExplorer.CarbonCycle.Chemistry;

public sealed class Reaction
{
    // The largest multiplier tried when looking for whole-number display coefficients.
    private const int MaxDisplayMultiplier = 100;

    public Reaction(string id, string name, IReadOnlyList<ReactionTerm> reactants, IReadOnlyList<ReactionTerm> products, string? conditions = null)
    {
        if (reactants.Count == 0 || products.Count == 0)
        {
            throw new ArgumentException($"Reaction {id} needs at least one reactant and one product.");
        }

        if (reactants.Concat(products).Any(term => !double.IsFinite(term.Coefficient) || term.Coefficient <= 0))
        {
            throw new ArgumentException($"Reaction {id} has a coefficient that is not a positive number.");
        }

        Id = id;
        Name = name;
        Reactants = reactants;
        Products = products;
        Conditions = conditions;
        EnthalpyChange = DeriveEnthalpyChange(reactants, products);
    }

    public string Id { get; }

    public string Name { get; }

    public IReadOnlyList<ReactionTerm> Reactants { get; }

    public IReadOnlyList<ReactionTerm> Products { get; }

    // Display text such as "heat", "sunlight", "+M", "in cloud", "flame temperature"
    public string? Conditions { get; }

    // kJ per mole of reaction as written; negative when heat is released (Hess's law: products minus reactants).
    // Absent if any species lacks an enthalpy of formation.
    public double? EnthalpyChange { get; }

    public IEnumerable<ReactionTerm> Terms => Reactants.Concat(Products);

    // g per mole of reaction as written
    public double ReactantMass => Reactants.Sum(term => term.Coefficient * term.Species.MolarMass);

    public double ProductMass => Products.Sum(term => term.Coefficient * term.Species.MolarMass);

    public bool IsBalanced()
    {
        var elements = Terms.SelectMany(term => term.Species.Formula.Atoms.Select(atom => atom.Element)).Distinct();

        return elements.All(element => Tolerances.AreClose(AtomCount(Reactants, element), AtomCount(Products, element)));
    }

    public bool Involves(Species species) => Terms.Any(term => term.Species == species);

    // The coefficient on whichever side the species appears; reactants first if it is on both.
    public double CoefficientOf(Species species) =>
        Terms.FirstOrDefault(term => term.Species == species)?.Coefficient
        ?? throw new ArgumentException($"{species.Id} is not in reaction {Id}.", nameof(species));

    public ScaledReaction ScaleByExtent(double extent)
    {
        if (!double.IsFinite(extent) || extent < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(extent), extent, "The extent of a reaction must be zero or more.");
        }

        return new ScaledReaction(this, extent);
    }

    public ScaledReaction ScaleByMoles(Species species, double moles) => ScaleByExtent(moles / CoefficientOf(species));

    public ScaledReaction ScaleByMass(Species species, double massKg) => ScaleByMoles(species, (massKg * 1000) / species.MolarMass);

    // enthalpyChangeJoules uses the reaction's sign convention: negative for heat released.
    public ScaledReaction ScaleByHeat(double enthalpyChangeJoules)
    {
        if (EnthalpyChange is not double perMole || perMole == 0)
        {
            throw new InvalidOperationException($"Reaction {Id} has no enthalpy change to scale by.");
        }

        var extent = enthalpyChangeJoules / (perMole * 1000);

        if (extent < 0)
        {
            throw new ArgumentException($"The heat has the opposite sign to reaction {Id}'s enthalpy change.", nameof(enthalpyChangeJoules));
        }

        return ScaleByExtent(extent);
    }

    public Reaction Multiply(double factor)
    {
        if (!double.IsFinite(factor) || factor <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(factor), factor, "The factor must be a positive number.");
        }

        return new Reaction(Id, Name, Multiply(Reactants, factor), Multiply(Products, factor), Conditions);
    }

    // Rescale so the species has coefficient 1 ("per mole of fuel").
    public Reaction Normalise(Species species) => Multiply(1 / CoefficientOf(species));

    // This reaction plus multiple × other, cancelling species that appear on both sides.
    public Reaction Add(Reaction other, double multiple = 1)
    {
        var net = new NetStoichiometry();
        net.Add(this, 1);
        net.Add(other, multiple);

        return net.ToReaction($"{Id}+{other.Id}", $"{Name} + {other.Name}");
    }

    // True if both reactions turn the same amounts of the same species into each other, after cancelling.
    public bool HasSameNetStoichiometry(Reaction other)
    {
        var mine = new NetStoichiometry();
        mine.Add(this, 1);

        var theirs = new NetStoichiometry();
        theirs.Add(other, 1);

        return mine.Species.Union(theirs.Species).All(species => Tolerances.AreClose(mine.Of(species), theirs.Of(species)));
    }

    // The smallest whole-number form where one exists ("2 C₈H₁₈ + 25 O₂ → 16 CO₂ + 18 H₂O"), otherwise rounded decimals.
    public string ToEquationString(bool includePhases = false)
    {
        var multiplier = WholeNumberMultiplier();

        string Format(ReactionTerm term)
        {
            var coefficient = term.Coefficient * multiplier;
            var species = term.Species.ToDisplayString(includePhases);

            return Tolerances.AreClose(coefficient, 1) ? species : $"{ChemicalText.FormatNumber(coefficient)} {species}";
        }

        return string.Join(" + ", Reactants.Select(Format)) + " → " + string.Join(" + ", Products.Select(Format));
    }

    public override string ToString() => ToEquationString();

    private static double AtomCount(IEnumerable<ReactionTerm> terms, Element element) =>
        terms.Sum(term => term.Coefficient * term.Species.Formula.CountOf(element));

    private static List<ReactionTerm> Multiply(IEnumerable<ReactionTerm> terms, double factor) =>
        terms.Select(term => term with { Coefficient = term.Coefficient * factor }).ToList();

    private static double? DeriveEnthalpyChange(IReadOnlyList<ReactionTerm> reactants, IReadOnlyList<ReactionTerm> products)
    {
        if (reactants.Concat(products).Any(term => term.Species.EnthalpyOfFormation is null))
        {
            return null;
        }

        return products.Sum(term => term.Coefficient * term.Species.EnthalpyOfFormation!.Value)
            - reactants.Sum(term => term.Coefficient * term.Species.EnthalpyOfFormation!.Value);
    }

    private static bool IsWhole(double value) =>
        Math.Abs(value - Math.Round(value)) <= Tolerances.Relative * Math.Max(1.0, Math.Abs(value));

    private static long GreatestCommonDivisor(long a, long b) => b == 0 ? a : GreatestCommonDivisor(b, a % b);

    // The factor that turns every coefficient into the smallest whole numbers, or 1 if there is none.
    private double WholeNumberMultiplier()
    {
        var coefficients = Terms.Select(term => term.Coefficient).ToList();

        for (var multiplier = 1; multiplier <= MaxDisplayMultiplier; multiplier++)
        {
            var scaled = coefficients.Select(coefficient => coefficient * multiplier).ToList();

            if (scaled.All(IsWhole))
            {
                var divisor = scaled.Select(value => (long)Math.Round(value)).Aggregate(GreatestCommonDivisor);

                return multiplier / (double)divisor;
            }
        }

        return 1;
    }

    // Net moles of each species per mole of reaction: products positive, reactants negative.
    private sealed class NetStoichiometry
    {
        private readonly List<Species> order = [];
        private readonly Dictionary<Species, double> net = [];

        public IEnumerable<Species> Species => order;

        public double Of(Species species) => net.GetValueOrDefault(species);

        public void Add(Reaction reaction, double multiple)
        {
            foreach (var term in reaction.Reactants)
            {
                Accumulate(term.Species, -term.Coefficient * multiple);
            }

            foreach (var term in reaction.Products)
            {
                Accumulate(term.Species, term.Coefficient * multiple);
            }
        }

        public Reaction ToReaction(string id, string name)
        {
            var reactants = order.Where(species => net[species] < -Tolerances.Relative).Select(species => new ReactionTerm(-net[species], species)).ToList();
            var products = order.Where(species => net[species] > Tolerances.Relative).Select(species => new ReactionTerm(net[species], species)).ToList();

            return new Reaction(id, name, reactants, products);
        }

        private void Accumulate(Species species, double moles)
        {
            if (!net.ContainsKey(species))
            {
                order.Add(species);
            }

            net[species] = net.GetValueOrDefault(species) + moles;
        }
    }
}
