using UnityEngine;
using System.Collections;

public class IdleSwitch : MonoBehaviour
{
    private Animator animator;
    private PrefabMove mover;

    private void Start()
    {
        animator = GetComponentInChildren<Animator>();
        mover = GetComponent<PrefabMove>();
        StartCoroutine(SwitchToIdle());
    }

    private IEnumerator SwitchToIdle()
    {
        yield return new WaitForSeconds(Random.Range(0f, 4.5f));
        animator.SetBool("isWalking", false);
        if (mover != null) mover.enabled = false;
    }
}