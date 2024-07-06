using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting.Antlr3.Runtime.Misc;
using UnityEngine;

public class PlayerJumpState : PlayerBaseState, IRootState
{
    public PlayerJumpState(PlayerStateMachine currentContext, PlayerStateFactory playerStateFactory)
        : base(currentContext, playerStateFactory) {
        IsRootState = true;
    }

    IEnumerator IJumpResetRoutine()
    {
        yield return new WaitForSeconds(.5f);
        Ctx.JumpCount = 0;
    }
    public override void EnterState()
    {
        InitalizeSubState();
        HandleJump();
        
    }

    public override void InitalizeSubState()
    {
        if (!Ctx.IsMovementPressed && !Ctx.IsRunPressed)
        {
            SetSubState(Factory.Idle());
        }
        else if (Ctx.IsMovementPressed && !Ctx.IsRunPressed)
        {
            SetSubState(Factory.Walk());
        }
        else
        {
            SetSubState(Factory.Run());
        }
    }

    public override void UpdateState()
    {
        HandleGravity();
        CheckSwitchState();
    }
    public override void ExitState()
    {
        Ctx.Animator.SetBool(Ctx.IsJumpingHash, false);
        if(Ctx.IsJumpPressed)
        {
        Ctx.RequireNewJumpPress = true;
        }
        Ctx.CurrentJumpResetRoutine = Ctx.StartCoroutine(IJumpResetRoutine());
        if (Ctx.JumpCount == 3)
        {
            Ctx.JumpCount = 0;
            Ctx.Animator.SetInteger(Ctx.JumpCountHash, Ctx.JumpCount);
        }
    }
    public override void CheckSwitchState() {
        if (Ctx.CharacterController.isGrounded)
        {
            SwitchState(Factory.Grounded());
        }
    }

    void HandleJump()
    {
        if (Ctx.JumpCount < 3 && Ctx.CurrentJumpResetRoutine != null)
        {
            Ctx.StopCoroutine(Ctx.CurrentJumpResetRoutine);
        }

        Ctx.Animator.SetBool(Ctx.IsJumpingHash, true);
        Ctx.IsJumping = true;
        Ctx.JumpCount +=1;
        
        Ctx.Animator.SetInteger(Ctx.JumpCountHash,Ctx.JumpCount);
        Ctx.CurrentMovementY = Ctx.InitialJumpVelocities[Ctx.JumpCount];
        Ctx.AppliedMovementY = Ctx.InitialJumpVelocities[Ctx.JumpCount];
    }
    public void HandleGravity()
    {
        bool isFalling = Ctx.CurrentMovementY <= 0.0f || !Ctx.IsJumpPressed;
        float fallMultiplier = 2.0f;

        if (isFalling)
        {

            float priviousYVelocity = Ctx.CurrentMovementY;
            Ctx.CurrentMovementY = Ctx.CurrentMovementY + (Ctx.JumpGravities[Ctx.JumpCount] * fallMultiplier * Time.deltaTime);
            Ctx.AppliedMovementY = Mathf.Max((priviousYVelocity + Ctx.CurrentMovementY) * .5f, -20.0f);

        }
        else
        {
            float priviousYVelocity = Ctx.CurrentMovementY;
            Ctx.CurrentMovementY = Ctx.CurrentMovementY + (Ctx.JumpGravities[Ctx.JumpCount] * Time.deltaTime);
            Ctx.AppliedMovementY = (priviousYVelocity + Ctx.CurrentMovementY) * .5f;

        }
    }
}

