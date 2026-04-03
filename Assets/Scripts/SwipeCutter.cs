using UnityEngine;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using static SwipeDirectionHelper;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

public class SwipeCutter : MonoBehaviour
{
    public static SwipeCutter Instance;

    [Header("References")]
    public Camera cam;

    [Header("Swipe")]
    public float minSwipePixels = 30f;
    public float maxHitDistancePixels = 170f;
    public float forwardScreenPadding = 140f;
    public float noteRadiusBias = 50f;

    [Header("Live Cut")]
    public bool cutDuringDrag = true;
    public float liveCutMinPixels = 35f;

    [Header("Note Selection")]
    public bool prioritizeClosestNote = true;
    public float noteDistanceWeight = 1f;
    public float directionMatchWeight = 0.5f;

    [Header("Debug")]
    public bool enableDebugLogs = true;
    public bool drawDebugSwipe = true;
    public float debugDrawTime = 1f;

    // Input System Actions
    private PlayerInput playerInput;
    private InputAction touchPressAction;
    private InputAction touchPositionAction;
    
    // Touch tracking
    private Vector2 touchStartScreen;
    private Vector2 touchCurrentScreen;
    private Vector2 lastSwipeScreen;
    private bool swiping;
    private bool processedThisSwipe;
    private float currentSwipeVelocity = 0f;

    private void Awake()
    {
        Instance = this;

        if (cam == null)
            cam = Camera.main;
        
        // Setup Input System
        SetupInputSystem();
    }

    private void SetupInputSystem()
    {
        // Enable EnhancedTouch for better touch handling
        EnhancedTouchSupport.Enable();
        
        // Create input actions
        touchPressAction = new InputAction("TouchPress", InputActionType.Button);
        touchPositionAction = new InputAction("TouchPosition", InputActionType.Value);
        
        // Bind to primary touch
        touchPressAction.AddBinding("<Touchscreen>/primaryTouch/press");
        touchPositionAction.AddBinding("<Touchscreen>/primaryTouch/position");
        
        // Enable actions
        touchPressAction.Enable();
        touchPositionAction.Enable();
        
        // Setup callbacks
        touchPressAction.performed += OnTouchPress;
        touchPressAction.canceled += OnTouchRelease;
        touchPositionAction.performed += OnTouchMove;
    }

    private void OnEnable()
    {
        // Enable EnhancedTouch if not already enabled
        if (!EnhancedTouchSupport.enabled)
            EnhancedTouchSupport.Enable();
    }

    private void OnDisable()
    {
        if (touchPressAction != null)
        {
            touchPressAction.Disable();
            touchPositionAction.Disable();
        }
    }

    private void OnDestroy()
    {
        if (touchPressAction != null)
        {
            touchPressAction.Dispose();
            touchPositionAction.Dispose();
        }
        
        if (EnhancedTouchSupport.enabled)
            EnhancedTouchSupport.Disable();
    }

    private void Update()
    {
        // For mouse input (for editor testing)
        HandleMouse();
    }

    private void OnTouchPress(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            Vector2 touchPos = touchPositionAction.ReadValue<Vector2>();
            StartSwipe(touchPos);
        }
    }

    private void OnTouchRelease(InputAction.CallbackContext context)
    {
        if (swiping)
        {
            if (!processedThisSwipe)
            {
                CutResult result = ProcessSwipe(touchStartScreen, touchCurrentScreen);
                NOIZManager.Haptics.EndSwipeHaptic(result == CutResult.Success);
            }
            else
            {
                NOIZManager.Haptics.EndSwipeHaptic(true);
            }

            swiping = false;
        }
    }

    private void OnTouchMove(InputAction.CallbackContext context)
    {
        if (!swiping) return;
        
        Vector2 currentPos = context.ReadValue<Vector2>();
        touchCurrentScreen = currentPos;
        
        UpdateSwipeHaptic(currentPos);
        
        if (cutDuringDrag && !processedThisSwipe)
        {
            Vector2 segment = currentPos - lastSwipeScreen;
            
            if (segment.magnitude >= liveCutMinPixels)
            {
                CutResult result = ProcessSwipe(lastSwipeScreen, currentPos);
                lastSwipeScreen = currentPos;
                
                if (result == CutResult.Success)
                {
                    processedThisSwipe = true;
                    swiping = false;
                    NOIZManager.Haptics.EndSwipeHaptic(true);
                }
            }
        }
    }

    private void HandleMouse()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null) return;
        
        if (mouse.leftButton.wasPressedThisFrame)
        {
            StartSwipe(mouse.position.ReadValue());
        }
        
        if (mouse.leftButton.isPressed && swiping)
        {
            Vector2 currentPos = mouse.position.ReadValue();
            touchCurrentScreen = currentPos;
            
            UpdateSwipeHaptic(currentPos);
            
            if (cutDuringDrag && !processedThisSwipe)
            {
                Vector2 segment = currentPos - lastSwipeScreen;
                
                if (segment.magnitude >= liveCutMinPixels)
                {
                    CutResult result = ProcessSwipe(lastSwipeScreen, currentPos);
                    lastSwipeScreen = currentPos;
                    
                    if (result == CutResult.Success)
                    {
                        processedThisSwipe = true;
                        swiping = false;
                        NOIZManager.Haptics.EndSwipeHaptic(true);
                    }
                }
            }
        }
        
        if (mouse.leftButton.wasReleasedThisFrame && swiping)
        {
            if (!processedThisSwipe)
            {
                CutResult result = ProcessSwipe(touchStartScreen, mouse.position.ReadValue());
                NOIZManager.Haptics.EndSwipeHaptic(result == CutResult.Success);
            }
            else
            {
                NOIZManager.Haptics.EndSwipeHaptic(true);
            }
            
            swiping = false;
        }
    }

    // Alternative: Use EnhancedTouch for more detailed touch tracking
    private void HandleEnhancedTouch()
    {
        if (Touch.activeTouches.Count == 0) return;
        
        var touch = Touch.activeTouches[0];
        
        switch (touch.phase)
        {
            case TouchPhase.Began:
                StartSwipe(touch.screenPosition);
                break;
                
            case TouchPhase.Moved:
            case TouchPhase.Stationary:
                if (!swiping) return;
                
                touchCurrentScreen = touch.screenPosition;
                UpdateSwipeHaptic(touch.screenPosition);
                
                if (cutDuringDrag && !processedThisSwipe)
                {
                    Vector2 segment = touch.screenPosition - lastSwipeScreen;
                    
                    if (segment.magnitude >= liveCutMinPixels)
                    {
                        CutResult result = ProcessSwipe(lastSwipeScreen, touch.screenPosition);
                        lastSwipeScreen = touch.screenPosition;
                        
                        if (result == CutResult.Success)
                        {
                            processedThisSwipe = true;
                            swiping = false;
                            NOIZManager.Haptics.EndSwipeHaptic(true);
                        }
                    }
                }
                break;
                
            case TouchPhase.Ended:
            case TouchPhase.Canceled:
                if (!swiping) return;
                
                if (!processedThisSwipe)
                {
                    CutResult result = ProcessSwipe(touchStartScreen, touch.screenPosition);
                    NOIZManager.Haptics.EndSwipeHaptic(result == CutResult.Success);
                }
                else
                {
                    NOIZManager.Haptics.EndSwipeHaptic(true);
                }
                
                swiping = false;
                break;
        }
    }

    private void StartSwipe(Vector2 startPosition)
    {
        swiping = true;
        processedThisSwipe = false;
        touchStartScreen = startPosition;
        touchCurrentScreen = startPosition;
        lastSwipeScreen = startPosition;
        currentSwipeVelocity = 0f;
        
        if (NOIZManager.Settings.enableHaptics)
        {
            NOIZManager.Haptics.StartContinuousHaptic();
        }
        
        Log($"Swipe started at {startPosition}");
    }

    private void UpdateSwipeHaptic(Vector2 currentPosition)
    {
        if (!NOIZManager.Settings.enableHaptics || !swiping) return;
        
        Vector2 delta = currentPosition - touchCurrentScreen;
        float deltaTime = Time.deltaTime;
        
        if (deltaTime > 0f)
        {
            float instantVelocity = delta.magnitude / deltaTime;
            currentSwipeVelocity = Mathf.Lerp(currentSwipeVelocity, instantVelocity, 0.3f);
        }
        
        NOIZManager.Haptics.UpdateContinuousHaptic(currentSwipeVelocity);
    }

    private CutResult ProcessSwipe(Vector2 startScreen, Vector2 endScreen)
    {
        Vector2 swipe = endScreen - startScreen;
        float swipeLength = swipe.magnitude;
        
        Log($"Processing swipe, length: {swipeLength:F1}px");
        
        if (swipeLength < minSwipePixels)
        {
            Log("Swipe too short");
            NOIZManager.Haptics.PlaySliceFailHaptic();
            return CutResult.None;
        }
        
        Vector2 swipeDir = swipe / swipeLength;
        
        if (drawDebugSwipe)
            DrawScreenSwipeDebug(startScreen, endScreen, Color.green, debugDrawTime);
        
        List<PotentialCut> potentialCuts = new();
        
        for (int i = 0; i < LaneManager.Lanes.Count; i++)
        {
            Lane lane = LaneManager.Lanes[i];
            List<NoteMover> notesInLane = lane.GetAllCuttableNotesInOrder();
            
            foreach (NoteMover note in notesInLane)
            {
                if (note == null || !note.CanBeCut())
                    continue;
                
                Vector3 noteScreen3 = cam.WorldToScreenPoint(note.transform.position);
                if (noteScreen3.z <= 0f)
                    continue;
                
                Vector2 noteScreen = new Vector2(noteScreen3.x, noteScreen3.y);
                
                float alongSwipe = Vector2.Dot(noteScreen - startScreen, swipeDir);
                if (alongSwipe < -forwardScreenPadding || alongSwipe > swipeLength + forwardScreenPadding)
                {
                    continue;
                }
                
                float distToSwipe = DistancePointToSegment2D(noteScreen, startScreen, endScreen);
                float allowedDistance = maxHitDistancePixels + EstimateScreenRadius(note) + noteRadiusBias;
                
                if (distToSwipe > allowedDistance)
                {
                    continue;
                }
                
                float score = CalculateNoteScore(note, distToSwipe, alongSwipe, swipeLength, swipeDir);
                
                potentialCuts.Add(new PotentialCut
                {
                    note = note,
                    score = score,
                    distToSwipe = distToSwipe,
                    alongSwipe = alongSwipe
                });
            }
        }
        
        if (potentialCuts.Count == 0)
        {
            Log("No notes matched swipe");
            NOIZManager.Haptics.PlaySliceFailHaptic();
            return CutResult.None;
        }
        
        potentialCuts.Sort((a, b) => a.score.CompareTo(b.score));
        
        PotentialCut best = potentialCuts[0];
        NoteMover bestNote = best.note;
        NoteVisual visual = bestNote.GetComponent<NoteVisual>();
        
        Log($"Best note: {bestNote.name}, score: {best.score:F2}");
        
        Vector3 swipeWorldStart = ScreenToWorldAtDepth(startScreen, bestNote.transform.position);
        Vector3 swipeWorldEnd = ScreenToWorldAtDepth(endScreen, bestNote.transform.position);
        
        CutResult result = bestNote.TryCut(swipeWorldStart, swipeWorldEnd, swipeDir);
        
        if (result == CutResult.WrongDirection)
        {
            Log("Wrong direction");
            NOIZManager.Haptics.PlayWrongDirectionHaptic();
            
            if (visual != null)
                visual.PlayWrongDirectionFlash();

            NOIZEventHandler.WrongCut();
            
            return CutResult.WrongDirection;
        }
        
        if (result == CutResult.Success)
        {
            Log("Cut success!");
            return CutResult.Success;
        }
        
        NOIZManager.Haptics.PlaySliceFailHaptic();
        return CutResult.None;
    }

    private float CalculateNoteScore(NoteMover note, float distToSwipe, float alongSwipe, float swipeLength, Vector2 swipeDir)
    {
        float score = 0f;
        
        // Distance to swipe line
        score += distToSwipe * noteDistanceWeight;
        
        // Position along swipe (center is better)
        float centerBias = Mathf.Abs(Mathf.Clamp(alongSwipe, 0f, swipeLength) - swipeLength * 0.5f);
        score += centerBias * 0.1f;
        
        // Direction matching
        Vector2 targetDir = note.requiredCut switch
        {
            CutDirection.Up => Vector2.up,
            CutDirection.Down => Vector2.down,
            CutDirection.Left => Vector2.left,
            CutDirection.Right => Vector2.right,
            _ => Vector2.up
        };
        
        float angle = Vector2.Angle(targetDir, swipeDir.normalized);
        score += angle * directionMatchWeight;
        
        return score;
    }
    
    private Vector3 ScreenToWorldAtDepth(Vector2 screenPos, Vector3 worldReference)
    {
        float depth = cam.WorldToScreenPoint(worldReference).z;
        return cam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, depth));
    }
    
    private float EstimateScreenRadius(NoteMover note)
    {
        Collider col = note.GetComponentInChildren<Collider>();
        if (col == null)
            return 0f;
        
        Vector3 center = col.bounds.center;
        Vector3 edge = center + note.transform.right * Mathf.Max(col.bounds.extents.x, 0.25f);
        
        Vector3 centerScreen = cam.WorldToScreenPoint(center);
        Vector3 edgeScreen = cam.WorldToScreenPoint(edge);
        
        return Vector2.Distance(
            new Vector2(centerScreen.x, centerScreen.y),
            new Vector2(edgeScreen.x, edgeScreen.y)
        );
    }
    
    private float DistancePointToSegment2D(Vector2 point, Vector2 a, Vector2 b)
    {
        Vector2 ab = b - a;
        if (ab.sqrMagnitude < 0.0001f)
            return Vector2.Distance(point, a);
        
        float t = Vector2.Dot(point - a, ab) / ab.sqrMagnitude;
        t = Mathf.Clamp01(t);
        Vector2 closest = a + ab * t;
        return Vector2.Distance(point, closest);
    }
    
    private void DrawScreenSwipeDebug(Vector2 screenA, Vector2 screenB, Color color, float duration)
    {
        Vector3 a = ScreenToWorldAtDebugDepth(screenA);
        Vector3 b = ScreenToWorldAtDebugDepth(screenB);
        Debug.DrawLine(a, b, color, duration);
    }
    
    private Vector3 ScreenToWorldAtDebugDepth(Vector2 screenPos)
    {
        float depth = Mathf.Max(1f, cam.nearClipPlane + 2f);
        return cam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, depth));
    }
    
    private void Log(string msg)
    {
        if (enableDebugLogs)
            Debug.Log($"[SwipeCutter] {msg}");
    }
    
    private class PotentialCut
    {
        public NoteMover note;
        public float score;
        public float distToSwipe;
        public float alongSwipe;
    }
}