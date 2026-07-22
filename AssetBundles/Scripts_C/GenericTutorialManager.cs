using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace ColorSwipeGame
{
// ── Enums ─────────────────────────────────────────────────────────────────────

    
    public enum TutorialInputType
{
    Auto,              // advance after autoAdvanceDelay seconds
    Tap,               // any tap / click anywhere
    SwipeUp,
    SwipeDown,
    SwipeLeft,
    SwipeRight,
    SwipeUpAndDown,    // accepts up OR down; hand sweeps both ways
    SwipeLeftAndRight, // accepts left OR right; hand sweeps both ways
    Drag,              // drag from start target to end target
    TapTarget,         // tap on any of the listed tapTargets (or their children); hand auto-tracks center-most target
    ZigZag,            // hand paints back-and-forth strokes like a painter
    Scribble,          // same input as Drag; hand traces a Z-shaped stroke mimicking drawing
}

public enum DragDetectionMode
{
    Auto,          // any meaningful drag — no target required
    ByObjectName,  // finds GameObjects by name at runtime (works for UI or sprites)
    ManualTarget,  // assign TutorialTarget directly in Inspector
}

public enum PanelSlideDirection
{
    None,
    FromLeft,
    FromRight,
    FromUp,
    FromDown,
}

// ── TutorialTarget ────────────────────────────────────────────────────────────
// Unified reference that works for Canvas UI elements AND world/sprite objects.

[Serializable]
public class TutorialTarget
{
    [Tooltip("Canvas UI element (RectTransform)")]
    public RectTransform uiElement;

    [Tooltip("2D sprite or 3D world object (Transform)")]
    public Transform worldObject;

    [Tooltip("Fixed screen position fallback if both above are null")]
    public Vector2 fixedScreenPosition;

    public bool IsAssigned => uiElement != null || worldObject != null;

    /// <summary>Returns the screen-space position of this target using the supplied camera.</summary>
    public Vector2 GetScreenPos(Camera cam)
    {
        if (uiElement  != null)
            return RectTransformUtility.WorldToScreenPoint(cam, uiElement.position);
        if (worldObject != null)
            return cam != null
                ? (Vector2)cam.WorldToScreenPoint(worldObject.position)
                : (Vector2)worldObject.position;
        return fixedScreenPosition;
    }
}

// ── Step Data ─────────────────────────────────────────────────────────────────

[Serializable]
public class GenericTutorialStep
{
    [Header("Content")]
    [Tooltip("Editor-only label so you can identify steps in the list")]
    public string label;

    [TextArea(2, 5)]
    public string bodyText;

    [Tooltip("TGM_AudioManager voice line key (RuntimeAudioLoader clip name) to play when this step begins. Leave empty for silent steps. Played in code via TGM_AudioManager.Instance — no Inspector event wiring needed.")]
    public string voiceOverKey;

    [Tooltip("Optional typewriter effect for body text (0 = instant)")]
    public float typewriterSpeed = 0f;   // chars/sec; 0 = instant

    [Header("Character (optional)")]
    public bool   showCharacter;
    public Sprite characterSprite;

    [Header("Input Required to Advance")]
    public TutorialInputType inputType        = TutorialInputType.Tap;
    public float             autoAdvanceDelay = 2f;

    [Header("Drag Settings")]
    public DragDetectionMode dragMode             = DragDetectionMode.Auto;
    public string            dragStartObjectName;   // used by ByObjectName
    public string            dragEndObjectName;
    public TutorialTarget    dragStart;             // used by ManualTarget
    public TutorialTarget    dragEnd;
    [Tooltip("Screen-pixel acceptance radius around drag start/end")]
    public float             dragProximity = 120f;

    [Header("Tap Target (TapTarget input only)")]
    [Tooltip("Tap on any of these GameObjects (or their children) to advance. Hand dynamically tracks whichever is closest to screen center.")]
    public GameObject[] tapTargets;
    [Tooltip("Pixel offset applied to the hand while it tracks tap targets.")]
    public Vector2 tapTargetHandOffset;

    [Header("Hand Hint")]
    public bool showHandHint = true;
    [Tooltip("Where to place the hand at the start of this step. Works for both UI (RectTransform) and world sprites.")]
    public Transform handTarget;
    [Tooltip("Pixel offset applied on top of handTarget position (tune by eye).")]
    public Vector2 handTargetOffset;

    [Header("Behaviour")]
    [Tooltip("Advance this step the moment the player taps anywhere on screen, regardless of input type.")]
    public bool completeOnTapAnywhere;
    [Tooltip("Show the dialogue panel for this step. Uncheck to hide the panel (e.g. silent hint steps).")]
    public bool showDialogue = true;

    [Header("Highlight Target (optional pulse)")]
    [Tooltip("Pulse-highlight this UI element while the step is active")]
    public RectTransform highlightTarget;

    [Header("Panel Animation")]
    [Tooltip("Direction the panel slides IN from when this step begins")]
    public PanelSlideDirection slideIn  = PanelSlideDirection.None;
    [Tooltip("Direction the panel slides OUT to when this step ends")]
    public PanelSlideDirection slideOut = PanelSlideDirection.None;

    [Header("Game Time")]
    public bool slowDownGame = true;

    [Header("Events")]
    public UnityEvent onStepBegin;
    public UnityEvent onStepComplete;
}

// ── Manager ───────────────────────────────────────────────────────────────────

/// <summary>
/// Generic, Inspector-driven tutorial system that works with Canvas UI elements,
/// 2D sprites, and 3D world objects simultaneously.
///
/// PUBLIC API
///   GenericTutorialManager.Instance.StartTutorial()
///   GenericTutorialManager.Instance.ShowStep(int index)
///   GenericTutorialManager.Instance.NextStep()
///   GenericTutorialManager.Instance.SkipStep()
///   GenericTutorialManager.Instance.EndTutorial()
///
/// INSPECTOR WIRING
///   steps[]            — add GenericTutorialStep entries
///   panel              — root overlay GameObject (shown while tutorial active)
///   characterContainer — RectTransform that slides up from below screen
///   characterImage     — Image inside container for character sprite
///   bodyLabel          — TextMeshProUGUI for step text
///   handRect           — RectTransform of finger/hand icon
///   skipButton         — optional Button that calls SkipStep (leave null to disable)
///   referenceCamera    — camera used for world-to-screen conversions
///                        (leave null to use Camera.main)
/// </summary>
public class GenericTutorialManager : MonoBehaviour
{
    public static GenericTutorialManager Instance { get; private set; }

    // ── Inspector ─────────────────────────────────────────────────────────

    [Header("Steps")]
    [SerializeField] private GenericTutorialStep[] steps;

    [Header("Auto Start")]
    [SerializeField] private bool   autoStartOnEnable;
    [SerializeField] private bool   usePlayerPrefsGuard;
    [SerializeField] private string playerPrefsKey = "TutorialDone";

    [Header("UI References")]
    [SerializeField] private GameObject       panel;
    [Tooltip("The dialogue popup container (e.g. Tut_DilogueBG). Hidden when showDialogue=false while the rest of the panel stays active.")]
    [SerializeField] private GameObject       dialogueContainer;
    [SerializeField] private RectTransform    characterContainer;
    [SerializeField] private Image            characterImage;
    [SerializeField] private TextMeshProUGUI  bodyLabel;
    [Tooltip("Background behind bodyLabel — shown/hidden together with whether the current step has any text (e.g. hidden during silent idle-hint steps).")]
    [SerializeField] private GameObject       bodyBackground;
    [SerializeField] private RectTransform    handRect;
    [SerializeField] private Image            handImage;
    [SerializeField] private Button           skipButton;

    [Header("Camera (for world/sprite target conversion)")]
    [Tooltip("Leave null to use Camera.main automatically")]
    [SerializeField] private Camera referenceCamera;

    [Header("Character Popup")]
    [SerializeField] private float charHiddenY  = -500f;
    [SerializeField] private float charShownY   = 0f;
    [SerializeField] private float charPopSpeed = 10f;

    [Header("Slow Motion")]
    [SerializeField] private float slowdownDuration = 1.5f;
    [SerializeField] private float resumeDuration   = 0.6f;

    [Header("Hand Hint")]
    [SerializeField] private float handMoveDistance  = 160f;
    [SerializeField] private float handMoveDuration  = 0.65f;
    [SerializeField] private float handPauseDuration = 0.25f;

    [Header("ZigZag Hand")]
    [SerializeField] private float handZigZagStep = 55f;   // vertical distance between strokes
    [SerializeField] private int   handZigZagRows = 3;     // number of strokes per loop

    [Header("Swipe Detection")]
    [SerializeField] private float minSwipeDistance = 50f;

    [Header("Panel Slide")]
    [Tooltip("RectTransform to slide (leave null to use Tutorial Content, or panel root)")]
    [SerializeField] private RectTransform  panelSlideTarget;
    [SerializeField] private float          panelSlideDuration = 0.30f;
    [SerializeField] private AnimationCurve panelSlideInCurve  = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField] private AnimationCurve panelSlideOutCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Spotlight Overlay")]
    [Tooltip("Full-screen dark image inside TUT_Panel. Set color to black, alpha=0.")]
    [SerializeField] private Image         darkOverlay;
    [Tooltip("Content container inside TUT_Panel (everything except DarkOverlay). Lifted above the overlay when spotlight is active.")]
    [SerializeField] private RectTransform tutorialContent;
    [SerializeField] [Range(0f,1f)] private float spotlightAlpha     = 0.80f;
    [SerializeField]                private float spotlightFadeSpeed  = 6f;

[Header("Highlight Pulse")]
    [SerializeField] private float highlightPulseScale = 1.08f;
    [SerializeField] private float highlightPulseSpeed = 2f;

    [Header("Global Events")]
    public UnityEvent onTutorialStarted;
    public UnityEvent onTutorialEnded;

    // ── State ──────────────────────────────────────────────────────────────

    private int       currentStepIndex = -1;
    private bool      stepActive;
    private bool      skipRequested;

    private Coroutine slowMoRoutine;
    private Coroutine        spotlightFXRoutine;
    private GameObject        spotlightFXContainer;
    private Canvas           spotlightCanvas;     // temp Canvas added to highlighted element
    private Canvas           contentCanvas;       // temp Canvas added to tutorialContent
    private GraphicRaycaster contentRaycaster;

    private GraphicRaycaster spotlightRaycaster;
    private Coroutine        overlayRoutine;

    private Coroutine slideRoutine;
    private Coroutine handRoutine;
    private Coroutine stepRoutine;
    private Coroutine typewriterRoutine;
    private Coroutine highlightRoutine;
    private Coroutine charRoutine;

    // Resolved drag targets for the current step
    private TutorialTarget resolvedDragStart;
    private TutorialTarget resolvedDragEnd;

    [Header("Scene Context")]
    [Tooltip("Assign the Game_Scene root GameObject. Back button during an active tutorial in this scene dismisses the tutorial.")]
    [SerializeField] private GameObject gameSceneRoot;

    private Camera Cam => referenceCamera != null ? referenceCamera : Camera.main;

    // ── Lifecycle ──────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        ResetPanelImmediate();

        if (skipButton != null)
            skipButton.onClick.AddListener(SkipStep);

        if (!autoStartOnEnable) return;
        if (usePlayerPrefsGuard && PlayerPrefs.HasKey(playerPrefsKey)) return;
        StartTutorial();
    }

    private void Update()
    {
        if (!stepActive) return;
        if (!Input.GetKeyDown(KeyCode.Escape)) return;
        bool inGameScene = gameSceneRoot == null || gameSceneRoot.activeInHierarchy;
        if (inGameScene) HideImmediately();
    }

    // ── Public API ─────────────────────────────────────────────────────────

    public void StartTutorial()
    {
        onTutorialStarted?.Invoke();
        ShowStep(0);
    }

    public void NextStep() => ShowStep(currentStepIndex + 1);

    public void SkipStep()
    {
        skipRequested = true;
    }

    public void ShowStep(int index)
    {
        if (steps == null || steps.Length == 0) return;
        if (index < 0 || index >= steps.Length) { EndTutorial(); return; }
        if (stepRoutine != null) StopCoroutine(stepRoutine);
        stepRoutine = StartCoroutine(RunStep(index));
    }

    /// <summary>
    /// Same visual teardown as EndTutorial() but never touches the PlayerPrefs guard —
    /// for callers (like an idle-hint loop) that need to dismiss the overlay on demand
    /// without marking the real intro tutorial as permanently "done".
    /// </summary>
    public void HideImmediately()
    {
        if (stepRoutine != null) { StopCoroutine(stepRoutine); stepRoutine = null; }
        TGM_AudioManager.Instance?.StopVoice();
        StopHand();
        StopHighlight();
        StartCoroutine(HideTutorialRoutine());
    }

    public void EndTutorial()
    {
        if (usePlayerPrefsGuard) { PlayerPrefs.SetInt(playerPrefsKey, 1); PlayerPrefs.Save(); }
        if (stepRoutine != null) { StopCoroutine(stepRoutine); stepRoutine = null; }
        TGM_AudioManager.Instance?.StopVoice();
        StopHand();
        StopHighlight();
        StartCoroutine(HideTutorialRoutine());
        onTutorialEnded?.Invoke();
    }

    public bool IsTutorialActive => stepActive;
    public int  CurrentStepIndex => currentStepIndex;
    public bool IsSwipeBlocked   { get; private set; }
    public RectTransform HandRect => handRect;

    /// <summary>Fired at the start of each guided step (not ad-hoc idle steps) with the step index.</summary>
    public static event System.Action<int> OnStepBeganAt;

#if UNITY_EDITOR
    [ContextMenu("Clear All PlayerPrefs")]
    private void ClearAllPlayerPrefs()
    {
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
        Debug.Log("[GenericTutorialManager] Cleared all PlayerPrefs — the intro tutorial's PlayerPrefsGuard (and anything else stored) will re-trigger on next Play.");
    }
#endif
    public TutorialInputType CurrentInputType { get; private set; }

    /// <summary>
    /// Runs a step that isn't part of the guided steps[] sequence and never auto-advances
    /// afterward — for callers (like an idle-hint loop) that decide themselves when to move
    /// on. currentStepIndex/the guided chain are left untouched.
    /// </summary>
    public void ShowAdHocStep(GenericTutorialStep step)
    {
        if (stepRoutine != null) StopCoroutine(stepRoutine);
        stepRoutine = StartCoroutine(RunStepCore(step, autoAdvance: false, advanceFromIndex: -1));
    }

    // ── Step runner ────────────────────────────────────────────────────────

    private IEnumerator RunStep(int index)
    {
        currentStepIndex = index;
        yield return RunStepCore(steps[index], autoAdvance: true, advanceFromIndex: index);
    }

    private IEnumerator RunStepCore(GenericTutorialStep step, bool autoAdvance, int advanceFromIndex)
    {
        stepActive       = true;
        skipRequested    = false;
        CurrentInputType = step.inputType;

        // Always stop whatever VO is currently playing before this step's own — even if
        // this step has none — so quickly skipping through steps never overlaps two lines.
        TGM_AudioManager.Instance?.StopVoice();
        if (!string.IsNullOrEmpty(step.voiceOverKey))
            TGM_AudioManager.Instance?.PlayVoiceLine(step.voiceOverKey);

        step.onStepBegin?.Invoke();
        if (advanceFromIndex >= 0) OnStepBeganAt?.Invoke(advanceFromIndex);

        // Panel always on; only the dialogue container toggles
        panel.SetActive(true);
        if (dialogueContainer != null) dialogueContainer.SetActive(step.showDialogue);
        if (step.showDialogue) SetBodyText(step);

        // Character
        if (characterContainer != null)
        {
            if (charRoutine != null) StopCoroutine(charRoutine);
            if (step.showDialogue && step.showCharacter && step.characterSprite != null)
            {
                if (characterImage != null) characterImage.sprite = step.characterSprite;
                characterContainer.gameObject.SetActive(true);
                charRoutine = StartCoroutine(SlideCharacter(charShownY));
            }
            else
            {
                charRoutine = StartCoroutine(SlideCharacter(charHiddenY, hideAfter: true));
            }
        }

        // Slow-mo
        if (step.slowDownGame) SetSlowMo(true);

        // Highlight + spotlight
        StopHighlight();
        if (step.highlightTarget != null)
        {
            highlightRoutine = StartCoroutine(PulseHighlight(step.highlightTarget));
            ShowSpotlight(step.highlightTarget);
        }

        // Resolve drag targets before animating hand (hand uses them for direction)
        resolvedDragStart = null;
        resolvedDragEnd   = null;
        if (step.inputType == TutorialInputType.Drag || step.inputType == TutorialInputType.Scribble)
            ResolveDragRefs(step);

        // Hand
        StopHand();
        if (handRect != null && step.showHandHint)
        {
            PlaceHandAtTarget(step.handTarget, step.handTargetOffset);
            handRoutine = StartCoroutine(AnimateHand(step));
        }
        else if (handRect != null)
            handRect.gameObject.SetActive(false);

        // Wait for trigger
        yield return WaitForTrigger(step);

        step.onStepComplete?.Invoke();

        // Resume time
        if (step.slowDownGame) SetSlowMo(false);

        StopHand();
        StopHighlight();
        stepActive = false;

        // Advance — only for steps that are part of the guided steps[] chain.
        if (autoAdvance) ShowStep(advanceFromIndex + 1);
    }

    // ── Text ───────────────────────────────────────────────────────────────

    private void SetBodyText(GenericTutorialStep step)
    {
        bool hasText = !string.IsNullOrEmpty(step.bodyText);
        if (bodyBackground != null) bodyBackground.SetActive(hasText);

        if (bodyLabel == null) return;
        if (typewriterRoutine != null) StopCoroutine(typewriterRoutine);

        if (step.typewriterSpeed > 0f)
            typewriterRoutine = StartCoroutine(TypewriterRoutine(step.bodyText, step.typewriterSpeed));
        else
            bodyLabel.text = step.bodyText;
    }

    private IEnumerator TypewriterRoutine(string text, float charsPerSec)
    {
        bodyLabel.text = "";
        float delay = 1f / charsPerSec;
        foreach (char c in text)
        {
            bodyLabel.text += c;
            yield return new WaitForSecondsRealtime(delay);
        }
    }

    // ── Input waiting ──────────────────────────────────────────────────────

    private IEnumerator WaitForTrigger(GenericTutorialStep step)
    {
        yield return null; // skip frame so the press that triggered ShowStep doesn't count

        if (step.completeOnTapAnywhere)
        {
            yield return WaitForTap();
            yield break;
        }

        switch (step.inputType)
        {
            case TutorialInputType.Auto:
                float elapsed = 0f;
                while (elapsed < step.autoAdvanceDelay && !skipRequested)
                {
                    elapsed += Time.unscaledDeltaTime;
                    yield return null;
                }
                break;

            case TutorialInputType.Tap:
                yield return WaitForTap();
                break;

            case TutorialInputType.SwipeUp:
            case TutorialInputType.SwipeDown:
            case TutorialInputType.SwipeLeft:
            case TutorialInputType.SwipeRight:
            case TutorialInputType.SwipeUpAndDown:
            case TutorialInputType.SwipeLeftAndRight:
                yield return WaitForSwipe(step.inputType);
                break;

            case TutorialInputType.Drag:
            case TutorialInputType.Scribble:
                yield return WaitForDrag(step);
                break;

            case TutorialInputType.TapTarget:
                yield return WaitForTapOnTarget(step);
                break;

            case TutorialInputType.ZigZag:
                yield return WaitForTap();
                break;
        }
    }

    // ── Tap ────────────────────────────────────────────────────────────────

    private IEnumerator WaitForTap()
    {
        while (!skipRequested && !AnyPressDown()) yield return null;
    }

    // ── Swipe ──────────────────────────────────────────────────────────────

    private IEnumerator WaitForSwipe(TutorialInputType dir)
    {
        Vector2 startPos = Vector2.zero;
        bool    tracking = false;

        while (!skipRequested)
        {
            if (AnyPressDown()) { startPos = CurrentPressPos(); tracking = true; }
            if (tracking && AnyPressUp())
            {
                Vector2 delta = CurrentPressPos() - startPos;
                if (delta.magnitude >= minSwipeDistance && MatchesSwipeDir(delta, dir)) yield break;
                tracking = false;
            }
            yield return null;
        }
    }

    private static bool MatchesSwipeDir(Vector2 d, TutorialInputType dir)
    {
        switch (dir)
        {
            case TutorialInputType.SwipeUp:           return d.y >  Mathf.Abs(d.x);
            case TutorialInputType.SwipeDown:         return d.y < -Mathf.Abs(d.x);
            case TutorialInputType.SwipeLeft:         return d.x < -Mathf.Abs(d.y);
            case TutorialInputType.SwipeRight:        return d.x >  Mathf.Abs(d.y);
            case TutorialInputType.SwipeUpAndDown:    return Mathf.Abs(d.y) > Mathf.Abs(d.x);
            case TutorialInputType.SwipeLeftAndRight: return Mathf.Abs(d.x) > Mathf.Abs(d.y);
        }
        return false;
    }

    // ── Drag ───────────────────────────────────────────────────────────────

    private void ResolveDragRefs(GenericTutorialStep step)
    {
        switch (step.dragMode)
        {
            case DragDetectionMode.ManualTarget:
                resolvedDragStart = step.dragStart;
                resolvedDragEnd   = step.dragEnd;
                break;

            case DragDetectionMode.ByObjectName:
                resolvedDragStart = ResolveTargetByName(step.dragStartObjectName);
                resolvedDragEnd   = ResolveTargetByName(step.dragEndObjectName);
                break;

            case DragDetectionMode.Auto:
                break; // any meaningful drag accepted
        }
    }

    private static TutorialTarget ResolveTargetByName(string objectName)
    {
        if (string.IsNullOrEmpty(objectName)) return null;
        var go = GameObject.Find(objectName);
        if (go == null) return null;

        var target = new TutorialTarget();
        var rt = go.GetComponent<RectTransform>();
        if (rt != null)
            target.uiElement = rt;
        else
            target.worldObject = go.transform;
        return target;
    }

    private IEnumerator WaitForDrag(GenericTutorialStep step)
    {
        bool    dragging  = false;
        Vector2 dragStart = Vector2.zero;

        while (!skipRequested)
        {
            if (!dragging && AnyPressDown())
            {
                Vector2 pos = CurrentPressPos();
                bool nearStart = resolvedDragStart == null || !resolvedDragStart.IsAssigned
                    || Vector2.Distance(pos, resolvedDragStart.GetScreenPos(Cam)) < step.dragProximity;

                if (nearStart) { dragging = true; dragStart = pos; }
            }

            if (dragging && AnyPressUp())
            {
                Vector2 pos = CurrentPressPos();
                bool success;
                if (resolvedDragEnd != null && resolvedDragEnd.IsAssigned)
                    success = Vector2.Distance(pos, resolvedDragEnd.GetScreenPos(Cam)) < step.dragProximity;
                else
                    success = Vector2.Distance(pos, dragStart) >= minSwipeDistance;

                if (success) yield break;
                dragging = false;
            }

            yield return null;
        }
    }

    // ── TapTarget ──────────────────────────────────────────────────────────────

    private IEnumerator WaitForTapOnTarget(GenericTutorialStep step)
    {
        Vector2 pressDownPos = Vector2.zero;
        bool pressing = false;
        bool pressedOnTarget = false;

        while (!skipRequested)
        {
            if (!pressing && AnyPressDown())
            {
                pressDownPos = CurrentPressPos();
                // Check target at press-DOWN while targets are still in the scene
                pressedOnTarget = IsPressOnAnyTarget(step.tapTargets, pressDownPos);
                pressing = true;
            }

            if (pressing && AnyPressUp())
            {
                Vector2 releasePos = CurrentPressPos();
                pressing = false;
                if (pressedOnTarget && Vector2.Distance(releasePos, pressDownPos) < minSwipeDistance)
                    yield break;
                pressedOnTarget = false;
            }

            yield return null;
        }
    }

    private bool IsPressOnAnyTarget(GameObject[] targets, Vector2 screenPos)
    {
        if (targets == null || targets.Length == 0) return false;
        foreach (var go in targets)
        {
            if (go == null) continue;
            if (IsScreenPosOnGameObject(go, screenPos)) return true;
        }
        return false;
    }

    private bool IsScreenPosOnGameObject(GameObject go, Vector2 screenPos)
    {
        // UI — check all RectTransforms in hierarchy
        foreach (var rt in go.GetComponentsInChildren<RectTransform>(false))
        {
            Camera cam = ResolveCameraFor(rt.transform, Cam);
            if (RectTransformUtility.RectangleContainsScreenPoint(rt, screenPos, cam))
                return true;
        }

        // World / 2D — check Collider2D bounds using the correct camera for this object
        Camera worldCam = ResolveCameraFor(go.transform, Cam) ?? Cam;
        Vector2 worldPos = worldCam != null ? (Vector2)worldCam.ScreenToWorldPoint(screenPos) : screenPos;
        foreach (var col in go.GetComponentsInChildren<Collider2D>(true))
            if (col.OverlapPoint(worldPos)) return true;

        return false;
    }

    /// <summary>Returns the transform in targets[] whose screen position is closest to screen center.
    /// Only considers targets that are currently on-screen.</summary>
    private Transform GetTargetNearestScreenCenter(GameObject[] targets)
    {
        if (targets == null) return null;
        Vector2 center = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
        Transform best = null;
        float bestDist = float.MaxValue;

        foreach (var go in targets)
        {
            if (go == null || !go.activeInHierarchy) continue;
            Camera sourceCam = ResolveCameraFor(go.transform, Cam);
            Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(sourceCam, go.transform.position);
            if (screenPos.x < 0 || screenPos.x > Screen.width || screenPos.y < 0 || screenPos.y > Screen.height)
                continue;
            float dist = Vector2.Distance(screenPos, center);
            if (dist < bestDist) { bestDist = dist; best = go.transform; }
        }
        return best;
    }

    // ── Unified input helpers (mouse + touch) ──────────────────────────────

    private static bool AnyPressDown()
    {
        if (Input.GetMouseButtonDown(0)) return true;
        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began) return true;
        return false;
    }

    private static bool AnyPressUp()
    {
        if (Input.GetMouseButtonUp(0)) return true;
        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Ended) return true;
        return false;
    }

    private static Vector2 CurrentPressPos()
    {
        if (Input.touchCount > 0) return Input.GetTouch(0).position;
        return Input.mousePosition;
    }

    // ── Slow-mo ────────────────────────────────────────────────────────────

    private void SetSlowMo(bool slow)
    {
        if (slowMoRoutine != null) StopCoroutine(slowMoRoutine);
        float target = slow ? 0f : 1f;
        float dur    = slow ? slowdownDuration : resumeDuration;
        slowMoRoutine = StartCoroutine(LerpTimeScale(Time.timeScale, target, dur));
    }

    private IEnumerator LerpTimeScale(float from, float to, float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            Time.timeScale = Mathf.Lerp(from, to, Mathf.Clamp01(t / duration));
            yield return null;
        }
        Time.timeScale = to;
    }

    // ── Panel slide ────────────────────────────────────────────────────────

    private RectTransform GetSlideRT()
    {
        if (panelSlideTarget != null) return panelSlideTarget;
        if (tutorialContent  != null) return tutorialContent;
        return panel != null ? panel.GetComponent<RectTransform>() : null;
    }

    private IEnumerator SlidePanel(PanelSlideDirection dir, bool entering)
    {
        RectTransform rt = GetSlideRT();
        if (rt == null || dir == PanelSlideDirection.None) yield break;

        Vector2 rest   = rt.anchoredPosition;
        Vector2 offset = GetSlideOffset(rt, dir);
        Vector2 from   = entering ? rest + offset : rest;
        Vector2 to     = entering ? rest          : rest + offset;
        AnimationCurve curve = entering ? panelSlideInCurve : panelSlideOutCurve;

        float t = 0f;
        while (t < panelSlideDuration)
        {
            t += Time.unscaledDeltaTime;
            rt.anchoredPosition = Vector2.LerpUnclamped(from, to,
                curve.Evaluate(Mathf.Clamp01(t / panelSlideDuration)));
            yield return null;
        }
        rt.anchoredPosition = to;
        if (!entering) rt.anchoredPosition = rest;
    }

    private static Vector2 GetSlideOffset(RectTransform rt, PanelSlideDirection dir)
    {
        var  canvas = rt.GetComponentInParent<Canvas>();
        Rect cr     = canvas != null
            ? ((RectTransform)canvas.transform).rect
            : new Rect(-960f, -540f, 1920f, 1080f);
        switch (dir)
        {
            case PanelSlideDirection.FromLeft:  return new Vector2(-cr.width,  0f);
            case PanelSlideDirection.FromRight: return new Vector2( cr.width,  0f);
            case PanelSlideDirection.FromUp:    return new Vector2(0f,  cr.height);
            case PanelSlideDirection.FromDown:  return new Vector2(0f, -cr.height);
            default: return Vector2.zero;
        }
    }

    // ── Character popup ────────────────────────────────────────────────────

    private IEnumerator SlideCharacter(float targetY, bool hideAfter = false)
    {
        if (characterContainer == null) yield break;
        characterContainer.gameObject.SetActive(true);

        while (Mathf.Abs(characterContainer.anchoredPosition.y - targetY) > 0.5f)
        {
            var pos = characterContainer.anchoredPosition;
            pos.y = Mathf.Lerp(pos.y, targetY, Time.unscaledDeltaTime * charPopSpeed);
            characterContainer.anchoredPosition = pos;
            yield return null;
        }

        var fp = characterContainer.anchoredPosition; fp.y = targetY;
        characterContainer.anchoredPosition = fp;
        if (hideAfter) characterContainer.gameObject.SetActive(false);
    }

    // ── Hand animation ─────────────────────────────────────────────────────

    private void StopHand()
    {
        if (handRoutine != null) { StopCoroutine(handRoutine); handRoutine = null; }
        if (handRect != null) handRect.gameObject.SetActive(false);
    }

    private IEnumerator AnimateHand(GenericTutorialStep step)
    {
        handRect.gameObject.SetActive(true);
        Vector2 restPos = handRect.anchoredPosition;

        bool bidirectional = step.inputType == TutorialInputType.SwipeUpAndDown ||
                             step.inputType == TutorialInputType.SwipeLeftAndRight;

        if (bidirectional)
        {
            Vector2 axis = step.inputType == TutorialInputType.SwipeUpAndDown
                ? Vector2.up : Vector2.right;
            Vector2 posA = restPos + axis * handMoveDistance;
            Vector2 posB = restPos - axis * handMoveDistance;

            // Ease from center to one end first, then oscillate A↔B smoothly — no snapping.
            yield return SweepHand(restPos, posA - restPos);
            while (stepActive)
            {
                yield return new WaitForSecondsRealtime(handPauseDuration);
                yield return SweepHand(posA, posB - posA);
                yield return new WaitForSecondsRealtime(handPauseDuration);
                yield return SweepHand(posB, posA - posB);
            }
        }
        else if (step.inputType == TutorialInputType.Drag &&
                 resolvedDragStart != null && resolvedDragStart.IsAssigned &&
                 resolvedDragEnd   != null && resolvedDragEnd.IsAssigned)
        {
            // Derive hand positions from screen positions of actual targets
            // Convert screen → canvas-local so the hand moves correctly in UI space
            Vector2 startCanvas = ScreenToHandCanvas(resolvedDragStart.GetScreenPos(Cam));
            Vector2 endCanvas   = ScreenToHandCanvas(resolvedDragEnd.GetScreenPos(Cam));

            while (stepActive)
            {
                handRect.anchoredPosition = startCanvas;
                yield return SweepHand(startCanvas, endCanvas - startCanvas);
                yield return new WaitForSecondsRealtime(handPauseDuration);
                handRect.anchoredPosition = startCanvas;
                yield return new WaitForSecondsRealtime(handPauseDuration);
            }
        }
        else if (step.inputType == TutorialInputType.ZigZag)
        {
            // Z-shape: top-left → top-right → diagonal to bottom-left → bottom-left → bottom-right
            float halfW = handMoveDistance;
            float halfH = handZigZagStep * 0.5f;

            Vector2 topLeft     = restPos + new Vector2(-halfW,  halfH);
            Vector2 topRight    = restPos + new Vector2( halfW,  halfH);
            Vector2 bottomLeft  = restPos + new Vector2(-halfW, -halfH);
            Vector2 bottomRight = restPos + new Vector2( halfW, -halfH);

            while (stepActive)
            {
                handRect.anchoredPosition = topLeft;
                yield return new WaitForSecondsRealtime(handPauseDuration);

                // Top stroke: left → right
                yield return SweepHand(topLeft, topRight - topLeft);
                yield return new WaitForSecondsRealtime(handPauseDuration);

                // Diagonal: top-right → bottom-left
                yield return SweepHand(topRight, bottomLeft - topRight);
                yield return new WaitForSecondsRealtime(handPauseDuration);

                // Bottom stroke: left → right
                yield return SweepHand(bottomLeft, bottomRight - bottomLeft);
                yield return new WaitForSecondsRealtime(handPauseDuration);

                // Smooth return to start
                Vector2 endPos = handRect.anchoredPosition;
                float rt = 0f, retDur = handMoveDuration * 0.5f;
                while (rt < retDur && stepActive)
                {
                    rt += Time.unscaledDeltaTime;
                    handRect.anchoredPosition = Vector2.LerpUnclamped(endPos, topLeft, Mathf.SmoothStep(0f, 1f, rt / retDur));
                    yield return null;
                }
                yield return new WaitForSecondsRealtime(handPauseDuration);
            }
        }
        else if (step.inputType == TutorialInputType.Scribble)
        {
            // Z-shaped stroke mimicking a natural drawing scribble:
            // top-left → top-right, diagonal down to bottom-left, → bottom-right.
            float w = handMoveDistance;
            float h = handMoveDistance * 0.7f;
            Vector2[] zPath =
            {
                restPos + new Vector2(-w * 0.5f,  h * 0.5f),
                restPos + new Vector2( w * 0.5f,  h * 0.5f),
                restPos + new Vector2(-w * 0.5f, -h * 0.5f),
                restPos + new Vector2( w * 0.5f, -h * 0.5f),
            };

            while (stepActive)
            {
                handRect.anchoredPosition = zPath[0];
                yield return SweepHandAlongPath(zPath);
                yield return new WaitForSecondsRealtime(handPauseDuration);
            }
        }
        else if (step.inputType == TutorialInputType.TapTarget && step.tapTargets != null && step.tapTargets.Length > 0)
        {
            // Mirror TutorialHandPositioner.Place() exactly:
            // resolve sourceCam from the target's canvas and destCam from the hand's parent canvas
            // so both Screen-Space-Overlay and Screen-Space-Camera canvases are handled correctly.
            Camera fallback = Cam;
            var parentRT = handRect.parent as RectTransform;
            Camera destCam = parentRT != null ? ResolveCameraFor(handRect, fallback) : fallback;

            while (stepActive)
            {
                Transform target = GetTargetNearestScreenCenter(step.tapTargets);
                if (target != null && parentRT != null)
                {
                    Camera sourceCam = ResolveCameraFor(target, fallback);
                    Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(sourceCam, target.position);
                    RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRT, screenPos, destCam, out Vector2 local);
                    handRect.anchoredPosition = local + step.tapTargetHandOffset;
                }
                yield return null;
            }
        }
        else
        {
            Vector2 dir = GetHandDir(step.inputType) * handMoveDistance;
            while (stepActive)
            {
                yield return SweepHand(restPos, dir);
                yield return new WaitForSecondsRealtime(handPauseDuration);
                handRect.anchoredPosition = restPos;
                yield return new WaitForSecondsRealtime(handPauseDuration);
            }
        }

        handRect.anchoredPosition = restPos;
        handRect.gameObject.SetActive(false);
    }

    private IEnumerator SweepHand(Vector2 origin, Vector2 offset)
    {
        float t = 0f;
        while (t < handMoveDuration && stepActive)
        {
            t += Time.unscaledDeltaTime;
            handRect.anchoredPosition = origin + offset * Mathf.SmoothStep(0f, 1f, t / handMoveDuration);
            yield return null;
        }
    }

    /// <summary>
    /// Moves the hand along a polyline at pen-like speed: one gentle ease over the whole
    /// stroke (no stop at each corner), so it reads as continuous drawing rather than
    /// separate swipes.
    /// </summary>
    private IEnumerator SweepHandAlongPath(Vector2[] points)
    {
        float totalLen = 0f;
        for (int i = 1; i < points.Length; i++)
            totalLen += Vector2.Distance(points[i - 1], points[i]);
        if (totalLen <= 0f) yield break;

        // Scale duration so longer strokes take proportionally longer than a single sweep
        float duration = handMoveDuration * (totalLen / Mathf.Max(handMoveDistance, 1f));

        float t = 0f;
        while (t < duration && stepActive)
        {
            t += Time.unscaledDeltaTime;
            float dist = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / duration)) * totalLen;

            // Walk the polyline to the point 'dist' along it
            Vector2 pos = points[points.Length - 1];
            float acc = 0f;
            for (int i = 1; i < points.Length; i++)
            {
                float seg = Vector2.Distance(points[i - 1], points[i]);
                if (dist <= acc + seg)
                {
                    pos = Vector2.Lerp(points[i - 1], points[i], seg > 0f ? (dist - acc) / seg : 1f);
                    break;
                }
                acc += seg;
            }
            handRect.anchoredPosition = pos;
            yield return null;
        }
    }

    /// <summary>Snaps the hand to a world or UI target before the step animation begins.</summary>
    private void PlaceHandAtTarget(Transform target, Vector2 offset = default)
    {
        if (target == null || handRect == null) return;
        var parentRT = handRect.parent as RectTransform;
        if (parentRT == null) return;
        Camera fallback = Cam;
        Camera sourceCam = ResolveCameraFor(target, fallback);
        Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(sourceCam, target.position);
        Camera destCam = ResolveCameraFor(handRect, fallback);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRT, screenPos, destCam, out Vector2 local);
        handRect.anchoredPosition = local + offset;
    }

    private static Camera ResolveCameraFor(Transform t, Camera fallback)
    {
        Canvas canvas = t.GetComponentInParent<Canvas>();
        if (canvas == null) return fallback;
        return canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
    }

    /// <summary>Converts a screen position to the anchored-position space of handRect's parent canvas.</summary>
    private Vector2 ScreenToHandCanvas(Vector2 screenPos)
    {
        var parentRT = handRect.parent as RectTransform;
        if (parentRT == null) return screenPos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parentRT, screenPos, Cam, out Vector2 local);
        return local;
    }

    private static Vector2 GetHandDir(TutorialInputType type)
    {
        switch (type)
        {
            case TutorialInputType.SwipeUp:    return Vector2.up;
            case TutorialInputType.SwipeDown:  return Vector2.down;
            case TutorialInputType.SwipeLeft:  return Vector2.left;
            case TutorialInputType.SwipeRight: return Vector2.right;
            case TutorialInputType.Drag:       return Vector2.right;
            default:                           return Vector2.zero;
        }
    }

    // ── Highlight pulse ────────────────────────────────────────────────────

private void StopHighlight()
    {
        if (highlightRoutine != null) { StopCoroutine(highlightRoutine); highlightRoutine = null; }
        HideSpotlight();
    }

private IEnumerator PulseHighlight(RectTransform target)
    {
        if (target == null) yield break;
        Vector3 baseScale = target.localScale;

        while (stepActive)
        {
            float s = 1f + (highlightPulseScale - 1f) *
                      Mathf.Abs(Mathf.Sin(Time.unscaledTime * highlightPulseSpeed * Mathf.PI));
            target.localScale = baseScale * s;
            yield return null;
        }

        target.localScale = baseScale;
    }

    // ── Spotlight (darken all, lift highlighted element) ────────────────────

private void ShowSpotlight(RectTransform target)
    {
        if (overlayRoutine != null) StopCoroutine(overlayRoutine);
        overlayRoutine = StartCoroutine(FadeOverlay(spotlightAlpha, hide: false));

        // Lift highlighted element above overlay
        if (target != null && target.GetComponent<Canvas>() == null)
        {
            spotlightCanvas = target.gameObject.AddComponent<Canvas>();
            spotlightCanvas.overrideSorting = true;
            spotlightCanvas.sortingOrder    = 999;
            spotlightRaycaster = target.gameObject.AddComponent<GraphicRaycaster>();
        }

        // Lift tutorial text/UI above overlay
        if (tutorialContent != null && tutorialContent.GetComponent<Canvas>() == null)
        {
            contentCanvas = tutorialContent.gameObject.AddComponent<Canvas>();
            contentCanvas.overrideSorting = true;
            contentCanvas.sortingOrder    = 998;
            contentRaycaster = tutorialContent.gameObject.AddComponent<GraphicRaycaster>();
        }

        if (spotlightFXRoutine   != null) StopCoroutine(spotlightFXRoutine);
        if (spotlightFXContainer != null) Destroy(spotlightFXContainer);
        if (target != null)
            spotlightFXRoutine = StartCoroutine(RunSpotlightFX(target));
    }

private void HideSpotlight()
    {
        if (overlayRoutine != null) StopCoroutine(overlayRoutine);
        overlayRoutine = StartCoroutine(FadeOverlay(0f, hide: true));

        if (spotlightFXRoutine   != null) { StopCoroutine(spotlightFXRoutine);   spotlightFXRoutine   = null; }
        if (spotlightFXContainer != null) { Destroy(spotlightFXContainer);        spotlightFXContainer = null; }

        // Raycaster must be destroyed before its Canvas dependency
        if (spotlightRaycaster != null) { Destroy(spotlightRaycaster); spotlightRaycaster = null; }
        if (spotlightCanvas    != null) { Destroy(spotlightCanvas);    spotlightCanvas    = null; }
        if (contentRaycaster   != null) { Destroy(contentRaycaster);   contentRaycaster   = null; }
        if (contentCanvas      != null) { Destroy(contentCanvas);      contentCanvas      = null; }
    }

private IEnumerator FadeOverlay(float targetAlpha, bool hide)
    {
        if (darkOverlay == null) yield break;
        darkOverlay.gameObject.SetActive(true);
        Color c     = darkOverlay.color;
        float start = c.a;
        float dur   = spotlightFadeSpeed > 0f ? Mathf.Abs(targetAlpha - start) / spotlightFadeSpeed : 0f;
        float e     = 0f;
        while (e < dur)
        {
            e += Time.unscaledDeltaTime;
            c.a = Mathf.Lerp(start, targetAlpha, e / dur);
            darkOverlay.color = c;
            yield return null;
        }
        c.a = targetAlpha; darkOverlay.color = c;
        if (hide) darkOverlay.gameObject.SetActive(false);
    }

    // ── Spotlight FX ─────────────────────────────────────────────────────────

private IEnumerator RunSpotlightFX(RectTransform target)
    {
        var fxGO = new GameObject("SpotlightFX");
        spotlightFXContainer = fxGO;
        fxGO.transform.SetParent(target, false);
        fxGO.transform.SetAsFirstSibling();
        var fxRT = fxGO.AddComponent<RectTransform>();
        fxRT.anchorMin = fxRT.anchorMax = new Vector2(0.5f, 0.5f);
        fxRT.anchoredPosition = Vector2.zero;
        fxRT.sizeDelta        = target.sizeDelta;

        // 4 arrows (TMP) bouncing inward from each direction
        float baseArrowDist = Mathf.Max(target.sizeDelta.x, target.sizeDelta.y) * 1.4f;
        var arrowDirs  = new Vector2[] { Vector2.up, Vector2.down, Vector2.left, Vector2.right };
        var arrowChars = new string[]  { "▼", "▲", "►", "◄" };
        var arrowRTs   = new RectTransform[4];
        var arrowTexts = new TMPro.TextMeshProUGUI[4];
        for (int i = 0; i < 4; i++)
        {
            var aGO = new GameObject("Arrow" + i);
            aGO.transform.SetParent(fxRT, false);
            var aRT = aGO.AddComponent<RectTransform>();
            aRT.anchorMin = aRT.anchorMax = new Vector2(0.5f, 0.5f);
            aRT.sizeDelta = new Vector2(70f, 70f);
            aRT.anchoredPosition = arrowDirs[i] * baseArrowDist;
            var tmp = aGO.AddComponent<TMPro.TextMeshProUGUI>();
            tmp.text      = arrowChars[i];
            tmp.fontSize  = 52f;
            tmp.color     = new Color(1f, 0.9f, 0f, 0f);
            tmp.alignment = TMPro.TextAlignmentOptions.Center;
            arrowRTs[i]   = aRT;
            arrowTexts[i] = tmp;
        }

        // Entry pop on target: 0 -> 1.5 -> 0.85 -> 1.1 -> 1.0
        Vector3 baseScale = target.localScale;
        float[] popS = { 0f, 1.5f, 0.85f, 1.1f, 1.0f };
        float[] popT = { 0.04f, 0.14f, 0.10f, 0.09f, 0.07f };
        for (int i = 0; i < popS.Length; i++)
        {
            float from = target.localScale.x / (baseScale.x > 0f ? baseScale.x : 1f);
            float elapsed = 0f;
            while (elapsed < popT[i])
            {
                elapsed += Time.unscaledDeltaTime;
                float s = Mathf.Lerp(from, popS[i], elapsed / popT[i]);
                target.localScale = baseScale * s;
                yield return null;
            }
        }
        target.localScale = baseScale;

        // Continuous loop: pulse target scale + animate arrows
        float t = 0f;
        while (true)
        {
            t += Time.unscaledDeltaTime;

            // Gentle target float
            float targetPulse = 1f + 0.10f * Mathf.Sin(t * Mathf.PI * 1.8f);
            target.localScale = baseScale * targetPulse;

            // Arrows bounce inward/outward
            float arrowPhase = Mathf.Sin(t * Mathf.PI * 2.4f);
            float arrowDist  = baseArrowDist * (1f - 0.22f * (arrowPhase * 0.5f + 0.5f));
            float arrowAlpha = 0.55f + 0.45f * (arrowPhase * 0.5f + 0.5f);
            float arrowScale = 0.9f + 0.2f  * (arrowPhase * 0.5f + 0.5f);
            for (int i = 0; i < 4; i++)
            {
                arrowRTs[i].anchoredPosition = arrowDirs[i] * arrowDist;
                arrowRTs[i].localScale       = Vector3.one * arrowScale;
                arrowTexts[i].color          = new Color(1f, 0.92f, 0.1f, arrowAlpha);
            }

            yield return null;
        }
    }

    private static Image CreateGlowCircle(RectTransform parent, Vector2 size, Color color, string goName)
    {
        var go  = new GameObject(goName);
        go.transform.SetParent(parent, false);
        go.transform.SetAsFirstSibling();
        var rt  = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta        = size;
        var img = go.AddComponent<Image>();
        img.sprite = GetRadialGradientSprite();
        img.color  = color;
        img.raycastTarget = false;
        return img;
    }

    // Ring = large circle minus inner cutout approximated by two layers;
    // simpler: just a slightly larger gradient circle (looks like a halo).
    private static Image CreateRing(RectTransform parent, Vector2 outerSize, Vector2 innerSize,
                                    Color color, string goName)
    {
        // Outer ring image
        var img = CreateGlowCircle(parent, outerSize, color, goName);
        // Layer a dark one on top to eat the centre, making it look like a ring
        var hole  = CreateGlowCircle(img.GetComponent<RectTransform>(), innerSize,
                                     new Color(0f, 0f, 0f, 0f), goName + "_Hole");
        hole.raycastTarget = false;
        return img;
    }

    private static void SetImgAlpha(Image img, float a)
    {
        if (img == null) return;
        var c = img.color; c.a = a; img.color = c;
    }

    // Cached soft radial gradient sprite (white circle, quadratic alpha falloff)
    private static Sprite _radialGradientSprite;
    private static Sprite GetRadialGradientSprite()
    {
        if (_radialGradientSprite != null) return _radialGradientSprite;
        const int sz = 128;
        var tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;
        float c = sz * 0.5f;
        var pixels = new Color32[sz * sz];
        for (int y = 0; y < sz; y++)
            for (int x = 0; x < sz; x++)
            {
                float d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c)) / c;
                byte a = (byte)(Mathf.Clamp01(1f - d * d) * 255f);
                pixels[y * sz + x] = new Color32(255, 255, 255, a);
            }
        tex.SetPixels32(pixels);
        tex.Apply();
        _radialGradientSprite = Sprite.Create(tex, new Rect(0, 0, sz, sz), new Vector2(0.5f, 0.5f));
        return _radialGradientSprite;
    }

    // ── Panel helpers ──────────────────────────────────────────────────────

private void ResetPanelImmediate()
    {
        if (panel      != null) panel.SetActive(false);
        if (handRect   != null) handRect.gameObject.SetActive(false);
        if (darkOverlay != null)
        {
            var c = darkOverlay.color; c.a = 0f; darkOverlay.color = c;
            darkOverlay.gameObject.SetActive(false);
        }
        if (spotlightCanvas    != null) { Destroy(spotlightCanvas);    spotlightCanvas    = null; }
        if (spotlightRaycaster != null) { Destroy(spotlightRaycaster); spotlightRaycaster = null; }
        if (characterContainer != null)
        {
            var p = characterContainer.anchoredPosition; p.y = charHiddenY;
            characterContainer.anchoredPosition = p;
            characterContainer.gameObject.SetActive(false);
        }
    }

    private IEnumerator HideTutorialRoutine()
    {
        SetSlowMo(false);

        if (characterContainer != null && characterContainer.gameObject.activeSelf)
            yield return SlideCharacter(charHiddenY, hideAfter: true);
        else
            yield return new WaitForSecondsRealtime(resumeDuration);

        if (panel != null) panel.SetActive(false);
        stepActive     = false;
        Time.timeScale = 1f;
    }
}

}
