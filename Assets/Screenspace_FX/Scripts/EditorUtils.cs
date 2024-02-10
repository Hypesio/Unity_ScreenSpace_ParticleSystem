using System;
using System.Collections.Generic;
using UnityEngine;

using System.Data;
using Unity.VisualScripting;




#if UNITY_EDITOR
using UnityEditor;
#endif
public static class EditorUtils
{
    private static double _LastFrameTime = 0;
    private static float _EditorDeltaTime = 0;
    private static float _TimeSinceStartup = 0;
    private static float _TimeSpeed = 1;
    
    public static void UpdateTimePassed()
    {
        if (IsInEditMode())
        {
            #if UNITY_EDITOR
            _EditorDeltaTime = (float)(EditorApplication.timeSinceStartup - _LastFrameTime);
            _TimeSinceStartup += GetDeltaTime();
            _LastFrameTime = EditorApplication.timeSinceStartup;
            #endif
        }
        else
        {
            _TimeSinceStartup += GetDeltaTime();
        }
    }
    public static float GetTimePassed()
    {
        return _TimeSinceStartup;
    }

    public static void SetTimeSpeed(float speed)
    {
        _TimeSpeed = speed;
    }

    public static float GetDeltaTime()
    {
        float res;
        if (!IsInEditMode())
        {
            res = Time.deltaTime;
        }
        else 
        {
            res = _EditorDeltaTime;
        }

        return res * _TimeSpeed;
    }

    public static bool IsInEditMode()
    {
        #if UNITY_EDITOR
            //return !EditorApplication.isPlaying;
            return !Application.isPlaying;
        #else
            retrun false;
        #endif
    }

}
