// using UnityEngine;

// public class GuitarTrack : BaseBehavior
// {
//     public override GameObject Spawn()
//     {
//         GameObject spawned = base.Spawn();
//         Debug.Log("Guitar visual spawned.");
//         return spawned;
//     }
// }


using UnityEngine;

public class GuitarTrack : BaseBehavior
{
    public override void Spawn()
    {
        base.Spawn();
        Debug.Log("Guitar visual spawned.");
    }
}