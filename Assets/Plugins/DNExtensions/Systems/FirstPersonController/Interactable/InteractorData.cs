using UnityEngine;

namespace DNExtensions.FirstPersonController.Interactable
{
    public class InteractorData
    {
        public FPCInteraction FpcInteraction;


        public InteractorData(FPCInteraction fpcInteraction)
        {
            FpcInteraction = fpcInteraction;
        }
    }
}