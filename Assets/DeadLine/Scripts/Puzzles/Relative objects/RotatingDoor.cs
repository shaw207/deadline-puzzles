using UnityEngine;
using System.Collections;

public class RotatingDoor : MonoBehaviour, IRelativeObjects
{
    [SerializeField] private float openAngle = 90f;
    [SerializeField] private float moveDuration = 1f;
    [SerializeField] private Vector3 rotationAxis = Vector3.up;

    private Quaternion _closedRot;
    private Quaternion _openRot;

    private Coroutine _rotateRoutine;
    private bool _isMoving = false;
    private bool _isOpen = false;

    private void Awake()
    {
        _closedRot = transform.localRotation;

        if (rotationAxis == Vector3.zero)
            rotationAxis = Vector3.up;
        rotationAxis.Normalize();

        _openRot = _closedRot * Quaternion.AngleAxis(openAngle, rotationAxis);
    }

    public void action()
    {
        if (_isOpen) return;
        StartRotate(_openRot, true);
    }

    public void action(object obj)
    {
        action();
    }

    public void Noaction()
    {
        if (!_isOpen) return;
        StartRotate(_closedRot, false);
    }

    public void Noaction(object obj)
    {
        Noaction();
    }

    public bool checkStatu() => !_isMoving;
    public bool UseParam() => false;

    private void StartRotate(Quaternion targetRot, bool open)
    {
        if (_rotateRoutine != null)
            StopCoroutine(_rotateRoutine);

        _rotateRoutine = StartCoroutine(RotateRoutine(targetRot, open));
    }

    private IEnumerator RotateRoutine(Quaternion targetRot, bool open)
    {
        _isMoving = true;

        Quaternion startRot = transform.localRotation;
        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime / moveDuration;
            float lerp = Mathf.Clamp01(t);

            transform.localRotation = Quaternion.Slerp(startRot, targetRot, lerp);
            yield return null;
        }

        transform.localRotation = targetRot;

        _isMoving = false;
        _isOpen = open;
        _rotateRoutine = null;
    }
}

