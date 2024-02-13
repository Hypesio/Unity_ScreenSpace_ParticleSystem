using System.Collections;
using System.Collections.Generic;
using Codice.CM.Client.Gui;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;
using UnityEngine.XR.WSA;

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
    private SerializedObject serializedTarget;
    private bool initDone = false;

    public override bool RequiresConstantRepaint() => IsEffectPlaying();

    void OnEnable()
    {
        targetScript = (SSFXParticleSystem)target;
        effectState = EffectState.None;
        serializedTarget = new SerializedObject(targetScript);

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

        centeredStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter };

        buttonPressedStyle = new GUIStyle(GUI.skin.button);
        buttonPressedStyle.fontStyle = FontStyle.Bold;
        buttonPressedStyle.normal.textColor = Color.green;

        initDone = true;
    }

    bool IsEffectPlaying()
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
            targetScript.ResetEffect();
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

        if (!paused && IsEffectPlaying() && GUILayout.Button("Pause "))
        {
            targetScript.PauseEffect();
            paused = true;
            EditorUtils.SetTimeSpeed(0);
        }
        else if (paused && IsEffectPlaying() && GUILayout.Button("Resume"))
        {
            targetScript.PlayEffect();
            paused = false;
            EditorUtils.SetTimeSpeed(1);
        }
        else if (!IsEffectPlaying() && GUILayout.Button("       "))
        {

        }

        if (!IsEffectPlaying() && paused)
        {
            paused = false;
            EditorUtils.SetTimeSpeed(1);
        }

        bool resetThisFrame = false;
        if (GUILayout.Button("Reset"))
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

        if (GUILayout.Button("Use all child renderers"))
        {
            List<Renderer> renderers = new List<Renderer>();

            foreach (Renderer rend in targetScript.transform.GetComponentsInChildren<Renderer>())
            {
                renderers.Add(rend);
            }

            targetScript.renderers = renderers.ToArray();
        }
        EditorGUILayout.PropertyField(serializedObject.FindProperty("renderers"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("durationEffect"));

        EditorGUILayout.PropertyField(serializedObject.FindProperty("particleSpawnRate"));
        GUILayout.Space(4);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("durationMin"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("durationMax"));
        GUILayout.Space(4);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("startSizeMin"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("startSizeMax"));
        GUILayout.Space(4);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("startSpeedMin"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("startSpeedMax"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("startSpeedType"));
        if (targetScript.startSpeedType == SSFXParticleSystem.StartSpeedType.StartDirection)
            EditorGUILayout.PropertyField(serializedObject.FindProperty("startDirection"));

        GUILayout.Space(4);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("enableGravityModifier"));
        if (targetScript.enableGravityModifier)
            EditorGUILayout.PropertyField(serializedObject.FindProperty("gravityModifier"));

        //DrawDefaultInspector();
        GUILayout.Space(8);
        targetScript.enableSpeedOverLifetime = GUILayout.Toggle(targetScript.enableSpeedOverLifetime, "Enable SpeedOverLifetime");
        if (targetScript.enableSpeedOverLifetime)
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("speedOverLifetime"));
        }

        GUILayout.Space(8);
        targetScript.enableSizeOverLifetime = GUILayout.Toggle(targetScript.enableSizeOverLifetime, "Enable SizeOverLifetime");
        if (targetScript.enableSizeOverLifetime)
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("sizeOverLifetime"));
        }

        GUILayout.Space(8);
        targetScript.enableTarget = GUILayout.Toggle(targetScript.enableTarget, "Enable Target");
        if (targetScript.enableTarget)
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("particlesTarget"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("particlesTargetAttractionForce"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("particleDieWhenReachingTarget"));
            if (targetScript.particleDieWhenReachingTarget)
                EditorGUILayout.PropertyField(serializedObject.FindProperty("targetKillRadius"));

        }

        serializedObject.ApplyModifiedProperties();

        UpdateSystemState();

        if (!resetThisFrame && GUI.changed)
        {
            targetScript.UpdateConfig();
        }
    }
}
