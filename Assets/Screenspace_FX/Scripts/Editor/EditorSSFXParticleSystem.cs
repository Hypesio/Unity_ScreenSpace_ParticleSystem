using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

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
        buttonPressedStyle.normal = buttonPressedStyle.active;

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
        if (GUILayout.Button("Once"))
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
        }
        
        if (paused && GUILayout.Button("Resume"))
        {
            targetScript.PlayEffect();
        }
        
        if (effectState != EffectState.None && GUILayout.Button("Reset"))
        {
            targetScript.ResetEffect();
            effectState = EffectState.None;
        }
        GUILayout.EndHorizontal();

    
        if (IsEffectPlaying())
        {
            GUILayout.Label($"Progress {targetScript.GetEffectProgress()}%", centeredStyle);
        }


        GUILayout.Space(10);
        GUILayout.Label("Options", headerStyleH2);

        DrawDefaultInspector();


        UpdateSystemState();

        if (IsEffectPlaying())
        {
           Repaint();
        }

        if (GUI.changed)
        {
            targetScript.UpdateConfig();
        }
    }
}
