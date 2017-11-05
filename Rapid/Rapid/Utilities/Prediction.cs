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
        private static Obj_AI_Hero Player => ObjectManager.GetLocalPlayer();

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

            if (input.AoE) input.Radius /= 2f;

            for (var i = 0; i < paths.Length - 1; i++)
            {
                var previousPath = paths[i];
                var currentPath = paths[i + 1];
                var remainingPath = input.Unit.ServerPosition.Distance(currentPath);

                var direction = (currentPath - previousPath).Normalized();
                var velocity = direction * input.Unit.MoveSpeed;

                var delays = Game.Ping / 1000f + input.Delay;

                predictedPosition = input.Unit.ServerPosition + velocity * delays;

                var unitPositionImpactTime = input.From.Distance(predictedPosition) / input.Speed;
                result.UnitPosition = input.Unit.ServerPosition + velocity * unitPositionImpactTime;

                if (input.From.Distance(result.UnitPosition) > input.Range) result.HitChance = HitChance.OutOfRange;

                var toUnit = (result.UnitPosition - input.From).Normalized();
                var cosTheta = Vector3.Dot(direction, toUnit);

                var castDirection = (direction + toUnit) * cosTheta;
                predictedPosition = result.UnitPosition - castDirection * (input.Unit.BoundingRadius + input.Radius);

                var predictedPositionDistance = input.From.Distance(predictedPosition);

                var a = Vector3.Dot(velocity, velocity) - (Math.Abs(input.Speed - float.MaxValue) <= 0
                                                               ? float.MaxValue
                                                               : (float)Math.Pow(input.Speed, 2));

                var b = 2 * predictedPositionDistance * input.Unit.MoveSpeed * cosTheta;
                var c = (float)Math.Pow(predictedPositionDistance, 2);

                var discriminant = b * b - 4f * a * c;

                if (discriminant < 0) result.HitChance = HitChance.OutOfRange;

                var impactTime = 2f * c / ((float)Math.Sqrt(discriminant) - b);

                if (remainingPath / input.Unit.MoveSpeed < impactTime) continue;

                result.CastPosition = input.Unit.ServerPosition + velocity * impactTime;

                if (input.From.Distance(result.CastPosition) + input.Delay * input.Unit.MoveSpeed > input.Range)
                    result.HitChance = HitChance.OutOfRange;
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
            if (!input.Unit.IsValidTarget()) return null;

            return input.Unit.IsMoving ? this.GetMovementPrediction(input, true) : this.GetIdlePrediction(input, true);
        }

        public PredictionOutput GetPrediction(PredictionInput input, bool ft, bool collision)
        {
            if (!input.Unit.IsValidTarget()) return null;

            return input.Unit.IsMoving
                       ? this.GetMovementPrediction(input, collision)
                       : this.GetIdlePrediction(input, collision);
        }
    }
}