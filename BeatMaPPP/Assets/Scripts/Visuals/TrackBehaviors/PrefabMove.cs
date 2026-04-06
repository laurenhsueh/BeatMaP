using UnityEngine;

public class PrefabMove : MonoBehaviour
{
    public float speed = 5f;
    public bool isMoving = true;

    private Vector3 moveDirection;
    private bool directionSet = false;

    public void SetDirection(Vector3 direction)
    {
        moveDirection = direction.normalized;
        directionSet = true;
    }

    // private void Start()
    // {
    //     if (!directionSet)
    //         moveDirection = transform.forward;
    // }

    // private void Update()
    // {
    //     if (!isMoving) return;
    //     transform.position += moveDirection * speed * Time.deltaTime;
    // }

    private void Start()
    {
        if (!directionSet)
            moveDirection = transform.forward;
        
        Debug.Log($"PrefabMove Start — direction: {moveDirection}, speed: {speed}, isMoving: {isMoving}");
    }

    private void Update()
    {
        if (!isMoving) return;
        Debug.Log($"PrefabMove moving — direction: {moveDirection}, speed: {speed}");
        transform.position += moveDirection * speed * Time.deltaTime;
    }

    private void OnCollisionEnter(Collision collision)
    {
        float newY = Random.Range(0f, 360f);
        transform.rotation = Quaternion.Euler(0f, newY, 0f);
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