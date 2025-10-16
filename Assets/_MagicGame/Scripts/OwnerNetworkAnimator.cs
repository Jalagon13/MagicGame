using Unity.Netcode.Components;


namespace ProjectTinker
{
	public class OwnerNetworkAnimator : NetworkAnimator
	{
	    protected override bool OnIsServerAuthoritative()
	    {
	        return false;
	    }
	}
}