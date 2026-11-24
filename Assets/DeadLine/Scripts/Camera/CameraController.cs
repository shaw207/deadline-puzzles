using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Splines;

public class CameraController : MonoBehaviour
{
    public enum CameraMode { Follow, Fixed, Spline }

    [Header("Mode")]
    [SerializeField] private CameraMode mode = CameraMode.Follow;

    [Header("Target")]
    [SerializeField] private Transform target;
    private Rigidbody _targetRigidbody;

    [Header("Follow Settings")]
    [SerializeField] private Vector3 offset = new Vector3(9f, 2.5f, 0f);

    [Header("Transition Durations")]
    [SerializeField] private float transitionFollowTime = 1.0f;
    [SerializeField] private float transitionFixedTime = 1.0f;
    [SerializeField] private float transitionSplineTime = 1.0f;

    [Header("Follow Movement")]
    [SerializeField] private float maxFollowSpeed = 5.5f;
    [SerializeField] private float acceleration = 20f;
    [SerializeField] private float deceleration = 25f;
    [SerializeField] private float followStopDistance = 0.1f;
    [SerializeField] private float maxAllowedOffset = 0.5f;

    [Header("Catch-Up (No Overshoot)")]
    [SerializeField] private float maxCatchUpSpeed = 18f;
    [SerializeField] private float catchUpGain = 6f;
    [SerializeField] private float brakeDistance = 1.4f;
    [SerializeField] private float snapToDesiredDistance = 0.06f;

    [Header("Smart Snap")]
    [SerializeField] private bool useSmartSnap = true;
    [SerializeField] private float snapSpeedThreshold = 0.15f;
    [SerializeField] private float snapAccelThreshold = 2.0f;
    [SerializeField] private float snapSettleTime = 0.08f;

    [Header("Vertical Follow")]
    [SerializeField] private float verticalLerpSpeed = 5f;
    [SerializeField] private float verticalStopDistance = 0.02f;

    [Header("Player Velocity (for pacing)")]
    [SerializeField] private float playerWalkVelocity = 5f;
    [SerializeField] private float playerRunVelocity = 10f;

    [Header("Look Ahead Rotation")]
    [SerializeField] private float moveThreshold = 0.075f;
    [SerializeField] private float lookAheadMaxAngle = 3f;
    [SerializeField] private float lookAheadRotateSpeed = 0.4f;
    [SerializeField] private float lookAheadRotateReturnSpeed = 0.05f;

    [Header("Movement Start/Stop Delay")]
    [SerializeField] private bool useMoveDelays = true;
    [SerializeField] private float startMoveDelay = 0.10f;
    [SerializeField] private float stopMoveDelay = 0.15f;

    [Header("Idle Catch-Up")]
    [SerializeField] private float idleCatchUpSpeed = 1.25f;
    [SerializeField] private float idleCatchUpAccel = 6.0f;

    [Header("World Bounds")]
    [SerializeField] private bool useBounds = true;
    [SerializeField] private Transform minBound;
    [SerializeField] private Transform maxBound;

    [Header("Fixed Mode")]
    [SerializeField] private Transform fixedCameraPoint;     // NEW: camera position point
    [SerializeField] private Transform fixedLookPoint;       // NEW: camera look point (optional)
    [SerializeField] private bool fixedFollowPlayer = true;  // NEW: follow player look

    [Header("Spline Mode")]
    [SerializeField] private SplineContainer activeSpline;
    [SerializeField] private int splineSamples = 64;

    [Header("Spline Projection")]
    [SerializeField] private bool useClosestPointProjection = true;
    [Range(0, 16)]
    [SerializeField] private int projectionRefineIterations = 8;

    private float[] _splineSampleTs;
    private Vector3[] _splineSamplePositions;

    private static Vector3 ConvertToVector3(Unity.Mathematics.float3 value) => new Vector3(value.x, value.y, value.z);

    [Header("Camera Noise")]
    [SerializeField] private float noiseAmount = 0f;
    [SerializeField] private float noisePositionFrequency = 1.5f;
    [SerializeField] private float noiseRotationFrequency = 2.0f;
    [SerializeField] private float baseNoisePositionAmplitude = 0.05f;
    [SerializeField] private float baseNoiseRotationAmplitude = 0.4f;

    private float _noiseTime;
    private Vector3 _noisePositionSeed;
    private Vector3 _noiseRotationSeed;

    private Vector3 _lastTargetPos;
    private Vector3 _followVelocity;
    private Quaternion _baseFollowRotation;
    private Vector3 _baseFollowForwardXZ;
    private float _lookAheadAngle;

    private bool _isTransitioning;
    private float _transitionDuration;
    private float _transitionElapsed;
    private Vector3 _transitionStartPos;

    private bool _hasLeftTarget;
    private bool _usingPlayerVelocity;

    private const float AtTargetEpsilon = 0.01f;

    [Header("Fixed Tick Gating")]
    [SerializeField] private bool updateCameraOnlyOnNewFixedTick = true;

    private int _fixedTick = 0;
    private int _lastCameraTick = -1;

    private bool _hasFixedSamples = false;
    private Vector3 _fixedPrevPos;
    private Vector3 _fixedCurrPos;

    private bool _gatedMoving = false;
    private float _moveDelayTimer = 0f;

    private bool _inMoveDelayWindow = false;

    private float _snapStableTimer = 0f;

    [Serializable]
    public struct CameraSnapshot
    {
        public CameraMode Mode;
        public Transform Target;
        public Vector3 Offset;

        public float TransitionFollowTime;
        public float TransitionFixedTime;
        public float TransitionSplineTime;

        public float MaxFollowSpeed;
        public float Acceleration;
        public float Deceleration;
        public float FollowStopDistance;
        public float MaxAllowedOffset;

        public float MaxCatchUpSpeed;
        public float CatchUpGain;
        public float BrakeDistance;
        public float SnapToDesiredDistance;

        public bool UseSmartSnap;
        public float SnapSpeedThreshold;
        public float SnapAccelThreshold;
        public float SnapSettleTime;

        public float VerticalLerpSpeed;
        public float VerticalStopDistance;

        public float PlayerWalkVelocity;
        public float PlayerRunVelocity;

        public float MoveThreshold;
        public float LookAheadMaxAngle;
        public float LookAheadRotateSpeed;
        public float LookAheadRotateReturnSpeed;

        public bool UseMoveDelays;
        public float StartMoveDelay;
        public float StopMoveDelay;

        public float IdleCatchUpSpeed;
        public float IdleCatchUpAccel;

        public bool UseBounds;
        public Transform MinBound;
        public Transform MaxBound;

        // Fixed mode (NEW)
        public Transform FixedCameraPoint;
        public Transform FixedLookPoint;
        public bool FixedFollowPlayer;

        public SplineContainer ActiveSpline;
        public int SplineSamples;

        public bool UseClosestPointProjection;
        public int ProjectionRefineIterations;

        public float NoiseAmount;
        public float NoisePositionFrequency;
        public float NoiseRotationFrequency;
        public float BaseNoisePositionAmplitude;
        public float BaseNoiseRotationAmplitude;

        public bool UpdateCameraOnlyOnNewFixedTick;

        public Quaternion Rotation;
    }

    [Serializable]
    public class CameraOverrides
    {
        [Header("Mode")]
        public bool overrideMode;
        public CameraMode mode;

        [Header("Target")]
        public bool overrideTarget;
        public Transform target;

        [Header("Follow Settings")]
        public bool overrideOffset;
        public Vector3 offset;

        [Header("Transition Durations")]
        public bool overrideTransitionTimes;
        public float transitionFollowTime = 1f;
        public float transitionFixedTime = 1f;
        public float transitionSplineTime = 1f;

        [Header("Follow Movement")]
        public bool overrideFollowMovement;
        public float maxFollowSpeed = 5.5f;
        public float acceleration = 20f;
        public float deceleration = 25f;
        public float followStopDistance = 0.1f;
        public float maxAllowedOffset = 0.5f;

        [Header("Catch-Up")]
        public bool overrideCatchUp;
        public float maxCatchUpSpeed = 18f;
        public float catchUpGain = 6f;
        public float brakeDistance = 1.4f;
        public float snapToDesiredDistance = 0.06f;

        [Header("Smart Snap")]
        public bool overrideSmartSnap;
        public bool useSmartSnap = true;
        public float snapSpeedThreshold = 0.15f;
        public float snapAccelThreshold = 2.0f;
        public float snapSettleTime = 0.08f;

        [Header("Vertical Follow")]
        public bool overrideVerticalFollow;
        public float verticalLerpSpeed = 5f;
        public float verticalStopDistance = 0.02f;

        [Header("Player Velocity")]
        public bool overridePlayerVelocity;
        public float playerWalkVelocity = 5f;
        public float playerRunVelocity = 10f;

        [Header("Look Ahead")]
        public bool overrideLookAhead;
        public float moveThreshold = 0.075f;
        public float lookAheadMaxAngle = 3f;
        public float lookAheadRotateSpeed = 0.4f;
        public float lookAheadRotateReturnSpeed = 0.05f;

        [Header("Start/Stop Delay")]
        public bool overrideMoveDelays;
        public bool useMoveDelays = true;
        public float startMoveDelay = 0.10f;
        public float stopMoveDelay = 0.15f;

        [Header("Idle Catch-Up")]
        public bool overrideIdleCatchUp;
        public float idleCatchUpSpeed = 1.25f;
        public float idleCatchUpAccel = 6.0f;

        [Header("Bounds")]
        public bool overrideBounds;
        public bool useBounds = true;
        public Transform minBound;
        public Transform maxBound;

        [Header("Fixed Mode (NEW)")]
        public bool overrideFixedCameraPoint;
        public Transform fixedCameraPoint;
        public bool overrideFixedLookPoint;
        public Transform fixedLookPoint;
        public bool overrideFixedFollowPlayer;
        public bool fixedFollowPlayer = true;

        [Header("Spline Mode")]
        public bool overrideSpline;
        public SplineContainer activeSpline;
        public bool overrideSplineSamples;
        public int splineSamples = 64;

        [Header("Spline Projection")]
        public bool overrideSplineProjection;
        public bool useClosestPointProjection = true;
        [Range(0, 16)]
        public int projectionRefineIterations = 8;

        [Header("Noise")]
        public bool overrideNoise;
        public float noiseAmount = 0f;
        public float noisePositionFrequency = 1.5f;
        public float noiseRotationFrequency = 2f;
        public float baseNoisePositionAmplitude = 0.05f;
        public float baseNoiseRotationAmplitude = 0.4f;

        [Header("Fixed Tick Gating")]
        public bool overrideFixedTickGating;
        public bool updateCameraOnlyOnNewFixedTick = true;

        [Header("Rotation")]
        public bool overrideRotation;
        public Vector3 rotationEuler;
    }

    private class OverrideEntry
    {
        public object Key;
        public int Priority;
        public long Order;
        public CameraOverrides Overrides;
    }

    private readonly List<OverrideEntry> _overrides = new List<OverrideEntry>();
    private long _overrideOrderCounter;
    private CameraSnapshot _defaultSnapshot;

    private void Awake()
    {
        _defaultSnapshot = CaptureSnapshot();
    }

    public void BeginOverride(object key, int priority, CameraOverrides overrides)
    {
        if (key == null) return;

        int existingIndex = _overrides.FindIndex(entry => ReferenceEquals(entry.Key, key));
        if (existingIndex >= 0)
        {
            _overrides[existingIndex].Priority = priority;
            _overrides[existingIndex].Overrides = overrides;
            _overrides[existingIndex].Order = ++_overrideOrderCounter;
        }
        else
        {
            _overrides.Add(new OverrideEntry
            {
                Key = key,
                Priority = priority,
                Order = ++_overrideOrderCounter,
                Overrides = overrides
            });
        }

        RecomputeFromDefaultAndApply();
    }

    public void EndOverride(object key)
    {
        if (key == null) return;
        _overrides.RemoveAll(entry => ReferenceEquals(entry.Key, key));
        RecomputeFromDefaultAndApply();
    }

    private void RecomputeFromDefaultAndApply()
    {
        CameraSnapshot snapshot = _defaultSnapshot;

        if (_overrides.Count > 0)
        {
            _overrides.Sort((left, right) =>
            {
                int priorityComparison = right.Priority.CompareTo(left.Priority);
                if (priorityComparison != 0) return priorityComparison;
                return right.Order.CompareTo(left.Order);
            });

            foreach (OverrideEntry overrideEntry in _overrides)
                ApplyOverridesToSnapshot(ref snapshot, overrideEntry.Overrides);
        }

        ApplySnapshot(snapshot);
    }

    private static void ApplyOverridesToSnapshot(ref CameraSnapshot snapshot, CameraOverrides cameraOverrides)
    {
        if (cameraOverrides == null) return;

        if (cameraOverrides.overrideMode) snapshot.Mode = cameraOverrides.mode;
        if (cameraOverrides.overrideTarget) snapshot.Target = cameraOverrides.target;
        if (cameraOverrides.overrideOffset) snapshot.Offset = cameraOverrides.offset;

        if (cameraOverrides.overrideTransitionTimes)
        {
            snapshot.TransitionFollowTime = cameraOverrides.transitionFollowTime;
            snapshot.TransitionFixedTime = cameraOverrides.transitionFixedTime;
            snapshot.TransitionSplineTime = cameraOverrides.transitionSplineTime;
        }

        if (cameraOverrides.overrideFollowMovement)
        {
            snapshot.MaxFollowSpeed = cameraOverrides.maxFollowSpeed;
            snapshot.Acceleration = cameraOverrides.acceleration;
            snapshot.Deceleration = cameraOverrides.deceleration;
            snapshot.FollowStopDistance = cameraOverrides.followStopDistance;
            snapshot.MaxAllowedOffset = cameraOverrides.maxAllowedOffset;
        }

        if (cameraOverrides.overrideCatchUp)
        {
            snapshot.MaxCatchUpSpeed = cameraOverrides.maxCatchUpSpeed;
            snapshot.CatchUpGain = cameraOverrides.catchUpGain;
            snapshot.BrakeDistance = cameraOverrides.brakeDistance;
            snapshot.SnapToDesiredDistance = cameraOverrides.snapToDesiredDistance;
        }

        if (cameraOverrides.overrideSmartSnap)
        {
            snapshot.UseSmartSnap = cameraOverrides.useSmartSnap;
            snapshot.SnapSpeedThreshold = Mathf.Max(0f, cameraOverrides.snapSpeedThreshold);
            snapshot.SnapAccelThreshold = Mathf.Max(0f, cameraOverrides.snapAccelThreshold);
            snapshot.SnapSettleTime = Mathf.Max(0f, cameraOverrides.snapSettleTime);
        }

        if (cameraOverrides.overrideVerticalFollow)
        {
            snapshot.VerticalLerpSpeed = cameraOverrides.verticalLerpSpeed;
            snapshot.VerticalStopDistance = cameraOverrides.verticalStopDistance;
        }

        if (cameraOverrides.overridePlayerVelocity)
        {
            snapshot.PlayerWalkVelocity = cameraOverrides.playerWalkVelocity;
            snapshot.PlayerRunVelocity = cameraOverrides.playerRunVelocity;
        }

        if (cameraOverrides.overrideLookAhead)
        {
            snapshot.MoveThreshold = cameraOverrides.moveThreshold;
            snapshot.LookAheadMaxAngle = cameraOverrides.lookAheadMaxAngle;
            snapshot.LookAheadRotateSpeed = cameraOverrides.lookAheadRotateSpeed;
            snapshot.LookAheadRotateReturnSpeed = cameraOverrides.lookAheadRotateReturnSpeed;
        }

        if (cameraOverrides.overrideMoveDelays)
        {
            snapshot.UseMoveDelays = cameraOverrides.useMoveDelays;
            snapshot.StartMoveDelay = Mathf.Max(0f, cameraOverrides.startMoveDelay);
            snapshot.StopMoveDelay = Mathf.Max(0f, cameraOverrides.stopMoveDelay);
        }

        if (cameraOverrides.overrideIdleCatchUp)
        {
            snapshot.IdleCatchUpSpeed = Mathf.Max(0f, cameraOverrides.idleCatchUpSpeed);
            snapshot.IdleCatchUpAccel = Mathf.Max(0f, cameraOverrides.idleCatchUpAccel);
        }

        if (cameraOverrides.overrideBounds)
        {
            snapshot.UseBounds = cameraOverrides.useBounds;
            snapshot.MinBound = cameraOverrides.minBound;
            snapshot.MaxBound = cameraOverrides.maxBound;
        }

        if (cameraOverrides.overrideFixedCameraPoint) snapshot.FixedCameraPoint = cameraOverrides.fixedCameraPoint;
        if (cameraOverrides.overrideFixedLookPoint) snapshot.FixedLookPoint = cameraOverrides.fixedLookPoint;
        if (cameraOverrides.overrideFixedFollowPlayer) snapshot.FixedFollowPlayer = cameraOverrides.fixedFollowPlayer;

        if (cameraOverrides.overrideSpline) snapshot.ActiveSpline = cameraOverrides.activeSpline;
        if (cameraOverrides.overrideSplineSamples) snapshot.SplineSamples = Mathf.Max(2, cameraOverrides.splineSamples);

        if (cameraOverrides.overrideSplineProjection)
        {
            snapshot.UseClosestPointProjection = cameraOverrides.useClosestPointProjection;
            snapshot.ProjectionRefineIterations = Mathf.Clamp(cameraOverrides.projectionRefineIterations, 0, 16);
        }

        if (cameraOverrides.overrideNoise)
        {
            snapshot.NoiseAmount = cameraOverrides.noiseAmount;
            snapshot.NoisePositionFrequency = cameraOverrides.noisePositionFrequency;
            snapshot.NoiseRotationFrequency = cameraOverrides.noiseRotationFrequency;
            snapshot.BaseNoisePositionAmplitude = cameraOverrides.baseNoisePositionAmplitude;
            snapshot.BaseNoiseRotationAmplitude = cameraOverrides.baseNoiseRotationAmplitude;
        }

        if (cameraOverrides.overrideFixedTickGating)
        {
            snapshot.UpdateCameraOnlyOnNewFixedTick = cameraOverrides.updateCameraOnlyOnNewFixedTick;
        }

        if (cameraOverrides.overrideRotation)
        {
            snapshot.Rotation = Quaternion.Euler(cameraOverrides.rotationEuler);
        }
    }

    private CameraSnapshot CaptureSnapshot()
    {
        return new CameraSnapshot
        {
            Mode = mode,
            Target = target,
            Offset = offset,

            TransitionFollowTime = transitionFollowTime,
            TransitionFixedTime = transitionFixedTime,
            TransitionSplineTime = transitionSplineTime,

            MaxFollowSpeed = maxFollowSpeed,
            Acceleration = acceleration,
            Deceleration = deceleration,
            FollowStopDistance = followStopDistance,
            MaxAllowedOffset = maxAllowedOffset,

            MaxCatchUpSpeed = maxCatchUpSpeed,
            CatchUpGain = catchUpGain,
            BrakeDistance = brakeDistance,
            SnapToDesiredDistance = snapToDesiredDistance,

            UseSmartSnap = useSmartSnap,
            SnapSpeedThreshold = snapSpeedThreshold,
            SnapAccelThreshold = snapAccelThreshold,
            SnapSettleTime = snapSettleTime,

            VerticalLerpSpeed = verticalLerpSpeed,
            VerticalStopDistance = verticalStopDistance,

            PlayerWalkVelocity = playerWalkVelocity,
            PlayerRunVelocity = playerRunVelocity,

            MoveThreshold = moveThreshold,
            LookAheadMaxAngle = lookAheadMaxAngle,
            LookAheadRotateSpeed = lookAheadRotateSpeed,
            LookAheadRotateReturnSpeed = lookAheadRotateReturnSpeed,

            UseMoveDelays = useMoveDelays,
            StartMoveDelay = startMoveDelay,
            StopMoveDelay = stopMoveDelay,

            IdleCatchUpSpeed = idleCatchUpSpeed,
            IdleCatchUpAccel = idleCatchUpAccel,

            UseBounds = useBounds,
            MinBound = minBound,
            MaxBound = maxBound,

            // Fixed mode (NEW)
            FixedCameraPoint = fixedCameraPoint,
            FixedLookPoint = fixedLookPoint,
            FixedFollowPlayer = fixedFollowPlayer,

            ActiveSpline = activeSpline,
            SplineSamples = splineSamples,

            UseClosestPointProjection = useClosestPointProjection,
            ProjectionRefineIterations = projectionRefineIterations,

            NoiseAmount = noiseAmount,
            NoisePositionFrequency = noisePositionFrequency,
            NoiseRotationFrequency = noiseRotationFrequency,
            BaseNoisePositionAmplitude = baseNoisePositionAmplitude,
            BaseNoiseRotationAmplitude = baseNoiseRotationAmplitude,

            UpdateCameraOnlyOnNewFixedTick = updateCameraOnlyOnNewFixedTick,

            Rotation = transform.rotation
        };
    }

    private void ApplySnapshot(CameraSnapshot snapshot)
    {
        bool rotationChanged = Quaternion.Angle(transform.rotation, snapshot.Rotation) > 0.001f;
        if (rotationChanged)
        {
            transform.rotation = snapshot.Rotation;
            CacheFollowBaseRotation();
        }

        target = snapshot.Target;
        if (target != null)
        {
            if (_targetRigidbody == null || _targetRigidbody.transform != target)
                _targetRigidbody = target.GetComponent<Rigidbody>();
        }
        else
        {
            _targetRigidbody = null;
        }

        offset = snapshot.Offset;

        transitionFollowTime = snapshot.TransitionFollowTime;
        transitionFixedTime = snapshot.TransitionFixedTime;
        transitionSplineTime = snapshot.TransitionSplineTime;

        maxFollowSpeed = snapshot.MaxFollowSpeed;
        acceleration = snapshot.Acceleration;
        deceleration = snapshot.Deceleration;
        followStopDistance = snapshot.FollowStopDistance;
        maxAllowedOffset = snapshot.MaxAllowedOffset;

        maxCatchUpSpeed = snapshot.MaxCatchUpSpeed;
        catchUpGain = snapshot.CatchUpGain;
        brakeDistance = snapshot.BrakeDistance;
        snapToDesiredDistance = snapshot.SnapToDesiredDistance;

        useSmartSnap = snapshot.UseSmartSnap;
        snapSpeedThreshold = snapshot.SnapSpeedThreshold;
        snapAccelThreshold = snapshot.SnapAccelThreshold;
        snapSettleTime = snapshot.SnapSettleTime;

        verticalLerpSpeed = snapshot.VerticalLerpSpeed;
        verticalStopDistance = snapshot.VerticalStopDistance;

        playerWalkVelocity = snapshot.PlayerWalkVelocity;
        playerRunVelocity = snapshot.PlayerRunVelocity;

        moveThreshold = snapshot.MoveThreshold;
        lookAheadMaxAngle = snapshot.LookAheadMaxAngle;
        lookAheadRotateSpeed = snapshot.LookAheadRotateSpeed;
        lookAheadRotateReturnSpeed = snapshot.LookAheadRotateReturnSpeed;

        useMoveDelays = snapshot.UseMoveDelays;
        startMoveDelay = snapshot.StartMoveDelay;
        stopMoveDelay = snapshot.StopMoveDelay;

        idleCatchUpSpeed = snapshot.IdleCatchUpSpeed;
        idleCatchUpAccel = snapshot.IdleCatchUpAccel;

        useBounds = snapshot.UseBounds;
        minBound = snapshot.MinBound;
        maxBound = snapshot.MaxBound;

        // Fixed mode (NEW)
        fixedCameraPoint = snapshot.FixedCameraPoint;
        fixedLookPoint = snapshot.FixedLookPoint;
        fixedFollowPlayer = snapshot.FixedFollowPlayer;

        activeSpline = snapshot.ActiveSpline;
        splineSamples = Mathf.Max(2, snapshot.SplineSamples);

        useClosestPointProjection = snapshot.UseClosestPointProjection;
        projectionRefineIterations = Mathf.Clamp(snapshot.ProjectionRefineIterations, 0, 16);

        noiseAmount = snapshot.NoiseAmount;
        noisePositionFrequency = snapshot.NoisePositionFrequency;
        noiseRotationFrequency = snapshot.NoiseRotationFrequency;
        baseNoisePositionAmplitude = snapshot.BaseNoisePositionAmplitude;
        baseNoiseRotationAmplitude = snapshot.BaseNoiseRotationAmplitude;

        updateCameraOnlyOnNewFixedTick = snapshot.UpdateCameraOnlyOnNewFixedTick;

        if (rotationChanged)
        {
            _lookAheadAngle = 0f;
        }

        if (snapshot.Mode == CameraMode.Fixed) SetFixedMode(snapshot.FixedCameraPoint, snapshot.FixedLookPoint, snapshot.FixedFollowPlayer);
        else if (snapshot.Mode == CameraMode.Spline) SetSplineMode(snapshot.ActiveSpline);
        else SetFollowMode();

        if (snapshot.Mode == CameraMode.Spline && snapshot.ActiveSpline != null)
            BuildSplineSampleTable();
    }

    private void Start()
    {
        if (target != null)
        {
            _lastTargetPos = target.position;
            _targetRigidbody = target.GetComponent<Rigidbody>();

            _fixedPrevPos = _fixedCurrPos = target.position;
            _hasFixedSamples = true;
            _fixedTick = 1;
            _lastCameraTick = -1;
        }

        CacheFollowBaseRotation();
        _followVelocity = Vector3.zero;

        _hasLeftTarget = false;
        _usingPlayerVelocity = false;

        _gatedMoving = false;
        _moveDelayTimer = 0f;

        _inMoveDelayWindow = false;
        _snapStableTimer = 0f;

        if (mode == CameraMode.Spline && activeSpline != null)
            BuildSplineSampleTable();

        _noiseTime = UnityEngine.Random.Range(0f, 1000f);
        _noisePositionSeed = new Vector3(
            UnityEngine.Random.Range(0f, 1000f),
            UnityEngine.Random.Range(0f, 1000f),
            UnityEngine.Random.Range(0f, 1000f)
        );
        _noiseRotationSeed = new Vector3(
            UnityEngine.Random.Range(0f, 1000f),
            UnityEngine.Random.Range(0f, 1000f),
            UnityEngine.Random.Range(0f, 1000f)
        );

        _defaultSnapshot = CaptureSnapshot();
    }

    private void FixedUpdate()
    {
        if (target == null) return;

        if (!_hasFixedSamples)
        {
            _fixedPrevPos = _fixedCurrPos = target.position;
            _hasFixedSamples = true;
            _fixedTick++;
            return;
        }

        _fixedPrevPos = _fixedCurrPos;
        _fixedCurrPos = target.position;
        _fixedTick++;
    }

    private void LateUpdate()
    {
        if (mode != CameraMode.Fixed && target == null) return;

        if (updateCameraOnlyOnNewFixedTick && target != null)
        {
            if (!_hasFixedSamples) return;
            if (_fixedTick == _lastCameraTick) return;
            _lastCameraTick = _fixedTick;
        }

        float deltaTime = Time.deltaTime;
        if (deltaTime <= 0f) return;

        Vector3 targetPosition =
            (target != null && _hasFixedSamples)
                ? _fixedCurrPos
                : (target != null ? target.position : Vector3.zero);

        Vector3 desiredPos = transform.position;
        bool isMovingHorizontally = false;
        Vector3 horizontalDelta = Vector3.zero;

        if (mode == CameraMode.Follow && target != null)
        {
            Vector3 delta = targetPosition - _lastTargetPos;
            horizontalDelta = new Vector3(delta.x, 0f, delta.z);
            float horizontalMagnitude = horizontalDelta.magnitude;

            bool rawMoving = horizontalMagnitude > moveThreshold;
            bool gatedMoving = GetGatedMoving(rawMoving, deltaTime);
            isMovingHorizontally = gatedMoving;

            _inMoveDelayWindow = useMoveDelays && (rawMoving != gatedMoving);

            if (gatedMoving && horizontalDelta.sqrMagnitude > 0.000001f)
            {
                Vector3 moveDirection = horizontalDelta.normalized;
                float angleFromBase = Vector3.SignedAngle(_baseFollowForwardXZ, moveDirection, Vector3.up);
                float targetAngle = Mathf.Clamp(angleFromBase, -lookAheadMaxAngle, lookAheadMaxAngle);

                float rotationLerpFactor = Mathf.Clamp01(lookAheadRotateSpeed * deltaTime);
                _lookAheadAngle = Mathf.Lerp(_lookAheadAngle, targetAngle, rotationLerpFactor);
            }
            else
            {
                float returnLerpFactor = Mathf.Clamp01(lookAheadRotateReturnSpeed * deltaTime);
                _lookAheadAngle = Mathf.Lerp(_lookAheadAngle, 0f, returnLerpFactor);
            }

            desiredPos = targetPosition + offset;

            if (useBounds && minBound != null && maxBound != null)
            {
                Vector3 minPos = minBound.position;
                Vector3 maxPos = maxBound.position;

                desiredPos.x = Mathf.Clamp(desiredPos.x, minPos.x, maxPos.x);
                desiredPos.y = Mathf.Clamp(desiredPos.y, minPos.y, maxPos.y);
                desiredPos.z = Mathf.Clamp(desiredPos.z, minPos.z, maxPos.z);
            }

            _lastTargetPos = targetPosition;
        }
        else
        {
            _inMoveDelayWindow = false;
        }

        if (mode == CameraMode.Fixed && fixedCameraPoint != null)
        {
            desiredPos = fixedCameraPoint.position;
            if (target != null) _lastTargetPos = targetPosition;
        }
        else if (mode == CameraMode.Spline && activeSpline != null && target != null)
        {
            float tOnSpline = GetClosestTOnSpline(targetPosition);
            desiredPos = ConvertToVector3(activeSpline.EvaluatePosition(tOnSpline));
            _lastTargetPos = targetPosition;
        }

        Vector3 finalPos;

        if (_isTransitioning && _transitionDuration > 0f)
        {
            _transitionElapsed += deltaTime;
            float normalizedTime = Mathf.Clamp01(_transitionElapsed / _transitionDuration);
            float easedTime = normalizedTime * normalizedTime * (3f - 2f * normalizedTime);

            finalPos = Vector3.Lerp(_transitionStartPos, desiredPos, easedTime);

            if (normalizedTime >= 1f)
            {
                _isTransitioning = false;
                _followVelocity = Vector3.zero;
                _usingPlayerVelocity = false;
                _hasLeftTarget = false;

                _gatedMoving = false;
                _moveDelayTimer = 0f;

                _inMoveDelayWindow = false;
                _snapStableTimer = 0f;
            }
        }
        else
        {
            if (mode == CameraMode.Follow && target != null)
            {
                finalPos = StepFollow(desiredPos, targetPosition, horizontalDelta, isMovingHorizontally, deltaTime);
            }
            else
            {
                finalPos = desiredPos;
                _followVelocity = Vector3.zero;
                _usingPlayerVelocity = false;
                _hasLeftTarget = false;

                _gatedMoving = false;
                _moveDelayTimer = 0f;

                _inMoveDelayWindow = false;
                _snapStableTimer = 0f;
            }
        }

        // Rotation
        Quaternion baseRot = transform.rotation;

        if (mode == CameraMode.Follow)
        {
            Quaternion yawOffset = Quaternion.AngleAxis(_lookAheadAngle, Vector3.up);
            baseRot = yawOffset * _baseFollowRotation;
        }
        else if (mode == CameraMode.Spline && target != null)
        {
            Vector3 toTarget = targetPosition - finalPos;
            if (toTarget.sqrMagnitude > 0.0001f)
                baseRot = Quaternion.LookRotation(toTarget.normalized, Vector3.up);
        }
        else if (mode == CameraMode.Fixed)
        {
            // NEW: Fixed look logic
            Vector3 lookAtPos;

            bool canLookAtPlayer = (target != null);
            bool shouldFollowPlayer = fixedFollowPlayer || fixedLookPoint == null;

            if (shouldFollowPlayer && canLookAtPlayer)
            {
                lookAtPos = targetPosition;
            }
            else if (fixedLookPoint != null)
            {
                lookAtPos = fixedLookPoint.position;
            }
            else if (canLookAtPlayer)
            {
                lookAtPos = targetPosition;
            }
            else
            {
                lookAtPos = finalPos + transform.forward;
            }

            Vector3 lookDir = lookAtPos - finalPos;
            if (lookDir.sqrMagnitude > 0.0001f)
                baseRot = Quaternion.LookRotation(lookDir.normalized, Vector3.up);
        }

        // Noise
        Vector3 noisyPos = finalPos;
        Quaternion noisyRot = baseRot;

        if (noiseAmount > 0f)
        {
            _noiseTime += deltaTime;

            float positionNoiseTime = _noiseTime * noisePositionFrequency;
            float rotationNoiseTime = _noiseTime * noiseRotationFrequency;

            Vector3 positionNoise = new Vector3(
                Mathf.PerlinNoise(_noisePositionSeed.x, positionNoiseTime) - 0.5f,
                Mathf.PerlinNoise(_noisePositionSeed.y, positionNoiseTime) - 0.5f,
                Mathf.PerlinNoise(_noisePositionSeed.z, positionNoiseTime) - 0.5f
            );

            float positionAmplitude = baseNoisePositionAmplitude * noiseAmount;
            noisyPos += positionNoise * positionAmplitude;

            float yawNoise =
                (Mathf.PerlinNoise(_noiseRotationSeed.x, rotationNoiseTime) - 0.5f) *
                (baseNoiseRotationAmplitude * noiseAmount);
            float pitchNoise =
                (Mathf.PerlinNoise(_noiseRotationSeed.y, rotationNoiseTime) - 0.5f) *
                (baseNoiseRotationAmplitude * noiseAmount);
            float rollNoise =
                (Mathf.PerlinNoise(_noiseRotationSeed.z, rotationNoiseTime) - 0.5f) *
                (baseNoiseRotationAmplitude * noiseAmount);

            Quaternion noiseRotOffset = Quaternion.Euler(pitchNoise, yawNoise, rollNoise);
            noisyRot = noiseRotOffset * noisyRot;
        }

        transform.position = noisyPos;
        transform.rotation = noisyRot;
    }

    private bool GetGatedMoving(bool rawMoving, float deltaTime)
    {
        if (!useMoveDelays) return rawMoving;

        if (rawMoving)
        {
            if (_gatedMoving)
            {
                _moveDelayTimer = 0f;
                return true;
            }

            _moveDelayTimer += deltaTime;
            if (_moveDelayTimer >= startMoveDelay)
            {
                _gatedMoving = true;
                _moveDelayTimer = 0f;
            }
            return _gatedMoving;
        }
        else
        {
            if (!_gatedMoving)
            {
                _moveDelayTimer = 0f;
                return false;
            }

            _moveDelayTimer += deltaTime;
            if (_moveDelayTimer >= stopMoveDelay)
            {
                _gatedMoving = false;
                _moveDelayTimer = 0f;
            }
            return _gatedMoving;
        }
    }

    private Vector3 StepFollow(Vector3 desiredPos, Vector3 targetPos, Vector3 horizontalDelta, bool isMovingHorizontally, float deltaTime)
    {
        Vector3 currentPosition = transform.position;

        Vector3 toDesired = desiredPos - currentPosition;
        float distanceToDesired = toDesired.magnitude;

        if (distanceToDesired <= snapToDesiredDistance)
        {
            if (!useSmartSnap)
            {
                _snapStableTimer = 0f;
                _followVelocity = Vector3.zero;
                _usingPlayerVelocity = false;
                _hasLeftTarget = false;
                return ApplyVerticalAndBounds(desiredPos, desiredPos, deltaTime);
            }

            if (_inMoveDelayWindow)
            {
                _snapStableTimer = 0f;
            }
            else
            {
                float speed = _followVelocity.magnitude;

                Vector3 desiredDirectionTemporary = (distanceToDesired > 1e-6f) ? (toDesired / distanceToDesired) : Vector3.zero;
                float idleMaxSpeed = Mathf.Min(idleCatchUpSpeed, maxFollowSpeed);
                Vector3 idealVelocity = desiredDirectionTemporary * idleMaxSpeed;

                float accelerationProxy =
                    (idealVelocity - _followVelocity).magnitude / Mathf.Max(deltaTime, 1e-6f);
                bool settled = speed <= snapSpeedThreshold && accelerationProxy <= snapAccelThreshold;

                if (settled)
                {
                    _snapStableTimer += deltaTime;
                    if (_snapStableTimer >= snapSettleTime)
                    {
                        _snapStableTimer = 0f;
                        _followVelocity = Vector3.zero;
                        _usingPlayerVelocity = false;
                        _hasLeftTarget = false;
                        return ApplyVerticalAndBounds(desiredPos, desiredPos, deltaTime);
                    }
                }
                else
                {
                    _snapStableTimer = 0f;
                }
            }
        }
        else
        {
            _snapStableTimer = 0f;
        }

        if (!_hasLeftTarget && distanceToDesired > followStopDistance)
            _hasLeftTarget = true;

        bool hasMoveDirection = isMovingHorizontally && horizontalDelta.sqrMagnitude > 0.000001f;
        bool atTarget = distanceToDesired <= AtTargetEpsilon;

        if (!_usingPlayerVelocity && hasMoveDirection && _hasLeftTarget && atTarget)
            _usingPlayerVelocity = true;

        if (_usingPlayerVelocity)
        {
            if (!hasMoveDirection || distanceToDesired > maxAllowedOffset * 2f)
                _usingPlayerVelocity = false;
        }

        float currentMaxSpeed = maxFollowSpeed;

        float excess = distanceToDesired - maxAllowedOffset;
        if (excess > 0f)
        {
            float boosted = maxFollowSpeed + excess * Mathf.Max(0f, catchUpGain);
            currentMaxSpeed = Mathf.Clamp(boosted, maxFollowSpeed, maxCatchUpSpeed);
        }

        float brakeDistanceClamped = Mathf.Max(0.0001f, brakeDistance);
        if (distanceToDesired < brakeDistanceClamped)
        {
            float scale = Mathf.Clamp01(distanceToDesired / brakeDistanceClamped);
            currentMaxSpeed *= scale;
            currentMaxSpeed = Mathf.Max(currentMaxSpeed, 0.05f);
        }

        float accelerationValue = acceleration;
        float decelerationValue = deceleration;

        Vector3 desiredDirection = (distanceToDesired > 1e-6f) ? (toDesired / distanceToDesired) : Vector3.zero;

        if (_followVelocity.sqrMagnitude > 0.000001f)
        {
            float along = Vector3.Dot(_followVelocity, desiredDirection);
            if (along < 0f)
            {
                _followVelocity = Vector3.MoveTowards(
                    _followVelocity,
                    Vector3.zero,
                    (decelerationValue * 2f) * deltaTime
                );
            }
        }

        if (_usingPlayerVelocity)
        {
            Vector3 moveDirection;

            if (hasMoveDirection) moveDirection = horizontalDelta.normalized;
            else if (_followVelocity.sqrMagnitude > 0.000001f) moveDirection = _followVelocity.normalized;
            else moveDirection = _baseFollowForwardXZ;

            float targetSpeed = playerWalkVelocity;

            if (_targetRigidbody != null)
            {
                Vector3 velocity = _targetRigidbody.linearVelocity;
                float speedSqr = velocity.sqrMagnitude;

                float walkThreshold = playerWalkVelocity + 1f;
                walkThreshold *= walkThreshold;

                targetSpeed = (speedSqr > walkThreshold) ? playerRunVelocity : playerWalkVelocity;
            }

            targetSpeed = Mathf.Min(targetSpeed, currentMaxSpeed);

            if (distanceToDesired < brakeDistanceClamped * 1.5f)
            {
                moveDirection = Vector3.ProjectOnPlane(moveDirection, Vector3.up).normalized;
                Vector3 desiredDirectionXZ = Vector3.ProjectOnPlane(desiredDirection, Vector3.up).normalized;
                if (desiredDirectionXZ.sqrMagnitude > 0.000001f)
                    moveDirection = desiredDirectionXZ;
            }

            _followVelocity = moveDirection * targetSpeed;
        }
        else
        {
            if (isMovingHorizontally)
            {
                Vector3 targetVelocity = desiredDirection * currentMaxSpeed;
                _followVelocity = Vector3.MoveTowards(_followVelocity, targetVelocity, accelerationValue * deltaTime);
            }
            else
            {
                float idleMaxSpeed = Mathf.Min(idleCatchUpSpeed, currentMaxSpeed);

                float brakeDistanceClampedSecondary = Mathf.Max(0.0001f, brakeDistance);
                if (distanceToDesired < brakeDistanceClampedSecondary)
                {
                    float scale = Mathf.Clamp01(distanceToDesired / brakeDistanceClampedSecondary);
                    idleMaxSpeed *= scale;
                    idleMaxSpeed = Mathf.Max(idleMaxSpeed, 0.02f);
                }

                Vector3 targetVelocity = desiredDirection * idleMaxSpeed;
                _followVelocity = Vector3.MoveTowards(_followVelocity, targetVelocity, idleCatchUpAccel * deltaTime);
            }

            float speed = _followVelocity.magnitude;
            if (speed > currentMaxSpeed)
                _followVelocity = _followVelocity * (currentMaxSpeed / Mathf.Max(0.0001f, speed));
        }

        Vector3 newPos = currentPosition + _followVelocity * deltaTime;

        float newDist = (desiredPos - newPos).magnitude;
        if (newDist > distanceToDesired && distanceToDesired < brakeDistanceClamped * 1.25f)
        {
            newPos = desiredPos;
            _followVelocity = Vector3.zero;
            _usingPlayerVelocity = false;
            _hasLeftTarget = false;

            _snapStableTimer = 0f;
        }

        newPos = ApplyVerticalAndBounds(newPos, desiredPos, deltaTime);
        return newPos;
    }

    private Vector3 ApplyVerticalAndBounds(Vector3 currentCandidate, Vector3 desiredPos, float deltaTime)
    {
        Vector3 newPos = currentCandidate;

        float yDelta = desiredPos.y - newPos.y;
        if (Mathf.Abs(yDelta) > verticalStopDistance)
        {
            float verticalFactor = 1f - Mathf.Exp(-verticalLerpSpeed * deltaTime);
            newPos.y = Mathf.Lerp(newPos.y, desiredPos.y, verticalFactor);
        }
        else
        {
            newPos.y = desiredPos.y;
        }

        if (useBounds && minBound != null && maxBound != null)
        {
            Vector3 minPos = minBound.position;
            Vector3 maxPos = maxBound.position;

            newPos.x = Mathf.Clamp(newPos.x, minPos.x, maxPos.x);
            newPos.y = Mathf.Clamp(newPos.y, minPos.y, maxPos.y);
            newPos.z = Mathf.Clamp(newPos.z, minPos.z, maxPos.z);
        }

        return newPos;
    }

    private void BuildSplineSampleTable()
    {
        if (activeSpline == null)
        {
            _splineSampleTs = null;
            _splineSamplePositions = null;
            return;
        }

        int samples = Mathf.Max(2, splineSamples);
        _splineSampleTs = new float[samples];
        _splineSamplePositions = new Vector3[samples];

        for (int i = 0; i < samples; i++)
        {
            float t = (samples == 1) ? 0f : (float)i / (samples - 1);
            _splineSampleTs[i] = t;
            _splineSamplePositions[i] = ConvertToVector3(activeSpline.EvaluatePosition(t));
        }
    }

    private float GetClosestTOnSpline(Vector3 point)
    {
        if (activeSpline == null) return 0f;
        if (!useClosestPointProjection) return 0f;

        if (_splineSampleTs == null || _splineSamplePositions == null || _splineSampleTs.Length < 2)
        {
            BuildSplineSampleTable();
            if (_splineSampleTs == null || _splineSamplePositions == null || _splineSampleTs.Length < 2)
                return 0f;
        }

        int bestIndex = 0;
        float bestSqr = float.PositiveInfinity;

        for (int i = 0; i < _splineSamplePositions.Length; i++)
        {
            float d = (point - _splineSamplePositions[i]).sqrMagnitude;
            if (d < bestSqr)
            {
                bestSqr = d;
                bestIndex = i;
            }
        }

        float tLeft, tRight;
        if (_splineSampleTs.Length == 2)
        {
            tLeft = 0f;
            tRight = 1f;
        }
        else
        {
            float step = 1f / (_splineSampleTs.Length - 1);
            tLeft = Mathf.Clamp01(_splineSampleTs[bestIndex] - step);
            tRight = Mathf.Clamp01(_splineSampleTs[bestIndex] + step);
        }

        if (projectionRefineIterations <= 0 || (tRight - tLeft) <= 1e-6f)
            return _splineSampleTs[bestIndex];

        return GoldenSectionSearchClosestT(point, tLeft, tRight, projectionRefineIterations);
    }

    private float GoldenSectionSearchClosestT(Vector3 point, float a, float b, int iterations)
    {
        const float invPhi = 0.61803398875f;
        const float invPhi2 = 0.38196601125f;

        float c = a + invPhi2 * (b - a);
        float d = a + invPhi * (b - a);

        float fc = (ConvertToVector3(activeSpline.EvaluatePosition(c)) - point).sqrMagnitude;
        float fd = (ConvertToVector3(activeSpline.EvaluatePosition(d)) - point).sqrMagnitude;

        for (int i = 0; i < iterations; i++)
        {
            if (fc < fd)
            {
                b = d;
                d = c;
                fd = fc;
                c = a + invPhi2 * (b - a);
                fc = (ConvertToVector3(activeSpline.EvaluatePosition(c)) - point).sqrMagnitude;
            }
            else
            {
                a = c;
                c = d;
                fc = fd;
                d = a + invPhi * (b - a);
                fd = (ConvertToVector3(activeSpline.EvaluatePosition(d)) - point).sqrMagnitude;
            }
        }

        return (fc < fd) ? c : d;
    }

    private void CacheFollowBaseRotation()
    {
        _baseFollowRotation = transform.rotation;
        Vector3 forwardDirection = transform.forward;
        forwardDirection.y = 0f;
        _baseFollowForwardXZ =
            forwardDirection.sqrMagnitude > 0.000001f ? forwardDirection.normalized : Vector3.forward;
        _lookAheadAngle = 0f;
    }

    private void ResetMovementState()
    {
        _followVelocity = Vector3.zero;
        _lookAheadAngle = 0f;
        _usingPlayerVelocity = false;
        _hasLeftTarget = false;

        _gatedMoving = false;
        _moveDelayTimer = 0f;

        _inMoveDelayWindow = false;
        _snapStableTimer = 0f;

        if (target != null)
        {
            Vector3 targetPosition = (target != null && _hasFixedSamples) ? _fixedCurrPos : target.position;
            _lastTargetPos = targetPosition;

            if (_targetRigidbody == null)
                _targetRigidbody = target.GetComponent<Rigidbody>();
        }
    }

    private void StartTransition(float duration)
    {
        if (duration <= 0f)
        {
            _isTransitioning = false;
            _transitionDuration = 0f;
            return;
        }

        _isTransitioning = true;
        _transitionDuration = duration;
        _transitionElapsed = 0f;
        _transitionStartPos = transform.position;

        _followVelocity = Vector3.zero;
        _usingPlayerVelocity = false;
        _hasLeftTarget = false;

        _gatedMoving = false;
        _moveDelayTimer = 0f;

        _inMoveDelayWindow = false;
        _snapStableTimer = 0f;
    }

    public void SetBounds(Transform minimumBoundTransform, Transform maximumBoundTransform)
    {
        if (minBound == minimumBoundTransform && maxBound == maximumBoundTransform) return;

        minBound = minimumBoundTransform;
        maxBound = maximumBoundTransform;
        StartTransition(transitionFollowTime);
    }

    public void SetFollowMode()
    {
        if (mode == CameraMode.Follow) return;

        mode = CameraMode.Follow;
        ResetMovementState();
        CacheFollowBaseRotation();
        StartTransition(transitionFollowTime);
    }

    public void SetFixedMode(Transform cameraPoint)
    {
        SetFixedMode(cameraPoint, null, true);
    }

    public void SetFixedMode(Transform cameraPoint, Transform lookPoint, bool followPlayer)
    {
        if (mode == CameraMode.Fixed &&
            fixedCameraPoint == cameraPoint &&
            fixedLookPoint == lookPoint &&
            fixedFollowPlayer == followPlayer)
            return;

        mode = CameraMode.Fixed;
        fixedCameraPoint = cameraPoint;
        fixedLookPoint = lookPoint;
        fixedFollowPlayer = followPlayer;

        ResetMovementState();
        StartTransition(transitionFixedTime);
    }

    public void SetSplineMode(SplineContainer splineContainer)
    {
        if (mode == CameraMode.Spline && activeSpline == splineContainer) return;

        mode = CameraMode.Spline;
        activeSpline = splineContainer;

        BuildSplineSampleTable();
        ResetMovementState();
        StartTransition(transitionSplineTime);
    }
}
