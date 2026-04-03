using UnityEngine;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using DG.Tweening;

namespace Shady
{
    public class CursorTrail : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private LineRenderer trailPrefab = null;
        [SerializeField] private Camera cam = null;

        [Header("Trail Settings")]
        [SerializeField] private float distanceFromCamera = 1f;
        [SerializeField] private float clearSpeed = 2f;
        [SerializeField] private float minPointDistance = 0.05f; // Minimum distance between points
        [SerializeField] private int maxPoints = 200; // Maximum points to prevent memory issues

        [Header("Visual Settings")]
        [SerializeField] private Gradient trailGradient = new Gradient();
        [SerializeField] private AnimationCurve trailWidth = AnimationCurve.Linear(0, 0.1f, 1, 0.05f);
        [SerializeField] private bool fadeOutAtEnd = true;
        [SerializeField] private bool useWorldSpace = true;

        [Header("Effects")]
        [SerializeField] private bool addGlow = true;
        [SerializeField] private float glowIntensity = 0.5f;
        [SerializeField] private ParticleSystem sliceParticles; // Optional particle effect

        [Header("Performance")]
        [SerializeField] private bool useOptimizedRendering = true;
        [SerializeField] private int updateRate = 60; // Updates per second

        // Input System
        private PlayerInput playerInput;
        private InputAction touchPressAction;
        private InputAction touchPositionAction;

        // Trail data
        private LineRenderer currentTrail;
        private List<Vector3> points = new List<Vector3>();
        private List<float> pointTimes = new List<float>(); // Track time for better fading
        private float lastAddTime = 0f;
        private float updateInterval;
        private float lastUpdateTime;

        // Optimization
        private Vector2 lastMousePosition;
        private bool isDrawing = false;

        // Colors
        private Color defaultStartColor = Color.white;
        private Color defaultEndColor = new Color(1f, 0.5f, 0f, 0f);

        private void Awake()
        {
            // Setup camera
            if (cam == null)
                cam = Camera.main;

            // Setup input system
            SetupInputSystem();

            // Setup update interval
            updateInterval = 1f / updateRate;

            // Setup default gradient if not set
            if (trailGradient.colorKeys.Length == 0)
            {
                trailGradient.SetKeys(
                    new GradientColorKey[] {
                        new GradientColorKey(Color.white, 0f),
                        new GradientColorKey(Color.yellow, 0.5f),
                        new GradientColorKey(Color.red, 1f)
                    },
                    new GradientAlphaKey[] {
                        new GradientAlphaKey(1f, 0f),
                        new GradientAlphaKey(0.8f, 0.5f),
                        new GradientAlphaKey(0f, 1f)
                    }
                );
            }
        }

        private void SetupInputSystem()
        {
            // Create input actions
            touchPressAction = new InputAction("TouchPress", InputActionType.Button);
            touchPositionAction = new InputAction("TouchPosition", InputActionType.Value);

            // Bind to primary touch and mouse
            touchPressAction.AddBinding("<Touchscreen>/primaryTouch/press");
            touchPressAction.AddBinding("<Mouse>/leftButton");

            touchPositionAction.AddBinding("<Touchscreen>/primaryTouch/position");
            touchPositionAction.AddBinding("<Mouse>/position");

            // Enable actions
            touchPressAction.Enable();
            touchPositionAction.Enable();

            // Setup callbacks
            touchPressAction.performed += OnDrawStart;
            touchPressAction.canceled += OnDrawEnd;
            touchPositionAction.performed += OnDrawMove;
        }

        private void OnDestroy()
        {
            if (touchPressAction != null)
            {
                touchPressAction.Disable();
                touchPositionAction.Disable();
                touchPressAction.Dispose();
                touchPositionAction.Dispose();
            }
        }

        private void OnDrawStart(InputAction.CallbackContext context)
        {
            if (context.performed)
            {
                StartTrail();
            }
        }

        private void OnDrawEnd(InputAction.CallbackContext context)
        {
            if (context.canceled)
            {
                EndTrail();
            }
        }

        private void OnDrawMove(InputAction.CallbackContext context)
        {
            if (!isDrawing) return;

            Vector2 currentPos = context.ReadValue<Vector2>();
            lastMousePosition = currentPos;

            // Rate limit point addition
            if (Time.time - lastAddTime >= updateInterval)
            {
                AddPoint(currentPos);
                lastAddTime = Time.time;
            }
        }

        private void StartTrail()
        {
            // Play slice start haptic
            if (NOIZManager.Settings.enableHaptics)
            {
                NOIZManager.Haptics.StartContinuousHaptic();
            }

            // Play particle effect at start
            if (sliceParticles != null)
            {
                sliceParticles.Play();
            }

            DestroyCurrentTrail();
            CreateCurrentTrail();
            AddPoint(lastMousePosition);
            isDrawing = true;
        }

        private void EndTrail()
        {
            isDrawing = false;

            // Play slice end haptic
            if (NOIZManager.Settings.enableHaptics)
            {
                NOIZManager.Haptics.EndSwipeHaptic(true);
            }

            // Fade out trail
            if (fadeOutAtEnd && currentTrail != null)
            {
                FadeOutTrail();
            }
        }

        private void Update()
        {
            if (!isDrawing && currentTrail == null) return;

            UpdateTrailPoints();
            ClearTrailPoints();

            // Auto-clear if trail is empty
            if (points.Count <= 1 && !isDrawing)
            {
                DestroyCurrentTrail();
            }
        }

        private void DestroyCurrentTrail()
        {
            if (currentTrail != null)
            {
                // Animate fade out before destroying
                if (currentTrail.gameObject.activeSelf)
                {
                    Destroy(currentTrail.gameObject);
                }
                else
                {
                    Destroy(currentTrail.gameObject);
                }

                currentTrail = null;
                points.Clear();
                pointTimes.Clear();
            }
        }

        private void CreateCurrentTrail()
        {
            if (trailPrefab == null)
            {
                CreateDefaultTrail();
                return;
            }

            currentTrail = Instantiate(trailPrefab);
            currentTrail.transform.SetParent(transform, true);

            // Apply visual settings
            ApplyTrailSettings();
        }

        private void CreateDefaultTrail()
        {
            GameObject trailGO = new GameObject("CursorTrail");
            currentTrail = trailGO.AddComponent<LineRenderer>();
            currentTrail.transform.SetParent(transform, true);

            // Default settings
            currentTrail.startWidth = 0.1f;
            currentTrail.endWidth = 0.05f;
            currentTrail.material = new Material(Shader.Find("Sprites/Default"));
            currentTrail.useWorldSpace = useWorldSpace;

            ApplyTrailSettings();
        }

        private void ApplyTrailSettings()
        {
            if (currentTrail == null) return;

            currentTrail.useWorldSpace = useWorldSpace;
            currentTrail.colorGradient = trailGradient;
            currentTrail.widthCurve = trailWidth;

            // Add glow effect
            if (addGlow && currentTrail.material != null)
            {
                currentTrail.material.EnableKeyword("_EMISSION");
                currentTrail.material.SetColor("_EmissionColor", Color.white * glowIntensity);
            }
        }

        private void AddPoint(Vector2 screenPosition)
        {
            Vector3 worldPos = ScreenToWorldPoint(screenPosition);

            // Check distance from last point
            if (points.Count > 0)
            {
                float distance = Vector3.Distance(points[points.Count - 1], worldPos);
                if (distance < minPointDistance) return;
            }

            points.Add(worldPos);
            pointTimes.Add(Time.time);

            // Limit points
            while (points.Count > maxPoints)
            {
                points.RemoveAt(0);
                pointTimes.RemoveAt(0);
            }
        }

        private Vector3 ScreenToWorldPoint(Vector2 screenPos)
        {
            if (cam == null) return Vector3.zero;

            Vector3 viewportPos = new Vector3(
                screenPos.x / Screen.width,
                screenPos.y / Screen.height,
                distanceFromCamera
            );

            return cam.ViewportToWorldPoint(viewportPos);
        }

        private void UpdateTrailPoints()
        {
            if (currentTrail == null || points.Count < 2) return;

            // Update line renderer points
            currentTrail.positionCount = points.Count;
            currentTrail.SetPositions(points.ToArray());

            // Update gradient based on age if fading
            if (fadeOutAtEnd && !isDrawing)
            {
                UpdateTrailColorByAge();
            }
        }

        private void UpdateTrailColorByAge()
        {
            if (currentTrail == null || points.Count == 0 || pointTimes.Count != points.Count) return;

            float currentTime = Time.time;
            float maxAge = 1f / clearSpeed;

            Gradient dynamicGradient = new Gradient();
            List<GradientColorKey> colorKeys = new List<GradientColorKey>();
            List<GradientAlphaKey> alphaKeys = new List<GradientAlphaKey>();

            for (int i = 0; i < points.Count; i++)
            {
                float age = currentTime - pointTimes[i];
                float t = Mathf.Clamp01(age / maxAge);

                // Calculate color based on age
                Color color = trailGradient.Evaluate(1f - t);
                float alpha = Mathf.Lerp(1f, 0f, t);

                colorKeys.Add(new GradientColorKey(color, (float)i / points.Count));
                alphaKeys.Add(new GradientAlphaKey(alpha, (float)i / points.Count));
            }

            dynamicGradient.SetKeys(colorKeys.ToArray(), alphaKeys.ToArray());
            currentTrail.colorGradient = dynamicGradient;
        }

        private void ClearTrailPoints()
        {
            if (points.Count == 0) return;

            float clearDistance = Time.deltaTime * clearSpeed;

            while (points.Count > 1 && clearDistance > 0)
            {
                float distance = Vector3.Distance(points[0], points[1]);

                if (clearDistance >= distance)
                {
                    points.RemoveAt(0);
                    if (pointTimes.Count > 0) pointTimes.RemoveAt(0);
                    clearDistance -= distance;
                }
                else
                {
                    points[0] = Vector3.Lerp(points[0], points[1], clearDistance / distance);
                    clearDistance = 0;
                }
            }

            // If only one point left and not drawing, clear it
            if (points.Count == 1 && !isDrawing)
            {
                points.Clear();
                pointTimes.Clear();
            }
        }

        private void FadeOutTrail()
        {
            if (currentTrail == null) return;

            // Fade out the entire trail
            if (!isDrawing)
            {
                DestroyCurrentTrail();
            }
        }

        private void OnDisable()
        {
            DestroyCurrentTrail();
            isDrawing = false;
        }

        // Editor visualization
        private void OnDrawGizmos()
        {
            if (!Application.isPlaying || points.Count < 2) return;

            Gizmos.color = Color.red;
            for (int i = 0; i < points.Count - 1; i++)
            {
                Gizmos.DrawLine(points[i], points[i + 1]);
            }
        }

        // Optional: Get trail points for other systems
        public List<Vector3> GetTrailPoints()
        {
            return new List<Vector3>(points);
        }

        public float GetTrailLength()
        {
            float length = 0f;
            for (int i = 0; i < points.Count - 1; i++)
            {
                length += Vector3.Distance(points[i], points[i + 1]);
            }
            return length;
        }

        public void ClearTrail()
        {
            DestroyCurrentTrail();
            points.Clear();
            pointTimes.Clear();
            isDrawing = false;
        }
    }
}