using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

[ExecuteInEditMode]
public class SplineCreator : MonoBehaviour
{
    public BezierCubicPoint[] points;
    public int debug_displayPointCount = 50;
    public int debug_displayPrecisionStep = 20;
    public int _indexSelectedPoint = -1;
    public bool _influencerSelected = false;
    public bool _isMovingPoint = false;

    // Add a point at the specified index.
    public void AddPoint(int insertIndex, BezierCubicPoint point)
    {
        if (points == null)
            points = new BezierCubicPoint[0];

        BezierCubicPoint[] newPoints = new BezierCubicPoint[points.Length + 1];
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

        if (points == null || points.Length == 0)
        {
            newPoint.point = transform.position;
            indexInsert = 0;
        }
        // Add in first
        else if (indexPreviousPoint == -1)
        {
            newPoint.point = points[0].point;
            indexInsert = 0;
        }
        // Add in last
        else if (indexPreviousPoint >= points.Length - 1)
        {
            indexInsert = Mathf.Min(indexInsert, points.Length);
            // Add a little offset to avoid points to overlap
            newPoint.point = points[indexInsert].point + 1f * (points[indexInsert].point - points[indexInsert - 1].point);
        }
        // Add in middle 
        else
        {
            Vector3 link = points[indexInsert].point - points[indexInsert + 1].point;
            newPoint.point = points[indexInsert].point + (Vector3.Magnitude(link) / 2.0f * link);
        }

        AddPoint(indexInsert, newPoint);
    }

    // Add new point at the end of the spline
    public void AddPoint()
    {
        AddPoint(_indexSelectedPoint);
    }


    void Update()
    {
        //_isMovingPoint = false;

        // User has selected this spline
        if (UnityEditor.Selection.activeGameObject == this.gameObject)
        {
            Debug.Log("Object is selected");
            // On click we search closest point or influencer
        }
        else
        {
            _indexSelectedPoint = -1;
        }
    }

    #region Gizmo

    void ShowGizmosBezierPoint(BezierCubicPoint point, bool isSelected)
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

    void ShowGizmosSplineCurve(BezierCubicPoint[] points)
    {
        Gizmos.color = Color.green;
        Vector3[] linePositions = BezierCubic.GetPositions(points, points.Length * debug_displayPrecisionStep, debug_displayPointCount);
        //BezierCubic.PrintValues(linePositions);

        for (int i = 0; i < linePositions.Length - 1; i++)
        {
            Gizmos.DrawCube(linePositions[i], new Vector3(0.05f, 0.05f, 0.05f));
            Gizmos.DrawLine(linePositions[i], linePositions[i + 1]);
        }
    }

    void OnDrawGizmos()
    {
        ShowGizmosSplineCurve(points);
        for (int i = 0; i < points.Length; i++)
        {
            ShowGizmosBezierPoint(points[i], i == _indexSelectedPoint);
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
