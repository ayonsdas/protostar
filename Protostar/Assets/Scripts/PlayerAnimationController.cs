using UnityEngine;

public class PlayerAnimationController : MonoBehaviour
{
    [SerializeField] private PlayerController playerController;
    [SerializeField] private Animator animator;
    static readonly int JumpRequestedHash = Animator.StringToHash("JumpRequested");
    static readonly int IsGroundedHash = Animator.StringToHash("IsGrounded");
    static readonly int SpeedHash = Animator.StringToHash("Speed");
    private void OnEnable()
    {
        playerController.OnGroundedChanged += HandleGroundedChanged;
        playerController.OnJumpRequested += HandleJumpRequested;
    }

    private void OnDisable()
    {
        playerController.OnGroundedChanged -= HandleGroundedChanged;
        playerController.OnJumpRequested -= HandleJumpRequested;
    }

    private void Update()
    {
        animator.SetFloat(SpeedHash, playerController.GetNormalizedSpeed);
    }

    private void HandleGroundedChanged(bool value)
    {
        animator.SetBool(IsGroundedHash, value);
    }

    private void HandleJumpRequested()
    {
        animator.SetTrigger(JumpRequestedHash);
    }

    // Called by the jump animation event, makes controller trigger jump force
    public void OnJumpTakeoff()
    {
        playerController.ApplyJumpForce();
    }
}
