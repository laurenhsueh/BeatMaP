using UnityEngine;

public class InstrumentalTrack : BaseBehavior
{
    public override void Spawn()
    {
        base.Spawn();
        Debug.Log("Instrumental visual spawned.");
    }
}