using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System.Data;



#if UNITY_EDITOR
using UnityEditor;
#endif
public static class EditorUtils
{
    private static double _LastFrameTime = 0;


    public static float GetDeltaTime()
    {
        #if UNITY_EDITOR 
        if (Application.isPlaying)
        {
            return Time.deltaTime;
        }
        
        float deltatTime = (float)(EditorApplication.timeSinceStartup - _LastFrameTime);
        _LastFrameTime = EditorApplication.timeSinceStartup;
        return deltatTime > 1 ? 0 : deltatTime;
        #else
        return Time.deltaTime;
        #endif 
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
