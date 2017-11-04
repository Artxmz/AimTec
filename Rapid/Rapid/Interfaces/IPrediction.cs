namespace Rapid.Interfaces
{
    using Aimtec.SDK.Prediction.Skillshots;

    internal interface IPrediction
    {
        PredictionOutput GetDashPrediction(PredictionInput input);

        PredictionOutput GetIdlePrediction(PredictionInput input, bool checkCollision);

        PredictionOutput GetMovementPrediction(PredictionInput input, bool checkCollision);

        PredictionOutput GetImmobilePrediction(PredictionInput input);
    }
}