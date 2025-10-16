using UnityEngine;

namespace ProjectTinker
{
    [CreateAssetMenu(fileName = "ArmorSpritesSO", menuName = "Create ArmorSpritesSO")]
    public class ArmorSpritesSO : ScriptableObject
    {
        [field: SerializeField] public Texture2D HeadSprites { get; private set; }
        [field: SerializeField] public Texture2D ArmSprites { get; private set; }
        [field: SerializeField] public Texture2D ChestSprites { get; private set; }
        [field: SerializeField] public Texture2D LegsSprites { get; private set; }
    }
}
