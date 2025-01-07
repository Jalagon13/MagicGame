using System.Collections;
using System.Collections.Generic;
using MoreMountains.Feedbacks;
using Unity.Netcode;
using UnityEngine;

public class Item : NetworkBehaviour
{
    [SerializeField] private float _attractRange = 2.75f;
    [SerializeField] private float _attractSpeed = 5f;
    [SerializeField] private float _initialCollectDelay = 0.5f;
    [SerializeField] private MMF_Player _pickUpFeedback;

    private NetworkVariable<int> _itemIdNetworkVariable = new(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    private NetworkVariable<int> _itemAmountNetworkVariable = new(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    private InventoryItem _itemInventoryItem;
    private SpriteRenderer _sr;
    private bool _canCollect;
    private bool _itemCollected;
    private Rigidbody2D _rb;
    private ushort _itemId;
    private ushort _itemAmount;
	
    private void Awake()
    {
        _sr = transform.GetChild(0).GetComponent<SpriteRenderer>();
        _rb = GetComponent<Rigidbody2D>();
    }
	
    private IEnumerator Start()
    {
        yield return new WaitForSeconds(_initialCollectDelay);
        _canCollect = true;
    }

    public override void OnNetworkSpawn()
    {
        if(NetworkManager.LocalClientId == NetworkManager.ServerClientId)
        {
            _itemIdNetworkVariable.Value = _itemId;
            _itemAmountNetworkVariable.Value = _itemAmount;
        }
		
        UpdateItemDataAndVisuals();
		
        base.OnNetworkSpawn();
    }
	
    private void FixedUpdate()
    {
        if (!_canCollect || _itemCollected) return;

        Collider2D closestValidCollectCollider = null;
        float closestDistanceSqr = Mathf.Infinity;

        var collidersFound = Physics2D.OverlapCircleAll(transform.position, _attractRange);

        foreach (Collider2D collider in collidersFound)
        {
            if (collider.TryGetComponent(out CollectTag collectTag))
            {
                // NTFS: Implement logic for checking inventory space here later
                if (collectTag.transform.root.GetComponent<Player>().IsDead()) 
                    continue;

                float distanceSqr = (collider.transform.position - transform.position).sqrMagnitude;

                if (distanceSqr < closestDistanceSqr)
                {
                    closestDistanceSqr = distanceSqr;
                    closestValidCollectCollider = collider;
                }
            }
        }

        // Move towards the closest collider if found
        if (closestValidCollectCollider != null)
        {
            Vector2 currentPosition = _rb.position;
            Vector2 targetPosition = closestValidCollectCollider.transform.position;
            Vector2 direction = (targetPosition - currentPosition).normalized;

            _rb.MovePosition(currentPosition + direction * _attractSpeed * Time.fixedDeltaTime);
        }

        // Check if the item is within the bounds of any CollectTag collider
        foreach (Collider2D collider in collidersFound)
        {
            if (collider.TryGetComponent(out CollectTag collectTag))
            {
                // If player attached to this collect tag is dead, continue
                if (collectTag.transform.root.GetComponent<Player>().IsDead()) 
                    continue;

                if(collider.IsTouching(GetComponent<Collider2D>()))
                {
                    if (collectTag.OwnerClientId == NetworkManager.LocalClientId && _canCollect && !_itemCollected /* && !InventoryFull() */)
                    {
                        // Local player is collecting the item
                        InventoryManager.Instance.AddItem(_itemInventoryItem.Item, _itemInventoryItem.Quantity);

                        _pickUpFeedback?.PlayFeedbacks();

                        _itemCollected = true;

                        GameManager.Instance.DestroyItem(this);
                        return; // Exit once the item is collected
                    }
                }
            }
        }
    }

    public void SetItemIdAndAmount(ushort itemId, ushort itemAmount)
    {
        _itemId = itemId;
        _itemAmount = itemAmount;
    }
	
    private void UpdateItemDataAndVisuals()
    {
        ItemSO itemSO = GameManager.Instance.GetItemSOFromIndex(_itemIdNetworkVariable.Value);
		
        _itemInventoryItem = new(itemSO, _itemAmountNetworkVariable.Value);
		
        _sr.sprite = _itemInventoryItem.Item.UiDisplay;
        gameObject.name = $"Item_{_itemInventoryItem.Item.Name}";
    }
	
    public void DestroySelf()
    {
        Destroy(gameObject);
    }
}
