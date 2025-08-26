using System;
using System.Collections;
using System.Collections.Generic;
using FMODUnity;
using MoreMountains.Feedbacks;
using UnityEngine;

[SelectionBase]
public class ResourceObject : MonoBehaviour // Base class for every "physical" asset in the world
{	
	[SerializeField] 
	private ResourceDataSO _resourceData;
	public ResourceDataSO Data => _resourceData;
	
	protected CardinalDirection _orientation;


	private void Awake()
	{
		transform.GetChild(0).gameObject.SetActive(!_resourceData.PassThrough); // Disable local collider so player can walk through it
	}
	
	public virtual void SetOrientation(CardinalDirection orientation)
	{
		_orientation = orientation;
	}
	
	protected bool PlayerInRangeOfPosition(Vector2 position)
	{
		return Vector2.Distance(Player.Instance.transform.position, position) <= ResourceDataSO.InteractDistance;
	}
	
	public void PlayHitFeedback()
	{
	    transform.GetChild(2).GetChild(0).GetComponent<MMF_Player>().PlayFeedbacks();
	}

	public void PlayClientDestructionSequence()
	{
		StartCoroutine(ClientDestructionSequence());
	}
	
	private IEnumerator ClientDestructionSequence()
	{
		Debug.Log($"Playing Client side resource destruction");
		SoundManager.Instance.PlayOneShot(_resourceData.ResourceDestroyed, transform.position);
		Lightmap.Instance.UpdateLightMap();
		yield return null;
		Destroy(gameObject);
	}
}
