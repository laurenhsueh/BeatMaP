using UnityEngine;

public class CirclePath : MonoBehaviour
{
    public float radius = 3f;
    public float speed = 1f;

    private float angle = 0f;
    private Vector3 centerPoint;

    void Start()
    {
        centerPoint = transform.position;
    }

    void Update()
    {
        angle += speed * Time.deltaTime;

        float x = centerPoint.x + Mathf.Cos(angle) * radius;
        float z = centerPoint.z + Mathf.Sin(angle) * radius;

        transform.position = new Vector3(x, centerPoint.y, z);
    }
}