using UnityEngine;

public class OtherTrack : BaseBehavior
{
    [SerializeField] private float moveSpeed = 0.00000001f;

    public override GameObject Spawn()
    {
        GameObject spawned = base.Spawn();

        if (spawned != null)
        {
            PrefabMove mover = spawned.AddComponent<PrefabMove>();
            mover.speed = moveSpeed;
            spawned.AddComponent<IdleSwitch>();
            Debug.Log("Other visual spawned.");
        }

        return spawned;
    }
}