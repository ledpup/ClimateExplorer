namespace ClimateExplorer.CarbonCycle.Chemistry;

using System.Globalization;

public sealed class ChemicalFormula
{
    private ChemicalFormula(string text, IReadOnlyList<FormulaAtom> atoms)
    {
        Text = text;
        Atoms = atoms;
        MolarMass = atoms.Sum(atom => atom.Count * atom.Element.StandardAtomicWeight);
    }

    // As authored, e.g. "CaCO3". The order is kept for display, since chemists write CaCO₃ and not CCaO₃.
    public string Text { get; }

    // Each element once, in order of first appearance, with its total count ("C2H5OH" → C 2, H 6, O 1).
    public IReadOnlyList<FormulaAtom> Atoms { get; }

    // g/mol: Σ count × atomic weight
    public double MolarMass { get; }

    // Element symbols each followed by an optional whole-number count. No parentheses are needed for the catalog.
    public static ChemicalFormula Parse(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new ArgumentException("A formula can't be empty.", nameof(text));
        }

        var atoms = new List<FormulaAtom>();
        var index = 0;

        while (index < text.Length)
        {
            if (!char.IsAsciiLetterUpper(text[index]))
            {
                throw new ArgumentException($"Unexpected '{text[index]}' in formula '{text}'.", nameof(text));
            }

            var symbolStart = index;
            index++;

            while (index < text.Length && char.IsAsciiLetterLower(text[index]))
            {
                index++;
            }

            var symbol = text[symbolStart..index];
            var digitsStart = index;

            while (index < text.Length && char.IsAsciiDigit(text[index]))
            {
                index++;
            }

            var count = index > digitsStart
                ? int.Parse(text[digitsStart..index], CultureInfo.InvariantCulture)
                : 1;

            if (count == 0)
            {
                throw new ArgumentException($"A count of zero in formula '{text}'.", nameof(text));
            }

            Add(atoms, Elements.FromSymbol(symbol), count);
        }

        return new ChemicalFormula(text, atoms);
    }

    // For generated formulas such as a real fuel's per-carbon formula, CH₁.₉₆S₀.₀₀₀₀₀₄₃₆. Zero counts are dropped.
    public static ChemicalFormula FromAtoms(IEnumerable<FormulaAtom> atoms)
    {
        var list = new List<FormulaAtom>();

        foreach (var atom in atoms)
        {
            if (!double.IsFinite(atom.Count) || atom.Count < 0)
            {
                throw new ArgumentException($"Invalid count {atom.Count} for {atom.Element.Symbol}.", nameof(atoms));
            }

            if (atom.Count > 0)
            {
                Add(list, atom.Element, atom.Count);
            }
        }

        if (list.Count == 0)
        {
            throw new ArgumentException("A formula needs at least one atom.", nameof(atoms));
        }

        var text = string.Concat(list.Select(atom => atom.Count == 1 ? atom.Element.Symbol : atom.Element.Symbol + ChemicalText.FormatNumber(atom.Count)));

        return new ChemicalFormula(text, list);
    }

    public double CountOf(Element element) => Atoms.FirstOrDefault(atom => atom.Element == element)?.Count ?? 0;

    public string ToDisplayString() => ChemicalText.Subscript(Text);

    public override string ToString() => Text;

    private static void Add(List<FormulaAtom> atoms, Element element, double count)
    {
        var index = atoms.FindIndex(atom => atom.Element == element);

        if (index < 0)
        {
            atoms.Add(new FormulaAtom(element, count));
        }
        else
        {
            atoms[index] = atoms[index] with { Count = atoms[index].Count + count };
        }
    }
}
