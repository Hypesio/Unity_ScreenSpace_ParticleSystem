using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Unity.VisualScripting;
using Unity.VisualScripting.Dependencies.Sqlite;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Timeline;

namespace SSFX
{

    // If values are added here, changes should be copied in ParticlesCommons.cginc
    enum SSFXParticleSystemFlags
    {
        GravityModifier = 1 << 1,
        ColorOverLifetime = 1 << 2,
        AlphaOverLifetime = 1 << 3,
        SizeOverLifetime = 1 << 4,
        Target = 1 << 5,
        SpeedOverLifetime = 1 << 6,
        KillAll = 1 << 7,
        TargetDieOnReach = 1 << 8,
        FollowSpline = 1 << 9,
        LifetimeIsTargetDistanceBased = 1 << 10,

    }

    // If change are made here, it MUST be replicated to ParticlesCommon.cginc.
    unsafe public struct SSFXParticleConfig
    {
        // used to tell in shader what to use or not
        public int flagsFeature;
        public float gravity;

        // grouped by 4 - xyz = color,w = timestamp
        public fixed float grad_colorOverLifetime[10 * 4];
        // grouped by 2 - x = size, y = timestamp
        public fixed float grad_alphaOverLifetime[10 * 2];
        // grouped by 2 - x = size, y = timestamp
        public fixed float grad_sizeOverLifetime[10 * 2];
        // grouped by 2 - x = size, y = timestamp
        public fixed float grad_speedOverLifetime[10 * 2];

        // xyz - TargetPosition, w - Attraction Force / Death Radius
        public Vector4 targetDatas;
    }

    public struct SSFXSplineInfos
    {
        public BoundingBox box;
        public int startPositionIndex;
        public int splineNbSteps;
    }


    public static class SSFXParticleSystemHandler
    {
        private static List<SplineCreator> splines;
        private static int MAX_GRADIENT_KEYS = 10;
        private static int INVALID_FLOAT = 65536;
        private static List<SSFXParticleConfig> _configs = null;
        private static List<Transform> _configsTargets = null;
        private static ComputeBuffer _configsBuffer = null;

        // Contains SSFXSplineInfos[].
        private static ComputeBuffer _splinesInfo = new ComputeBuffer(1, 4);
        // Vec4 => xyz - positions, w - width.
        private static ComputeBuffer _splinesPositions = new ComputeBuffer(1, 4);

        // Used to know when _configs data need to be updated on GPU
        // Set to true on _configs changes
        private static bool _dirtyConfigs = false;

        private static int AddConfig(SSFXParticleConfig config)
        {
            if (_configs == null)
            {
                _configs = new List<SSFXParticleConfig>();
                _configs.Add(new SSFXParticleConfig { });

                _configsTargets = new List<Transform>();
                _configsTargets.Add(null);
            }

            _configs.Add(config);
            _configsTargets.Add(null);
            return _configs.Count() - 1;
        }
        unsafe private static bool SequenceEqual(float* a, float* b, int size)
        {
            if (a == null && b == null)
                return true;
            if ((a == null && b != null) || (b == null && a != null))
                return false;

            for (int i = 0; i < size; i++)
            {
                if (a[i] != b[i])
                    return false;
            }
            return true;
        }
        unsafe private static bool ConfigEquality(SSFXParticleConfig a, SSFXParticleConfig b)
        {
            return a.gravity == b.gravity &&
            SequenceEqual(a.grad_colorOverLifetime, b.grad_colorOverLifetime, MAX_GRADIENT_KEYS * 4) &&
            SequenceEqual(a.grad_sizeOverLifetime, b.grad_sizeOverLifetime, MAX_GRADIENT_KEYS * 2) &&
            a.targetDatas == b.targetDatas
            && SequenceEqual(a.grad_speedOverLifetime, b.grad_speedOverLifetime, MAX_GRADIENT_KEYS * 2);
        }

        private static int FindConfig(SSFXParticleConfig config)
        {
            if (_configs == null)
                return -1;
            for (int i = 0; i < _configs.Count; i++)
            {
                SSFXParticleConfig act = _configs[i];
                if (ConfigEquality(config, act))
                    return i;
            }
            return -1;
        }

        public static void WarningMaxGradKeys(int number)
        {
            if (number > MAX_GRADIENT_KEYS)
            {
                Debug.LogWarning("[SSFX] Your gradient will be truncated, it have to much keys. Max is 10");
            }
        }

        unsafe
        public static void SetArrayFromAnimationCurve(AnimationCurve curve, float* grad)
        {
            if (curve == null || curve.keys.Count() == 0)
                return;
            WarningMaxGradKeys(curve.keys.Count());

            float maxTime = curve.keys[curve.keys.Count() - 1].time;
            float minTime = curve.keys[0].time;
            float range = maxTime - minTime;

            int index_key = 0;
            foreach (var keys in curve.keys)
            {
                if (index_key > MAX_GRADIENT_KEYS)
                    break;
                grad[index_key * 2] = keys.value;
                grad[index_key * 2 + 1] = (keys.time - minTime) / range;

                index_key++;
            }
            for (int i = index_key; i < MAX_GRADIENT_KEYS; i++)
            {
                grad[index_key * 2 + 1] = INVALID_FLOAT;
            }
        }

        unsafe
        public static void SetArrayFromColorGradient(Gradient gradient, float* grad_color, float* grad_alpha)
        {
            if (gradient == null)
                return;
            // Color gradient
            WarningMaxGradKeys(gradient.colorKeys.Count());
            //config.grad_colorOverLifetime = new float[MAX_GRADIENT_KEYS];
            int index_key = 0;
            foreach (var keys in gradient.colorKeys)
            {
                if (index_key > MAX_GRADIENT_KEYS)
                    break;
                grad_color[index_key * 4] = keys.color.r;
                grad_color[index_key * 4 + 1] = keys.color.g;
                grad_color[index_key * 4 + 2] = keys.color.b;
                grad_color[index_key * 4 + 3] = keys.time;

                index_key++;
            }
            for (int i = index_key; i < MAX_GRADIENT_KEYS; i++)
            {
                grad_color[index_key * 4 + 3] = INVALID_FLOAT;
            }

            // Alpha gradient
            WarningMaxGradKeys(gradient.alphaKeys.Count());

            index_key = 0;
            foreach (var keys in gradient.alphaKeys)
            {
                if (index_key > MAX_GRADIENT_KEYS)
                    break;
                grad_alpha[index_key * 2] = keys.alpha;
                grad_alpha[index_key * 2 + 1] = keys.time;

                index_key++;
            }
            for (int i = index_key; i < MAX_GRADIENT_KEYS; i++)
            {
                grad_alpha[index_key * 2 + 1] = INVALID_FLOAT;
            }
        }

        public static float Pack2Float(float a, float aMaxValue, float b, float bMaxValue)
        {
            float scale = 65535.0f;
            float a0_1 = a / aMaxValue;
            int aPacked = (int)(a0_1 * scale);
            float b0_1 = b / bMaxValue;
            int bPacked = (int)(b0_1 * scale);

            float packedValue = aPacked + (bPacked * scale);
            return packedValue;
        }

        public static void Unpack2Float(float packedValue, float aMaxValue, float bMaxValue, out float aValue, out float bValue)
        {
            float scale = 65535.0f;
            float b = Mathf.Floor(packedValue / scale);
            float a = packedValue - (b * scale);
            aValue = (float)(a / scale) * aMaxValue;
            bValue = (float)(b / scale) * bMaxValue;
        }

        public static void ClearConfigParticles(int configID)
        {
            if (_configs == null)
                return;
            if (configID >= _configs.Count() && configID >= 0)
            {
                Debug.LogWarning("[SSFX] You are trying to clear an out of bound config");
                return;
            }

            SSFXParticleConfig conf = _configs[configID];

            conf.flagsFeature |= (int)SSFXParticleSystemFlags.KillAll;

            _configs[configID] = conf;
            _dirtyConfigs = true;
        }


        unsafe public static void UpdateConfig(int configID, SSFXParticleConfig config, Transform target)
        {
            // Avoid errors after domain reload
            if (_configs == null)
            {
                _configs = new List<SSFXParticleConfig>();
                _configsTargets = new List<Transform>();
            }
            if (configID >= _configs.Count)
            {
                for (int i = _configs.Count; i <= configID; i++)
                {
                    _configs.Add(new SSFXParticleConfig { });
                }
                for (int i = _configsTargets.Count; i <= configID; i++)
                {
                    _configsTargets.Add(null);
                }
            }

            _configsTargets[configID] = target;

            _configs[configID] = config;
            _dirtyConfigs = true;
        }

        // Return the index of the particle config
        // Will create a new config only if it doesn't already exist
        unsafe public static int NewConfig(SSFXParticleConfig config, Transform target)
        {
            int index_config = AddConfig(config);
            UpdateConfig(index_config, config, target);

            return index_config;
        }

        // Create or update size of compute buffer if needed
        public static ComputeBuffer CreateConfigsComputeBuffer()
        {
            if (_configsBuffer != null && _configs != null && _configs.Count >= _configsBuffer.count)
                _configsBuffer.Release();
            else if (_configsBuffer != null && (_configs == null || _configs.Count < _configsBuffer.count))
                return _configsBuffer;

            if (_configs != null)
                _configsBuffer = new ComputeBuffer(_configs.Count() * 2, Marshal.SizeOf(typeof(SSFXParticleConfig)), ComputeBufferType.Structured);
            else
                _configsBuffer = new ComputeBuffer(10, Marshal.SizeOf(typeof(SSFXParticleConfig)), ComputeBufferType.Structured);

            return _configsBuffer;
        }

        // This should be done as rarely as possible 
        public static ComputeBuffer UpdateConfigsComputeBuffer()
        {
            _configsBuffer = CreateConfigsComputeBuffer();

            if (_configs == null)
                return _configsBuffer;

            // Update target position if needed
            for (int i = 1; i < _configs.Count(); i++)
            {
                if (_configsTargets != null && _configsTargets[i] != null)
                {
                    Vector3 tarPos = _configsTargets[i].position;
                    SSFXParticleConfig conf = _configs[i];
                    if (tarPos != (Vector3)conf.targetDatas)
                    {
                        _dirtyConfigs = true;
                        conf.targetDatas = new Vector4(tarPos.x, tarPos.y, tarPos.z, _configs[i].targetDatas.w);
                        _configs[i] = conf;
                    }
                }
            }

            if (_dirtyConfigs)
            {
                _configsBuffer.SetData(_configs);
                _dirtyConfigs = false;
            }

            return _configsBuffer;
        }


        public static int GetSplinesBuffers(out ComputeBuffer splinePositions, out ComputeBuffer splinesInfo)
        {

            splinePositions = _splinesPositions;
            splinesInfo = _splinesInfo;

            if (splines == null)
            {
                Debug.Log($"SplineGetted 0");
                return 0;
            }

            bool updateBuffers = false;
            for (int i = 0; i < splines.Count; i++)
            {
                if (splines[i].isDirty)
                {
                    updateBuffers = true;
                    break;
                }
            }

            if (!updateBuffers)
                return splines.Count();

            Debug.Log("Update spline buffers");

            if (_splinesInfo != null)
                _splinesInfo.Release();
            _splinesInfo = new ComputeBuffer(splines.Count, Marshal.SizeOf(typeof(SSFXSplineInfos)));

            SSFXSplineInfos[] infos = new SSFXSplineInfos[splines.Count];

            int totalSplineSteps = 0;
            for (int i = 0; i < splines.Count; i++)
            {
                splines[i].isDirty = false;
                SSFXSplineInfos info = new()
                {
                    box = splines[i].attractionBox,
                    startPositionIndex = totalSplineSteps,
                    splineNbSteps = splines[i].curveStepsWithWidth.Length

                };
                Debug.Log($"Add spline count {info.splineNbSteps}");
                totalSplineSteps += info.splineNbSteps;
                infos[i] = info;

            }

            Vector4[] points = new Vector4[totalSplineSteps];
            int currentIndex = 0;
            for (int i = 0; i < splines.Count; i++)
            {
                for (int j = 0; j < splines[i].curveStepsWithWidth.Length; j++)
                {
                    points[currentIndex] = splines[i].curveStepsWithWidth[j];
                    currentIndex++;
                }
            }

            _splinesInfo.SetData(infos);

            if (_splinesPositions != null)
                _splinesPositions.Release();
            _splinesPositions = new ComputeBuffer(totalSplineSteps, Marshal.SizeOf(typeof(Vector4)));
            _splinesPositions.SetData(points);

            splinePositions = _splinesPositions;
            splinesInfo = _splinesInfo;
            return splines.Count();
        }

        // Register spline.
        public static void RegisterSpline(SplineCreator spline)
        {
            if (splines == null)
                splines = new List<SplineCreator>();
            splines.Add(spline);
        }

        // Unregister spline.
        public static void UnregisterSpline(SplineCreator spline)
        {
            if (splines != null)
                splines.Remove(spline);
        }

        // Get spline index in spline resistered list.
        public static int GetSplineIndex(SplineCreator spline)
        {
            if (splines != null)
                return splines.IndexOf(spline);
            return 0;
        }

    }

}