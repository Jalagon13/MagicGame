using System;
using System.Collections;
using UnityEngine;
using DG.Tweening;

public class MagicCircle : MonoBehaviour
{
    [field: SerializeField] public float RotationSpeed { get; private set; } = 90f; // degrees per second
    [field: SerializeField] public float StopDuration { get; private set; } = 0.135f;

    private SpriteRenderer _magicCircleSr;
    private ParticleSystem _castParticles;
    private Vector3 _originalScale;

    private void Awake()
    {
        _castParticles = transform.GetChild(0).GetComponent<ParticleSystem>();
        _magicCircleSr = GetComponent<SpriteRenderer>();
        _originalScale = transform.localScale;
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
        // Ensure alpha is reset before playing animation
        Color color = _magicCircleSr.color;
        color.a = 1f;
        _magicCircleSr.color = color;
        transform.localScale = Vector3.zero;
        transform.DOScale(_originalScale, castTime);
        _castParticles.Play();
    }

    public void StopAnimation(bool destroyGameObject)
    {
        Sequence stopSequence = DOTween.Sequence();

        stopSequence.Append(transform.DOScale(_originalScale * 1.5f, StopDuration));
        stopSequence.Join(_magicCircleSr.DOFade(0f, StopDuration));
        _castParticles.Stop();

        if (destroyGameObject)
        {
            stopSequence.OnComplete(() => Destroy(gameObject)); // safer than OnKill
        }
        else
        {
            // After fade out, reset alpha and disable sprite renderer for next use
            stopSequence.OnComplete(() =>
            {
                if (this != null && _magicCircleSr != null)
                {
                    Color c = _magicCircleSr.color;
                    c.a = 1f;
                    _magicCircleSr.color = c;
                    _magicCircleSr.enabled = false;
                }
            });
        }
    }

    private void OnDestroy()
    {
        transform.DOKill();
        if (_magicCircleSr != null)
            _magicCircleSr.DOKill();
    }
}