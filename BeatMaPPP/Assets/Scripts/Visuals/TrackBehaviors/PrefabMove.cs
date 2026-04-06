using UnityEngine;

public class PrefabMove : MonoBehaviour
{
    public float speed = 5f;
    public bool isMoving = true;

    private Vector3 moveDirection;

    /// <summary>
    /// Call this after Instantiate to lock a world-space travel direction.
    /// If not called, defaults to the object's own transform.forward.
    /// </summary>
    public void SetDirection(Vector3 direction)
    {
        moveDirection = direction.normalized;
    }

    private void Start()
    {
        // Fall back to transform.forward if no direction was injected
        if (moveDirection == Vector3.zero)
            moveDirection = transform.forward;
    }

    private void Update()
    {
        if (!isMoving) return;
        transform.position += moveDirection * speed * Time.deltaTime;
    }

    private void OnCollisionEnter(Collision collision)
    {
        float newY = Random.Range(0f, 360f);
        transform.rotation = Quaternion.Euler(0f, newY, 0f);
        // Re-align movement direction to new facing after bounce
        moveDirection = transform.forward;
    }
}


// using UnityEngine;

// public class PrefabMove : MonoBehaviour
// {
//     public float speed = 0.01f;
//     public bool isMoving = true;

//     private void Update()
//     {
//         transform.position += transform.forward * speed * Time.deltaTime;
//     }

//     private void OnCollisionEnter(Collision collision)
//     {
//         float newY = Random.Range(0f, 360f);
//         transform.rotation = Quaternion.Euler(0f, newY, 0f);
//     }
// }