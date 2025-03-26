using UnityEngine;

public class TopTile : MonoBehaviour
{
    public SpriteRenderer TileSpriteRenderer { get; private set; }
    
    private void Awake()
    {
        TileSpriteRenderer = transform.GetChild(0).GetComponent<SpriteRenderer>();
    }
    
    public void UpdateTopTile()
    {
        Debug.Log("Updating top tile");
        
        // Check surrounding top tiles and update them
    }
}
