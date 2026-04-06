using UnityEngine;

public class PianoTrack : BaseBehavior
{
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float laneOffset = 0.9f;
    [SerializeField] private float laneHeight = 0.04f;

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
            Debug.Log("Piano visual spawned.");
        }

        return spawned;
    }
}