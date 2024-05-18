using System.Collections;
using System.Collections.Generic;
using SSFX;
using Unity.Burst.Intrinsics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

[System.Serializable]
public struct SplinePoint
{
    public BezierCubicPoint point;
    public float width;

    public Transform transform;

    public SplinePoint(BezierCubicPoint p, float w)
    {
        width = w;
        point = p;
        transform = null;
    }

    public void UpdatePointPos()
    {
        if (transform != null)
        {
            point.point = transform.position;
        }
    }

}

[System.Serializable]
public struct BoundingBox
{
    public Vector3 cornerMin;
    public Vector3 cornerMax;

    public BoundingBox(bool setMax)
    {
        cornerMax = Vector3.zero;
        cornerMin = Vector3.zero;
        if (setMax)
        {
            cornerMin.x = float.MaxValue;
            cornerMin.y = float.MaxValue;
            cornerMin.z = float.MaxValue;

            cornerMax.x = float.MinValue;
            cornerMax.y = float.MinValue;
            cornerMax.z = float.MinValue;
        }
    }
}

public enum SplineAttractionBoxType
{
    Automatic,
    Manual
}

[ExecuteInEditMode]
public class SplineCreator : MonoBehaviour
{
    public SplinePoint[] points = new SplinePoint[0];

    public Vector3[] curveSteps = new Vector3[0];
    public Vector4[] curveStepsWithWidth = new Vector4[0];

    public SplineAttractionBoxType attractionBoxType = SplineAttractionBoxType.Automatic;
    public BoundingBox boundingBox;
    [Tooltip("Taken into account only if attraction type is manual")]
    public BoundingBox attractionBox;
    public int debug_displayPointCount = 50;
    public int debug_displayPrecisionStep = 20;
    public int _indexSelectedPoint = -1;
    public bool _influencerSelected = false;
    public bool _isMovingPoint = false;
    // Set to true when pipeline values changes.
    public bool isDirty = false;
    [HideInInspector]
    public int splineIndex = 0;

    private void Start()
    {
        UpdateSplineDatas();

        // Register splines for particle system.
        SSFXParticleSystemHandler.RegisterSpline(this);
    }

    private void OnDestroy()
    {
        SSFXParticleSystemHandler.UnregisterSpline(this);
    }



    // Compute spline bounding box.
    public BoundingBox GetSplineBoundingBox(Vector3[] steps)
    {
        BoundingBox bb = new BoundingBox(true);

        foreach (var s in steps)
        {
            if (s.x > bb.cornerMax.x)
                bb.cornerMax.x = s.x;
            if (s.y > bb.cornerMax.y)
                bb.cornerMax.y = s.y;
            if (s.z > bb.cornerMax.z)
                bb.cornerMax.z = s.z;

            if (s.x < bb.cornerMin.x)
                bb.cornerMin.x = s.x;
            if (s.y < bb.cornerMin.y)
                bb.cornerMin.y = s.y;
            if (s.z < bb.cornerMin.z)
                bb.cornerMin.z = s.z;
        }

        return bb;
    }

    public BezierCubicPoint[] GetSplineBezierPoints()
    {
        BezierCubicPoint[] bcp = new BezierCubicPoint[points.Length];
        for (int i = 0; i < points.Length; i++)
        {
            bcp[i] = points[i].point;
        }
        return bcp;
    }


    // Update spline computed datas. 
    public void UpdateSplineDatas()
    {
        if (points.Length > 1)
        {
            for (int i = 0; i < points.Length; i++)
            {
                points[i].UpdatePointPos();
            }

            isDirty = true;
            curveSteps = BezierCubic.GetPositions(GetSplineBezierPoints(), points.Length * debug_displayPrecisionStep, out (int, float)[] stepInPart, debug_displayPointCount);
            boundingBox = GetSplineBoundingBox(curveSteps);

            if (attractionBoxType == SplineAttractionBoxType.Automatic)
                attractionBox = boundingBox;

            curveStepsWithWidth = new Vector4[debug_displayPointCount];
            for (int i = 0; i < debug_displayPointCount; i++)
            {
                float width = Mathf.Lerp(points[stepInPart[i].Item1].width, points[stepInPart[i].Item1 + 1].width, stepInPart[i].Item2);
                Vector3 step = curveSteps[i];
                curveStepsWithWidth[i] = new Vector4(step.x, step.y, step.z, width);
            }
        }
    }

    // Add a point at the specified index.
    public void AddPoint(int insertIndex, SplinePoint point)
    {
        if (points == null)
            points = new SplinePoint[0];

        SplinePoint[] newPoints = new SplinePoint[points.Length + 1];
        for (int i = 0; i < newPoints.Length; i++)
        {
            if (i < insertIndex)
                newPoints[i] = points[i];
            else if (i == insertIndex)
                newPoints[i] = point;
            else
                newPoints[i] = points[i - 1];
        }

        points = newPoints;
    }

    // Add a point after the specified index. -1 means add in first place.
    // Point will spawn in middle of two points if possible.
    public void AddPoint(int indexPreviousPoint)
    {
        BezierCubicPoint newPoint = new();
        int indexInsert = indexPreviousPoint + 1;
        float width = 0.3f;

        if (points == null || points.Length == 0)
        {
            newPoint.point = transform.position;
            indexInsert = 0;
        }
        // Add in first
        else if (indexPreviousPoint == -1)
        {
            newPoint.point = points[0].point.point;
            width = points[0].width;
            indexInsert = 0;
        }
        // Add in last
        else if (indexPreviousPoint >= points.Length - 1)
        {
            indexInsert = Mathf.Min(indexInsert, points.Length);

            // Add a little offset to avoid points to overlap
            newPoint.point = points[indexPreviousPoint].point.point + 1f * (points[indexPreviousPoint].point.point - points[indexPreviousPoint - 1].point.point);
            width = points[indexPreviousPoint].width;
        }
        // Add in middle 
        else
        {
            Vector3 link = points[indexInsert - 1].point.point - points[indexInsert].point.point;
            newPoint.point = points[indexInsert - 1].point.point + (Vector3.Magnitude(link) / 2.0f * link);
            width = points[indexPreviousPoint].width;
        }

        AddPoint(indexInsert, new SplinePoint(newPoint, width));
    }

    // Add new point at the end of the spline
    public void AddPoint()
    {
        AddPoint(_indexSelectedPoint);
    }


#if UNITY_EDITOR
    void Update()
    {
        // User has selected this spline
        if (UnityEditor.Selection.activeGameObject == this.gameObject)
        {
            //Debug.Log("Object is selected");
            // On click we search closest point or influencer
        }
        else
        {
            _indexSelectedPoint = -1;
        }
    }
#endif

    #region Gizmo

    void ShowGizmosBezierPoint(BezierCubicPoint point, float width, bool isSelected)
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(point.point, point.point + point.influencer);
        Gizmos.DrawSphere(point.point + point.influencer, 0.10f);
        Gizmos.color = Color.green;
        Gizmos.DrawSphere(point.point, 0.25f);


        if (isSelected)
        {
            Gizmos.color = Color.blue;
            if (_isMovingPoint)
            {
                Gizmos.DrawSphere(point.point + point.influencer, 0.12f);
                Gizmos.DrawSphere(point.point, 0.27f);
            }
            else
            {
                Gizmos.DrawWireSphere(point.point + point.influencer, 0.12f);
                Gizmos.DrawWireSphere(point.point, 0.27f);
            }
        }
    }

    void ShowGizmosSplineCurve()
    {
        Gizmos.color = Color.green;
        Vector3[] linePositions = curveSteps;

        if (curveSteps == null)
            return;
        //BezierCubic.PrintValues(linePositions);
        for (int i = 0; i < curveStepsWithWidth.Length; i++)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawCube(linePositions[i], new Vector3(0.05f, 0.05f, 0.05f));
            Gizmos.DrawLine(linePositions[i], linePositions[i + 1]);

            Vector3 tangent = Vector3.Normalize(linePositions[i + 1] - linePositions[i]);

            Vector3 normal = Vector3.Cross(Vector3.right, tangent);
            Vector3 normal2 = Vector3.Cross(Vector3.forward, tangent);
            normal = normal2.magnitude > normal.magnitude ? normal2 : normal;
            Vector3 toBorder = normal * curveStepsWithWidth[i].w;

            Gizmos.DrawLine(linePositions[i], linePositions[i] + toBorder);
            Gizmos.DrawLine(linePositions[i], linePositions[i] - toBorder);

            float theta = 1.7f;
            toBorder = toBorder * Mathf.Cos(theta) + Vector3.Cross(tangent, toBorder) * Mathf.Sin(theta) + (1 - Mathf.Cos(theta)) * Vector3.Dot(tangent, toBorder) * tangent;
            Gizmos.DrawLine(linePositions[i], linePositions[i] + toBorder);
            Gizmos.DrawLine(linePositions[i], linePositions[i] - toBorder);
        }


        // Bounding box
        Gizmos.color = Color.cyan;
        BoundingBox bb = attractionBox;
        Gizmos.DrawLine(bb.cornerMax, new Vector3(bb.cornerMin.x, bb.cornerMax.y, bb.cornerMax.z));
        Gizmos.DrawLine(bb.cornerMax, new Vector3(bb.cornerMax.x, bb.cornerMin.y, bb.cornerMax.z));
        Gizmos.DrawLine(bb.cornerMax, new Vector3(bb.cornerMax.x, bb.cornerMax.y, bb.cornerMin.z));

        Gizmos.DrawLine(bb.cornerMin, new Vector3(bb.cornerMax.x, bb.cornerMin.y, bb.cornerMin.z));
        Gizmos.DrawLine(bb.cornerMin, new Vector3(bb.cornerMin.x, bb.cornerMax.y, bb.cornerMin.z));
        Gizmos.DrawLine(bb.cornerMin, new Vector3(bb.cornerMin.x, bb.cornerMin.y, bb.cornerMax.z));
    }

    void OnDrawGizmos()
    {
        ShowGizmosSplineCurve();
        for (int i = 0; i < points.Length; i++)
        {
            ShowGizmosBezierPoint(points[i].point, points[i].width, i == _indexSelectedPoint);
        }

#if UNITY_EDITOR
        // Ensure continuous Update calls.
        if (!Application.isPlaying)
        {
            UnityEditor.EditorApplication.QueuePlayerLoopUpdate();
            UnityEditor.SceneView.RepaintAll();
        }
#endif


    }

    #endregion
}
