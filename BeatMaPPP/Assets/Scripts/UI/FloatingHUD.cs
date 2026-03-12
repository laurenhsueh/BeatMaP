using UnityEngine;

public class FloatingHUD : MonoBehaviour
{
    public Transform scoreTarget;
    Transform camTrans; // The position to move toward

    void Start()
    {
        camTrans = Camera.main.transform; // Looks for the main camera and gets a reference to its transform
    }

    void Update()
    {
        transform.position = Vector3.Lerp(transform.position, scoreTarget.position, .02f); // Move this object a little closer to its target every frame
        transform.LookAt(new Vector3(camTrans.position.x, transform.position.y, camTrans.position.z)); // Makes the text look at the camera at all times but keeps it just rotating on the Y-axis
    }
}