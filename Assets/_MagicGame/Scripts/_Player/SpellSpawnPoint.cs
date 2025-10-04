using System;
using DigitalRuby.LightningBolt;
using FMOD.Studio;
using FMODUnity;
using UnityEngine;

namespace ProjectWizard
{
    public class SpellSpawnPoint : MonoBehaviour
    {
        [SerializeField]
        private LightningBoltScript _lightningStream;

        [SerializeField]
        private EventReference _sustainedExtractionRaySound;

        private EventInstance _sustainedExtractionRaySoundEventInstance;
        private ParticleSystem _beginningParticles, _endParticles;
        private bool _isMining;

        private void Awake()
        {
            _beginningParticles = transform.GetChild(1).GetComponent<ParticleSystem>();
            _endParticles = transform.GetChild(2).GetComponent<ParticleSystem>();
            _lightningStream.gameObject.SetActive(false);
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
                if (miningSpellItemSO == null) return;

                float miningRange = miningSpellItemSO.MiningRange;
                Vector3 direction = (Vector3)ActionManager.MouseWorldPosition - transform.position;
                if (direction.magnitude > miningRange)
                {
                    // Clamp the target position to the mining range
                    Vector3 clampedPosition = transform.position + direction.normalized * miningRange;
                    _lightningStream.SetPositions(transform.position, clampedPosition);
                    _endParticles.transform.position = clampedPosition;
                }
                else
                {
                    _lightningStream.SetPositions(transform.position, ActionManager.MouseWorldPosition);
                    _endParticles.transform.position = ActionManager.MouseWorldPosition;
                }
            }
        }

        private void OnDetectMineablesStarted(object sender, EventArgs e)
        {
            _isMining = true;
            _sustainedExtractionRaySoundEventInstance.start();
            _lightningStream.gameObject.SetActive(true);
            _beginningParticles.Play();
            _endParticles.Play();
        }

        private void OnDetectMineablesStopped(object sender, System.EventArgs e)
        {
            _isMining = false;
            _sustainedExtractionRaySoundEventInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
            _lightningStream.gameObject.SetActive(false);
            _beginningParticles.Stop();
            _endParticles.Stop();
        }
    }
}
