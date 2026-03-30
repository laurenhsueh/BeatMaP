using UnityEngine;

public class OtherTrack : BaseBehavior
{
    public override void Spawn()
    {
        base.Spawn();
        Debug.Log("Other visual spawned.");
    }
}