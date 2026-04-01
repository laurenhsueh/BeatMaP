using UnityEngine;

public class PrefabMove : MonoBehaviour
{
    public float speed = 0.00000001f;
    public bool isMoving = true;

    private void Update()
    {
        transform.position += transform.forward * speed * Time.deltaTime;
    }

    private void OnCollisionEnter(Collision collision)
    {
        float newY = Random.Range(0f, 360f);
        transform.rotation = Quaternion.Euler(0f, newY, 0f);
    }
}