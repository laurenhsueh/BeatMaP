using UnityEngine;

public class BassTrack : BaseBehavior
{
    public override void Spawn()
    {
        base.Spawn();
        Debug.Log("Bass visual spawned.");
    }
}