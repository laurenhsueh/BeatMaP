using UnityEngine;
using TMPro;

[RequireComponent(typeof(TMP_Text))]
public class LookAtCamera : MonoBehaviour
{
    public bool lockYAxis = true;

    Transform cameraTransform;

    void Start()
    {
        if (Camera.main != null)
            cameraTransform = Camera.main.transform;
    }

    void LateUpdate()
    {
        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;

        if (cameraTransform == null)
            return;

        Vector3 lookTarget = cameraTransform.position;
        if (lockYAxis)
            lookTarget.y = transform.position.y;

        transform.LookAt(lookTarget);
        transform.Rotate(0f, 180f, 0f);
    }
}