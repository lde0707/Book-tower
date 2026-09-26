using UnityEngine;
using BookTower.PlayerSystem;

namespace BookTower.Presentation
{
    /// <summary>Visual-only animation. PlayerController remains responsible for all motion.</summary>
    [DefaultExecutionOrder(100)]
    [RequireComponent(typeof(Animator))]
    public sealed class PaperSpiritAnimator : MonoBehaviour
    {
        static readonly int Moving = Animator.StringToHash("Moving");
        static readonly int Jumping = Animator.StringToHash("Jumping");
        Animator animator;
        PlayerController player;
        bool moving;
        bool jumpActive;
        void Awake()
        {
            animator = GetComponent<Animator>();
            player = GetComponentInParent<PlayerController>();
            animator.applyRootMotion = false;
        }
        void Update()
        {
            if (player == null) return;
            float speed = player.HorizontalVelocity.magnitude;
            // Hysteresis prevents alternating clips while stopping on a slope.
            moving = speed > (moving ? .18f : .35f);
            bool airborne = player.Machine != null && player.Machine.Current == player.Airborne;
            if (!airborne) jumpActive = false;
            else if (player.JumpedThisAir && player.VerticalVelocity > 0f) jumpActive = true;
            if (player.Machine != null && player.Machine.Current == player.Respawn) moving = false;
            animator.SetBool(Moving, moving);
            animator.SetBool(Jumping, jumpActive);
        }
    }
}
