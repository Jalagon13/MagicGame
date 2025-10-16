using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ProjectTinker
{
    [RequireComponent(typeof(SpellCaster))]
    [RequireComponent(typeof(DamageReceiver))]
    [RequireComponent(typeof(ServerCharacter))]
    [RequireComponent(typeof(PlayerNetworkVisibility))]
    [RequireComponent(typeof(Collider2D))]
    public class Player : NetworkBehaviour
    {
        public static event EventHandler<PlayerIdEventArgs> OnAnyPlayerSpawned;
        public class PlayerIdEventArgs : EventArgs
        {
            public ulong PlayerId;
        }

        public static Player Instance { get; private set; }

        [SerializeField]
        private CollectTag _collectTag;
        public CollectTag CollectTag => _collectTag;

        [SerializeField]
        private PlayerHand _playerHand;
        public PlayerHand PlayerHand => _playerHand;

        [SerializeField]
        private CollisionDetector _playerCollisionDetector;
        public CollisionDetector PlayerCollisionDetector => _playerCollisionDetector;

        [SerializeField]
        private GameObject _breadCrumbPrefab;

        public Collider2D HitCollider { get; private set; }

        private Vector2 _spawnPoint;
        private BiomeType _spawnBiome;
        private Vector2Int _lastTilePosition;

        public NetworkVariable<ushort> SelectedItemId { get; private set; } = new(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        public NetworkVariable<BiomeType> CurrentBiome { get; set; } = new(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

        private ServerCharacter _serverCharacter;
        public ServerCharacter ServerCharacter => _serverCharacter;

        private PlayerNetworkVisibility _playerNetworkVisibility;
        public PlayerNetworkVisibility PlayerNetworkVisibility => _playerNetworkVisibility;

        private DamageReceiver _damageReceiver;

        private SpellCastController _spellCastController;
        public SpellCastController SpellCastController => _spellCastController;

        private SpellCaster _spellCaster;
        public SpellCaster SpellCaster => _spellCaster;

        private void Awake()
        {
            _spellCaster = GetComponent<SpellCaster>();
            _damageReceiver = GetComponent<DamageReceiver>();
            _serverCharacter = GetComponent<ServerCharacter>();
            _playerNetworkVisibility = GetComponent<PlayerNetworkVisibility>();
            HitCollider = GetComponent<Collider2D>();
        }

        public void OnNetworkSpawnLocalClientInitializations()
        {
            Instance = this;
            CurrentBiome.Value = BiomeType.None; // Initialize it to none and the GameManager sets it to the correct biome on start at the time of writing this comment

            _spellCastController = new SpellCastController(this);
            _spawnBiome = BiomeType.Forest;
            _spawnPoint = transform.position;

            OnAnyPlayerSpawned?.Invoke(this, new PlayerIdEventArgs
            {
                PlayerId = OwnerClientId
            });

            // local player start up code here, maybe input
            GameInput.Instance.OnMove += GameInput_OnPlayerMove;
            HotbarManager.Instance.OnFocusSlotUpdated += HotbarManager_OnSelectedItemUpdated;
            CurrentBiome.OnValueChanged += UpdateCollisionDetection;
        }

        public override void OnNetworkDespawn()
        {
            if (IsClient && !_serverCharacter.Data.IsNpc && _serverCharacter.IsOwner)
            {
                GameInput.Instance.OnMove -= GameInput_OnPlayerMove;
                HotbarManager.Instance.OnFocusSlotUpdated -= HotbarManager_OnSelectedItemUpdated;
                CurrentBiome.OnValueChanged -= UpdateCollisionDetection;
                _spellCastController.Dispose();
            }
        }

        private void UpdateCollisionDetection(BiomeType previousValue, BiomeType newValue)
        {
            _playerCollisionDetector.SetBiome(newValue);

        }

        private void Update()
        {
            if (!IsOwner) return;

            _spellCaster.SetCastingPoint(ActionManager.MouseWorldPosition);
            _spellCastController?.SpellCastControllerUpdate();

            // Breadcrumb stuff
            Vector2Int newTilePosition = new(Mathf.FloorToInt(transform.position.x), Mathf.FloorToInt(transform.position.y));
            if (newTilePosition != _lastTilePosition)
            {
                _lastTilePosition = newTilePosition;
                SpawnBreadCrumbServerRpc(_lastTilePosition);
            }
        }

        public void Respawn()
        {
            if (CurrentBiome.Value != _spawnBiome)
            {
                GameWorld.Instance.OnBiomeTransitionEnd += HandleRespawn;
                GameWorld.Instance.LoadBiome(_spawnBiome, _spawnPoint);
                return;
            }

            OnRespawnLogic();
        }

        private void HandleRespawn(object sender, EventArgs e)
        {
            GameWorld.Instance.OnBiomeTransitionEnd -= HandleRespawn;
            OnRespawnLogic();
        }

        private void OnRespawnLogic()
        {
            transform.SetPositionAndRotation(_spawnPoint, Quaternion.identity);
            StartCoroutine(_serverCharacter.StartIFrameTimer());
            _damageReceiver.ReceiveHP(_serverCharacter, _serverCharacter.Data.BaseHealth, false);
        }

        [Rpc(SendTo.Server, RequireOwnership = false)]
        private void SpawnBreadCrumbServerRpc(Vector2Int spawnPos, RpcParams rpcParams = default)
        {
            GameObject breadCrumb = Instantiate(_breadCrumbPrefab, new Vector2(spawnPos.x + 0.5f, spawnPos.y + 0.5f), Quaternion.identity);
            breadCrumb.GetComponent<BreadCrumb>().InitializeBreadCrumb(CurrentBiome.Value);
            GameManager.Instance.InvokeSpawnBreadCrumbEvent(breadCrumb);
        }

        private void GameInput_OnPlayerMove(object sender, InputAction.CallbackContext e)
        {
            if (_serverCharacter.LifeState != LifeState.Dead)
            {
                var desiredDirection = e.ReadValue<Vector2>();
                if (desiredDirection == Vector2.zero)
                {
                    _serverCharacter.Movement.StartIdle();
                }
                else
                {
                    _serverCharacter.Movement.StartMovement(desiredDirection);
                }
            }
        }

        private void HotbarManager_OnSelectedItemUpdated(object sender, HotbarManager.OnFocusItemSetEventArgs e)
        {
            if (IsOwner)
            {
                if (SelectedItemId.Value == e.SelectedItemId || e.SelectedItemId == GameDataRegistry.INVALID_ID) return;

                // NTFS: Network variables onvaluechanged is only executed if the value is different from the current value
                SelectedItemId.Value = e.SelectedItemId;
            }
        }
    }
}
