using UnityEngine;

public class PlayerAnimationController : MonoBehaviour
{
    [SerializeField] private PlayerController playerController;
    [SerializeField] private Animator animator;
    static readonly int JumpRequestedHash = Animator.StringToHash("JumpRequested");
    static readonly int IsGroundedHash = Animator.StringToHash("IsGrounded");
    static readonly int HorizontalSpeedHash = Animator.StringToHash("HorizontalSpeed");
    private void OnEnable()
    {
        playerController.OnGroundedChanged += HandleGroundedChanged;
        playerController.OnJumpSuccess += HandleJumpRequested;
    }

    private void OnDisable()
    {
        playerController.OnGroundedChanged -= HandleGroundedChanged;
        playerController.OnJumpSuccess -= HandleJumpRequested;
    }

    private void Update()
    {
        animator.SetFloat(HorizontalSpeedHash, playerController.GetHorizontalSpeed());
    }

    private void HandleGroundedChanged(bool value)
    {
        Debug.Log($"[Anim] GroundedChanged -> {value} | IsJumping was: {animator.GetBool("IsJumping")} | frame: {Time.frameCount}");
        animator.SetBool(IsGroundedHash, value);
        if (value && !playerController.HasJumpBuffered) animator.SetBool("IsJumping", false);
    }

    private void HandleJumpRequested()
    {
        Debug.Log($"[Anim] JumpRequested | IsGrounded: {animator.GetBool(IsGroundedHash)} | IsJumping was: {animator.GetBool("IsJumping")} | frame: {Time.frameCount}");
        animator.ResetTrigger(JumpRequestedHash);
        animator.SetTrigger(JumpRequestedHash);
        animator.SetBool("IsJumping", true);
    }
}
