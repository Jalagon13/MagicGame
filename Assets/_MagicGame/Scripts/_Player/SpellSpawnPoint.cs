using FMOD.Studio;
using FMODUnity;
using UnityEngine;

public class SpellSpawnPoint : MonoBehaviour
{
    [SerializeField] 
    private EventReference _sustainedExtractionRaySound;

    private EventInstance _sustainedExtractionRaySoundEventInstance;
    private ParticleSystem _miningParticleSystem;
    private bool _isMining;

    private void Awake()
    {
        _miningParticleSystem = transform.GetChild(0).GetComponent<ParticleSystem>();
    }

    private void Start()
    {
        _sustainedExtractionRaySoundEventInstance = SoundManager.Instance.CreateInstance(_sustainedExtractionRaySound);
        
        MiningHandler.Instance.OnMiningStarted += OnMiningStarted;
        MiningHandler.Instance.OnMiningStopped += OnMiningStopped;
    }
    
    private void OnDestroy()
    {
        MiningHandler.Instance.OnMiningStarted -= OnMiningStarted;
        MiningHandler.Instance.OnMiningStopped -= OnMiningStopped;
    }

    private void Update()
    {
        if (_isMining)
        {
            var main = _miningParticleSystem.main;
            float distance = Vector3.Distance(transform.position, ActionManager.MouseWorldPosition);
            main.startLifetime = distance / main.startSpeed.constant;
        }
    }

    private void OnMiningStarted(object sender, MiningHandler.MiningStartedEventArgs e)
    {
        // Handle mining started event
        _isMining = true;
        _sustainedExtractionRaySoundEventInstance.start();
        _miningParticleSystem.Play();
    }

    private void OnMiningStopped(object sender, System.EventArgs e)
    {
        // Handle mining stopped event
        _isMining = false;
        _sustainedExtractionRaySoundEventInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
        _miningParticleSystem.Stop();
    }
}
