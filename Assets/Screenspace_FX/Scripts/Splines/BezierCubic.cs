using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Rendering;

[System.Serializable]
public struct BezierCubicPoint
{
    public Vector3 point;
    // Influencer is local position from point
    public Vector3 influencer;

    BezierCubicPoint(Vector3 point, Vector3 influencer)
    {
        this.point = point;
        this.influencer = influencer;
    }
}

public static class BezierCubic
{

    // Returns an array of positions along a series of cubic bezier points.
    // nbPositions is the number of point to compute. They are uniformly distributed along space.
    // nbSteps is the precision wanted. More steps = better precision. 
    // progressInPart is the distance progress (range 0-1) of each point in their curve segment.
    public static Vector3[] GetPositions(BezierCubicPoint[] points, int nbPositions, out (int, float)[] progressInPart, int nbSteps = 100)
    {
        progressInPart = new (int, float)[nbPositions];
        // Can't get positions for a single point
        if (points.Length == 1)
            return new Vector3[] { points[0].point };



        float totalArcLength = GetLength(points, out float[] partsLength, nbSteps * (points.Length - 1));
        Vector3[] res = new Vector3[nbPositions];
        res[0] = points[0].point;
        float distStep = totalArcLength / nbSteps;

        int currentPart = 0;
        float currentTotalLengthCeil = partsLength[0];
        float currentTotalLengthFloor = 0;
        float[] currentLUT;
        GetLUT(points[0], points[1], out currentLUT, nbSteps);

        float currentDistance = distStep;
        for (int i = 1; i < nbPositions; i++)
        {
            int newPart = currentPart;

            // Get target part
            while (newPart + 1 < partsLength.Length && currentTotalLengthCeil < currentDistance)
            {
                newPart++;
                currentTotalLengthFloor = currentTotalLengthCeil;
                currentTotalLengthCeil += partsLength[newPart];
            }

            // Get new LUT if is in new part
            if (newPart != currentPart)
            {
                GetLUT(points[newPart], points[newPart + 1], out currentLUT, nbSteps);
                currentPart = newPart;
            }
            // Get position
            float step = DistanceToStep(currentLUT, currentDistance - currentTotalLengthFloor);
            res[i] = GetPosition(points[currentPart], points[currentPart + 1], step);
            progressInPart[i] = (currentPart, (currentDistance - currentTotalLengthFloor) / currentLUT[currentLUT.Length - 1]);
            currentDistance += distStep;
        }

        res[nbPositions - 1] = points[points.Length - 1].point;

        return res;
    }

    // Returns the derivative of a point => spline 'forward' direction
    // Can also be used to get point normal
    public static Vector3 GetDerivative(BezierCubicPoint pointA, BezierCubicPoint pointB, float step)
    {
        float stepSquare = step * step;
        Vector3 aInfluencer = pointA.point + pointA.influencer;
        Vector3 bInfluencer = pointB.point + pointB.influencer;

        Vector3 a = pointA.point * (-3 * stepSquare + 6 * step - 3);
        Vector3 b = aInfluencer * (9 * stepSquare - 12 * step + 3);
        Vector3 c = bInfluencer * (-9 * stepSquare + 6 * step);
        return a + b + c + 3 * pointB.point * stepSquare;
    }

    // Converts a distance to a step 't' used to get point in spline.
    public static float DistanceToStep(float[] LUT, float distance)
    {
        if (LUT.Length == 0)
            return 0;

        float previousLen = 0;
        float arcLen = LUT[LUT.Length - 1];
        float stepSize = 1.0f / LUT.Length;
        // Could be optimize for large LUT arrays by changing search algorithm
        for (int i = 0; i < LUT.Length; i++)
        {
            if (LUT[i] > distance)
            {
                float lerpStep = distance - previousLen;
                return Mathf.Lerp(Mathf.Max(0, (i - 1) * stepSize), i * stepSize, lerpStep);
            }
            previousLen = LUT[i];
        }
        return 1.0f;
    }

    // Returns position between two bezier cubic points.
    public static Vector3 GetPosition(BezierCubicPoint pointA, BezierCubicPoint pointB, float step)
    {
        Debug.Assert(step >= 0 && step <= 1.1, $"Invalid step value: {step}");
        step = Mathf.Min(step, 1.0f);

        float stepSquare = step * step;
        float stepCube = stepSquare * step;

        Vector3 aInfluencer = pointA.point + pointA.influencer;
        Vector3 bInfluencer = pointB.point + pointB.influencer;

        Vector3 a = pointA.point * (-stepCube + 3 * stepSquare - 3 * step + 1);
        Vector3 b = aInfluencer * (3 * stepCube - 6 * stepSquare + 3 * step);
        Vector3 c = bInfluencer * (-3 * stepCube + 3 * stepSquare);
        return a + b + c + pointB.point * stepCube;


        /*Vector3 aL = Vector3.Lerp(pointA.point, aInfluencer, step);
        Vector3 bL = Vector3.Lerp(aInfluencer, bInfluencer, step);
        Vector3 cL = Vector3.Lerp(bInfluencer, pointB.point, step);
        Vector3 dL = Vector3.Lerp(aL, bL, step);
        Vector3 eL = Vector3.Lerp(bL, cL, step);
        return Vector3.Lerp(dL, eL, step);*/
    }

    // Returns approximate length of the curve between 2 points.
    // More steps means better precision.
    public static float GetLength(BezierCubicPoint pointA, BezierCubicPoint pointB, float steps = 100)
    {
        float len = 0;
        Vector3 currentPosition = pointA.point;
        float stepSize = 1.0f / steps;
        float currentStep = stepSize;
        for (int i = 0; i < steps; i++)
        {
            Vector3 newPos = GetPosition(pointA, pointB, currentStep);
            len += Vector3.Distance(currentPosition, newPos);

            currentStep += stepSize;
            currentPosition = newPos;
        }

        return len;
    }

    // Returns approximate length of the curve between 2 points.
    // Out a cumulative length look up table
    // More steps means better precision.
    public static float GetLUT(BezierCubicPoint pointA, BezierCubicPoint pointB, out float[] LUT, int nbSteps)
    {
        float len = 0;
        Vector3 currentPosition = pointA.point;
        float stepSize = 1.0f / nbSteps;
        float currentStep = stepSize;
        LUT = new float[nbSteps];
        for (int i = 0; i < nbSteps; i++)
        {
            Vector3 newPos = GetPosition(pointA, pointB, currentStep);
            len += Vector3.Distance(currentPosition, newPos);
            currentStep += stepSize;
            currentPosition = newPos;
            LUT[i] = len;
        }

        return len;
    }

    // Returns approximate length of the curve formed by the points.
    // Steps is the total number of steps for approximation. More steps means better precision.
    // Returns length of each spline parts in an array.
    public static float GetLength(BezierCubicPoint[] points, out float[] partsLength, float steps = 100)
    {
        float len = 0;
        // Could be improved by choosing number of steps depending on scalar distance between points.
        int stepsPerCouple = (int)(steps / (points.Length / 2));
        partsLength = new float[points.Length - 1];

        for (int i = 0; i < points.Length - 1; i++)
        {
            float curlen = GetLength(points[i], points[i + 1], stepsPerCouple);
            len += curlen;
            partsLength[i] = curlen;
        }

        return len;
    }

    // Returns approximate length of the curve formed by the points.
    // Steps is the total number of steps for approximation. More steps means better precision.
    public static float GetLength(BezierCubicPoint[] points, float steps = 100)
    {
        return GetLength(points, out float[] pl);
    }


}
