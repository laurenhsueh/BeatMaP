using UnityEngine;

public class OtherTrack : BaseBehavior
{
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float laneOffset = 2.1f;
    [SerializeField] private float laneHeight = 0.02f;

    public override GameObject Spawn()
    {
        GameObject spawned = base.Spawn();

        if (spawned != null)
        {
            PrefabMove mover = spawned.AddComponent<PrefabMove>();
            mover.speed = moveSpeed;
            mover.SetDirection(Camera.main.transform.forward);
            mover.SetOffsets(laneOffset, laneHeight);
            // spawned.AddComponent<IdleSwitch>();
            Debug.Log("Other visual spawned.");
        }

        return spawned;
    }
}