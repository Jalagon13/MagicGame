using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace ProjectWizard
{
    public class PlayerArmSortingLayer : MonoBehaviour
    {
        [SerializeField] private GameObject _armPivotGO;

        private PlayerHand _playerHand;
        private SortingGroup _sortingGroup;

        private void Awake()
        {
            _sortingGroup = GetComponent<SortingGroup>();
            _playerHand = GetComponent<PlayerHand>();
            _playerHand.SwingDirection.OnValueChanged += OnSwingChanged;
            _playerHand.CastingDirection.OnValueChanged += OnCastingDirectionChanged;
        }

        private void OnDestroy()
        {
            _playerHand.SwingDirection.OnValueChanged += OnSwingChanged;
            _playerHand.CastingDirection.OnValueChanged -= OnCastingDirectionChanged;
        }

        private void OnCastingDirectionChanged(CardinalDirection previousValue, CardinalDirection newValue)
        {
            if (previousValue == CardinalDirection.None && newValue != CardinalDirection.None)
            {
                switch (newValue)
                {
                    case CardinalDirection.North:
                        PutSpriteBack();
                        PivotYToPositive();
                        break;
                    case CardinalDirection.South:
                        PutSpriteFront();
                        PivotYToPositive();
                        break;
                    case CardinalDirection.West:
                        PutSpriteFront();
                        PivotYToNegative();
                        break;
                    case CardinalDirection.East:
                        PutSpriteFront();
                        PivotYToPositive();
                        break;
                }
            }
            else if (previousValue != CardinalDirection.None && newValue != CardinalDirection.None)
            {
                switch (newValue)
                {
                    case CardinalDirection.North:
                        PutSpriteBack();
                        PivotYToPositive();
                        break;
                    case CardinalDirection.South:
                        PutSpriteFront();
                        PivotYToPositive();
                        break;
                    case CardinalDirection.West:
                        PutSpriteFront();
                        PivotYToNegative();
                        break;
                    case CardinalDirection.East:
                        PutSpriteFront();
                        PivotYToPositive();
                        break;
                }
            }
        }

        private void OnSwingChanged(CardinalDirection previousValue, CardinalDirection newValue)
        {
            if (newValue == CardinalDirection.None) return;

            switch (newValue)
            {
                case CardinalDirection.North:
                    PutSpriteBack();
                    PivotYToPositive();
                    break;
                case CardinalDirection.South:
                    PutSpriteFront();
                    PivotYToPositive();
                    break;
                case CardinalDirection.West:
                    PutSpriteFront();
                    PivotYToNegative();
                    break;
                case CardinalDirection.East:
                    PutSpriteFront();
                    PivotYToPositive();
                    break;
            }
        }

        private void PutSpriteFront()
        {
            _sortingGroup.sortingOrder = 10; // NTFS: 10 instead of 1 so it can be rendered above the arm sprites
        }

        private void PutSpriteBack()
        {
            _sortingGroup.sortingOrder = -1;
        }

        private void PivotYToPositive()
        {
            _armPivotGO.transform.localScale = new Vector3(1, 1, 1);
        }

        private void PivotYToNegative()
        {
            _armPivotGO.transform.localScale = new Vector3(1, -1, 1);
        }
    }
}
