using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using UnityEngine.Tilemaps;
using System;
using System.Collections;


namespace ProjectWizard
{
	// Wanders until it finds a player or breadcrumb to move to
	public class BasicNpcStateMachine : StateMachine
	{
	    public bool IsPursuingPlayerOrBreadCrumb { get; private set; }
	    public bool IsStrafing { get; private set; }
	    public bool PlayerInSight { get; private set; }
	    public bool IsAngry { get; private set; }
	    public Transform PursueTargetTransform { get; private set; } = null;

	    private Timer _breadCrumbDetectionTimer;
	    private Timer _strafeTimer;

	    private bool _playerPositionFound, _breadCrumbPositionFound;
	    public int StrafingDirection { get; private set; }
    
	    public BasicNpcStateMachine(ServerCharacter serverCharacter)
	    {
	        _serverCharacter = serverCharacter;
        
	        // Sub States
	        _states[AIState.Idle] = new BasicNpcIdleState(AIState.Idle, this);
	        _states[AIState.Moving] = new BasicNpcMoveState(AIState.Moving, this);
	        _states[AIState.Knockbacked] = new BasicNpcKnockbackState(AIState.Knockbacked, this);
	        _states[AIState.Pursuing] = new BasicNpcPursueState(AIState.Pursuing, this);
	        _states[AIState.Fleeing] = new BasicNpcFleeState(AIState.Fleeing, this);

	        // Super States
	        _states[AIState.Grounded] = new BasicNpcGroundedState(AIState.Grounded, this);
	        _states[AIState.Dead] = new BasicNpcDeadState(AIState.Dead, this);
        
	        // Start on the Grounded State
	        _currentState = _states[AIState.Grounded];
	    }

	    public override void OwnerInitialization()
	    {
	        _serverCharacter.MovementState.OnValueChanged += OnMovementStateChanged;
    
	        if (!CharacterData.IsFriendly)
	        {
	            _breadCrumbDetectionTimer = new Timer(_serverCharacter.Data.DetectionIntervalDuration);
	            _breadCrumbDetectionTimer.OnTimerEnd += TryToFindBreadcrumbOrPlayer;
	        }
        
	        if(CharacterData.CanStrafe && CharacterData.WillChasePlayer)
	        {
	            _strafeTimer = new Timer(CharacterData.StrafingDuration);
	            _strafeTimer.OnTimerEnd += EndStrafe;
	        }
	    }

	    public override void Dispose()
	    {
	        base.Dispose();

	        _serverCharacter.MovementState.OnValueChanged -= OnMovementStateChanged;

	        if (!CharacterData.IsFriendly)
	        {
	            _breadCrumbDetectionTimer.OnTimerEnd -= TryToFindBreadcrumbOrPlayer;
	        }
        
	        if(CharacterData.CanStrafe && CharacterData.WillChasePlayer)
	        {
	            _strafeTimer.OnTimerEnd -= EndStrafe;
	        }
	    }

	    private void OnMovementStateChanged(MovementState previousValue, MovementState newValue)
	    {
	        if(newValue == MovementState.Pursuing)
	        {
	            // Try to strafe behavior
	            if (CharacterData.CanStrafe && !IsStrafing && CharacterData.WillChasePlayer)
	            {
	                _strafeTimer.Reset();
	                StrafingDirection = UnityEngine.Random.value > 0.5f ? 1 : -1;
	                IsStrafing = true;
	            }
	        }
	    }

	    public override void ReceiveHP(ServerCharacter inflicter, int amount)
	    {
	        if (inflicter == null) return;
        
	        if (amount < 0)
	        {
	            // Damaged
	            if (!CharacterData.IsFriendly)
	            {
	                IsAngry = true;
	            }
	        }
	        else
	        {
	            // Healed
	        }
	    }

	    public override void UpdateAI()
	    {
	        base.UpdateAI();

	        if (!CharacterData.IsFriendly)
	        {
	            _breadCrumbDetectionTimer?.Tick(Time.deltaTime);
	        }

	        if (IsStrafing)
	        {
	            _strafeTimer?.Tick(Time.deltaTime);
	        }
	    }

	    private void EndStrafe(object sender, EventArgs e)
	    {
	        IsStrafing = false;
	    }

	    private void TryToFindBreadcrumbOrPlayer(object sender, EventArgs e)
	    {
	        // If the Npc only chases when provoked, and it is not provoked, do not try to detect any breadcrumbs or players
	        if (CharacterData.OnlyChaseWhenProvoked && _serverCharacter.NetHealthState.HitPoints.Value >= CharacterData.BaseHealth)
	        {
	            _breadCrumbDetectionTimer.Reset();
	            return;
	        }

	        _playerPositionFound = false;
	        _breadCrumbPositionFound = false;
	        PursueTargetTransform = null;
	        PlayerInSight = false;
	        IsPursuingPlayerOrBreadCrumb = false;

	        // Find all colliders in detection radius for Player or Breadcrumb layers
	        Collider2D[] colliders = Physics2D.OverlapCircleAll(_serverCharacter.transform.position, CharacterData.DetectionRadius, LayerMask.GetMask("Player", "Breadcrumb"));
	        List<Collider2D> unObstructedColliders = new();

	        // Filter out obstructed targets
	        foreach (Collider2D collider in colliders)
	        {
	            if (IsPathUnObstructed(collider.transform.position))
	            {
	                unObstructedColliders.Add(collider);
	            }
	        }

	        // Find the nearest player
	        float closestDistance = float.MaxValue;
	        Transform closestPlayerTransform = null;

	        foreach (Collider2D collider in unObstructedColliders)
	        {
	            if (collider.transform.root.TryGetComponent(out Player player))
	            {
	                if (player.CurrentBiome.Value == _serverCharacter.CurrentBiome)
	                {
	                    float distance = Vector2.Distance(_serverCharacter.transform.position, player.transform.position);
	                    if (distance < closestDistance)
	                    {
	                        closestDistance = distance;
	                        closestPlayerTransform = player.transform;
	                        _playerPositionFound = true;
	                    }
	                }
	            }
	        }

	        if (_playerPositionFound)
	        {
	            // Pursue the closest player found
	            PursueTargetTransform = closestPlayerTransform;
	            PlayerInSight = true;
	            IsPursuingPlayerOrBreadCrumb = true;
	        }
	        else
	        {
	            // No player found - look for breadcrumb with highest lifetime
	            float highestLifetime = 0f;
	            Transform highestLifetimeBreadcrumbTransform = null;

	            foreach (Collider2D collider in unObstructedColliders)
	            {
	                if (collider.TryGetComponent(out BreadCrumb breadCrumb))
	                {
	                    if (breadCrumb.Biome == _serverCharacter.CurrentBiome)
	                    {
	                        if (breadCrumb.RemainingLifeTime > highestLifetime)
	                        {
	                            highestLifetime = breadCrumb.RemainingLifeTime;
	                            highestLifetimeBreadcrumbTransform = breadCrumb.transform;
	                            _breadCrumbPositionFound = true;
	                        }
	                    }
	                }
	            }

	            if (_breadCrumbPositionFound)
	            {
	                PursueTargetTransform = highestLifetimeBreadcrumbTransform;
	                IsPursuingPlayerOrBreadCrumb = true;
	                PlayerInSight = false;
	            }
	            else
	            {
	                // No player or breadcrumb found
	                PursueTargetTransform = null;
	                IsPursuingPlayerOrBreadCrumb = false;
	                PlayerInSight = false;
	            }
	        }

	        _breadCrumbDetectionTimer.Reset();
	    }



	    public bool IsPathUnObstructed(Vector2 desiredEndpoint)
	    {
	        Vector2 direction = desiredEndpoint - (Vector2)_serverCharacter.transform.position;
	        float distance = direction.magnitude;

	        TilemapCollider2D localBiomePfWallCollider = Pathfinding.Instance.GetPathfindingWallCollider(_serverCharacter.CurrentBiome);

	        if (localBiomePfWallCollider == null) return false;

	        RaycastHit2D[] hits = Physics2D.RaycastAll(_serverCharacter.transform.position, direction.normalized, distance, LayerMask.GetMask("PathfindingWall"));

	        foreach (var hit in hits)
	        {
	            if (hit.collider == localBiomePfWallCollider)
	            {
	                return false;
	            }
	        }

	        return true;
	    }
	}

}