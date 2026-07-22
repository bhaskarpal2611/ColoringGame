using System.Collections;
using UnityEngine;



namespace DrawingGame
{
    /// <summary>
    /// After N seconds with no touch input anywhere, repeatedly re-shows a set of silent
    /// hint steps (hand + highlight only — leave bodyText empty and don't wire a
    /// PlayVoiceLine call on these) as an idle nudge. Disappears instantly on any touch,
    /// and re-arms itself afterward.
    ///
    /// These steps are owned here, not appended to GenericTutorialManager's own steps[] —
    /// that array auto-chains to the next index when a step completes (see RunStep), so
    /// idle content living there would flow straight out of the guided intro tutorial
    /// instead of stopping when it's actually done. ShowAdHocStep() runs a step without
    /// touching that chain at all; this loop decides itself when to move to the next one.
    /// </summary>
    public class IdleTutorialLoop : MonoBehaviour
    {
        [SerializeField] private GenericTutorialManager _tutorialManager;

        [Tooltip("Seconds of no touch input before the idle loop starts.")]
        [SerializeField] private float _idleThreshold = 10f;

        [Tooltip("Seconds each idle step stays on screen before moving to the next one.")]
        [SerializeField] private float _stepDuration = 3f;

        [Tooltip("Silent (no text/VO) steps to loop through while idle.")]
        [SerializeField] private GenericTutorialStep[] _idleSteps;

        [Tooltip("Only run the idle loop when this GameObject is active. Assign the Game_Scene root — prevents idle hints showing on the selection screen.")]
        [SerializeField] private GameObject _allowedWhenActive;

        private float _idleTimer;
        private bool _loopActive;
        private Coroutine _loopRoutine;

        private void Update()
        {
            bool touching = Input.touchCount > 0 || Input.GetMouseButton(0);

            if (touching)
            {
                _idleTimer = 0f;
                if (_loopActive) StopLoop();
                return;
            }

            if (_loopActive) return;

            _idleTimer += Time.unscaledDeltaTime;
            if (_idleTimer >= _idleThreshold && _tutorialManager != null && !_tutorialManager.IsTutorialActive && IsAllowedScene())
                StartLoop();
        }

        private bool IsAllowedScene()
        {
            if (_allowedWhenActive == null) return true;
            return _allowedWhenActive.activeInHierarchy;
        }

        private void StartLoop()
        {
            _loopActive = true;
            _loopRoutine = StartCoroutine(LoopRoutine());
        }

        private void StopLoop()
        {
            _loopActive = false;
            if (_loopRoutine != null) { StopCoroutine(_loopRoutine); _loopRoutine = null; }
            if (_tutorialManager != null) _tutorialManager.HideImmediately();
        }

        private IEnumerator LoopRoutine()
        {
            if (_idleSteps == null || _idleSteps.Length == 0) yield break;

            // playVoice: false — idle hints must stay silent even if a voiceOverKey is set
            // in the Inspector; re-showing on a timer would restart the line every cycle,
            // which sounded like the tutorial VO looping.

            // Single idle step: show it once and hold. Re-showing it every _stepDuration
            // restarted the hand animation from scratch each cycle — the visible
            // "hand keeps resetting its position" jitter. The step's own hand loop keeps
            // animating until any touch stops the idle loop.
            if (_idleSteps.Length == 1)
            {
                _tutorialManager.ShowAdHocStep(_idleSteps[0], playVoice: false);
                yield break;
            }

            while (true)
            {
                foreach (GenericTutorialStep step in _idleSteps)
                {
                    _tutorialManager.ShowAdHocStep(step, playVoice: false);
                    yield return new WaitForSecondsRealtime(_stepDuration);
                }
            }
        }
    }
}
