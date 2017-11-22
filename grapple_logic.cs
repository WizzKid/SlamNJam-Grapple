using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityStandardAssets.Characters.FirstPerson;

public class grapple_logic : MonoBehaviour {
	//comment
	//Descriptions for variable purpose in code for context
	private Rigidbody rb;

	private grapple_input_cd grappleScript;
	private Rigidbody player_rb;
	private FirstPersonController fpsController;
	private GameObject endPos;
	private GameObject enemy;
	private Rigidbody enemy_rb;
	private stun stunScript;
	private stun_large stunScriptLarge;
	private small_shooter_ai aiScript;
	private NavMeshAgent nav;
    private Animator anim;
	public SphereCollider coll;
	public SphereCollider collTrig;

	public GameObject hookHitParticle;

	public float throwSpeed = 35.0f * level_controller.level_speed;
	public float hookTimer = 0.8f * level_controller.level_speed;
	public float maxRange = 19.0f * level_controller.level_speed;
	public float hookReturnLagHooked = 0.6f / level_controller.level_speed;
	public float hookReturnLagNotHooked = 0.2f / level_controller.level_speed;
	public float returnSpeed = 0.5f * level_controller.level_speed;
	public float hookTimeOut = 0.9f / level_controller.level_speed;

	public bool grappleRecalled;
	public bool hooked;
	public bool hookedLarge;

	private float startTime;
	private float journeyLength;
	private float selfPullJourneyLength;

	public float selfPullDistance = 2.0f;
	public float selfPullSpeed = 1.0f;
	public float disableControlsTime = 0.5f;
	public float pullDistance = 0.0f;
	public float pullStrength = 20.0f;
	private float verticalOffset = 1.0f;

	//public float movementPull_SpeedLimit = 3.0f;
	public float pullBoost = 5.0f;

	public bool triggered;

	// Use this for initialization
	void Start () {
		rb = GetComponent<Rigidbody> ();
		grappleScript = GameObject.FindGameObjectWithTag ("front").GetComponent<grapple_input_cd> ();
		player_rb = GameObject.FindGameObjectWithTag("Player").GetComponentInParent<Rigidbody> ();
		endPos = GameObject.FindGameObjectWithTag ("endPos");
		fpsController = player_rb.gameObject.GetComponent<FirstPersonController> ();
        anim = GameObject.FindGameObjectWithTag("Player").GetComponentInChildren<Animator>();

		// Conditions start false
		grappleRecalled = false;
		hooked = false;
		hookedLarge = false;
		triggered = false;

		// Used in Lerping hook back
		startTime = Time.time;

		// Launch hook
		rb.AddForce (rb.transform.forward * throwSpeed, ForceMode.Impulse);

		// Max time in addition to max range
		StartCoroutine (HookTimer());

		// Kills bugged hooks after hookTimeOut
		StartCoroutine (DebugDestroy ());

        updateSpeedVariables();
    }
	// Put in update to make the hook delete at the right time. Not good practice.
	void Update () {
		
    }
	// Scales based on multiplier
    void updateSpeedVariables() {
        throwSpeed = 35.0f * level_controller.level_speed;
		//hookTimer = 0.8f / level_controller.level_speed;
		maxRange = 19.0f * level_controller.level_speed;
        hookReturnLagHooked = 0.6f / level_controller.level_speed;
        hookReturnLagNotHooked = 0.2f / level_controller.level_speed;
        returnSpeed = 0.5f * level_controller.level_speed;
        hookTimeOut = 0.9f / level_controller.level_speed;
    }

	// Fixed update for smooth hook movement
	void FixedUpdate () {
		// Distance
		journeyLength = Vector3.Distance (transform.position, endPos.transform.position);

		// if grapple recalled/on the way back
		if (grappleRecalled) {
			float distCovered = (Time.time - startTime) * returnSpeed;
			float fracJourney = distCovered / journeyLength;
			// Lerps hook back to endPos if hooked, or player front if not.
			if (hooked) {
				pullEnemy ();
				transform.position = Vector3.Lerp(transform.position, endPos.transform.position, fracJourney);
			} else {
				transform.position = Vector3.Lerp(transform.position, grappleScript.transform.position, fracJourney);
			}
			// deletes hook if returned and didn't collide with player.
			if (journeyLength <= 1.3f) {
				DestroyHook ();
			}
		}
		// Hook comes back after stopping at maxRange.
		else if (journeyLength >= maxRange && grappleRecalled == false && !triggered) {
			StartCoroutine (ReturnBounce ());
			rb.velocity = Vector3.zero;
		}
		// Brings player to enemy
		else if (hookedLarge) {
			selfPullJourneyLength = Vector3.Distance (player_rb.transform.position, enemy.transform.position);
			float distCovered = (Time.time - startTime) * selfPullSpeed;
			float selfFracJourney = distCovered / selfPullJourneyLength;
			player_rb.transform.position = Vector3.Lerp(player_rb.transform.position, enemy.transform.position, selfFracJourney);
			if (selfPullJourneyLength < selfPullDistance) {
				hookedLarge = false;
				player_rb.velocity = Vector3.zero;
				player_rb.angularVelocity = Vector3.zero;
				player_rb.Sleep ();
				DestroyHook();
			}
		}
		// Updates multipliers every frame
	}

	// Lerps enemy to position of hook
	// 2 controls for distance and strength (basically)
	void pullEnemy () {
		enemy.transform.position = Vector3.Lerp (new Vector3 (enemy.transform.position.x,enemy.transform.position.y - verticalOffset,enemy.transform.position.z), rb.transform.position + rb.transform.forward * pullDistance, Time.deltaTime * pullStrength);
	}

	void OnCollisionEnter (Collision other) {
		// Hook may not collide with player by design -> temporary.
		/*
		if (other.gameObject.tag == "Player" && grappleRecalled || other.gameObject.tag == "player_HitBox" && grappleRecalled) {
			DestroyHook ();
			//Debug.Log ("Destroyed");
			}
		*/
		// Collision with anything not tagged.
		if (other.gameObject.tag == "Untagged" && !grappleRecalled && !triggered) {
			StartCoroutine (ReturnBounce ());
			//Debug.Log ("Bounce");
		}
	}

	void OnTriggerEnter (Collider trig) {
		// Small enemy=======================================================================================
		if (trig.gameObject.tag == "SmallGrab" && !grappleRecalled && !triggered) {
			SmallGrab (trig.gameObject);
            anim.SetBool("TetherHit", true);
			triggered = true;
        } // Large enemy =====================================================================================
		else if (trig.gameObject.tag == "LargeGrab" && !grappleRecalled && !triggered) {
			LargeGrab (trig.gameObject);
            anim.SetBool("TetherHit", true);
			triggered = true;
            //================================================================================================
        }
		else if (trig.gameObject.tag == "LargeObject" && !grappleRecalled && !triggered) {
			LargeObjectGrab (trig.gameObject);
            anim.SetBool("TetherHit", true);
			triggered = true;
            //================================================================================================
        }
	}

	public void SmallGrab (GameObject trig) {
		// Turn off collider on way back
		coll.enabled = false;
		collTrig.enabled = false;

		//Instantiate hit particle
		Instantiate(hookHitParticle,transform.position,transform.rotation);

		// Set game object reference
		enemy = trig.gameObject;
		enemy_rb = trig.gameObject.GetComponent<Rigidbody> ();

		//Turn off
		aiScript = enemy.GetComponent<small_shooter_ai> ();
		aiScript.isStunned = true;

		// Turn off nav mesh
		nav = enemy.GetComponent<NavMeshAgent> ();
		nav.enabled = false;

		// Used when hook disappears to call Stun();
		stunScript = enemy.GetComponent<stun> ();
		stunScript.anim.SetBool ("IsShooting", false);
		stunScript.anim.SetBool ("IsStunned", false);
		stunScript.stunCollider.enabled = true;

		verticalOffset = 0.0f;
		StartCoroutine (ReturnHooked ());
		/*
			// Movement detriment
		if (player_rb.velocity.z > movementPull_SpeedLimit) {
			rb.velocity -= new Vector3 (0,0,movementPull_Boost);
			float debugSpeed = player_rb.velocity.z;
			Debug.Log (debugSpeed);
		} 
		// Movement boost
		else if (player_rb.velocity.z < movementPull_SpeedLimit) {
			rb.velocity += new Vector3 (0,0,movementPull_Boost);
			float debugSpeed = player_rb.velocity.z;
			Debug.Log (debugSpeed);
		}
		*/

		//Debug.Log ("Small Grab");
	}

	public void LargeGrab (GameObject trig) {
		// Turn off collider on way back
		coll.enabled = false;
		collTrig.enabled = false;

		//Instantiate hit particle
		Instantiate(hookHitParticle,transform.position,transform.rotation);

		// Set game object reference
		enemy = trig.gameObject;
		enemy_rb = trig.gameObject.GetComponent<Rigidbody> ();

		//aiScript = enemy.GetComponent<small_shooter_ai> ();
		//aiScript.enabled = false;
		nav = enemy.GetComponent<NavMeshAgent> ();
		nav.enabled = false;

		// Used when hook disappears to call Stun();
		stunScriptLarge = enemy.GetComponent<stun_large> ();

		StartCoroutine (PullToEnemy ());

		selfPullJourneyLength = Vector3.Distance (player_rb.transform.position, enemy.transform.position);
		/*
			// Movement detriment
		if (player_rb.velocity.z > movementPull_SpeedLimit) {
			rb.velocity -= new Vector3 (0,0,movementPull_Boost);
			float debugSpeed = player_rb.velocity.z;
			Debug.Log (debugSpeed);
		} 
		// Movement boost
		else if (player_rb.velocity.z < movementPull_SpeedLimit) {
			rb.velocity += new Vector3 (0,0,movementPull_Boost);
			float debugSpeed = player_rb.velocity.z;
			Debug.Log (debugSpeed);
		}
		*/
	}

	public void LargeObjectGrab (GameObject trig) {
		// Turn off collider on way back
		coll.enabled = false;
		collTrig.enabled = false;

		// Set game object reference
		enemy = trig.gameObject;
		enemy_rb = trig.gameObject.GetComponent<Rigidbody> ();

		//Instantiate hit particle
		Instantiate(hookHitParticle,transform.position,transform.rotation);

		StartCoroutine (PullToEnemy ());
	}

	// Hook Timer is the time before the hook comes back without hitting anything
	IEnumerator HookTimer() {
		yield return new WaitForSeconds (hookTimer);
		if (!grappleRecalled && !triggered) {
			StartCoroutine (ReturnBounce ());
			rb.velocity = Vector3.zero;
		}
	}

	// Called on collision with objects but when hook misses everything
	IEnumerator ReturnBounce () {
		// sets triggered to stop particle from being created
		triggered = true;

		// Stops movement forward and limits bounce
		rb.velocity = new Vector3(rb.velocity.x/2, rb.velocity.y/2, 0);
        anim.SetBool("TetherMiss", true);
        anim.SetBool("TetherHit", false);
        anim.SetBool("IsTethering", false);

        yield return new WaitForSeconds (hookReturnLagNotHooked);
		// Hook Pull
		grappleRecalled = true;
	}

	// No bounce, just pull
	IEnumerator ReturnHooked () {
		// Stops movement
		rb.velocity = Vector3.zero;

		// Enemy Pull
		hooked = true;

		yield return new WaitForSeconds (hookReturnLagHooked);
		// Hook Pull
		grappleRecalled = true;
        anim.SetBool("IsTethering", false);
    }

	// Pulls you to enemy
	IEnumerator PullToEnemy () {
		// Sets journey distance
		selfPullJourneyLength = Vector3.Distance (player_rb.transform.position, enemy.transform.position);
		// Stops movement
		rb.velocity = Vector3.zero;
		transform.position = enemy.transform.position;
		yield return new WaitForSeconds (hookReturnLagHooked);
		// Self pull
		hookedLarge = true;
		// Disable input
		fpsController.enabled = false;
		yield return new WaitForSeconds (disableControlsTime);
		// Enable input
		fpsController.enabled = true;
        anim.SetBool("IsTethering", false);
        anim.SetBool("TetherHit", false);
        anim.SetBool("TetherMiss", false);
    }

	// Stuns if enemy hooked
	public void DestroyHook () {
		if (enemy) {
			if (enemy.gameObject.tag == "SmallGrab") {
				hooked = false;
                //aiScript.isStunned = true;
                //Debug.Log ("DestroyHook small");
                stunScript.Stun ();
			} else if (enemy.gameObject.tag == "LargeGrab") {
				stunScriptLarge.StunLarge ();
				fpsController.enabled = true;
			} else if (enemy.gameObject.tag == "LargeObject") {
				fpsController.enabled = true;
			}
		}
		grappleScript.InitiateCooldown();

        //Stop animations
        anim.SetBool("IsTethering", false);
        anim.SetBool("TetherHit", false);
        anim.SetBool("TetherMiss", false);

        // and destroys self
        Destroy (gameObject);
	}

	// Destroys if nothing else does
	IEnumerator DebugDestroy () {
		yield return new WaitForSeconds (hookTimeOut);
        anim.SetBool("TetherMiss", false);
        anim.SetBool("TetherHit", false);
        anim.SetBool("IsTethering", false);
        DestroyHook ();
	}
}
