namespace Rapid
{
    using Aimtec.SDK.Prediction.Skillshots;

    internal static class Program
    {
        private static void Main()
        {
            Prediction.Instance.AddPredictionImplementation("Rapid Prediction", new Utilities.Prediction());
        }
    }
}