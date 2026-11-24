using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

/// <summary>Replays the native camera trigger at an original mine entrance reached by a demo shortcut.</summary>
public sealed class OriginalRoomCameras : MonoBehaviour
{
    public Transform bridgeEye;
    public Transform bridgeLook;
    public bool bridgeFollowsPlayer;
    public SplineContainer crystalRail;
    public Transform valveEye;
    public Transform valveLook;
    public bool valveFollowsPlayer;

    public static void Restore(int room, Transform player)
    {
        var camera = Camera.main;
        if (!camera || !player || !camera.TryGetComponent(out OriginalRoomCameras setup)) return;
        setup.Enter(room, player);
    }

    void Enter(int room, Transform player)
    {
        var rig = GetComponent<CameraController>();
        if (room == 1)
        {
            // This authored rail has a unit-scale parent. The native controller subsequently
            // evaluates its own closest-point projection every fixed tick.
            float3 localTarget = (float3)crystalRail.transform.InverseTransformPoint(player.position);
            SplineUtility.GetNearestPoint(crystalRail.Spline, localTarget, out float3 _, out float parameter);
            Place((Vector3)crystalRail.EvaluatePosition(parameter), player.position);
            rig.SetSplineMode(crystalRail);
        }
        else
        {
            bool bridge = room == 0;
            var eye = bridge ? bridgeEye : valveEye;
            var look = bridge ? bridgeLook : valveLook;
            bool follows = bridge ? bridgeFollowsPlayer : valveFollowsPlayer;
            Place(eye.position, follows || !look ? player.position : look.position);
            rig.SetFixedMode(eye, look, follows);
        }
    }

    void Place(Vector3 eye, Vector3 aim)
    {
        var sight = aim - eye;
        transform.SetPositionAndRotation(eye, sight.sqrMagnitude > .0001f
            ? Quaternion.LookRotation(sight, Vector3.up) : transform.rotation);
    }
}