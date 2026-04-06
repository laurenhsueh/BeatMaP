using UnityEngine;

public class VocalsTrack : BaseBehavior
{
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float laneOffset = 1.5f;
    [SerializeField] private float laneHeight = 0.03f;

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
            Debug.Log("Vocals visual spawned.");
        }

        return spawned;
    }
}