using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using Unity.VisualScripting;
using UnityEngine;
using static UnityEditor.Experimental.GraphView.GraphView;


public class PlayerMovement : MonoBehaviour
{
    #region Changable Variables

    [SerializeField, Header("Normal Movement Values")]
                            float HorizontalMovementSpeedMultiplier = 0.1f;
    [SerializeField] float JumpingImpulse = 5;
    [SerializeField] float minJumpingPower = 0.5f;
    [SerializeField] float GravityValue = -5;
    [SerializeField] float CoyotteTime = 0.08f;
    private float curCoyotteTime = 0;
    [SerializeField] int CurrentOrientation = -1;

    [SerializeField, Header("Dash Movement Values")]
     float DashTime = 0.5f;
    [SerializeField] int FixedOrientation = -1;
    [SerializeField] int DashSpeedMultiplier = 3;
    private float currentDashTime;

    [SerializeField, Header("Collision Values")]
                             float BoxXCollisionSize = 0.5f;
    [SerializeField]  float BoxYCollisionSize = 1f;
    [SerializeField]  float BoxYFloorCollisionCheck = 0.2f;
    [SerializeField]  float BoxYHeadCollisionCheck = 0.2f;

    [SerializeField]  float BoxYHeadPosition = 0.9f;
    [SerializeField]  float BoxYFloorPosition = 0.9f;
    [SerializeField]  int FallThroughTime = 5;
    private int fallThroughCurTime = 0;

    [SerializeField, Header("Wall Climbing")]

    float HandReachYHeight = 1.2f;
    private int WallOrientation = 0;

    [SerializeField, Header("Attacking")]
    float comboTime = 0.5f;
    private float comboTimer = 0;
    private int currentAttackNum = 0;

    private int AttackOrientation = 0;
    #endregion

   public  int LayerToIgnoreHeadCollision = 6;
   public  int LayerToIgnoreFloorCollision = 6;
   public  int LayerToIgnoreXCollision = 6;

    Animator animator;
    SpriteRenderer spriteRenderer;

    // Wall Data For Grabbing
    float MaxYPosOfAWall = 0;

    private int CurrentInput = 0;
    private int CurrentWallPosition = 0; //1 is right, -1 is left to the player

    /// <summary>
    /// Used for collision checks and smooth gameplay
    /// </summary>
    private float lastFrameXPosition;
    private float lastFrameYPosition;

    private bool isGrounded = true;
    //Jumping
    private bool fallThrough = false;

    /// <summary>
    /// Used to communicate between Update and Fixed Update functions
    /// </summary>
    struct MovementValues
    {
        public float SpeedX;
        public float SpeedY;
    }
    MovementValues PlayerMovementValues;


    public enum PlayerCoyotteState
    {
        None,
        Coyotte,
        CoyotteEnd
    }
    public PlayerCoyotteState PlayerCurrentCoyotteState = PlayerCoyotteState.None;

    public enum PlayerJumpState
    {
        //Attack state
        None,
        JumpStarted,
        JumpProgress,
        JumpInterrupted,
        JumpEnded
    }
    public PlayerJumpState PlayerCurrentJumpState = PlayerJumpState.None;

    public enum PlayerWallGrab
    {
        None,
        StartGrab,
        Grabbed,
        EndGrabToJump,
        EndGrabToCoyotte
    }
    PlayerWallGrab PlayerCurrentWallGrab = PlayerWallGrab.None;

    public enum PlayerDashState
    {
        None,
        StartDash,
        Dash,
        EndDash
    }
    public PlayerDashState PlayerCurrentDashState = PlayerDashState.None;

    public enum PlayerAirState
    {
        Grounded,
        Air
    }
    public PlayerAirState PlayerCurrentAirState = PlayerAirState.Grounded;

    public enum PlayerAttackState
    {
        //Attack state
        None,
        AttackStarted,
        AttackProgress,
        AttackTransition,
        AttackDowntime,
    }
    public PlayerAttackState PlayerCurrentAttackState = PlayerAttackState.None;


    private void Start()
    {
        Screen.SetResolution(1920, 1080, false);
        lastFrameXPosition = transform.position.x;
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    // Inputs
    private void Update()
    {
        CurrentInput = (int)Input.GetAxisRaw("Horizontal");
        // Player Speed Inputs
        PlayerMovementValues.SpeedX = CurrentInput * HorizontalMovementSpeedMultiplier;

        // Set orientation of the player sprite and remember it
        if (PlayerMovementValues.SpeedX < 0)
        {
            CurrentOrientation = -1;
        }
        if (PlayerMovementValues.SpeedX > 0)
        {
            CurrentOrientation = 1;
        }




        // If pressed S,
        //if (Input.GetKey(KeyCode.S))
        //{
        //    //And space, while on the ground and not dashing, 
        //    if (Input.GetKeyDown(KeyCode.Space) && isGrounded && !isDash)
        //    {
        //        // Fall through
        //        fallThrough = true;
        //    }
        //}
        //else
        //{
        //    // If S is not pressed, when pressing space and in jumpable state
        //    //if (Input.GetKeyDown(KeyCode.Space) && (isGrounded || isCoyotte || canWallJump))
        //    //{
        //    //    // Jump

        //    //    // If jumping you are not on the ground
        //    //    isGrounded = false;
        //    //    // You have jumped
        //    //    hasJumped = true;
        //    //    // You are not on the wall anymore
        //    //    isWallClimb = false;
        //    //    // You cannot jump from the said wall
        //    //    canWallJump = false;
        //    //    // Coyotte time is not applicabel as well
        //    //    isCoyotte = false;
        //    //}


        // Player Air
        switch (PlayerCurrentAirState)
        {
            case PlayerAirState.Grounded:
                {
                    // Player Grounded Attack Code
                    switch (PlayerCurrentAttackState)
                    {
                        //PlayerAttackState
                        case PlayerAttackState.None:
                            {
                                if (Input.GetMouseButtonDown(0))
                                {
                                    currentAttackNum = 0;
                                    AttackOrientation = CurrentOrientation;
                                    //
                                    PlayerCurrentAttackState = PlayerAttackState.AttackStarted;
                                }
                                break;
                            }
                        case PlayerAttackState.AttackStarted:
                            {
                                currentAttackNum++;
                                animator.SetTrigger("isAttack");
                                animator.SetBool("hasAttackEnded", false);
                                PlayerCurrentAttackState = PlayerAttackState.AttackProgress;
                            }
                            break;
                        case PlayerAttackState.AttackProgress:
                            {
                                if (animator.GetCurrentAnimatorStateInfo(0).IsName("Attack " + currentAttackNum.ToString()) && animator.GetCurrentAnimatorStateInfo(0).normalizedTime > 0.5f)
                                {
                                    PlayerCurrentAttackState = PlayerAttackState.AttackTransition;
                                }
                            }
                            break;
                        case PlayerAttackState.AttackTransition:
                            {
                                if (animator.GetCurrentAnimatorStateInfo(0).IsName("Attack " + currentAttackNum.ToString()) && animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1.0f && !animator.IsInTransition(0))
                                {
                                    animator.SetBool("hasAttackEnded", true);
                                    PlayerCurrentAttackState = PlayerAttackState.None;
                                    break;
                                }

                                if (Input.GetMouseButtonDown(0) && currentAttackNum < 3)
                                {
                                    PlayerCurrentAttackState = PlayerAttackState.AttackDowntime;
                                }
                            }
                            break;
                        case PlayerAttackState.AttackDowntime:
                            {
                                if (animator.GetCurrentAnimatorStateInfo(0).IsName("Attack " + currentAttackNum.ToString()) && animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1.0f && !animator.IsInTransition(0))
                                {
                                    PlayerCurrentAttackState = PlayerAttackState.AttackStarted;
                                }

                                break;
                            }
                    }
                    // Player Jump State Code
                    PlayerJumpUpdateStateCode(PlayerCurrentJumpState);
                }
                break;
            case PlayerAirState.Air:
                {
                    // Player Wall Grabbing
                    switch (PlayerCurrentWallGrab)
                    {
                        case PlayerWallGrab.None:
                            {
                                // If input is in the direction of the wall
                                if (CurrentInput == CurrentWallPosition &&
                                    // Where is a wall collision
                                    CurrentWallPosition != 0 &&
                                    // And character is in the position to logically grab the ledge / wall
                                    MaxYPosOfAWall >= this.transform.position.y + HandReachYHeight &&
                                     //And at last player is falling
                                     PlayerMovementValues.SpeedY <= 0
                                    )
                                {
                                    // Start Grabbing the ledge
                                    PlayerCurrentWallGrab = PlayerWallGrab.StartGrab;
                                }
                            }
                            break;
                        case PlayerWallGrab.StartGrab:
                            {
                                animator.SetBool("animWallWait", true);
                                PlayerCurrentWallGrab = PlayerWallGrab.Grabbed;
                                WallOrientation = CurrentWallPosition;
                            }
                            break;
                        case PlayerWallGrab.Grabbed:
                            {
                                if (CurrentInput == -WallOrientation)
                                {
                                    PlayerCurrentWallGrab = PlayerWallGrab.EndGrabToCoyotte;
                                }
                                if (Input.GetKeyDown(KeyCode.Space))
                                {
                                    PlayerCurrentWallGrab = PlayerWallGrab.EndGrabToJump;
                                }
                            }
                            break;
                        case PlayerWallGrab.EndGrabToJump:
                            {
                                animator.SetBool("animWallWait", false);
                                PlayerCurrentWallGrab = PlayerWallGrab.None;
                            }
                            break;
                        case PlayerWallGrab.EndGrabToCoyotte:
                            {
                                animator.SetBool("animWallWait", false);
                                PlayerCurrentWallGrab = PlayerWallGrab.None;
                                // Set Coyotte On
                                PlayerCurrentCoyotteState = PlayerCoyotteState.Coyotte;
                            }
                            break;
                    }

                    switch (PlayerCurrentCoyotteState)
                    {
                        case PlayerCoyotteState.None:
                            {
                            }
                            break;
                        case PlayerCoyotteState.Coyotte:
                            {
                                // Coyotte Timer Check
                                curCoyotteTime += Time.deltaTime;
                                if (CoyotteTime <= curCoyotteTime)
                                {
                                    PlayerCurrentCoyotteState = PlayerCoyotteState.CoyotteEnd;
                                }

                                // All The Coyotte Actions (Jumping)
                                PlayerJumpUpdateStateCode(PlayerCurrentJumpState);

                                // Resetting Coyotte
                                if (PlayerCurrentAirState   == PlayerAirState.Grounded ||
                                   PlayerCurrentWallGrab == PlayerWallGrab.Grabbed)
                                {
                                    PlayerCurrentCoyotteState = PlayerCoyotteState.CoyotteEnd;
                                }
                            }
                        break;
                        case PlayerCoyotteState.CoyotteEnd:
                            {
                                curCoyotteTime = 0;
                            }
                            break;
                    }


                }
                break;
        }
        // Player Dashing
        switch (PlayerCurrentDashState)
        {
            case PlayerDashState.None:
                {
                    if (Input.GetKeyDown(KeyCode.LeftShift))
                    {
                        PlayerCurrentDashState = PlayerDashState.StartDash;
                    }
                }
                break;
            case PlayerDashState.StartDash:
                {
                    currentDashTime = 0;
                    animator.SetBool("animDash", true);
                    FixedOrientation = CurrentOrientation;
                    PlayerCurrentDashState = PlayerDashState.Dash;
                }
                break;
            case PlayerDashState.Dash:
                {
                    currentDashTime += Time.deltaTime;
                    CurrentOrientation = FixedOrientation;
                    if (currentDashTime >= DashTime)
                    {
                        PlayerCurrentDashState = PlayerDashState.EndDash;
                    }
                }
                break;
            case PlayerDashState.EndDash:
                {
                    animator.SetBool("animDash", false);
                    currentDashTime = 0;
                    PlayerCurrentDashState = PlayerDashState.None;
                }
                break;
        }




        // Here we change the sprite orientation
        if (CurrentOrientation > 0)
        {
            spriteRenderer.flipX = false;
        }
        else
        {
            spriteRenderer.flipX = true;
        }

    }

    // Physics and movement
    private void FixedUpdate()
    {

        if(PlayerCurrentAttackState != PlayerAttackState.None)
        {
            return;
        }

        switch (PlayerCurrentJumpState)
        {
            case PlayerJumpState.JumpStarted:
                {
                    PlayerMovementValues.SpeedY = JumpingImpulse;
                    PlayerCurrentJumpState = PlayerJumpState.JumpProgress;
                }
                break;
            case PlayerJumpState.JumpInterrupted:
                {
                    if(PlayerMovementValues.SpeedY < 0)
                    {
                        PlayerCurrentJumpState = PlayerJumpState.JumpProgress;
                    }

                    if (PlayerMovementValues.SpeedY <= minJumpingPower)
                    {
                        PlayerMovementValues.SpeedY = 0;
                        PlayerCurrentJumpState = PlayerJumpState.JumpProgress;
                    }
                }
                break;
            case PlayerJumpState.JumpProgress:
                {
                    if (PlayerCurrentAirState == PlayerAirState.Grounded)
                    {
                        PlayerCurrentJumpState = PlayerJumpState.JumpEnded;
                    }
                }
                break;
        }

        // Update Jumping Speed (Y)
        PlayerMovementValues.SpeedY  += GravityValue * Time.deltaTime; // Because time squared
        // Get new YPosition
        float newFrameYPosition = this.transform.position.y + PlayerMovementValues.SpeedY;
        float newFrameYFloorCheckCenterPosition = newFrameYPosition  - BoxYFloorPosition;
        // We get the position of floor collision based on character size

        // Jumping on to the platforms
        // When player falls or stays at same height, we make them collide with platforms
        if(PlayerMovementValues.SpeedY <= 0)
        {
            LayerToIgnoreFloorCollision = -1;
        }
        // When player jumps up, we ignore platform collision
        else
        {
            LayerToIgnoreFloorCollision = 6;
        }

        // Falling through platforms
        if(fallThrough)
        {
            // Simple timer to give player time to fall through the platform
            fallThroughCurTime++;
            if(fallThroughCurTime >= FallThroughTime) {
                fallThroughCurTime = 0;
                fallThrough = false;
            }
        }

        //Check if new position overlaps (Y) Floor
        // Same x position, but new Y position
        // When falling or dashing, we ignore platforms
        Collider2D floorCollision = Physics2D.OverlapBox(new Vector2(transform.position.x, newFrameYFloorCheckCenterPosition), new Vector2(BoxXCollisionSize, BoxYFloorCollisionCheck), 0f, LayerToIgnoreFloorCollision);

        // If player is not grounded and falling , we additionally check for floors player could go through on the high speed
        if (!isGrounded && PlayerMovementValues.SpeedY < -0.1f)
        {
            Vector2 newPos = new Vector2(transform.position.x, newFrameYFloorCheckCenterPosition);
            Vector2 oldPos = new Vector2(lastFrameXPosition, lastFrameYPosition - BoxYFloorPosition);

            RaycastHit2D rayBox = Physics2D.BoxCast(newPos, new Vector2(BoxXCollisionSize, BoxYFloorCollisionCheck), 0, newPos - oldPos, Vector2.Distance(newPos, oldPos));
            if(rayBox.collider != null)
            {
                floorCollision = rayBox.collider;
            }
        }

        // If there is no ground or floor object is platform, throu which we are falling through
        if (floorCollision == null || (floorCollision.gameObject.layer == 6 && fallThrough))
        {
            PlayerCurrentAirState = PlayerAirState.Air;
            // Update animation
            animator.SetBool("isLanded", false);
            
            if(PlayerCurrentCoyotteState == PlayerCoyotteState.None)
            {
                PlayerCurrentCoyotteState = PlayerCoyotteState.Coyotte;
            }
        }
        // if there is solid ground, set player as landed
        else
        {
            //PlayerCurrentJumpState = PlayerJumpState.JumpStopped;
            PlayerCurrentAirState = PlayerAirState.Grounded;
            // Reset fall through
           // fallThrough = false;
            // Set player to be just above the ground
            newFrameYPosition = floorCollision.bounds.max.y + BoxYCollisionSize * 0.5f;
            // Set animation as landed
            animator.SetBool("isLanded", true);
            animator.SetBool("animIsJumping", false);
            //// Is grounded
            //isGrounded = true;
            ////Set speed to 0
            PlayerMovementValues.SpeedY = 0;

            PlayerCurrentCoyotteState = PlayerCoyotteState.None;
        }

        //Update position for this frame (X)
        float newFrameXPosition = this.transform.position.x + PlayerMovementValues.SpeedX;


        switch (PlayerCurrentDashState)
        {
            case PlayerDashState.StartDash:
            case PlayerDashState.Dash:
                {
                    newFrameYPosition = transform.position.y;
                    newFrameXPosition = this.transform.position.x + HorizontalMovementSpeedMultiplier * DashSpeedMultiplier * FixedOrientation;
                }
                break;
            case PlayerDashState.EndDash:
                {
                    PlayerMovementValues.SpeedY = 0;
                }
                break;
        }


        // Checking for the head collision
        float newFrameYHeadCheckCenterPosition = newFrameYPosition + BoxYHeadPosition;
        Collider2D headCollision = Physics2D.OverlapBox(new Vector2(transform.position.x, newFrameYHeadCheckCenterPosition), new Vector2(BoxXCollisionSize, BoxYHeadCollisionCheck), 0f, LayerToIgnoreHeadCollision);
        if (headCollision != null)
        {
            // In case of collision, put character just under the roof
            newFrameYPosition = headCollision.bounds.min.y - BoxYCollisionSize * 0.5f;
            PlayerMovementValues.SpeedY = -0.05f;
        }

        //Check if new position overlaps (X)
        // Where might be times, when player collides with 2 or more x walls simultaniously. Usually while half inside of the platform
        // LayerMask gives strange bug, so fix it by storing all colliders
        Collider2D[] xCollision = Physics2D.OverlapBoxAll(new Vector2(newFrameXPosition, newFrameYPosition), new Vector2(BoxXCollisionSize, BoxYCollisionSize * 0.9f), 0f);
        Collider2D xWallCollision = null;
        // And getting only the wall collider from them
       for (int i = 0; i < xCollision.Length; i++)
       {
                    if (xCollision[i].gameObject.layer != 6)
                    {
                        xWallCollision = xCollision[i];
                        break;
                    }
                }
        
       // Check the Orientation and if while grabbing a wall, player moves from the wall, stom wall grab
        //if (CurrentOrientation == -WallOrientation&& isWallClimb)
        //{
        //    //In that case no more wall climb
        //    isWallClimb = false;
        //    // Cannot jump from the wall
        //    canWallJump = false;
        //    // But have some grace coyotte time for jumping (cur 5 frames)
        //    isCoyotte = true;
    
        //    animator.SetBool("animWallWait", false);
        //}
        // As walls are strictrly perbendicular to the ground, if there is a wall
        if (xWallCollision != null)
        {
            MaxYPosOfAWall = xWallCollision.bounds.max.y;
            // newFrameXPosition = lastFrameXPosition;
            // Instead of puting player to the previous frame, will put him just before the wall
            // There should not be any situations where player will collide on X axis with 2 objects with different x bounds, so should work just fine
            // REASON: When trying to collide with wall because Unity not pixel perfect engine, sprite and player collision will always be different (espesially after dash)

            // IMPORTANT:: Because of this, player WILL be closer to wall and floor check will fire and will put player to the top.
            // To fix this made X collision a little bigger

            // Put the player maximally close to the wall
            if (transform.position.x < xWallCollision.transform.position.x)
            {
                newFrameXPosition = xWallCollision.bounds.min.x - BoxXCollisionSize * 0.55f;
                CurrentWallPosition = 1;
            }
            else
            {
                newFrameXPosition = xWallCollision.bounds.max.x + BoxXCollisionSize * 0.55f;
                CurrentWallPosition = -1;
            }

            // If falling in the air and not climbing the wall and player position is under the ledge of the wall
            //if (!isGrounded && PlayerMovementValues.SpeedY <= 0 && !isWallClimb && this.transform.position.y + HandReachYHeight <= xWallCollision.bounds.max.y)
            //{
            //    // Start wall climbing
            //    isWallClimb = true;
            //    // Set wall Orientation
            //    WallOrientation = CurrentOrientation;
            //    // Add ability to jump from the wall
            //    canWallJump = true;
            //}
        }
        else
        {
            CurrentWallPosition = 0;
        }

        // If wall climb
        //if(isWallClimb)
        //{
        //    // Fix y speed
        //    PlayerMovementValues.SpeedY = 0;
        //    // Setting y position as constant to stop character falling while on thew wall
        //    newFrameYPosition = transform.position.y;
        //    animator.SetBool("animWallWait", true);
        //}

        switch (PlayerCurrentWallGrab)
        {
            case PlayerWallGrab.Grabbed:
                {
                    PlayerMovementValues.SpeedY = 0;
                    newFrameYPosition = transform.position.y;
                    if (CurrentInput != CurrentWallPosition)
                    {
                        PlayerCurrentWallGrab = PlayerWallGrab.EndGrabToCoyotte;
                    }
                    if (Input.GetKeyDown(KeyCode.Space))
                    {
                        PlayerCurrentWallGrab = PlayerWallGrab.EndGrabToJump;
                    }
                }
                break;
            case PlayerWallGrab.EndGrabToJump:
                {

                }
                break;
            case PlayerWallGrab.EndGrabToCoyotte:
                {

                }
                break;
        }

        // While we are landed we are not falling and not in the air
        //if (isGrounded)
        //{
        //    //If on the ground fix speedY to 0
        //    PlayerMovementValues.SpeedY = 0;
        //    isCoyotte = false;
        //}
        animator.SetFloat("JumpingYSpeed", PlayerMovementValues.SpeedY);
        // Change animation to run if Speed is not 0
        if (PlayerMovementValues.SpeedX != 0)
        {
            animator.SetBool("animIsRunning", true);
            animator.SetBool("animIsIdle", false);
        }
        else
        {
            animator.SetBool("animIsRunning", false);
            animator.SetBool("animIsIdle", true);
        }

        // Update real position
        this.transform.position = new Vector3(newFrameXPosition, newFrameYPosition);


        // Save new position
        lastFrameXPosition = newFrameXPosition;
        lastFrameYPosition = newFrameYPosition;
    }

    
    void PlayerJumpUpdateStateCode(PlayerJumpState jumpState)
    {
        // Player Grounded jump
        switch (jumpState)
        {
            case PlayerJumpState.None:
                {
                    if (Input.GetKeyDown(KeyCode.Space))
                    {
                        Debug.Log("Currently pressed Space");
                        PlayerCurrentJumpState = PlayerJumpState.JumpStarted;
                    }
                }
                break;
            case PlayerJumpState.JumpStarted:
                {
                    animator.SetBool("animIsJumping", true);
                }
                break;
            case PlayerJumpState.JumpProgress:
                {
                    if (!Input.GetKeyUp(KeyCode.Space))
                    {
                        return;
                    }

                    if (PlayerMovementValues.SpeedY > minJumpingPower)
                    {
                        PlayerCurrentJumpState = PlayerJumpState.JumpInterrupted;
                    }
                    else
                    {
                        PlayerMovementValues.SpeedY = 0;
                        PlayerCurrentJumpState = PlayerJumpState.JumpProgress;
                    }

                }
                break;
            case PlayerJumpState.JumpEnded:
                {
                    PlayerMovementValues.SpeedY = 0;
                    PlayerCurrentJumpState = PlayerJumpState.None;
                }
                break;
        }
    }


    /// <summary>
    /// UtilityFunctions
    /// </summary>
    /// 

    

}
