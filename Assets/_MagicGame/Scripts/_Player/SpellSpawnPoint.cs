using System;
using FMOD.Studio;
using FMODUnity;
using UnityEngine;

public class SpellSpawnPoint : MonoBehaviour
{
    [SerializeField] 
    private EventReference _sustainedExtractionRaySound;

    private EventInstance _sustainedExtractionRaySoundEventInstance;
    private ParticleSystem _miningParticleSystem;
    private MagicCircle _magicCircle;
    private bool _isMining;

    private void Awake()
    {
        _magicCircle = transform.GetChild(0).GetComponent<MagicCircle>();
        _miningParticleSystem = transform.GetChild(1).GetComponent<ParticleSystem>();
    }

    private void Start()
    {
        _sustainedExtractionRaySoundEventInstance = SoundManager.Instance.CreateInstance(_sustainedExtractionRaySound);
        
        MiningManager.Instance.OnDetectMineablesStarted += OnDetectMineablesStarted;
        MiningManager.Instance.OnDetectMineablesStopped += OnDetectMineablesStopped;
    }

    private void OnDestroy()
    {
        MiningManager.Instance.OnDetectMineablesStarted -= OnDetectMineablesStarted;
        MiningManager.Instance.OnDetectMineablesStopped -= OnDetectMineablesStopped;
    }

    private void Update()
    {
        if (_isMining)
        {
            MiningSpellItemSO miningSpellItemSO = MiningManager.Instance.CurrentMiningSpellItemSO;
            if(miningSpellItemSO == null) return;
        
            var main = _miningParticleSystem.main;
            float distance = Vector3.Distance(transform.position, ActionManager.MouseWorldPosition);
            distance = Mathf.Clamp(distance, 0f, miningSpellItemSO.MiningRange);
            main.startLifetime = distance / main.startSpeed.constant;
        }
    }

    private void OnDetectMineablesStarted(object sender, EventArgs e)
    {
        _isMining = true;
        _sustainedExtractionRaySoundEventInstance.start();
        _magicCircle.StartAnimation(0.25f);
        _miningParticleSystem.Play();
    }

    private void OnDetectMineablesStopped(object sender, System.EventArgs e)
    {
        _isMining = false;
        _sustainedExtractionRaySoundEventInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
        _miningParticleSystem.Stop();
        _magicCircle.StopAnimation(false);
    }
}
