using UnityEngine;

public class PianoTrack : BaseBehavior
{
    public override void Spawn()
    {
        base.Spawn();
        Debug.Log("Piano visual spawned.");
    }
}