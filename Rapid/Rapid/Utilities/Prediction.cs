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
            var castPosition = Vector3.Zero;
            var unitPosition = Vector3.Zero;
            var predictedPosition = Vector3.Zero;

            var paths = input.Unit.Path;

            for (var i = 0; i < paths.Length - 1; i++)
            {
                var previousPath = paths[i];
                var currentPath = paths[i + 1];
                var remainingPath = input.Unit.ServerPosition.Distance(currentPath);

                var direction = (currentPath - previousPath).Normalized();
                var velocity = direction * input.Unit.MoveSpeed;

                var delays = Game.Ping / 1000f + input.Delay;

                unitPosition = input.Unit.ServerPosition + velocity * delays;

                var unitDistance = input.From.Distance(unitPosition);
                var unitPositionImpactTime = unitDistance / input.Speed;
                unitPosition = input.Unit.ServerPosition + velocity * unitPositionImpactTime;

                var toUnit = (unitPosition - input.From).Normalized();
                var cosTheta = Vector3.Dot(direction, toUnit);
                var castDirection = 2 * (direction + toUnit) * cosTheta;
                predictedPosition = unitPosition - castDirection * (input.Unit.BoundingRadius + input.Radius);

                var centerPosition = (unitPosition + predictedPosition) * 0.5f;
                var centerPositionDistance = input.From.Distance(centerPosition);

                var a = Vector3.Dot(velocity, velocity) - (Math.Abs(input.Speed - float.MaxValue) <= 0
                                                               ? float.MaxValue
                                                               : (float)Math.Pow(input.Speed, 2));

                var b = 2 * centerPositionDistance * input.Unit.MoveSpeed * cosTheta;
                var c = (float)Math.Pow(centerPositionDistance, 2);

                var discriminant = b * b - 4f * a * c;

                if (discriminant < 0) return new PredictionOutput { HitChance = HitChance.OutOfRange };

                var castPositionImpactTime = 2f * c / ((float)Math.Sqrt(discriminant) - b);

                if (remainingPath / input.Unit.MoveSpeed < castPositionImpactTime) continue;

                castPosition = input.Unit.ServerPosition + velocity * castPositionImpactTime;

                if (input.From.Distance(castPosition) + input.Delay * input.Unit.MoveSpeed > input.Range)
                    return new PredictionOutput { HitChance = HitChance.OutOfRange };
            }

            var result = new PredictionOutput
                             {
                                 Input = input,
                                 UnitPosition = unitPosition,
                                 CastPosition = castPosition,
                                 HitChance = HitChance.VeryHigh
                             };

            if (!checkCollision || !input.Collision) return result;

            var collisionObjects = Collision.GetCollision(
                new List<Vector3> { input.Unit.ServerPosition, unitPosition, castPosition },
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