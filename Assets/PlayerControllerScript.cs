using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//Controls the camera and movement of the player from keyboard and mouse
public class PlayerControllerScript : MonoBehaviour
{
    [Header("Camera Movements")]
    public float Sensivity;//The sensivity of the camera rotation
    public Camera Camera;//The actual camera gameobject
    public float MinRotationDown;//The minimun rotation when looking down
    public float MaxRotationUp;//The maximum rotation when looking up
    private float CameraRotationXAxis;
    [Header("Player Movement")]
    [Header("----------------")]
    [Header("FOV Control")]
    public float IdleFov;//The FOV of the camera when the player is not moving (idle)
    public float WalkingFov;//The FOV of the camera when walking
    public float SprintingFov;//The FOV of the camera when sprinting
    [Header("Speed")]
    public float WalkingSpeed; //The speed of movement of the player
    public float SprintingSpeed; //The sprinting speed of the movement of the player
    public float walkingFactorSpeed;//How fast to make walking factor go to 1
    public float sprintingFactorSpeed;//How fast to make sprinting factor go to 1
    public float decelerationFactor;//Variable as base variable so we can always reset the decelerationFactor
    [Header("Gravity")]
    public float Gravity;//How much gravity is applied to the player
    public float Jump;//How high we can jump

    private CharacterController characterController;//Variable for the charachter controller
    private Vector3 Movement;//Vector3 that is applied to charachterController
    private Vector3 lastMovement;//Movement last fram
    private Vector2 inputMovement;//Input data from keyboard WASD
    private float Speed;//The overall speed of the player (Smoothed between the walking speed and sprinting speed)
    private float _decelerationFactor;//How much you decelerate in general (Used for ice and other physics materials)
    private bool isWalking;
    private bool isSprinting;
    private float walkingFactor;//Value used to lerp between fov when walking
    private float sprintingFactor;//Value used to lerp between fov when sprinting
    private float camFOV;//Current camera fov

    // Start is called before the first frame update
    void Start()
    {
        #region Cursor Setup
        Cursor.lockState = CursorLockMode.Locked;//Locks the cursor to the middle of the screen
        Cursor.visible = false;//Make the cursor invisible
        #endregion
        characterController = GetComponent<CharacterController>();//Sets the CharachterController from the component
        _decelerationFactor = decelerationFactor;//Setup decelerationFactor
    }

    // Update is called once per frame
    void Update()
    {
        #region Camera Control
        transform.Rotate(new Vector3(0, Input.GetAxis("Mouse X") * Sensivity));//Rotate the whole player around and around
        CameraRotationXAxis -= Input.GetAxis("Mouse Y") * Sensivity;//Sets the up-down value of camera rotation
        CameraRotationXAxis = Mathf.Clamp(CameraRotationXAxis, MinRotationDown, MaxRotationUp);
        Camera.transform.localEulerAngles = new Vector3(CameraRotationXAxis, 0, 0);//Rotates the camera up-down motion from variable
        Camera.fieldOfView = GetCameraFOV(IdleFov, WalkingFov, SprintingFov, walkingFactor, sprintingFactor);//Changes the FOV of the player camera if walking
        #endregion
        #region Player Movement Control
        inputMovement.x = Input.GetAxis("LeftRight"); inputMovement.y = Input.GetAxis("ForwardBackward");//Set input movement values
        Speed = Mathf.Lerp(WalkingSpeed, SprintingSpeed, sprintingFactor);//Lerp between walking speed and sprinting speed with the left shift button axis to smooth out the transition
        Movement.x = inputMovement.x * Speed;//Left/right movement
        Movement.z = inputMovement.y * Speed;//Forward/backwawrd movement
     
        #region Walking and Sprinting values
        isWalking = Mathf.Abs(inputMovement.magnitude) > 0;//Are we walking ?
        isSprinting = isWalking && Input.GetAxis("Sprint") > 0;
        if (isWalking) walkingFactor += (1 - walkingFactor) * walkingFactorSpeed * Time.deltaTime;//Smoothly go to 1
        else walkingFactor -= walkingFactor * walkingFactorSpeed * Time.deltaTime;//Smoothly go to 0
        if (isSprinting) sprintingFactor += (1 - sprintingFactor) * sprintingFactorSpeed * Time.deltaTime;//Smoothly go to 1
        else sprintingFactor -= sprintingFactor * sprintingFactorSpeed * Time.deltaTime;//Smoothly go to 0
        if (walkingFactor > 0.99) walkingFactor = 1;//Snap the value since it will never be 1.0
        if (walkingFactor < 0.01) walkingFactor = 0;//Snap the value since it will never be 0.0
        if (sprintingFactor > 0.99) sprintingFactor = 1;//Snap the value since it will never be 1.0
        if (sprintingFactor < 0.01) sprintingFactor = 0;//Snap the value since it will never be 0.0
        #endregion
        Movement = transform.TransformDirection(Movement);//Takes account the rotation of the player when moving
        if (characterController.isGrounded)//Only allows us to jump when we are touching ground
        {
            Movement.y = 0;//Sets the movement in the Y axis to stop, thus allowing us in-air movement
            if (Input.GetAxis("Jump") > 0.5)//Jumping by the Jump axis
            {
                Movement.y = Jump;
            }
        }
        Movement.y -= Gravity * Time.deltaTime;//Applies gravity as acceleration
        lastMovement.y = Movement.y;//Same gravity so it doesnt lerp between gravities
        lastMovement = Vector3.Lerp(lastMovement, Movement, _decelerationFactor * Time.deltaTime);//Set last frame movement
        characterController.Move(lastMovement * Time.deltaTime);//Moves the characterController by the Movement Vector and the deceleration
        #endregion
        if (Debug.isDebugBuild)
        {
            if (Input.GetKeyDown(KeyCode.Tab))
            {
                Cursor.lockState = CursorLockMode.None;//Unlocks the cursor from the middle of the screen
                Cursor.visible = true;//Make the cursor visible
            }
            if (Input.GetKeyDown(KeyCode.LeftShift))
            {
                Cursor.lockState = CursorLockMode.Locked;//Locks the cursor to the middle of the screen
                Cursor.visible = false;//Make the cursor invisible
            }
        }//Allow debug stuff
    }
    //Three value interpolation for idle, walking and sprinting fov values
    private float GetCameraFOV(float idle, float walk, float sprint, float walkfactor, float sprintfactor) 
    {
        camFOV = Mathf.Lerp(Mathf.Lerp(idle, walk, walkfactor), sprint, sprintfactor);//Lerp of lerp
        return camFOV;
    }
    /*
    void OnGUI()
    {
        if (Debug.isDebugBuild)
        {
            GUI.Box(new Rect(0, 0, 200, 200), "");
            GUI.Label(new Rect(100, 0, 100, 30), "Sensivity");
            GUI.Label(new Rect(100, 25, 100, 30), "Walking Speed");
            GUI.Label(new Rect(100, 50, 100, 30), "Jump");
            GUI.Label(new Rect(100, 75, 100, 30), "Gravity");
            GUI.Label(new Rect(100, 100, 100, 30), "Sprinting Speed");
            GUI.Label(new Rect(100, 125, 100, 30), "Walking FOV");
            GUI.Label(new Rect(100, 150, 100, 30), "Sprinting FOV");
            Sensivity = float.Parse(GUI.TextField(new Rect(0, 0, 100, 30), Sensivity.ToString()));
            WalkingSpeed = float.Parse(GUI.TextField(new Rect(0, 25, 100, 30), WalkingSpeed.ToString()));
            Jump = float.Parse(GUI.TextField(new Rect(0, 50, 100, 30), Jump.ToString()));
            Gravity = float.Parse(GUI.TextField(new Rect(0, 75, 100, 30), Gravity.ToString()));
            SprintingSpeed = float.Parse(GUI.TextField(new Rect(0, 100, 100, 30), SprintingSpeed.ToString()));
            WalkingFov = float.Parse(GUI.TextField(new Rect(0, 125, 100, 30), WalkingFov.ToString()));
            SprintingFovAdd = float.Parse(GUI.TextField(new Rect(0, 150, 100, 30), SprintingFovAdd.ToString()));
        }
    }//Debugging GUI stuff*/

    #region Collisions
    //When collision happens
    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        GameObject otherObject = hit.gameObject;
        if (otherObject.GetComponent<PhysicsObjectScript>() != null) //Is physics object
        {
            _decelerationFactor = otherObject.gameObject.GetComponent<PhysicsObjectScript>().DecelerationFactor;//Set new deceleration factor
        }
        else 
        { 
            _decelerationFactor = decelerationFactor;//Reset the decelerationFactor to base
        }
    } 
    #endregion
}
