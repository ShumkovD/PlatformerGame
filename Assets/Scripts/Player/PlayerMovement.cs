using System.Collections;
using System.Collections.Generic;
using UnityEngine;


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
    [SerializeField] float BoxYCollisionSize = 1f;
    [SerializeField] float BoxYFloorCollisionCheck = 0.2f;
    [SerializeField] float BoxYHeadCollisionCheck = 0.2f;

    [SerializeField] float BoxYHeadPosition = 0.9f;
    [SerializeField] float BoxYFloorPosition = 0.9f;
    [SerializeField] int FallThroughTime = 5;
    private int fallThroughCurTime = 0;

    [SerializeField, Header("Wall Climbing")]

    float HandReachYHeight = 1.2f;
    private int WallOrientation = 0;

    [SerializeField, Header("Attacking")]
    float comboTime = 0.5f;
    private float comboTimer = 0;
    private int currentAttackNum = 0;

    private int AttackOrientation = 0;


    [SerializeField, Header("Breathing")]
    Camera mainCamera;
    
    [SerializeField] float minZoom;
    [SerializeField] float maxZoom;

    // Targets are bigger than min and max zooms.
    // They are used to control the power with which easing will be made
    [SerializeField] float targetMinZoom;
    [SerializeField] float targetMaxZoom;
    // Zoom Speed
    [SerializeField] float zoomSpeed;

    [SerializeField] float movementMult;
    #endregion

    [SerializeField, Header("Collisions")]
    public int LayerToIgnoreHeadCollision = 6;
    public int LayerToIgnoreFloorCollision = 6;
    public int LayerToIgnoreXCollision = 6;

    [SerializeField, Header("Debug, States, etc...")]

    Animator animator;
    SpriteRenderer spriteRenderer;

    // Wall Data For Grabbing
    float MaxYPosOfAWall = 0;

    private int CurrentInput = 0;
    private int CurrentWallPosition = 0; //1 is right, -1 is left to the player

    private float CurrentCameraZoom;
    private float TargetCameraZoom;

    /// <summary>
    /// Used for collision checks and smooth gameplay
    /// </summary>
    private float lastFrameXPosition;
    private float lastFrameYPosition;
    //Jumping
    private bool fallThrough = false;

    private bool jumpWasPressed = false;
    private bool dashWasPressed = false;
    private bool attackWasPressed = false;
    private bool focusWasPressed = false;

    // Wall To Jump State fix
    private bool wallGrabLocking = false;

    /// <summary>
    /// Used to communicate between Update and Fixed Update functions
    /// </summary>
    struct MovementValues
    {
        public float SpeedX;
        public float SpeedY;
    }
    MovementValues PlayerMovementValues;

    /// <summary>
    /// Global Player States
    /// These states are updated in real time
    /// </summary>
    // Player air state <Grounded / Air>
    public enum PlayerAirState
    {
        Grounded,
        Air
    }
    public PlayerAirState PlayerCurrentAirState = PlayerAirState.Grounded;

    // Player coyotte state
    public enum PlayerCoyotteState
    {
        None,
        Coyotte,
        CoyotteEnd
    }
    public PlayerCoyotteState PlayerCurrentCoyotteState = PlayerCoyotteState.None;


    // Player coyotte state
    public enum PlayerFocusState
    {
        None,
        InFocusTransition,
        Focus,
        OutFocusTransition
    }
    public PlayerFocusState PlayerCurrentFocusState = PlayerFocusState.None;


    // Player global action state
    public enum PlayerGlobalActionState
    {
        None,
        Jump,
        WallGrabbing,
        Dashing,
        Attacking,
        Focus
    }
    public PlayerGlobalActionState PlayerCurrentGlobalAction = PlayerGlobalActionState.None;


    /// <summary>
    /// Local Player states
    /// These states are usually locked and while they might change from one to another, they will be overidden by the global states
    /// </summary>
    // Player normal jump action
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
    // Player wall grabbing action
    public enum PlayerWallGrab
    {
        None,
        StartGrab,
        Grabbed,
        EndGrabToJump,
        EndGrabToCoyotte
    }
    public PlayerWallGrab PlayerCurrentWallGrab = PlayerWallGrab.None;
    // Player dashing action
    public enum PlayerDashState
    {
        None,
        StartDash,
        Dash,
        EndDash
    }
    public PlayerDashState PlayerCurrentDashState = PlayerDashState.None;

    // Player attacking action
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
        /// <summary>
        /// Horizontal Input block
        /// </summary>

        // Save current input in to the variable
        CurrentInput = (int)Input.GetAxisRaw("Horizontal");
        // Player change the speed based on input
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

        /// <summary>
        /// Other input block \
        /// TODO:  So depending on the need of the crouch, lying states might rework this block in to the state system
        /// </summary>

        // Here reset all the input flags
        jumpWasPressed = false;
        dashWasPressed = false;
        attackWasPressed = false;
        focusWasPressed = false;

        if(Input.GetKeyDown(KeyCode.LeftControl))
        {
            focusWasPressed = true;
        }

        // Fall through
        if (Input.GetKey(KeyCode.S))
        {
            // If S and If Space were pressed 
            if (Input.GetKeyDown(KeyCode.Space) &&
                // While grounded
                PlayerCurrentAirState == PlayerAirState.Grounded)
            {
                // We set fall through flag
                fallThrough = true;
            }
        }
        // Jump processing
        else
        // We do the normal jump if ÅufallThroughÅv button is not pressed
        {
            //We will only set the flag. All the relations and state changes will be decided by the Global system
            if (Input.GetKeyDown(KeyCode.Space))
            {
                jumpWasPressed = true;
            }
        }

        // Dashing
        if (Input.GetKeyDown(KeyCode.LeftShift))
        {
            dashWasPressed = true;
        }

        // Mouse Input (Attack)
        if (Input.GetMouseButtonDown(0))
        {
            attackWasPressed = true;
        }
        /// <summary>
        /// STATE CHANGES BLOCK
        /// </summary>


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



                    // Resetting Coyotte
                    if (PlayerCurrentAirState == PlayerAirState.Grounded ||
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


        // If input is in the direction of the wall
        if (CurrentInput == CurrentWallPosition &&
            // Where is a wall collision
            CurrentWallPosition != 0 &&
            // And character is in the position to logically grab the ledge / wall
            MaxYPosOfAWall >= this.transform.position.y + HandReachYHeight &&
             //And at last player is falling
             PlayerMovementValues.SpeedY < 0 &&
             // And is not on the ground
             PlayerCurrentAirState == PlayerAirState.Air &&
             !wallGrabLocking
            )
        {
            // Start Grabbing the ledge
            PlayerCurrentGlobalAction = PlayerGlobalActionState.WallGrabbing;
            PlayerCurrentJumpState = PlayerJumpState.None;
        }

        // Here process all the actions for player to do
        switch (PlayerCurrentGlobalAction)
        {
            // Here process all the inputs to change the states
            case PlayerGlobalActionState.None:
                {

                    if(focusWasPressed)
                    {
                        // Reset is probably not needed, but to keep things clean will put it here
                        focusWasPressed = false;

                        PlayerCurrentGlobalAction = PlayerGlobalActionState.Focus;
                        break;
                    }
                    //Jump was pressed, 
                    if (jumpWasPressed)
                    {
                        // Reset is probably not needed, but to keep things clean will put it here
                        jumpWasPressed = false;

                        // Here we check for global checks
                        // If Is not Grounded and not Coyotte cannot jump
                        if (PlayerCurrentAirState != PlayerAirState.Grounded && PlayerCurrentCoyotteState != PlayerCoyotteState.Coyotte)
                            break;

                        PlayerCurrentGlobalAction = PlayerGlobalActionState.Jump;
                        break;
                    }
                    //Dash was pressed, so do the dash
                    if (dashWasPressed)
                    {
                        PlayerCurrentGlobalAction = PlayerGlobalActionState.Dashing;
                        // Reset is not needed, but to keep things clean will put it here
                        dashWasPressed = false;
                        break;
                    }

                    if(attackWasPressed)
                    {
                        PlayerCurrentGlobalAction = PlayerGlobalActionState.Attacking;
                        // Reset is not needed, but to keep things clean will put it here
                        attackWasPressed = false;
                        break;
                    }
                }
                break;
            
            // Player global jumping state
            case PlayerGlobalActionState.Jump:
                {
                    // Player Local jumping state
                    switch (PlayerCurrentJumpState)
                    {
                        // Default Jumping state, used as a transition in to the Jump
                        case PlayerJumpState.None:
                            {
                                PlayerCurrentJumpState = PlayerJumpState.JumpStarted;
                            }
                            break;

                        // Jump started, used to change the animations in update and physical properties in Fixed update
                        case PlayerJumpState.JumpStarted:
                            {
                                // Set jumping inpulse
                                PlayerMovementValues.SpeedY = JumpingImpulse;
                                // Set animation start
                                animator.SetBool("animIsJumping", true);
                                // Here set player to be in the air (Reason for that is first JumpProgress will cancel on grounded)
                                PlayerCurrentAirState = PlayerAirState.Air;
                                // Go to progress
                                PlayerCurrentJumpState = PlayerJumpState.JumpProgress;
                            }
                            break;

                        // While GetKeyDown is used to change states, here we use GetKeyUp, as it does not have any influence on the other states and fully local
                        //TODO: Will need to fix the interruption and space input
                        case PlayerJumpState.JumpProgress:
                            {
                                if (PlayerMovementValues.SpeedY > 0)
                                    wallGrabLocking = false;

                                // In case of dash being pressed, Jump is being ended in to the dash.
                                if (dashWasPressed)
                                {
                                    PlayerCurrentJumpState = PlayerJumpState.None;
                                    PlayerCurrentGlobalAction = PlayerGlobalActionState.Dashing;
                                    break;
                                }

                                if (PlayerCurrentAirState == PlayerAirState.Grounded)
                                {
                                    PlayerCurrentJumpState = PlayerJumpState.JumpEnded;
                                    break;
                                }

                                //If player does not stop to press space, no need to interrupt the jump
                                if (!Input.GetKeyUp(KeyCode.Space))
                                {
                                    break;
                                }
                                // If players speed is less or 0, player already is falling, so no need to do anything as well
                                if (PlayerMovementValues.SpeedY <= 0)
                                {
                                    break;
                                }

                                // In case if SpeedY is more than minimal jumping power
                                if (PlayerMovementValues.SpeedY > minJumpingPower)
                                {
                                    // Interrupt it
                                    PlayerCurrentJumpState = PlayerJumpState.JumpInterrupted;
                                }
                                else
                                {
                                    // Continue until can Interrupt
                                    PlayerMovementValues.SpeedY = 0;
                                    PlayerCurrentJumpState = PlayerJumpState.JumpProgress;
                                }
                            }
                            break;
                        case PlayerJumpState.JumpInterrupted:
                            {
                                // In case of dash being pressed, Jump is being ended in to the dash.
                                if (dashWasPressed)
                                {
                                    PlayerCurrentJumpState = PlayerJumpState.None;
                                    PlayerCurrentGlobalAction = PlayerGlobalActionState.Dashing;
                                }

                                if (PlayerMovementValues.SpeedY < 0)
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
                        // Resetting all the values
                        case PlayerJumpState.JumpEnded:
                            {
                                PlayerMovementValues.SpeedY = 0;
                                //Set None for Player Jump as a default State
                                PlayerCurrentJumpState = PlayerJumpState.None;
                                // Reset global action to not be jump anymore
                                PlayerCurrentGlobalAction = PlayerGlobalActionState.None;
                            }
                            break;
                    }
                }
                break;
                break;
            case PlayerGlobalActionState.Dashing:
                {
                    switch (PlayerCurrentDashState)
                    {
                        // Default Dashing state, used as a transition in to the dash
                        case PlayerDashState.None:
                            {
                                PlayerCurrentDashState = PlayerDashState.StartDash;
                            }
                            break;
                        // Dash started, used to change the animations in update and timer for the lenght of the dash
                        case PlayerDashState.StartDash:
                            {
                                currentDashTime = 0;
                                animator.SetBool("animDash", true);
                                FixedOrientation = CurrentOrientation;
                                PlayerCurrentDashState = PlayerDashState.Dash;
                            }
                            break;
                        //Here calculate the length of the dash
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
                                PlayerMovementValues.SpeedY = 0;
                                PlayerCurrentDashState = PlayerDashState.None;
                                currentDashTime = 0;
                                // Reset global action to not be dash anymore
                                PlayerCurrentGlobalAction = PlayerGlobalActionState.None;
                            }
                            break;
                    }
                }
                break;
            case PlayerGlobalActionState.WallGrabbing:
                {
                    // Player Wall Grabbing
                    switch (PlayerCurrentWallGrab)
                    {
                        case PlayerWallGrab.None:
                            {
                                PlayerCurrentWallGrab = PlayerWallGrab.StartGrab;
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

                                if (jumpWasPressed)
                                {
                                    PlayerCurrentWallGrab = PlayerWallGrab.EndGrabToJump;
                                }
                            }
                            break;
                        case PlayerWallGrab.EndGrabToJump:
                            {
                                animator.SetBool("animWallWait", false);
                                PlayerCurrentWallGrab = PlayerWallGrab.None;
                                PlayerCurrentGlobalAction = PlayerGlobalActionState.Jump;
                                wallGrabLocking = true;
                            }
                            break;
                        case PlayerWallGrab.EndGrabToCoyotte:
                            {
                                animator.SetBool("animWallWait", false);
                                PlayerCurrentWallGrab = PlayerWallGrab.None;
                                // Set Coyotte On
                                PlayerCurrentCoyotteState = PlayerCoyotteState.Coyotte;
                                PlayerCurrentGlobalAction = PlayerGlobalActionState.None;
                            }
                            break;
                    }
                }
                break;
            case PlayerGlobalActionState.Attacking:
                {
                    //Player Grounded Attack Code
                    switch (PlayerCurrentAttackState)
                    {
                        //PlayerAttackState
                        case PlayerAttackState.None:
                            {
                                    currentAttackNum = 0;
                                    PlayerCurrentAttackState = PlayerAttackState.AttackStarted;
                            }
                            break;
                        case PlayerAttackState.AttackStarted:
                            {
                                AttackOrientation = CurrentOrientation;
                                currentAttackNum++;
                                animator.SetTrigger("isAttack");
                                animator.SetBool("hasAttackEnded", false);
                                PlayerCurrentAttackState = PlayerAttackState.AttackProgress;
                            }
                            break;
                        case PlayerAttackState.AttackProgress:
                            {
                                CurrentOrientation = AttackOrientation;
                                if (animator.GetCurrentAnimatorStateInfo(0).IsName("Attack " + currentAttackNum.ToString()) && animator.GetCurrentAnimatorStateInfo(0).normalizedTime > 0.5f)
                                {
                                    PlayerCurrentAttackState = PlayerAttackState.AttackTransition;
                                }
                            }
                            break;
                        case PlayerAttackState.AttackTransition:
                            {
                                CurrentOrientation = AttackOrientation;
                                if (animator.GetCurrentAnimatorStateInfo(0).IsName("Attack " + currentAttackNum.ToString()) && animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1.0f && !animator.IsInTransition(0))
                                {
                                    animator.SetBool("hasAttackEnded", true);
                                    PlayerCurrentAttackState = PlayerAttackState.None;
                                    // Reset global action to none
                                    PlayerCurrentGlobalAction = PlayerGlobalActionState.None;
                                    break;
                                }
                                // Attack was pressed
                                // And current attack combo is less than amount of attacks in combo
                                if (attackWasPressed && currentAttackNum < 3)
                                {
                                    PlayerCurrentAttackState = PlayerAttackState.AttackDowntime;
                                }
                            }
                            break;
                        case PlayerAttackState.AttackDowntime:
                            {
                                CurrentOrientation = AttackOrientation;
                                if (animator.GetCurrentAnimatorStateInfo(0).IsName("Attack " + currentAttackNum.ToString()) && animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1.0f && !animator.IsInTransition(0))
                                {
                                    PlayerCurrentAttackState = PlayerAttackState.AttackStarted;
                                }

                                break;
                            }
                    }
                }
                break;
            case PlayerGlobalActionState.Focus:
                {
                    switch (PlayerCurrentFocusState)
                    {
                        case PlayerFocusState.None:
                            {
                                PlayerCurrentFocusState = PlayerFocusState.InFocusTransition;
                            }
                            break;
                        case PlayerFocusState.InFocusTransition:
                            {
                                mainCamera.orthographicSize =  Mathf.Lerp(mainCamera.orthographicSize, targetMinZoom, zoomSpeed * Time.deltaTime);
                                if(mainCamera.orthographicSize < minZoom)
                                {
                                    mainCamera.orthographicSize = minZoom;
                                    PlayerCurrentFocusState = PlayerFocusState.Focus;
                                }
                            }
                            break;
                        case PlayerFocusState.Focus:
                            {
                                if(focusWasPressed)
                                {
                                    PlayerCurrentFocusState = PlayerFocusState.OutFocusTransition;
                                }
                            }
                            break;
                        case PlayerFocusState.OutFocusTransition:
                            {
                                 mainCamera.orthographicSize = Mathf.Lerp(mainCamera.orthographicSize, targetMaxZoom, zoomSpeed * Time.deltaTime);
                                if (mainCamera.orthographicSize >= maxZoom)
                                {
                                    mainCamera.orthographicSize = maxZoom;
                                    PlayerCurrentFocusState = PlayerFocusState.None;
                                    PlayerCurrentGlobalAction = PlayerGlobalActionState.None;
                                }
                            }
                            break;
                    }
                    break;
                }
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
        if (PlayerCurrentGlobalAction == PlayerGlobalActionState.Attacking)
        {
            return;
        }

        // Update Jumping Speed (Y)
        PlayerMovementValues.SpeedY += GravityValue * Time.deltaTime; // Because time squared
        // Get new YPosition
        float newFrameYPosition = this.transform.position.y + PlayerMovementValues.SpeedY;
        float newFrameYFloorCheckCenterPosition = newFrameYPosition - BoxYFloorPosition;

        // Jumping on to the platforms
        // When player falls or stays at same height, we make them collide with platforms
        if (PlayerMovementValues.SpeedY <= 0)
        {
            LayerToIgnoreFloorCollision = -1;
        }
        // When player jumps up, we ignore platform collision
        else
        {
            LayerToIgnoreFloorCollision = 6;
        }

        // Falling through platforms
        if (fallThrough)
        {
            // Simple timer to give player time to fall through the platform
            fallThroughCurTime++;
            if (fallThroughCurTime >= FallThroughTime)
            {
                fallThroughCurTime = 0;
                fallThrough = false;
            }
        }

        //Check if new position overlaps (Y) Floor
        // Same x position, but new Y position
        // When falling or dashing, we ignore platforms
        Collider2D floorCollision = Physics2D.OverlapBox(new Vector2(transform.position.x, newFrameYFloorCheckCenterPosition), new Vector2(BoxXCollisionSize, BoxYFloorCollisionCheck), 0f, LayerToIgnoreFloorCollision);

        // If player is not grounded and falling , we additionally check for floors player could go through on the high speed
        if (PlayerMovementValues.SpeedY < -0.1f)
        {
            Vector2 newPos = new Vector2(transform.position.x, newFrameYFloorCheckCenterPosition);
            Vector2 oldPos = new Vector2(lastFrameXPosition, lastFrameYPosition - BoxYFloorPosition);

            RaycastHit2D rayBox = Physics2D.BoxCast(newPos, new Vector2(BoxXCollisionSize, BoxYFloorCollisionCheck), 0, newPos - oldPos, Vector2.Distance(newPos, oldPos));
            if (rayBox.collider != null)
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

            if (PlayerCurrentCoyotteState == PlayerCoyotteState.None)
            {
                PlayerCurrentCoyotteState = PlayerCoyotteState.Coyotte;
            }
        }
        // if there is solid ground, set player as landed
        else
        {
            //PlayerCurrentJumpState = PlayerJumpState.JumpStopped;
            PlayerCurrentAirState = PlayerAirState.Grounded;
            wallGrabLocking = false;
            // Reset fall through
            // fallThrough = false;
            // Set player to be just above the ground
            newFrameYPosition = floorCollision.bounds.max.y + BoxYCollisionSize * 0.5f;
            // Set animation as landed
            animator.SetBool("isLanded", true);
            animator.SetBool("animIsJumping", false);
            //// Is grounded
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



    /// <summary>
    /// UtilityFunctions
    /// </summary>
    /// 



}
