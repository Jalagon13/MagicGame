using System;
using System.Collections;
using UnityEngine;
using DG.Tweening;

public class MagicCircle : MonoBehaviour
{
    [field: SerializeField] public float RotationSpeed { get; private set; } = 90f; // degrees per second
    [field: SerializeField] public float StopDuration { get; private set; } = 0.135f; // degrees per second
    
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
        _magicCircleSr.enabled = true;
        transform.localScale = Vector3.zero;
        transform.DOScale(Vector3.one, castTime);
    }

    public void StopAnimation()
    {
        Sequence stopSequence = DOTween.Sequence();

        // Scale from 1 to 1.5
        stopSequence.Append(transform.DOScale(1.5f, StopDuration));

        // Fade out the sprite renderer
        stopSequence.Join(_magicCircleSr.DOFade(0f, StopDuration));

        // After animation, destroy the game object
        stopSequence.OnKill(() => Destroy(gameObject));
    }
}