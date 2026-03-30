using UnityEngine;

public class NewMonoBehaviourScript : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}


// i am working with c# in unity. i currently have a json file of a song split up into multiple tracks (bass, drums, guitar, instrumental, other, piano, vocals), where at every second, it shows details like frequencies and amplitudes of each track.

// SCRIPTS
// Shared prefab movement/behavior (ex: moving forward through physics/force)
    // Each track inherits from main prefab movement to have specialized behavior (7 separate scripts: bass, drums, guitar, instrumental, other, piano, vocals). Do one for now, for instrumental
    // Track functions instantiate/spawn the prefab when called. Specify anchor and location. --> might need ground/surface detection data here
    // Each function has a prefab attached to it, a field that determines which prefab it is
    // Arrow is defined by the vocal track. If vocals are unavailable, switch to the next most dominant track

// DONE Dominance ordering --> return list (of track name) where index indicates how dominant it is; call at every 5 seconds or timestamp to generate new dominance list
    // Rare visual appearance: 30% chance to ignore first 3 dominant indices and take random index from index 3 to end of list. Make sure the value at that index is greater than 0. On the same script as dominance list. Change output of dominance list

// Display visuals: parameter is dominance list. Calls track functions based on if/else based on dominance
    // Sync to the music. ask how to do this. connect to the audio time?


// JSON: add parameter called visual (determines which track will dictate the arrow; boolean value)