using UnityEngine;

namespace DNExtensions.Systems.FirstPersonController.Interactable
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