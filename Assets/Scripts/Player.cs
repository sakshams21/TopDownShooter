﻿using UnityEngine;
using System.Collections;

[RequireComponent(typeof(PlayerController))]
[RequireComponent(typeof(GunController))]

public class Player : LivingEntity {

	public float moveSpeed = 5;
	public Crosshairs crosshairs;

	PlayerController controller;
	Camera viewCamera;
	GunController gunController;


	// Use this for initialization
	protected override void Start () {
		base.Start ();

	}

	void Awake(){
		controller = GetComponent<PlayerController> ();
		gunController = GetComponent<GunController> ();
		viewCamera = Camera.main;
		FindObjectOfType<Spawner> ().OnNewWave += OnNewWave;
	}

	void OnNewWave(int waveNumber){
		health = startingHealth;
		gunController.EquipGun (waveNumber - 1);
	}
	
	// Update is called once per frame
	void Update () {
		//movement input
		Vector3 moveInput = new Vector3(Input.GetAxis("Horizontal"),0,Input.GetAxisRaw("Vertical"));
		Vector3 moveVelocity = moveInput.normalized * moveSpeed;
		controller.Move (moveVelocity);
		//look input
		Ray ray = viewCamera.ScreenPointToRay (Input.mousePosition);
		Plane groundPlane = new Plane (Vector3.up, Vector3.up*gunController.GunHeight);
		float rayDistance;

		if (groundPlane.Raycast (ray, out rayDistance)) {
			Vector3 point = ray.GetPoint (rayDistance);
			//Debug.DrawLine (ray.origin, point, Color.red);
			controller.lookAt(point);
			crosshairs.transform.position = point;
			crosshairs.DetectTargets (ray);
			if((new Vector2 (point.x, point.z) - new Vector2 (transform.position.x, transform.position.z)).magnitude>1.5){
				gunController.Aim (point);
			}
		}

		//weapon input
		if(Input.GetMouseButton(0)){
			gunController.OnTriggerHold ();
		}
		if(Input.GetMouseButtonUp(0)){
			gunController.OnTriggerRelease ();
		}
		if (Input.GetKeyDown (KeyCode.R)) {
			gunController.Reload ();
		}

		if (transform.position.y < -10) {
			TakeDamage (health);
		}
	}

	public override void Die(){
		AudioManager.instance.PlaySound ("Player Death", transform.position);
		base.Die ();
	}
}
