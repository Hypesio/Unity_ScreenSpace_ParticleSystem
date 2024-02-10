using System.Collections;
using System.Collections.Generic;
using Codice.CM.Client.Gui;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

[CustomEditor(typeof(SSFXParticleSystem))]
public class EditorSSFXParticleSystem : Editor 
{
    SSFXParticleSystem targetScript; 

    private enum EffectState
    {
        None, 
        Playing,
        Looping, 
    }

    private EffectState effectState = EffectState.None;
    private bool paused = false;
    private GUIStyle headerStyleH2;
    private GUIStyle headerStyleH1;
    private GUIStyle headerStyleH3;
    private GUIStyle buttonPressedStyle;
    private GUIStyle centeredStyle;

    private bool initDone = false;

    public override bool RequiresConstantRepaint() => IsEffectPlaying();

    void OnEnable()
    {
        targetScript = (SSFXParticleSystem)target; 
        effectState = EffectState.None;
        
    }
    void InitStyles()
    {
        headerStyleH2 = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 15,
            fontStyle = FontStyle.Bold
        };

        headerStyleH1 = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 20,
            fontStyle = FontStyle.Bold
        };

        headerStyleH3 = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontStyle = FontStyle.Italic
        };

        centeredStyle = new GUIStyle(GUI.skin.label){alignment = TextAnchor.MiddleCenter};

        buttonPressedStyle = new GUIStyle(GUI.skin.button);
        buttonPressedStyle.fontStyle = FontStyle.Bold;
        buttonPressedStyle.normal.textColor = Color.green;

        initDone = true;
    }

    bool IsEffectPlaying ()
    {   
        return effectState == EffectState.Playing || effectState == EffectState.Looping;
    }

    void UpdateSystemState()
    {
        if (effectState == EffectState.Looping && !targetScript.isPlaying)
            targetScript.StartEffect();
        else if (effectState == EffectState.Playing && !targetScript.isPlaying)
            effectState = EffectState.None;
    }

    public override void OnInspectorGUI()
    {
        if (!initDone)
            InitStyles();
        GUILayout.Label("SSFX Particles", headerStyleH1);
        GUILayout.Label("Alpha 0.1", centeredStyle);
        GUILayout.Label("Made by Hypesio", headerStyleH3);
        GUILayout.Space(5);
        GUILayout.Label("Play effect", headerStyleH2);
        GUILayout.BeginHorizontal();

        GUIStyle onceStyle = effectState == EffectState.Playing ? buttonPressedStyle : GUI.skin.button;
        if (GUILayout.Button("Once", onceStyle))
        {
            targetScript.StartEffect();
            effectState = EffectState.Playing;
        }
        
        if (effectState != EffectState.Looping && GUILayout.Button("Loop"))
        {
            targetScript.StartEffect();
            effectState = EffectState.Looping;
        }
        
        if (effectState == EffectState.Looping)
            GUILayout.Button("Loop", buttonPressedStyle);
        
        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();

        if (!paused && IsEffectPlaying() && GUILayout.Button("Pause"))
        {
            targetScript.PauseEffect();
            paused = true;
            EditorUtils.SetTimeSpeed(0);
        }
        
        if (paused && IsEffectPlaying() && GUILayout.Button("Resume"))
        {
            targetScript.PlayEffect();
            paused = false;
            EditorUtils.SetTimeSpeed(1);
        }

        if (!IsEffectPlaying() && paused)
        {
            paused = false;
            EditorUtils.SetTimeSpeed(1);
        }
        
        bool resetThisFrame = false;
        if (effectState != EffectState.None && GUILayout.Button("Reset"))
        {
            targetScript.ResetEffect();
            effectState = EffectState.None;
            EditorUtils.SetTimeSpeed(1);
            paused = false;
            resetThisFrame = true;
        }

        GUILayout.EndHorizontal();

    
        if (IsEffectPlaying())
        {
            GUILayout.Label($"Progress {targetScript.GetEffectProgress().ToString("0.00")}%", centeredStyle);
        }

        GUILayout.Space(10);
        GUILayout.Label("Options", headerStyleH2);

        DrawDefaultInspector();


        UpdateSystemState();

        if (!resetThisFrame && GUI.changed)
        {
            targetScript.UpdateConfig();
        }
    }
}
