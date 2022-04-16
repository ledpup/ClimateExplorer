namespace AcornSat.Visualiser
{
    public class ColourServer
    {
        int colourIndex;
        public string GetNextColour(int preferredColour = 0)
        {
            if (colourIndex < preferredColour)
            {
                colourIndex = preferredColour;
            }
            if (colourIndex > set10.Length)
            {
                throw new Exception("We're all out of colours.");
            }
            var nextColour = set10[colourIndex];
            colourIndex++;
            return nextColour;
        }

        string[] category10 = new [] { "#4e79a7", "#f28e2c", "#e15759", "#76b7b2", "#59a14f", "#edc949", "#af7aa1", "#ff9da7", "#9c755f", "#bab0ab" };
        string[] tableau10 = new [] { "#4e79a7", "#f28e2c", "#e15759", "#76b7b2", "#59a14f", "#edc949", "#af7aa1", "#ff9da7", "#9c755f", "#bab0ab" };
        string[] set10 = new [] { "#e41a1c", "#377eb8", "#4daf4a", "#984ea3", "#ff7f00", "#ffff33", "#a65628", "#f781bf", "#000000" };
        string[] paired = new[] { "#a6cee3", "#1f78b4", "#b2df8a", "#33a02c", "#fb9a99", "#e31a1c", "#fdbf6f", "#ff7f00", "#cab2d6", "#6a3d9a", "#ffff99", "#b15928" };
    }
}
