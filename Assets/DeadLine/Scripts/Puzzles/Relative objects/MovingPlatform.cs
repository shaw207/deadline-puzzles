using System.Collections;
using UnityEngine;

public class MovingPlatform : MonoBehaviour, IRelativeObjects
{
    public enum Mode { OneShot, PingPong, Loop }

    [Header("Target")]
    [SerializeField] private Transform target;

    [Header("Path Settings")]
    [SerializeField] private Transform[] waypoints;

    [Header("Movement Settings")]
    [SerializeField] private float speed = 2f;
    [SerializeField] private Mode mode = Mode.OneShot;

    [Header("Auto start?")]
    [SerializeField] private bool autoStart = false;

    [Header("Snap target to first waypoint on Start")]
    [SerializeField] private bool snapToFirstWaypoint = true;

    [Header("Drawer SFX")]
    [SerializeField] private bool playDrawerSfx = false;
    [SerializeField] private string sfxOpen = "Drawer_Open";
    [SerializeField] private string sfxClose = "Drawer_Close";

    private int currentIndex = 0;
    private bool isMoving = false;
    private bool forward = true;
    private Coroutine moveRoutine;

    private bool _isOpen = false;

    private void Awake()
    {
        if (target == null)
            target = transform;
    }

    private void Start()
    {
        if (snapToFirstWaypoint && waypoints != null && waypoints.Length > 0 && waypoints[0] != null)
        {
            target.position = waypoints[0].position;
            currentIndex = 0;
            forward = true;
        }

        if (autoStart)
            StartMovement();
    }

    public void action() => StartMovement();
    public void action(object obj) => action();

    public void Noaction() => StopMovement(manualStop: true);
    public void Noaction(object obj) => Noaction();

    public bool checkStatu() => !isMoving;
    public bool UseParam() => false;

    private bool HasValidPath()
    {
        if (waypoints == null || waypoints.Length < 2) return false;
        for (int i = 0; i < waypoints.Length; i++)
            if (waypoints[i] == null) return false;
        return true;
    }

    private void StartMovement()
    {
        if (isMoving) return;
        if (!HasValidPath())
        {
            Debug.LogWarning("MovingPlatform needs at least 2 non-null waypoints!");
            return;
        }

        PlayOpenSfx();

        isMoving = true;
        moveRoutine = StartCoroutine(MoveRoutine());
    }

    private void StopMovement(bool manualStop)
    {
        if (moveRoutine == null && !isMoving) return;

        if (moveRoutine != null)
        {
            StopCoroutine(moveRoutine);
            moveRoutine = null;
        }

        FinishMovement(playClose: manualStop);
    }

    private IEnumerator MoveRoutine()
    {
        while (true)
        {
            int nextIndex = GetNextIndex();
            Vector3 targetPos = waypoints[nextIndex].position;

            while ((target.position - targetPos).sqrMagnitude > 0.001f)
            {
                target.position = Vector3.MoveTowards(
                    target.position,
                    targetPos,
                    speed * Time.deltaTime
                );
                yield return null;
            }

            target.position = targetPos;
            currentIndex = nextIndex;

            if (mode == Mode.OneShot && currentIndex == waypoints.Length - 1)
                break;
        }

        moveRoutine = null;
        FinishMovement(playClose: true); 
    }

    private void FinishMovement(bool playClose)
    {
        isMoving = false;

        if (playClose)
            PlayCloseSfx();
    }

    private int GetNextIndex()
    {
        if (mode == Mode.OneShot)
            return Mathf.Min(currentIndex + 1, waypoints.Length - 1);

        if (mode == Mode.Loop)
            return (currentIndex + 1) % waypoints.Length;

        if (forward)
        {
            if (currentIndex == waypoints.Length - 1)
            {
                forward = false;
                return currentIndex - 1;
            }
            return currentIndex + 1;
        }
        else
        {
            if (currentIndex == 0)
            {
                forward = true;
                return currentIndex + 1;
            }
            return currentIndex - 1;
        }
    }

    private void PlayOpenSfx()
    {
        if (!playDrawerSfx) return;
        if (_isOpen) return; 
        if (string.IsNullOrWhiteSpace(sfxOpen)) return;

        var pos = (target != null) ? target.position : transform.position;
        DeadLine.AudioManager.Instance?.PlaySoundAtPoint(sfxOpen, pos);

        _isOpen = true;
    }

    private void PlayCloseSfx()
    {
        if (!playDrawerSfx) return;
        if (!_isOpen) return; 
        if (string.IsNullOrWhiteSpace(sfxClose)) return;

        var pos = (target != null) ? target.position : transform.position;
        DeadLine.AudioManager.Instance?.PlaySoundAtPoint(sfxClose, pos);

        _isOpen = false;
    }
}
