using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Hands;
using TMPro;

public class GestureTracking : MonoBehaviour
{
    public TextMeshPro outText;

    public GameObject lDirections;
    public GameObject rDirections;
    [SerializeField, Range(0f, 1f)] float grabThreshold = 1f;
    [SerializeField, Range(-1f, 1f)] float palmDownThreshold = 0.6f;

    InputAction LGrab;
    InputAction RGrab;
    InputAction LPinch;
    InputAction RPinch;
    XRHandSubsystem handSubsystem;

    void Start()
    {
        LGrab = InputSystem.actions.FindAction(actionNameOrId: "LeftGrab");
        RGrab = InputSystem.actions.FindAction(actionNameOrId: "RightGrab");
        LPinch = InputSystem.actions.FindAction(actionNameOrId: "LeftPinch");
        RPinch = InputSystem.actions.FindAction(actionNameOrId: "RightPinch");

        SetDirectionsActive(lDirections, false);
        SetDirectionsActive(rDirections, false);
    }

    void Update()
    {
        EnsureHandSubsystem();

        float leftGrabValue = ReadActionValue(LGrab);
        float rightGrabValue = ReadActionValue(RGrab);
        float leftPinchValue = ReadActionValue(LPinch);
        float rightPinchValue = ReadActionValue(RPinch);

        bool leftShowDirections = ShouldShowDirections(true, leftGrabValue);
        bool rightShowDirections = ShouldShowDirections(false, rightGrabValue);

        SetDirectionsActive(lDirections, leftShowDirections);
        SetDirectionsActive(rDirections, rightShowDirections);

        if (outText != null)
        {
            outText.text = "Left Grab: " + leftGrabValue
                + "\nRight Grab: " + rightGrabValue
                + "\nLeft Pinch: " + leftPinchValue
                + "\nRight Pinch: " + rightPinchValue
                + "\nLeft Palm Down: " + leftShowDirections
                + "\nRight Palm Down: " + rightShowDirections;
        }
    }

    void EnsureHandSubsystem()
    {
        if (handSubsystem != null && handSubsystem.running)
            return;

        var subsystems = new System.Collections.Generic.List<XRHandSubsystem>();
        SubsystemManager.GetSubsystems(subsystems);

        for (int index = 0; index < subsystems.Count; index++)
        {
            if (!subsystems[index].running)
                continue;

            handSubsystem = subsystems[index];
            return;
        }

        handSubsystem = null;
    }

    bool ShouldShowDirections(bool isLeftHand, float grabValue)
    {
        if (grabValue < grabThreshold || handSubsystem == null)
            return false;

        XRHand hand = isLeftHand ? handSubsystem.leftHand : handSubsystem.rightHand;
        if (!hand.isTracked)
            return false;

        Vector3 palmDirection = hand.rootPose.rotation * Vector3.down;
        return Vector3.Dot(palmDirection.normalized, Vector3.down) >= palmDownThreshold;
    }

    static float ReadActionValue(InputAction action)
    {
        return action != null ? action.ReadValue<float>() : 0f;
    }

    static void SetDirectionsActive(GameObject directionObject, bool isActive)
    {
        if (directionObject != null && directionObject.activeSelf != isActive)
            directionObject.SetActive(isActive);

    }
}
