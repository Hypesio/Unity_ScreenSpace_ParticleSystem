using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(SplineCreator))]
public class EditorSplineCreator : Editor
{
    SplineCreator targetScript;
    private Vector3 _previousMousePositionOnPlane;
    private Vector3 _mouseMovePlaneNormal;

    private SplinePoint[] previousPoints;
    private float prevStepCount = 0;
    private float prevPrecisionCount = 0;

    void OnEnable()
    {
        targetScript = (SplineCreator)target;
    }

    void MouseClick(Event e)
    {
        SplinePoint[] points = targetScript.points;

        Camera cam = SceneView.currentDrawingSceneView.camera;
        float ySize = SceneView.currentDrawingSceneView.cameraViewport.size.y;
        float yMouse = (ySize / 2) + -(e.mousePosition.y - (ySize / 2));
        Ray mouseRay = cam.ScreenPointToRay(new Vector3(e.mousePosition.x, yMouse, 1.0f));

        _mouseMovePlaneNormal = -mouseRay.direction;
        Vector3 mousePosition = Vector3.ProjectOnPlane(cam.ScreenToWorldPoint(new Vector3(e.mousePosition.x, yMouse, 0.005f)), _mouseMovePlaneNormal);
        _previousMousePositionOnPlane = mousePosition;

        int closestPoint = 0;
        float closestDist = float.MaxValue;
        bool closestIsInfluencer = false;
        for (int i = 0; i < points.Length; i++)
        {
            float dist = Vector3.Distance(Vector3.ProjectOnPlane(points[i].point.point, _mouseMovePlaneNormal), mousePosition);
            if (dist < closestDist)
            {
                closestPoint = i;
                closestIsInfluencer = false;
                closestDist = dist;
            }

            //Debug.DrawLine(mousePosition, Vector3.ProjectOnPlane(points[i].point, _mouseMovePlaneNormal), Color.red, 0.4f);
            dist = Vector3.Distance(Vector3.ProjectOnPlane(points[i].point.point + points[i].point.influencer, _mouseMovePlaneNormal), mousePosition);
            if (dist < closestDist)
            {
                closestPoint = i;
                closestIsInfluencer = true;
                closestDist = dist;
            }
        }

        targetScript._indexSelectedPoint = closestPoint;
        targetScript._influencerSelected = closestIsInfluencer;
    }

    void MouseDrag(Event e)
    {
        float ySize = SceneView.currentDrawingSceneView.cameraViewport.size.y;
        float yMouse = (ySize / 2) + -(e.mousePosition.y - (ySize / 2));
        Camera cam = SceneView.currentDrawingSceneView.camera;
        if (targetScript._indexSelectedPoint >= 0 && targetScript._indexSelectedPoint < targetScript.points.Length)
        {
            targetScript._isMovingPoint = true;
            Vector3 mousePosition = Vector3.ProjectOnPlane(cam.ScreenToWorldPoint(new Vector3(e.mousePosition.x, yMouse, 0.005f)), _mouseMovePlaneNormal);
            Vector3 offset = mousePosition - _previousMousePositionOnPlane;
            if (!targetScript._influencerSelected)
            {
                Vector3 onPlane = Vector3.ProjectOnPlane(targetScript.points[targetScript._indexSelectedPoint].point.point, _mouseMovePlaneNormal);
                targetScript.points[targetScript._indexSelectedPoint].point.point += (mousePosition - onPlane);
            }
            else
            {
                Vector3 onPlane = Vector3.ProjectOnPlane(targetScript.points[targetScript._indexSelectedPoint].point.point + targetScript.points[targetScript._indexSelectedPoint].point.influencer, _mouseMovePlaneNormal);
                targetScript.points[targetScript._indexSelectedPoint].point.influencer += (mousePosition - onPlane);
            }

            _previousMousePositionOnPlane = mousePosition;
        }

        UnityEditor.Selection.activeGameObject = targetScript.gameObject;
    }

    private void OnSceneGUI()
    {
        Event e = Event.current;

        if (e.type == EventType.MouseDown)
            MouseClick(e);

        if (e.type == EventType.KeyDown && e.keyCode == KeyCode.LeftControl)
            MouseDrag(e);
    }

    public override void OnInspectorGUI()
    {
        if (GUILayout.Button("Update display"))
        {
            targetScript.UpdateSplineDatas();
        }

        if (GUILayout.Button("Add point"))
        {
            targetScript.AddPoint(targetScript._indexSelectedPoint);
            targetScript.UpdateSplineDatas();
        }
        else if (GUI.changed)
        {
            targetScript.UpdateSplineDatas();
        }
        else if (previousPoints != null && previousPoints.Length != targetScript.points.Length)
        {
            targetScript.UpdateSplineDatas();
        }
        else if (previousPoints != null)
        {
            for (int i = 0; i < previousPoints.Length; i++)
            {
                BezierCubicPoint p0 = previousPoints[i].point;
                BezierCubicPoint p1 = targetScript.points[i].point;

                if (p0.point != p1.point || p0.influencer != p1.influencer || previousPoints[i].width != targetScript.points[i].width)
                {
                    targetScript.UpdateSplineDatas();
                    break;
                }
            }
        }
        else if (targetScript.stepToComputePerSegment != prevStepCount || targetScript.precisionStep != prevPrecisionCount)
        {
            prevStepCount = targetScript.stepToComputePerSegment;
            prevPrecisionCount = targetScript.precisionStep;
            targetScript.UpdateSplineDatas();
        }

        if (previousPoints == null || previousPoints.Length != targetScript.points.Length)
            previousPoints = new SplinePoint[targetScript.points.Length];

        targetScript.points.CopyTo(previousPoints, 0);

        base.OnInspectorGUI();
    }
}
