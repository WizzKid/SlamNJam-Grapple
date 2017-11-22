using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class grapple_input_cd : MonoBehaviour {
	private Transform front;
	public GameObject hookPrefab;
	private GameObject grapple;
	private grapple_logic gLogic;
    private Animator anim;

	public float default_coolDown = 2.0f;
	public float default_launchDelay = 0.2f;

    public float current_coolDown;
    public float current_launchDelay;

	public bool canHook;


	// Runs every time scene is loaded or player is loaded
	void Awake () {
		front = GameObject.FindGameObjectWithTag("front").GetComponent<Transform> ();
        anim = GameObject.FindGameObjectWithTag("Player").GetComponentInChildren<Animator>();
		canHook = true;
	}

	// Update is called once per frame - Don't use for physics. Use FixedUpdate, LateUpdate, Coroutine, or function.
	void Update () {
		if (Input.GetButtonDown("Hook") && canHook && !anim.GetBool("IsPunching")){
			canHook = false;
            Hook ();
		}
    }

	void OnCollisionEnter (Collision other) {
		if (other.gameObject.tag == "SmallGrab" && gLogic.hooked && gLogic.grappleRecalled) {
			gLogic.SmallGrab (other.gameObject);
			Debug.Log ("Enemy Collision with player");
		}
	}

	void Hook () {
		// Throw hook
		StartCoroutine (HookLaunchDelay());
		// Start cooldown
		//StartCoroutine (CoolDown());
	}

	public void InitiateCooldown () {
		StartCoroutine (CoolDown ());
	}

	IEnumerator HookLaunchDelay () {
        anim.SetBool("IsTethering", true);

        // Lag for animation to hook
        yield return new WaitForSeconds (current_launchDelay);

        // Gets a vector that points from the player's position to the target's.
        var heading = front.position - Camera.main.transform.position;
		Quaternion rotation = Quaternion.LookRotation (heading);

		// Hook instantiation
		grapple = (GameObject)Instantiate (hookPrefab, front.transform.position, rotation);
		gLogic = grapple.gameObject.GetComponent<grapple_logic> ();
	}

	IEnumerator CoolDown () {
		// Cooldown
		yield return new WaitForSeconds(current_coolDown);
		canHook = true;
	}
}
