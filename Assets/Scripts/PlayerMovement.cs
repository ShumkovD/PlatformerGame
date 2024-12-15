using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class PlayerMovement : MonoBehaviour
{
    #region Changable Variables

    [SerializeField, Header("Normal Movement Values")]
                            float HorizontalMovementSpeedMultiplier = 0.1f;
    [SerializeField] float JumpingImpulse = 5;
    [SerializeField] float GravityValue = -5;
    [SerializeField] int CoyotteTime = 5;
    [SerializeField] int curCoyotteTime = 0;
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
    private float WallOrientation = 0;
    #endregion

   public  int LayerToIgnoreHeadCollision = 6;
   public  int LayerToIgnoreFloorCollision = 6;
   public  int LayerToIgnoreXCollision = 6;

    Animator animator;
    SpriteRenderer spriteRenderer;

    /// <summary>
    /// Used for collision checks and smooth gameplay
    /// </summary>
    private float lastFrameXPosition;

    private bool isGrounded = true;
    private bool isCoyotte = false;
    private bool isCoyotteEnded = false;
    private bool isDash = false;
    private bool canDash = false;
    private bool hasJumped = false;
    private bool fallThrough = false;
    private bool isWallClimb = false;
    private bool canWallJump = false;
    /// <summary>
    /// Used to communicate between Update and Fixed Update functions
    /// </summary>
    struct MovementValues
    {
        public float SpeedX;
        public float SpeedY;
    }
    MovementValues PlayerMovementValues;

    

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
        PlayerMovementValues.SpeedX = Input.GetAxisRaw("Horizontal") * HorizontalMovementSpeedMultiplier;

        if(isDash)
        { PlayerMovementValues.SpeedX = 0; }

        if( PlayerMovementValues.SpeedX < 0)
        {
            spriteRenderer.flipX = true;
            CurrentOrientation = -1;
        }
        if (PlayerMovementValues.SpeedX > 0)
        {
            spriteRenderer.flipX = false;
            CurrentOrientation = 1;
        }

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


        if (Input.GetKey(KeyCode.S))
        {
            if (Input.GetKeyDown(KeyCode.Space) && isGrounded && !isDash)
            {
                fallThrough = true;
            }
        }
        else
        {
            if (Input.GetKeyDown(KeyCode.Space) && (isGrounded || isCoyotte || canWallJump))
            {
                PlayerMovementValues.SpeedY = JumpingImpulse;
                animator.SetBool("animIsJumping", true);
                isGrounded = false;
                hasJumped = true;
                isWallClimb = false;
                canWallJump = false;
                isCoyotte = false;
            }

            if (Input.GetKeyUp(KeyCode.Space) && hasJumped && PlayerMovementValues.SpeedY > 0)
            {
                PlayerMovementValues.SpeedY = 0;
            }
        }
        if(Input.GetKeyDown (KeyCode.LeftShift)&&!isDash && canDash)
        {
            animator.SetBool("animDash", true);
            isDash = true;
            canDash = false;
            FixedOrientation = CurrentOrientation;
            currentDashTime = 0;
        }




    }

    // Physics and movement
    private void FixedUpdate()
    {
        // Update Jumping Speed (Y)
        PlayerMovementValues.SpeedY  += GravityValue * Time.deltaTime; // Because time squared

        float newFrameYPosition = this.transform.position.y + PlayerMovementValues.SpeedY;
        float newFrameYFloorCheckCenterPosition = newFrameYPosition  - BoxYFloorPosition;

        // Coyotte timer
        if(isCoyotte)
        {
            curCoyotteTime++;
            if (CoyotteTime <= curCoyotteTime)
            {
                    isCoyotte = false;
                    isCoyotteEnded = true;
                    curCoyotteTime = 0;
                }
            }

        if(!isWallClimb)
        {
            animator.SetBool("animWallWait", false);
        }

        // Jumping on to the platforms
        if(PlayerMovementValues.SpeedY <=0)
        {
            LayerToIgnoreFloorCollision = -1;
        }
        else
        {
            LayerToIgnoreFloorCollision = 6;
        }

        // Falling through platforms
        if(fallThrough)
        {
            fallThroughCurTime++;
            if(fallThroughCurTime >= FallThroughTime) {
                fallThroughCurTime = 0;
                fallThrough = false;
            }
        }
            //Check if new position overlaps (Y) Floor
            Collider2D floorCollision = Physics2D.OverlapBox(new Vector2(transform.position.x, newFrameYFloorCheckCenterPosition), new Vector2(BoxXCollisionSize, BoxYFloorCollisionCheck), 0f, LayerToIgnoreFloorCollision);
        // If we there is floor collision and we are in the air (Usually landing after we jumped) Land
        if (floorCollision == null || (floorCollision.gameObject.layer == 6 && fallThrough))
        {
            animator.SetBool("isLanded", false);
            isGrounded = false;
            if (!hasJumped && !isCoyotteEnded)
            {
                isCoyotte = true;
            }
        }
        else
        {
            fallThrough = false;
            newFrameYPosition = floorCollision.bounds.max.y + BoxYCollisionSize * 0.5f;
            animator.SetBool("isLanded", true);
            isGrounded = true;
            isCoyotteEnded = false;
            hasJumped = false;
            canDash = true;

            animator.SetBool("animIsJumping", false);
        }

        //Update position for this frame (X)
        float newFrameXPosition = this.transform.position.x + PlayerMovementValues.SpeedX;
        // Dash
        if (isDash)
        {
            // Setting y position as constant to stop character falling while dashing
            newFrameYPosition = transform.position.y;

            // Dash acceleration
            newFrameXPosition = this.transform.position.x + HorizontalMovementSpeedMultiplier * DashSpeedMultiplier * FixedOrientation;

            currentDashTime += Time.deltaTime;
            if (currentDashTime > DashTime)
            {
                animator.SetBool("animDash", false);
                isDash = false;
                PlayerMovementValues.SpeedY = 0;
            }
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
        Collider2D xCollision = Physics2D.OverlapBox(new Vector2(newFrameXPosition, newFrameYPosition), new Vector2(BoxXCollisionSize, BoxYCollisionSize * 0.9f), 0f);

        if (CurrentOrientation == -WallOrientation&& isWallClimb)
        {
            isWallClimb = false;
            canWallJump = false;
            isCoyotte = true;

            animator.SetBool("animWallWait", false);
        }

        if (xCollision != null && xCollision.gameObject.layer != LayerToIgnoreXCollision)
        {
           // newFrameXPosition = lastFrameXPosition;
            // Instead of puting player to the previous frame, will put him just before the wall
            // There should not be any situations where player will collide on X axis with 2 objects with different x bounds, so should work just fine
            // REASON: When trying to collide with wall because Unity not pixel perfect engine, sprite and player collision will always be different (espesially after dash)

            // IMPORTANT:: Because of this, player WILL be closer to wall and floor check will fire and will put player to the top.
            // To fix this made X collision a little bigger
            if (transform.position.x < xCollision.transform.position.x)
            {
                newFrameXPosition = xCollision.bounds.min.x - BoxXCollisionSize * 0.55f;
            }
            else
            {
                newFrameXPosition = xCollision.bounds.max.x + BoxXCollisionSize * 0.55f;
            }

            if (!isGrounded && PlayerMovementValues.SpeedY <= 0 && !isWallClimb && this.transform.position.y + HandReachYHeight <= xCollision.bounds.max.y)
            {
                isWallClimb = true;
                WallOrientation = CurrentOrientation;
                canWallJump = true;
            }
        }

        if(isWallClimb)
        {
            animator.SetBool("animWallWait", true);
            PlayerMovementValues.SpeedY = 0;
            // Setting y position as constant to stop character falling while on thew wall
            newFrameYPosition = transform.position.y;
        }

        // While we are landed we are not falling and not in the air
        if (isGrounded)
        {
            PlayerMovementValues.SpeedY = 0;
            isCoyotte = false;
        }
        animator.SetFloat("JumpingYSpeed", PlayerMovementValues.SpeedY);
            // Update real position
         this.transform.position = new Vector3(newFrameXPosition, newFrameYPosition);


        // Save new position
        lastFrameXPosition = newFrameXPosition;
    }

    

    /// <summary>
    /// UtilityFunctions
    /// </summary>
    /// 

    

}
