using System;
using System.Collections.Generic;
using FMODUnity;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SpellWheelUI : MonoBehaviour
{
    [field: SerializeField] public GameObject SpellUIPrefab { get; private set; }
    [field: SerializeField] public float DistanceFromCenter { get; private set; }
    [field: SerializeField] public EventReference SpellSelectedSound { get; private set; }

    private Dictionary<GameObject, SpellItemSO> _activeSpellUIDict = new Dictionary<GameObject, SpellItemSO>();
    private int _numOfSpells;
    private GameObject _lastClosestUI = null;
    private GameObject _spellUIHolder;
    private TextMeshProUGUI _selectedSpellText;

    private void Awake()
    {
        _selectedSpellText = transform.GetChild(0).GetComponent<TextMeshProUGUI>();
        _spellUIHolder = transform.GetChild(1).gameObject;
    }

    private void Start()
    {
        SpellManager.Instance.OnSpellWheelOpened += ShowWheel;
        SpellManager.Instance.OnSpellWheelClosed += HideWheel;

        Hide();
    }
    
    private void Update()
    {
        if (_activeSpellUIDict.Count == 0) return;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            GetComponent<RectTransform>(),
            Input.mousePosition,
            null,
            out Vector2 mousePosition
        );

        GameObject closestUI = null;
        float closestDistance = float.MaxValue;

        foreach (GameObject ui in _activeSpellUIDict.Keys)
        {
            RectTransform rt = ui.GetComponent<RectTransform>();
            float distance = Vector2.Distance(mousePosition, rt.anchoredPosition);

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestUI = ui;
            }
        }

        foreach (GameObject ui in _activeSpellUIDict.Keys)
        {
            ui.transform.localScale = (ui == closestUI) ? Vector3.one * 1.5f : Vector3.one;
        }
        
        if (closestUI != _lastClosestUI)
        {
            _lastClosestUI = closestUI;
            OnClosestSpellChanged(_activeSpellUIDict[closestUI]);
        }
    }

    private void ShowWheel(object sender, EventArgs e)
    {
        Show();
    }

    private void HideWheel(object sender, EventArgs e)
    {
        SpellItemSO selectedSpell = null;

        foreach (var kvp in _activeSpellUIDict)
        {
            if (kvp.Key.transform.localScale == Vector3.one * 1.5f)
            {
                selectedSpell = kvp.Value;
                break;
            }
        }

        SpellManager.Instance.SetSelectedSpell(selectedSpell);
        Hide();
    }
    
    private void Show()
    {
        List<SpellItemSO> spellList = SpellManager.Instance.GetSpells();

        _numOfSpells = spellList != null ? spellList.Count : 0;

        if (_numOfSpells > 0)
        {
            float angleStep = 360f / _numOfSpells;
            float angleOffset = 90f;

            for (int i = 0; i < _numOfSpells; i++)
            {
                SpellItemSO spell = spellList[i];
                GameObject spellUI = Instantiate(SpellUIPrefab, _spellUIHolder.transform);
                spellUI.GetComponent<Image>().sprite = spell.SpellUIDisplaySprite;
                _activeSpellUIDict.Add(spellUI, spell);
                RectTransform rt = spellUI.GetComponent<RectTransform>();

                float angle = (i * angleStep + angleOffset) * Mathf.Deg2Rad;
                float x = Mathf.Cos(angle) * DistanceFromCenter;
                float y = Mathf.Sin(angle) * DistanceFromCenter;

                rt.anchoredPosition = new Vector2(x, y);
            }
        }

        gameObject.SetActive(true);
    }
    
    private void Hide()
    {
        foreach (GameObject ui in _activeSpellUIDict.Keys)
        {
            Destroy(ui);
        }
        _activeSpellUIDict.Clear();

        gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        SpellManager.Instance.OnSpellWheelOpened -= ShowWheel;
        SpellManager.Instance.OnSpellWheelClosed -= HideWheel;
    }

    private void OnClosestSpellChanged(SpellItemSO newSpell)
    {
        // Put your desired logic here
        _selectedSpellText.text = newSpell.Name;
        SoundManager.Instance.PlayOneShot(SpellSelectedSound, transform.position);
    }
}
