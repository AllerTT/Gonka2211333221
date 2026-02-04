using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class carController : MonoBehaviour
{
    [Header("Suspension")]
    [Range(0,5)]
    public float SuspensionDistance = 0.2f;
    public float suspensionForce = 30000f;
    public float suspensionDamper = 200f;
    public Transform groundCheck;
    public Transform fricAt;
    public Transform CentreOfMass;

    private CarInputs carControls;
    private Rigidbody rb;

    //private CinemachineVirtualCamera cinemachineVirtualCamera;
    [Header("Car Stats")]
    public float speed = 200f;
    public float turn = 100f;
    public float brake = 150;
    public float friction = 70f;
    public float dragAmount = 4f;
    public float TurnAngle = 30f;
    
    public float maxRayLength = 0.8f, slerpTime = 0.2f;
    [HideInInspector]
    public bool grounded;

    

    [Header("Visuals")]
    public Transform[] TireMeshes;
    public Transform[] TurnTires;

    [Header("Curves")]
    public AnimationCurve frictionCurve;
    public AnimationCurve speedCurve;
    public AnimationCurve turnCurve;
    public AnimationCurve driftCurve;
    public AnimationCurve engineCurve;

    
    

    private float speedValue, fricValue, turnValue, curveVelocity, brakeInput;
    [HideInInspector]
    public Vector3 carVelocity;
    [HideInInspector]
    public RaycastHit hit;
    //public bool drftSndMachVel;

    [Header("Other Settings")]
    public AudioSource[] engineSounds;
    public bool airDrag;
    public bool AirVehicle = false;
    public float UpForce;
    public float SkidEnable = 20f;
    public float skidWidth = 0.12f;
    private float frictionAngle;

    


    private void Awake()
    {
        carControls = new CarInputs();
        //cinemachineVirtualCamera = transform.Find("CM vcam1").GetComponent<CinemachineVirtualCamera>();

        rb = GetComponent<Rigidbody>();
        grounded = false;
        engineSounds[1].mute = true;
        rb.centerOfMass = CentreOfMass.localPosition;
    }
    private void OnEnable()
    {
	    carControls.Enable();
    }
    private void OnDisable()
    {
	    carControls.Disable();
    }

    void FixedUpdate()
    {
        carVelocity = transform.InverseTransformDirection(rb.linearVelocity); //local velocity of car
        
        curveVelocity = Mathf.Abs(carVelocity.magnitude) / 100;

        //inputs
        float turnInput = turn * carControls.carAction.moveH.ReadValue<float>() * Time.fixedDeltaTime * 1000;
        float speedInput = speed * carControls.carAction.moveV.ReadValue<float>() * Time.fixedDeltaTime * 1000;
        brakeInput = brake * carControls.carAction.brake.ReadValue<float>() * Time.fixedDeltaTime * 1000;

        //helping veriables
        speedValue = speedInput * speedCurve.Evaluate(Mathf.Abs(carVelocity.z) / 100);
        fricValue = friction * frictionCurve.Evaluate(carVelocity.magnitude / 100);
        turnValue = turnInput * turnCurve.Evaluate(carVelocity.magnitude / 100);
	    
        //grounded check
        if (Physics.Raycast(groundCheck.position, -transform.up, out hit, maxRayLength) && AirVehicle == false)
        {
            accelarationLogic();
            turningLogic();
            frictionLogic();
            brakeLogic();
            //for drift behaviour
            rb.angularDamping = dragAmount * driftCurve.Evaluate(Mathf.Abs(carVelocity.x) / 70);

            //draws green ground checking ray ....ingnore
            Debug.DrawLine(groundCheck.position, hit.point, Color.green);
            grounded = true;

	        rb.centerOfMass = Vector3.zero;
        }
        else if (!Physics.Raycast(groundCheck.position, -transform.up, out hit, maxRayLength) && AirVehicle == false)
        {
            grounded = false;
            rb.linearDamping = 0.1f;
            rb.centerOfMass = CentreOfMass.localPosition;
            if (!airDrag)
            {
                rb.angularDamping = 0.1f;
            }
        }

        if(AirVehicle == true)
        {
            AirController();
        }
        
    }

    void Update()
	{
		
        tireVisuals();
        //ShakeCamera(1.2f, 10f);
		audioControl();
	    
    }

    public void ShakeCamera(float amplitude, float frequency)
    {
       // CinemachineBasicMultiChannelPerlin cinemachineBasicMultiChannelPerlin =
            //cinemachineVirtualCamera.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();

        //cinemachineBasicMultiChannelPerlin.m_AmplitudeGain = curveVelocity * amplitude;
        //cinemachineBasicMultiChannelPerlin.m_FrequencyGain = curveVelocity * frequency;
    }

    public void audioControl()
    {
        //audios
        if (grounded)
        {
            if (Mathf.Abs(carVelocity.x) > SkidEnable - 0.1f)
            {
                engineSounds[1].mute = false;
            }
            else { engineSounds[1].mute = true; }
        }
        else
        {
            engineSounds[1].mute = true;
        }

        /*if (drftSndMachVel) 
        { 
            engineSounds[1].pitch = (0.7f * (Mathf.Abs(carVelocity.x) + 10f) / 40);
        }
        else { engineSounds[1].pitch = 1f; }*/

        engineSounds[1].pitch = 1f;

        engineSounds[0].pitch = 2 * engineCurve.Evaluate(curveVelocity);
        if (engineSounds.Length == 2)
        {
            return;
        }
        else { engineSounds[2].pitch = 2 * engineCurve.Evaluate(curveVelocity); }

        

    }

    public void tireVisuals()
    {
        //Tire mesh rotate
        foreach (Transform mesh in TireMeshes)
        {
	        mesh.transform.RotateAround(mesh.transform.position, mesh.transform.right, carVelocity.z/3);
            mesh.transform.localPosition = Vector3.zero;
        }

        //TireTurn
        foreach (Transform FM in TurnTires)
        {
            FM.localRotation = Quaternion.Slerp(FM.localRotation, Quaternion.Euler(FM.localRotation.eulerAngles.x,
                               TurnAngle * carControls.carAction.moveH.ReadValue<float>(), FM.localRotation.eulerAngles.z), slerpTime);
        }
    }

    public void accelarationLogic()
    {
        //speed control
        if (carControls.carAction.moveV.ReadValue<float>() > 0.1f)
        {
            rb.AddForceAtPosition(transform.forward * speedValue, groundCheck.position);
        }
        if (carControls.carAction.moveV.ReadValue<float>() < -0.1f)
        {
            rb.AddForceAtPosition(transform.forward * speedValue / 2, groundCheck.position);
        }
    }

    public void turningLogic()
    {
        //turning
        if (carVelocity.z > 0.1f)
        {
            rb.AddTorque(transform.up * turnValue);
        }
        else if (carControls.carAction.moveV.ReadValue<float>() > 0.1f)
        {
            rb.AddTorque(transform.up * turnValue);
        }
        if (carVelocity.z < -0.1f && carControls.carAction.moveV.ReadValue<float>() < -0.1f)
        {
            rb.AddTorque(transform.up * -turnValue);
        }
    }

    public void frictionLogic()
    {
        //Friction
        if (carVelocity.magnitude > 1)
        {
            frictionAngle = (-Vector3.Angle(transform.up, Vector3.up)/90f) + 1 ;
            rb.AddForceAtPosition(transform.right * fricValue * frictionAngle * 100 * -carVelocity.normalized.x, fricAt.position);
        }
    }

    public void brakeLogic()
    {
        //brake
	    if (carVelocity.z > 1f)
        {
            rb.AddForceAtPosition(transform.forward * -brakeInput, groundCheck.position);
        }
	    if (carVelocity.z < -1f)
        {
            rb.AddForceAtPosition(transform.forward * brakeInput, groundCheck.position);
        }
	    if(carVelocity.magnitude < 1)
	    {
	    	rb.linearDamping = 5f;
	    }
	    else
	    {
	    	rb.linearDamping = 0.1f;
	    }
    }

    public void AirController()
    {
        rb.useGravity = false;
        var forwardDir = Vector3.ProjectOnPlane(transform.forward, Vector3.up);
        

        float upForceValue = (-Physics.gravity.y/Time.deltaTime) + UpForce;
        rb.AddForce(transform.up * upForceValue * carControls.carAction.up_down.ReadValue<float>() * Time.deltaTime);
       
        //speed control
        if (carControls.carAction.moveV.ReadValue<float>() > 0.1f)
        {
            rb.AddForceAtPosition(forwardDir * speedValue, groundCheck.position);
        }
        if (carControls.carAction.moveV.ReadValue<float>() < -0.1f)
        {
            rb.AddForceAtPosition(forwardDir * speedValue / 2, groundCheck.position);
        }

        //turning
        //if (carVelocity.z > 0.1f)
        //{
            rb.AddTorque(Vector3.up * turnValue);
        //}
        //else if (carControls.carAction.moveV.ReadValue<float>() > 0.1f)
        //{
        //    rb.AddTorque(Vector3.up * turnValue);
        //}
        //if (carVelocity.z < -0.1f && carControls.carAction.moveV.ReadValue<float>() < -0.1f)
        //{
        //    rb.AddTorque(Vector3.up * -turnValue);
        //}

        //friction(drag) 
        if (carVelocity.magnitude > 1)
        {
            float frictionAngle = (-Vector3.Angle(transform.up, Vector3.up) / 90f) + 1;
            rb.AddForceAtPosition(Vector3.ProjectOnPlane( transform.right,Vector3.up) * fricValue * frictionAngle * 100 * -carVelocity.normalized.x, fricAt.position);
        }

        //brake
        if (carVelocity.z > 1f)
        {
            rb.AddForceAtPosition(Vector3.ProjectOnPlane(transform.forward, Vector3.up) * -brakeInput, groundCheck.position);
        }
        if (carVelocity.z < -1f)
        {
            rb.AddForceAtPosition(Vector3.ProjectOnPlane(transform.forward, Vector3.up) * brakeInput, groundCheck.position);
        }
        if (carVelocity.magnitude < 1)
        {
            rb.linearDamping = 5f;
        }
        else
        {
            rb.linearDamping = 1f;
        }


    }

    private void OnDrawGizmos()
    {
        
        if (!Application.isPlaying)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(groundCheck.position, groundCheck.position - maxRayLength * groundCheck.up);
            Gizmos.DrawWireCube(groundCheck.position - maxRayLength * (groundCheck.up.normalized), new Vector3(5, 0.02f, 10));
            Gizmos.color = Color.magenta;
            if (GetComponent<BoxCollider>())
            {
                Gizmos.DrawWireCube(transform.position, GetComponent<BoxCollider>().size);
            }
            if (GetComponent<CapsuleCollider>())
            {
                Gizmos.DrawWireCube(transform.position, GetComponent<CapsuleCollider>().bounds.size);
            }
            

            
            Gizmos.color = Color.red;
            foreach (Transform mesh in TireMeshes)
            {
                var ydrive = mesh.parent.parent.GetComponent<ConfigurableJoint>().yDrive;
                ydrive.positionDamper = suspensionDamper;
                ydrive.positionSpring = suspensionForce;


                mesh.parent.parent.GetComponent<ConfigurableJoint>().yDrive = ydrive;

                var jointLimit = mesh.parent.parent.GetComponent<ConfigurableJoint>().linearLimit;
                jointLimit.limit = SuspensionDistance;
                mesh.parent.parent.GetComponent<ConfigurableJoint>().linearLimit = jointLimit;

                Handles.color = Color.red;
                //Handles.DrawWireCube(mesh.position, new Vector3(0.02f, 2 * jointLimit.limit, 0.02f));
                Handles.ArrowHandleCap(0, mesh.position, mesh.rotation * Quaternion.LookRotation(Vector3.up), jointLimit.limit, EventType.Repaint);
                Handles.ArrowHandleCap(0, mesh.position, mesh.rotation * Quaternion.LookRotation(Vector3.down), jointLimit.limit, EventType.Repaint);

            }
            float wheelRadius = TurnTires[0].parent.GetComponent<SphereCollider>().radius;
            float wheelYPosition = TurnTires[0].parent.parent.localPosition.y + TurnTires[0].parent.localPosition.y;
            maxRayLength = (groundCheck.localPosition.y - wheelYPosition + (0.05f + wheelRadius));

        }
        
    }

    

}
