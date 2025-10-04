using DG.Tweening;
using System;
using System.Collections.Generic;
using FMODUnity;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectWizard
{
    public class SpellWheelUI : MonoBehaviour
    {
        private static bool _spellWheelOpen;
        public static bool SpellWheelOpen => _spellWheelOpen;

        [SerializeField]
        private GameObject _spellWheelSlotUIPrefab;

        [Header("Spell Wheel Settings")]
        [SerializeField]
        private float _distanceFromCenter;
        [SerializeField]
        private float _defaultScale = 0.5f;
        [SerializeField]
        private float _selectedScale = 1.0f;
        [SerializeField]
        private float _lerpDuration = 0.25f;
        [SerializeField]
        private float _textYOffset = 50f;

        [Header("Audio Settings")]
        [SerializeField]
        private EventReference _spellSelectedSound;
        [SerializeField]
        private EventReference _spellWheelCloseSound;


        private Dictionary<GameObject, int> _activeSpellUIDict = new Dictionary<GameObject, int>();
        private int _numOfSpells;
        private GameObject _lastClosestUI = null;
        private GameObject _spellUIHolder;
        private TextMeshProUGUI _selectedSpellText;

        private void Awake()
        {
            _selectedSpellText = transform.GetChild(0).GetChild(0).GetComponent<TextMeshProUGUI>();
            _spellUIHolder = transform.GetChild(0).GetChild(1).gameObject;

            Player.OnAnyPlayerSpawned += RegisterDeathPanelLogic;
        }

        private void Start()
        {
            GameInput.Instance.OnSpaceStarted += OpenSpellWheel;
            GameInput.Instance.OnSpaceCanceled += CloseSpellWheel;

            Hide();
        }


        private void OnDestroy()
        {
            GameInput.Instance.OnSpaceStarted -= OpenSpellWheel;
            GameInput.Instance.OnSpaceCanceled -= CloseSpellWheel;

            Player.OnAnyPlayerSpawned -= RegisterDeathPanelLogic;
            if (Player.Instance != null)
            {
                Player.Instance.ServerCharacter.NetLifeState.LifeState.OnValueChanged -= OnPlayerLifeStateChanged;
            }
        }

        private void RegisterDeathPanelLogic(object sender, Player.PlayerIdEventArgs e)
        {
            if (Player.Instance != null)
            {
                Player.Instance.ServerCharacter.NetLifeState.LifeState.OnValueChanged += OnPlayerLifeStateChanged;
            }
        }

        private void OnPlayerLifeStateChanged(LifeState previousValue, LifeState newValue)
        {
            if (previousValue == LifeState.Alive && newValue == LifeState.Dead)
            {
                Hide();
            }
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
                ui.transform.localScale = (ui == closestUI) ? Vector3.one * _selectedScale : Vector3.one * _defaultScale;
            }

            if (closestUI != _lastClosestUI)
            {
                _lastClosestUI = closestUI;
                SpellItemSO newSpell = Player.Instance.SpellCastController.SelectedWandInventoryItem.MagicArray[_activeSpellUIDict[closestUI]];
                _selectedSpellText.text = newSpell.InGameName;
                SoundManager.Instance.PlayOneShot(_spellSelectedSound, transform.position);
            }

            if (_spellWheelOpen && closestUI != null)
            {
                // Reposition _selectedSpellText above the selected spell UI
                // Convert closestUI world position to screen position
                Vector3 screenPos = RectTransformUtility.WorldToScreenPoint(
                    null, // use null for main display
                    closestUI.transform.position
                );
                // Offset by (0, _textYOffset)
                screenPos += new Vector3(0, _textYOffset, 0);
                _selectedSpellText.rectTransform.position = screenPos;
            }
        }

        private void OpenSpellWheel(object sender, EventArgs e)
        {
            _selectedSpellText.enabled = false;

            SpellItemSO[] spellList = Player.Instance.SpellCastController.SelectedWandInventoryItem.MagicArray;

            _numOfSpells = 0;
            foreach (SpellItemSO spell in spellList)
            {
                if (spell != null)
                {
                    _numOfSpells++;
                }
            }

            if (_numOfSpells > 0)
            {
                float angleStep = 360f / _numOfSpells;
                float angleOffset = 90f;
                List<Tweener> tweens = new List<Tweener>();

                for (int i = 0; i < spellList.Length; i++)
                {
                    SpellItemSO spell = spellList[i];

                    if (spell == null)
                        continue;

                    GameObject spellUI = Instantiate(_spellWheelSlotUIPrefab, _spellUIHolder.transform);
                    Image spellUIImage = spellUI.GetComponent<Image>();
                    spellUIImage.sprite = spell.UiDisplay;
                    _activeSpellUIDict.Add(spellUI, i);
                    RectTransform rt = spellUI.GetComponent<RectTransform>();

                    float angle = (i * angleStep + angleOffset) * Mathf.Deg2Rad;
                    float x = Mathf.Cos(angle) * _distanceFromCenter;
                    float y = Mathf.Sin(angle) * _distanceFromCenter;

                    rt.anchoredPosition = Vector2.zero;
                    spellUI.transform.localScale = Vector3.zero;

                    // Set initial alpha to 0
                    Color imgColor = spellUIImage.color;
                    imgColor.a = 0f;
                    spellUIImage.color = imgColor;
                    // Fade in image
                    Tweener fadeTween = spellUIImage.DOFade(1f, _lerpDuration).SetEase(Ease.Linear);

                    Tweener posTween = rt.DOAnchorPos(new Vector2(x, y), _lerpDuration).SetEase(Ease.Linear);
                    Tweener scaleTween = spellUI.transform.DOScale(Vector3.one * _defaultScale, _lerpDuration).SetEase(Ease.Linear);
                    tweens.Add(posTween);
                    tweens.Add(scaleTween);
                    tweens.Add(fadeTween);
                }

                transform.GetChild(0).gameObject.SetActive(true);
                // Only set _spellWheelOpen = true after all tweens complete
                if (tweens.Count > 0)
                {
                    DOTween.Sequence()
                        .AppendInterval(_lerpDuration)
                        .OnComplete(() => { _spellWheelOpen = true; _selectedSpellText.enabled = true; });
                }
                else
                {
                    _spellWheelOpen = true;
                    _selectedSpellText.enabled = true;
                }
            }
            else
            {
                transform.GetChild(0).gameObject.SetActive(true);
                _spellWheelOpen = true;
                _selectedSpellText.enabled = true;
            }
        }

        private void CloseSpellWheel(object sender, EventArgs e)
        {
            _selectedSpellText.enabled = false;

            SoundManager.Instance.PlayOneShot(_spellWheelCloseSound, transform.position);

            // Force closest UI detection even during lerp
            if (_activeSpellUIDict.Count > 0)
            {
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

                _lastClosestUI = closestUI;
            }

            int selectedSpellIndex = -1;

            // Use the last closest UI directly
            if (_lastClosestUI != null && _activeSpellUIDict.TryGetValue(_lastClosestUI, out int index))
            {
                selectedSpellIndex = index;
            }

            // Clamp to valid range just in case
            selectedSpellIndex = Mathf.Clamp(selectedSpellIndex, 0, Player.Instance.SpellCastController.SelectedWandInventoryItem.MagicArray.Length - 1);

            Player.Instance.SpellCastController.SelectSpellByIndex(selectedSpellIndex);

            if (_activeSpellUIDict.Count > 0)
            {
                List<Tweener> tweens = new List<Tweener>();
                foreach (GameObject ui in _activeSpellUIDict.Keys)
                {
                    // Kill any existing tweens to prevent DOTween errors
                    ui.transform.DOKill();
                    Image img = ui.GetComponent<Image>();
                    if (img != null) img.DOKill();

                    RectTransform rt = ui.GetComponent<RectTransform>();
                    Tweener posTween = rt.DOAnchorPos(Vector2.zero, _lerpDuration).SetEase(Ease.Linear);
                    Tweener scaleTween = ui.transform.DOScale(Vector3.zero, _lerpDuration).SetEase(Ease.Linear);
                    tweens.Add(posTween);
                    tweens.Add(scaleTween);

                    // Fade out image
                    if (img != null)
                    {
                        Tweener fadeTween = img.DOFade(0f, _lerpDuration).SetEase(Ease.Linear);
                        tweens.Add(fadeTween);
                    }
                }

                if (tweens.Count > 0)
                {
                    DOTween.Sequence()
                        .AppendInterval(_lerpDuration)
                        .OnComplete(() => { Hide(); });
                }
                else
                {
                    Hide();
                }
            }
            else
            {
                Hide();
            }
        }

        private void Hide()
        {
            _spellWheelOpen = false;
            foreach (GameObject ui in _activeSpellUIDict.Keys)
            {
                // Kill any remaining tweens before destroying
                ui.transform.DOKill();
                Image img = ui.GetComponent<Image>();
                if (img != null) img.DOKill();

                Destroy(ui);
            }
            _activeSpellUIDict.Clear();

            transform.GetChild(0).gameObject.SetActive(false);
        }
    }
}
