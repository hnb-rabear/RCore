using UnityEngine;

namespace RCore
{
    /// <summary>
    /// Helper class for trajectory calculations.
    /// Thanks JamesLeeNZ and other posters on this forum thread!
    /// https://forum.unity3d.com/threads/projectile-trajectory-accounting-for-gravity-velocity-mass-distance.425560/#post-2750631
    /// </summary>
    public static class TrajectoryHelper
    {
        /// <summary>
        /// Determine the first-order intercept using absolute target position.
        /// </summary>
        /// <param name="shooterPosition">Position of shooter</param>
        /// <param name="shooterVelocity">Current velocity of shooter</param>
        /// <param name="shotSpeed">Speed of the shot (projectile)</param>
        /// <param name="targetPosition">Position of target</param>
        /// <param name="targetVelocity">Velocity of target</param>
        /// <returns>First order intercept</returns>
        public static Vector3 CalcInterceptPos(Vector3 shooterPosition, Vector3 shooterVelocity, float shotSpeed, Vector3 targetPosition, Vector3 targetVelocity)
        {
            var targetRelativePosition = targetPosition - shooterPosition;
            var targetRelativeVelocity = targetVelocity - shooterVelocity;
            float t = CalcInterceptTime(shotSpeed, targetRelativePosition, targetRelativeVelocity);
            return targetPosition + t * targetRelativeVelocity;
        }

        public static Vector3 CalcInterceptPos(Vector3 shooterPosition, Vector3 targetPosition, Vector3 targetVelocity)
        {
            return CalcInterceptPos(shooterPosition, Vector3.zero, 0, targetPosition, targetVelocity);
        }

        /// <summary>
        /// Determine the first-order intercept using relative target position.
        /// </summary>
        /// <param name="shotSpeed">Speed of the shot (projectile)</param>
        /// <param name="targetRelativePosition">Position of target relative to shooter</param>
        /// <param name="targetRelativeVelocity">Current velocity of target relative to shooter</param>
        /// <returns>First order intercept</returns>
        public static float CalcInterceptTime(float shotSpeed, Vector3 targetRelativePosition, Vector3 targetRelativeVelocity = default)
        {
            float velocitySquared = targetRelativeVelocity.sqrMagnitude;
            if (velocitySquared < 0.001f)
            {
                return 0f;
            }

            float a = velocitySquared - shotSpeed * shotSpeed;

            //handle similar velocities
            if (Mathf.Abs(a) < 0.001f)
            {
                float t = -targetRelativePosition.sqrMagnitude / (2f * Vector3.Dot(targetRelativeVelocity, targetRelativePosition));
                return Mathf.Max(t, 0f); //don't shoot back in time
            }

            float b = 2f * Vector3.Dot(targetRelativeVelocity, targetRelativePosition);
            float c = targetRelativePosition.sqrMagnitude;
            float determinant = b * b - 4f * a * c;

            if (determinant > 0f)
            {
                //determinant > 0; two intercept paths (most common)
                float t1 = (-b + Mathf.Sqrt(determinant)) / (2f * a), t2 = (-b - Mathf.Sqrt(determinant)) / (2f * a);
                if (t1 > 0f)
                {
                    if (t2 > 0f)
                    {
                        return Mathf.Min(t1, t2); //both are positive
                    }
                    return t1; //only t1 is positive
                }
                return Mathf.Max(t2, 0f); //don't shoot back in time
            }
            if (determinant < 0f)
            {
                //determinant < 0; no intercept path
                return 0f;
            }
            return Mathf.Max(-b / (2f * a), 0f); //don't shoot back in time
        }

        /// <summary>
        /// Calculates the trajectory for a shot.
        /// </summary>
        /// <param name="pTargetDistance">Distance of target from shooter</param>
        /// <param name="pProjectileVelocity">Speed of projectile</param>
        /// <returns>True if it is possible to calculate the trajectory for this situation.</returns>
        public static float? CalcAngleOfElevation(float pTargetDistance, float pProjectileVelocity)
        {
            float calculatedAngle = 0.5f * (Mathf.Asin(-Physics.gravity.y * pTargetDistance / (pProjectileVelocity * pProjectileVelocity)) * Mathf.Rad2Deg);
            if (float.IsNaN(calculatedAngle))
                return null;
            return calculatedAngle;
        }

        /// <summary>
        /// Use the Artillery formula to return an angle to hit the target.
        /// Based on https://gist.github.com/benloong/4661336
        /// </summary>
        /// <param name="from">Where we are shooting from</param>
        /// <param name="to">What we are aiming at</param>
        /// <param name="pProjectileVelocity">The velocity of the projectile</param>
        /// <param name="pGravity">Gravity,</param>
        /// <param name="posAngle">Aim up to hit target or aim across?</param>
        /// <returns></returns>
        public static float? CalcAngleOfElevation(Vector3 from, Vector3 to, float pProjectileVelocity, float pGravity, bool posAngle = false)
        {
            //float x = Mathf.Abs((to - from).z);
            var fromNoY = new Vector3(from.x, 0, from.z);
            var toNoY = new Vector3(to.x, 0, to.z);
            float x = Vector3.Distance(fromNoY, toNoY);
            float y = (to - from).y;
            float v2 = pProjectileVelocity * pProjectileVelocity;
            float v4 = v2 * v2;
            float fac = v4 - pGravity * (pGravity * x * x + 2 * y * v2);
            if (fac < 0)
            {
                return null; //No angle
            }
            float theta = 0;
            if (posAngle)
            {
                theta = -Mathf.Atan((v2 + Mathf.Sqrt(fac)) / (pGravity * x)) * Mathf.Rad2Deg;
            }
            else
            {
                theta = -Mathf.Atan((v2 - Mathf.Sqrt(fac)) / (pGravity * x)) * Mathf.Rad2Deg;
            }
            //while (theta < 0)
            //{
            //theta += 360f;
            //}
            return theta;
        }

        public static float? CalcAngleOfElevation(Vector3 from, Vector3 to, float pProjectileVelocity)
        {
            return CalcAngleOfElevation(from, to, pProjectileVelocity, Physics.gravity.magnitude);
        }
    }

    /// <summary>
    /// Credit: https://github.com/IronWarrior/ProjectileShooting/tree/master
    /// </summary>
    public static class ProjectileHelper
    {
        /// <summary>
        /// Calculates the two possible initial angles that could be used to fire a projectile at the supplied
        /// speed to travel the desired distance
        /// </summary>
        /// <param name="speed">Initial speed of the projectile</param>
        /// <param name="distance">Distance along the horizontal axis the projectile will travel</param>
        /// <param name="yOffset">Elevation of the target with respect to the initial fire position</param>
        /// <param name="gravity">Downward acceleration in m/s^2</param>
        /// <param name="angle"></param>
        /// <param name="lowAngle"></param>
        /// <returns>False if the target is out of range</returns>
        public static bool CalcLaunchAngle(float speed, float distance, float yOffset, float gravity, out float angle, out float lowAngle)
        {
            angle = lowAngle = 0;

            float speedSquared = speed * speed;

            float operandA = Mathf.Pow(speed, 4);
            float operandB = gravity * (gravity * (distance * distance) + 2 * yOffset * speedSquared);

            // Target is not in range
            if (operandB > operandA)
                return false;

            float root = Mathf.Sqrt(operandA - operandB);

            angle = Mathf.Atan((speedSquared + root) / (gravity * distance));
            lowAngle = Mathf.Atan((speedSquared - root) / (gravity * distance));

            return true;

            //How to use
            //localRotation = Quaternion.Euler(angle * Mathf.Rad2Deg, transform.eulerAngles.y, transform.eulerAngles.z);
            //velocity = transform.up * speed;
        }

        public static bool CalcLaunchAngle(float speed, float distance, float yOffset, out float angle, out float lowAngle)
        {
            return CalcLaunchAngle(speed, distance, yOffset, Physics.gravity.magnitude, out angle, out lowAngle);
        }

        /// <summary>
        /// Calculates the initial launch speed required to hit a target at distance with elevation yOffset.
        /// </summary>
        /// <param name="distance">Planar distance from origin to the target</param>
        /// <param name="yOffset">Elevation of the origin with respect to the target</param>
        /// <param name="gravity">Downward acceleration in m/s^2</param>
        /// <param name="angle">Initial launch angle in radians</param>
        /// <returns>Initial launch speed</returns>
        public static float CalcLaunchSpeed(float distance, float yOffset, float gravity, float pAngleInDeg)
        {
            float angle = pAngleInDeg * Mathf.Deg2Rad;
            float speed = distance * Mathf.Sqrt(gravity) * Mathf.Sqrt(1 / Mathf.Cos(angle)) / Mathf.Sqrt(2 * distance * Mathf.Sin(angle) + 2 * yOffset * Mathf.Cos(angle));

            return speed;

            //How to use
            //rotation = Quaternion.Euler(initialAngle, transform.eulerAngles.y, transform.eulerAngles.z);
            //velocity = transform.up * speed;
        }

        public static float CalcLaunchSpeed(float distance, float yOffset, float pAngleInDeg)
        {
            return CalcLaunchSpeed(distance, yOffset, Physics.gravity.magnitude, pAngleInDeg);
        }

        /// <summary>
        /// Calculates how long a projectile will stay in the air before reaching its target
        /// </summary>
        /// <param name="speed">Initial speed of the projectile</param>
        /// <param name="angleInDeg">Initial launch angle</param>
        /// <param name="yOffset">Elevation of the target with respect to the initial fire position</param>
        /// <param name="gravity">Downward acceleration in m/s^2</param>
        /// <returns></returns>
        public static float CalcTimeOfFlight(float speed, float angleInDeg, float yOffset, float gravity)
        {
            float radians = angleInDeg * Mathf.Deg2Rad;
            float ySpeed = speed * Mathf.Sin(radians);
            float time = (ySpeed + Mathf.Sqrt(ySpeed * ySpeed + 2 * gravity * yOffset)) / gravity;

            return time;
        }

        public static float CalcTimeOfFlight(float speed, float angleInDeg, float yOffset)
        {
            return CalcTimeOfFlight(speed, angleInDeg, yOffset, Physics.gravity.magnitude);
        }

        /// <summary>
        /// Samples a series of points along a projectile arc
        /// </summary>
        /// <param name="iterations">Number of points to sample</param>
        /// <param name="speed">Initial speed of the projectile</param>
        /// <param name="distance">Distance the projectile will travel along the horizontal axis</param>
        /// <param name="gravity">Downward acceleration in m/s^2</param>
        /// <param name="angleInDeg">Initial launch angle</param>
        /// <returns>Array of sampled points with the length of the supplied iterations</returns>
        public static Vector2[] CalcProjectileArcPoints(Vector3 startPoint, float distance, int direction, int iterations, float speed, float gravity, float angleInDeg)
        {
            float iterationSize = distance / iterations;

            float radians = angleInDeg * Mathf.Deg2Rad;

            var points = new Vector2[iterations + 1];

            for (int i = 0; i <= iterations; i++)
            {
                float x = iterationSize * i;
                float xSpeed = speed * Mathf.Cos(radians);
                float t = x / xSpeed;
                float ySpeed = speed * Mathf.Sin(radians);
                float y = -0.5f * gravity * (t * t) + ySpeed * t;

                var p = new Vector2(startPoint.x + x * direction, startPoint.y + y);
                points[i] = p;
            }

            return points;
        }

        public static Vector2[] CalcProjectileArcPoints(Vector3 startPoint, float distance, int direction, int iterations, float speed, float angleInDeg)
        {
            return CalcProjectileArcPoints(startPoint, distance, iterations, direction, speed, Physics.gravity.magnitude, angleInDeg);
        }

        public static Vector2[] CalcProjectileArcPoints(Vector3 startPoint, Vector3 endPoint, int iterations, float speed, float angleInDeg)
        {
            float distance = Vector3.Distance(endPoint, startPoint);
            int direction = endPoint.x > startPoint.x ? 1 : -1;
            return CalcProjectileArcPoints(startPoint, distance, direction, iterations, speed, Physics.gravity.magnitude, angleInDeg);
        }

        public static Vector2 GetPoint(Vector3 startPoint, int direction, float time, float speed, float angleInDeg)
        {
            return GetPoint(startPoint, direction, time, speed, angleInDeg, Physics2D.gravity.magnitude);
        }

        public static Vector2 GetPoint(Vector3 startPoint, int direction, float time, float speed, float angleInDeg, float gravity)
        {
            float radians = angleInDeg * Mathf.Deg2Rad;
            float xSpeed = speed * Mathf.Cos(radians) * direction;
            float ySpeed = speed * Mathf.Sin(radians);
            float x = xSpeed * time;
            float y = -0.5f * gravity * (time * time) + ySpeed * time;
            return new Vector2(startPoint.x + x, startPoint.y + y);
        }

        /// <summary>
        /// NOTE: Positions of this object and the target on the same plane
        /// Calc velocity of projectile
        /// </summary>
        /// <param name="pStartPos">Start position</param>
        /// <param name="pTargetPos">Target position</param>
        /// <param name="pAngleInDeg">Initial angle</param>
        /// <returns></returns>
        public static Vector3 CalcProjectileVelocity(Vector3 pStartPos, Vector3 pTargetPos, float pAngleInDeg)
        {
            float gravity = Physics.gravity.magnitude;
            float angle = pAngleInDeg * Mathf.Deg2Rad;

            //Positions of this object and the target on the same plane
            var planarTarget = new Vector3(pTargetPos.x, 0, pTargetPos.z);
            var planarPostion = new Vector3(pStartPos.x, 0, pStartPos.z);

            //Planar distance between objects
            float distance = Vector3.Distance(planarTarget, planarPostion);
            //Distance along the y axis between objects
            float yOffset = pStartPos.y - pTargetPos.y;

            float initialVelocity = 1 / Mathf.Cos(angle) * Mathf.Sqrt(0.5f * gravity * Mathf.Pow(distance, 2) / (distance * Mathf.Tan(angle) + yOffset));

            var velocity = new Vector3(0, initialVelocity * Mathf.Sin(angle), initialVelocity * Mathf.Cos(angle));

            //Rotate our velocity to match the direction between the two objects
            float angleBetweenObjects = Vector3.Angle(Vector3.forward, planarTarget - planarPostion) * (pTargetPos.x > pStartPos.x ? 1 : -1);
            var finalVelocity = Quaternion.AngleAxis(angleBetweenObjects, Vector3.up) * velocity;

            return finalVelocity;

            //Then use
            //rigidbody.velocity = finalVelocity;
            //Or
            //rigidbody.AddForce(finalVelocity * rigid.mass, ForceMode.Impulse);
        }
    }
}