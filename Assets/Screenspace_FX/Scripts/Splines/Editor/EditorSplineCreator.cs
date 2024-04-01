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

    void OnEnable()
    {
        targetScript = (SplineCreator)target;
    }

    void MouseClick(Event e)
    {
        BezierCubicPoint[] points = targetScript.points;

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
            float dist = Vector3.Distance(Vector3.ProjectOnPlane(points[i].point, _mouseMovePlaneNormal), mousePosition);
            if (dist < closestDist)
            {
                closestPoint = i;
                closestIsInfluencer = false;
                closestDist = dist;
            }

            //Debug.DrawLine(mousePosition, Vector3.ProjectOnPlane(points[i].point, _mouseMovePlaneNormal), Color.red, 0.4f);
            dist = Vector3.Distance(Vector3.ProjectOnPlane(points[i].point + points[i].influencer, _mouseMovePlaneNormal), mousePosition);
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
                Vector3 onPlane = Vector3.ProjectOnPlane(targetScript.points[targetScript._indexSelectedPoint].point, _mouseMovePlaneNormal);
                targetScript.points[targetScript._indexSelectedPoint].point += (mousePosition - onPlane);
            }
            else
            {
                Vector3 onPlane = Vector3.ProjectOnPlane(targetScript.points[targetScript._indexSelectedPoint].point + targetScript.points[targetScript._indexSelectedPoint].influencer, _mouseMovePlaneNormal);
                targetScript.points[targetScript._indexSelectedPoint].influencer += (mousePosition - onPlane);
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
}
