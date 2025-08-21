using System;
using System.Collections;
using UnityEngine;
using DG.Tweening;

public class MagicCircle : MonoBehaviour
{
    [field: SerializeField] public float RotationSpeed { get; private set; } = 90f; // degrees per second
    [field: SerializeField] public float StopDuration { get; private set; } = 0.135f;

    private SpriteRenderer _magicCircleSr;

    private void Awake()
    {
        _magicCircleSr = GetComponent<SpriteRenderer>();
        _magicCircleSr.enabled = false;
    }

    private void Update()
    {
        transform.Rotate(0f, 0f, RotationSpeed * Time.deltaTime);
    }

    public void StartAnimation(float castTime)
    {
        _magicCircleSr = GetComponent<SpriteRenderer>();
        _magicCircleSr.enabled = true;
        transform.localScale = Vector3.zero;
        transform.DOScale(Vector3.one, castTime);
    }

    public void StopAnimation()
    {
        Sequence stopSequence = DOTween.Sequence();

        stopSequence.Append(transform.DOScale(1.5f, StopDuration));
        stopSequence.Join(_magicCircleSr.DOFade(0f, StopDuration));

        stopSequence.OnComplete(() => Destroy(gameObject)); // safer than OnKill
    }

    private void OnDestroy()
    {
        transform.DOKill();
        if (_magicCircleSr != null)
            _magicCircleSr.DOKill();
    }
}