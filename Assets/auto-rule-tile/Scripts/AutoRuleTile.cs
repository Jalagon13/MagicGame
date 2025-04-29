using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

// ----------------------------------------------------------------------------
// Author: Alexandre Brull - Pandaroo
// https://pandaroo.be
// ----------------------------------------------------------------------------

namespace Pandaroo.autoruletile
{
    [ExecuteInEditMode]
    [CreateAssetMenu(fileName = "New Auto Rule Tile", menuName = "Tiles/Auto Rule Tile")]
    public class AutoRuleTile : ScriptableObject
    {
#if (UNITY_EDITOR)

    [SerializeField]
    Texture2D TileMap;
    [SerializeField]
    RuleTile RuleTileTemplate;
    RuleTile RuleTileTemplate_Default;
    [SerializeField]
    RuleTile OverrideExistingRuleTile;
    
    private RuleTile _ruleTileTemplateRef;

    // private void OnValidate()
    // {
    //     // If there is a default template, load it when the asset is created.
    //     RuleTileTemplate_Default = Resources.Load("AutoRuleTile_default") as RuleTile;
    //     if (RuleTileTemplate_Default != null)
    //     {
    //         RuleTileTemplate = RuleTileTemplate_Default;
    //     }
    // }

    public void OverrideRuleTile()
    {
        _ruleTileTemplateRef = RuleTileTemplate;

        // Make a copy of the Rule Tile Template from a new asset.
        RuleTile _new = CreateInstance<RuleTile>();

        // Ensure the m_TilingRules are copied correctly
        _new.m_TilingRules = RuleTileTemplate.m_TilingRules.Select(rule => new RuleTile.TilingRule
        {
            m_Sprites = new Sprite[rule.m_Sprites.Length], // Ensure a new array for sprites
        }).ToList();

        // Now set the sprites for _new.m_TilingRules as intended
        for (int i = 0; i < RuleTileTemplate.m_TilingRules.Count; i++)
        {
            _new.m_TilingRules[i] = RuleTileTemplate.m_TilingRules[i].Clone();
            _new.m_TilingRules[i].m_Sprites[0] = RuleTileTemplate.m_TilingRules[i].m_Sprites[0];
        }

        Debug.Log("RuleTileTemplate: " + RuleTileTemplate.m_TilingRules.Count);
        Debug.Log("New RuleTile: " + _new.m_TilingRules.Count);

        // Get all the sprites in the Texture2D file (TileMap)
        string spriteSheet = AssetDatabase.GetAssetPath(TileMap);
        Sprite[] sprites = AssetDatabase.LoadAllAssetsAtPath(spriteSheet)
            .OfType<Sprite>().ToArray();
            
        foreach (var item in sprites)
        {
            Debug.Log(item.name);
        }

        Debug.Log("Number of Sprites: " + sprites.Length + "Number of Rules: " + RuleTileTemplate.m_TilingRules.Count);
        if (sprites.Length - 4 != RuleTileTemplate.m_TilingRules.Count)
        {
            Debug.LogWarning("The Tilemap doesn't have the same number of sprites than the Rule Tile template has rules.");
        }

        // Set all the sprites of the TileMap.
        for (int i = 0; i < RuleTileTemplate.m_TilingRules.Count; i++)
        {
            Debug.Log(_new.m_TilingRules.Count);
            _new.m_TilingRules[i].m_Sprites[0] = sprites[i];
            _new.m_DefaultSprite = sprites[0];
        }

        RuleTileTemplate = _ruleTileTemplateRef;

        // Replace this Asset with the new one.
        if (OverrideExistingRuleTile != null)
        {
            string name = OverrideExistingRuleTile.name;
            // EditorUtility.CopySerialized(_new, OverrideExistingRuleTile);
            OverrideExistingRuleTile.m_TilingRules = _new.m_TilingRules;
            OverrideExistingRuleTile.name = name;
            OverrideExistingRuleTile.m_DefaultSprite = _new.m_DefaultSprite;
            DestroyImmediate(_new);
            AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(this));
        }
        else AssetDatabase.CreateAsset(_new, AssetDatabase.GetAssetPath(this));
    }


#endif
    }

}

