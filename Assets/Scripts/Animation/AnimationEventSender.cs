using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimationEventSender : MonoBehaviour
{
    public enum AnimationEvent
    {
        EndAttack, Special, SelfKnockBack
    }

    public System.Action<AnimationEvent> OnAnimationEvent;

    public void OnAnimationEventRecieved(AnimationEvent Event) => OnAnimationEvent?.Invoke(Event);
}
