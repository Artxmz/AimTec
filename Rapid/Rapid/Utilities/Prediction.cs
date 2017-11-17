namespace Rapid.Utilities
{
    using System;
    using System.Collections.Generic;

    using Aimtec;
    using Aimtec.SDK.Extensions;
    using Aimtec.SDK.Prediction.Collision;
    using Aimtec.SDK.Prediction.Skillshots;

    using Rapid.Interfaces;

    internal class Prediction : ISkillshotPrediction, IPrediction
    {
        public PredictionOutput GetDashPrediction(PredictionInput input)
        {
            throw new NotImplementedException();
        }

        public PredictionOutput GetIdlePrediction(PredictionInput input, bool checkCollision)
        {
            var result = new PredictionOutput
                             {
                                 Input = input,
                                 UnitPosition = input.Unit.ServerPosition,
                                 CastPosition = input.Unit.ServerPosition,
                                 HitChance = HitChance.High
                             };

            if (!checkCollision || !input.Collision) return result;

            var collisionObjects = Collision.GetCollision(new List<Vector3> { input.Unit.ServerPosition }, input);

            result.CollisionObjects = collisionObjects;

            if (collisionObjects.Count > 0) result.HitChance = HitChance.Collision;

            return result;
        }

        public PredictionOutput GetImmobilePrediction(PredictionInput input)
        {
            throw new NotImplementedException();
        }

        public PredictionOutput GetMovementPrediction(PredictionInput input, bool checkCollision)
        {
            var predictedPosition = Vector3.Zero;

            var result = new PredictionOutput { Input = input, HitChance = HitChance.VeryHigh };

            var paths = input.Unit.Path;

            if (input.AoE || input.Type == SkillshotType.Circle) input.Radius /= 2f;

            for (var i = 0; i < paths.Length - 1; i++)
            {
                var previousPath = paths[i];
                var currentPath = paths[i + 1];
                var passedPath = previousPath.Distance(input.Unit.ServerPosition);
                var remainingPath = input.Unit.ServerPosition.Distance(currentPath);
                var pathRatio = passedPath / remainingPath;

                var direction = (currentPath - previousPath).Normalized();

                var delays = Game.Ping / 1000f + input.Delay;

                predictedPosition = input.Unit.ServerPosition + direction * (delays * input.Unit.MoveSpeed);

                var unitPositionImpactTime = input.From.Distance(predictedPosition) / input.Speed;
                result.UnitPosition = input.Unit.ServerPosition
                                      + direction * (unitPositionImpactTime * input.Unit.MoveSpeed);

                if (input.From.Distance(result.UnitPosition) + input.Delay * input.Unit.MoveSpeed > input.Range)
                    result.HitChance = HitChance.OutOfRange;

                var toUnit = (result.UnitPosition - input.From).Normalized();

                var castDirection = (direction + toUnit) / 2f;
                predictedPosition = result.UnitPosition - castDirection * (input.Unit.BoundingRadius + input.Radius);
                castDirection *= pathRatio;
                predictedPosition = predictedPosition + castDirection * input.Radius;

                if (input.From.Distance(predictedPosition) > input.Range) result.HitChance = HitChance.OutOfRange;

                var toPredictionPosition = (predictedPosition - input.From).Normalized();

                var cosTheta = Vector3.Dot(toUnit, toPredictionPosition);
                var distance = input.From.Distance(predictedPosition);

                var a = Vector3.Dot(direction, direction)
                        - (Math.Abs(input.Speed - float.MaxValue) <= 0
                               ? float.MaxValue
                               : (float)Math.Pow(input.Speed, 2));

                var b = 2 * distance * input.Unit.MoveSpeed * cosTheta;
                var c = (float)Math.Pow(distance, 2);

                var discriminant = b * b - 4f * a * c;

                if (discriminant < 0) result.HitChance = HitChance.OutOfRange;

                var impactTime = 2f * c / ((float)Math.Sqrt(discriminant) - b);

                if (remainingPath / input.Unit.MoveSpeed < impactTime) continue;

                result.CastPosition = input.Unit.ServerPosition + direction * (impactTime * input.Unit.MoveSpeed);
            }

            if (!checkCollision || !input.Collision) return result;

            var collisionObjects = Collision.GetCollision(
                new List<Vector3> { input.Unit.ServerPosition, result.UnitPosition, result.CastPosition },
                input);

            if (collisionObjects.Count > 0) result.HitChance = HitChance.Collision;

            return result;
        }

        public PredictionOutput GetPrediction(PredictionInput input)
        {
            return this.GetPrediction(input, false, true);
        }

        public PredictionOutput GetPrediction(PredictionInput input, bool ft, bool collision)
        {
            if (!input.Unit.IsValidTarget() || !input.Unit.IsValid) return new PredictionOutput { Input = input };

            return input.Unit.IsMoving
                       ? this.GetMovementPrediction(input, collision)
                       : this.GetIdlePrediction(input, collision);
        }
    }
}