using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class PlayerSpriteOverrider : MonoBehaviour
{
    [field: SerializeField] public ArmorType ArmorType { get; private set; }
    [field: SerializeField] public bool UseArmSheet { get; private set; }

    private HashSet<Sprite> _overrideSheet;
    private SpriteRenderer _playerPartRenderer;

    private void Awake()
    {
        _playerPartRenderer = GetComponent<SpriteRenderer>();
    }

    private void Start()
    {
        PlayerStats.Instance.OnArmorEquipped += OnArmorEquipped;
        PlayerStats.Instance.OnArmorUnEquipped += OnArmorUnEquipped;
    }

    private void OnArmorEquipped(object sender, PlayerStats.ArmorChangedEventArgs e)
    {
        if (e.ArmorItem.ArmorType != ArmorType) return;
    
        _overrideSheet = new();
        
        Texture2D armorSheet = null;
        
        if(UseArmSheet)
        {
            armorSheet = e.ArmorItem.ArmorSprites.ArmSprites;
        }
        else
        {
            switch (ArmorType)
            {
                case ArmorType.Head:
                    armorSheet = e.ArmorItem.ArmorSprites.HeadSprites;
                    break;
                case ArmorType.Chest:
                    armorSheet = e.ArmorItem.ArmorSprites.ChestSprites;
                    break;
                case ArmorType.Legs:
                    armorSheet = e.ArmorItem.ArmorSprites.LegsSprites;
                    break;
            }
        }
        
        UnityEngine.Object[] data = AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(armorSheet));
        if (data != null)
        {
            foreach (UnityEngine.Object obj in data)
            {
                if (obj.GetType() == typeof(Sprite))
                {
                    Sprite sprite = obj as Sprite;
                    _overrideSheet.Add(sprite);
                }
            }
        }
    }

    private void OnArmorUnEquipped(object sender, PlayerStats.ArmorChangedEventArgs e)
    {
        if (e.ArmorItem.ArmorType != ArmorType) return;
    
        _overrideSheet = null;
    }

    private void LateUpdate()
    {
        if(_overrideSheet == null) return;

        foreach (var sprite in _overrideSheet)
        {
            string spriteName = sprite.name;
            if (_playerPartRenderer.sprite.name == spriteName)
            {
                _playerPartRenderer.sprite = sprite;
            }
        }
    }
    
    private void OnDestroy()
    {
        PlayerStats.Instance.OnArmorEquipped -= OnArmorEquipped;
        PlayerStats.Instance.OnArmorUnEquipped -= OnArmorUnEquipped;
    }
}
