using Unity.Netcode.Components;


namespace ProjectWizard
{
	public class OwnerNetworkAnimator : NetworkAnimator
	{
	    protected override bool OnIsServerAuthoritative()
	    {
	        return false;
	    }
	}
}