namespace ClimateExplorer.CarbonCycle.Chemistry;

// Display data for the Level 2 reaction display. The stage 1 numbers don't use mechanisms.
public sealed class Mechanism
{
    public Mechanism(string id, string name, IReadOnlyList<MechanismStep> steps, Reaction net)
    {
        if (steps.Count == 0)
        {
            throw new ArgumentException($"Mechanism {id} needs at least one step.", nameof(steps));
        }

        Id = id;
        Name = name;
        Steps = steps;
        Net = net;
    }

    public string Id { get; }

    public string Name { get; }

    public IReadOnlyList<MechanismStep> Steps { get; }

    public Reaction Net { get; }

    public Reaction SumOfSteps() =>
        Steps.Skip(1).Aggregate(Steps[0].Reaction.Multiply(Steps[0].Multiple), (sum, step) => sum.Add(step.Reaction, step.Multiple));

    public bool StepsSumToNet() => SumOfSteps().HasSameNetStoichiometry(Net);
}
