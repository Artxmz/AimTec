namespace Rapid.Interfaces
{
    using Aimtec.SDK.Prediction.Skillshots;

    internal interface IPrediction
    {
        PredictionOutput GetDashPrediction(PredictionInput input);

        PredictionOutput GetIdlePrediction(PredictionInput input);

        PredictionOutput GetMovementPrediction(PredictionInput input);

        PredictionOutput GetImmobilePrediction(PredictionInput input);
    }
}