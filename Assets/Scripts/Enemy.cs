using UnityEngine;
using System.Collections;
using UnityEngine.AI;

[RequireComponent (typeof(NavMeshAgent))]

public class Enemy : LivingEntity {

	public enum State{Idle, Chasing, Attacking};
	State currentState;

	public ParticleSystem deathEffect;
	public static event System.Action OnDeathStatic;

	NavMeshAgent pathFinder;
	Transform target;
	LivingEntity targetEntity;
	Material skinMaterial;

	Color originalColor;

	float attackDistanceThreshhold = .5f;
	float timeBetweenAttacks = 1;
	float damage = 1f;

	float nextAttackTime;
	float myCollisionRadius;
	float targetCollisionRadius;

	bool hasTarget;

	void Awake(){
		pathFinder = GetComponent<NavMeshAgent> ();


		if (GameObject.FindGameObjectWithTag ("Player")!=null) {
			hasTarget = true;

			target = GameObject.FindGameObjectWithTag ("Player").transform;
			targetEntity = target.GetComponent<LivingEntity> ();

			myCollisionRadius = GetComponent<CapsuleCollider> ().radius;
			targetCollisionRadius = target.GetComponent<CapsuleCollider> ().radius;

		}
	} 
		
	protected override void Start () {
		base.Start ();
		skinMaterial = GetComponent<Renderer> ().material;
		originalColor = skinMaterial.color;

		if (hasTarget) {
			currentState = State.Chasing;

			targetEntity.OnDeath += OnTargetDeath;

			StartCoroutine (UpdatePath ());
		}
	}

	public void SetCharacteristics(float moveSpeed,int hitsToKillPlayer,float enemyHealth,Color skinColor){
		pathFinder.speed = moveSpeed;
		if (hasTarget) {
			damage = Mathf.Ceil(targetEntity.startingHealth / hitsToKillPlayer);
		}
		startingHealth = enemyHealth;

		deathEffect.startColor = new Color (skinColor.r, skinColor.g, skinColor.b, 1);
		skinMaterial = GetComponent<Renderer> ().material;
		skinMaterial.color = skinColor;
		originalColor = skinMaterial.color;
	}

	public override void TakeHit(float damage, Vector3 hitPoint, Vector3 hitDirection){
		AudioManager.instance.PlaySound ("Impact", transform.position);
		if (damage >= health) {
			if (OnDeathStatic != null) {
				OnDeathStatic ();
			}
			AudioManager.instance.PlaySound ("Enemy Death", transform.position);
			Destroy(Instantiate (deathEffect.gameObject, hitPoint, Quaternion.FromToRotation (Vector3.forward, hitDirection)) as GameObject,deathEffect.startLifetime);
		}
		base.TakeHit (damage, hitPoint, hitDirection);

	}

	void OnTargetDeath(){
		hasTarget = false;
		currentState = State.Idle;
	}

	// Update is called once per frame
	void Update () {
		if (hasTarget) {
			if (Time.time > nextAttackTime) {
				float sqrDstToTarget = (target.position - transform.position).sqrMagnitude;
				if (sqrDstToTarget < Mathf.Pow (attackDistanceThreshhold + targetCollisionRadius + myCollisionRadius, 2)) {
					nextAttackTime = Time.time + timeBetweenAttacks;
					AudioManager.instance.PlaySound ("Enemy Attack", transform.position);
					StartCoroutine (Attack ());
				}
			}
		}
	}

	IEnumerator Attack(){
		
		currentState = State.Attacking;
		pathFinder.enabled = false;

		Vector3 originalPosition = transform.position;
		Vector3 dirToTarget = (target.position - transform.position).normalized;
		Vector3 attackPosition = target.position-dirToTarget*(myCollisionRadius);


		float attackSpeed = 3;
		float percent = 0;
		skinMaterial.color = Color.red;
		bool hasAppliedDamge = false;

		while(percent<=1){
			if (percent >= .5f && !hasAppliedDamge) {
				hasAppliedDamge = true;
				targetEntity.TakeDamage (damage);
			}

			percent += Time.deltaTime * attackSpeed;
			float interpolation = (-Mathf.Pow (percent, 2) + percent) * 4;
			transform.position = Vector3.Lerp (originalPosition, attackPosition, interpolation);

			yield return null;
		}
		currentState = State.Chasing;
		pathFinder.enabled = true;
		skinMaterial.color = originalColor;
	}



	IEnumerator UpdatePath(){
		float refreshRate = 0.25f;
		while (hasTarget) {
			if (currentState == State.Chasing) {
				Vector3 dirToTarget = (target.position - transform.position).normalized;
				Vector3 targetPosition = target.position-dirToTarget*(attackDistanceThreshhold/2+myCollisionRadius+targetCollisionRadius);
				if (!dead) {
					pathFinder.SetDestination (targetPosition);
				}
			}



			yield return new WaitForSeconds (refreshRate);
		}
	}
}
