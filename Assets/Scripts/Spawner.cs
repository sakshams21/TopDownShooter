using UnityEngine;
using System.Collections;

public class Spawner : MonoBehaviour {
	public bool devMode;
	public Wave[] waves;
	public Enemy enemy;

	LivingEntity playerEntity;
	Transform playerT;

	int enemiesRemainingToSpawn;
	int currentWaveNumber;
	Wave currentWave;
	float nextSpawnTime;
	int enemiesRemainingAlive;

	MapGenerator map;

	float timeBetweenCampingChecks = 2;
	float campThresholdDistance = 1.5f;
	float nextCampCheckTime;
	Vector3 campPositionOld;
	bool isCamping;

	bool isDisabled;

	public event System.Action<int> OnNewWave;

	void Start(){
		playerEntity = FindObjectOfType<Player> ();
		playerT = playerEntity.transform;
		playerEntity.OnDeath += OnPlayerDeath;

		nextCampCheckTime = timeBetweenCampingChecks + Time.time;
		campPositionOld = playerT.position;

		map = FindObjectOfType<MapGenerator> ();
		NextWave ();
	}



	void Update(){
		if (!isDisabled) {
			if (Time.time > nextCampCheckTime) {
				nextCampCheckTime = Time.time + timeBetweenCampingChecks;
				isCamping = (Vector3.Distance (playerT.position, campPositionOld) < campThresholdDistance);
				campPositionOld = playerT.position;
			}

			if ((enemiesRemainingToSpawn > 0 || currentWave.infinite)&& Time.time > nextSpawnTime) {
				enemiesRemainingToSpawn--;
				nextSpawnTime = Time.time + currentWave.timeBetweenSpawn;

				StartCoroutine ("SpawnEnemy");

			}
		}

		if (devMode) {
			if (Input.GetKeyDown (KeyCode.Return)) {
				StopCoroutine ("SpawnEnemy");
				foreach (Enemy enemy in FindObjectsOfType<Enemy>()) {
					GameObject.Destroy (enemy.gameObject);
				}
				NextWave ();
			}
		}
	}

	IEnumerator SpawnEnemy(){
		float spawnDelay = 1;
		float tileFlashSpeed = 4;


		Transform spawnTile = map.GetRandomOpenTile ();
		if (isCamping) {
			spawnTile = map.GetTileFromPosition (playerT.position);
		}
		Material tileMat = spawnTile.GetComponent<Renderer> ().material;
		Color initialColor = Color.gray;
		Color flashColor = Color.red;
		float spawnTimer = 0;

		while(spawnTimer<spawnDelay){
			tileMat.color = Color.Lerp(initialColor,flashColor,Mathf.PingPong(spawnTimer*tileFlashSpeed,1));

			spawnTimer+=Time.deltaTime;
			yield return null;
		}

		Enemy spawnedEnemy = Instantiate (enemy, spawnTile.position+Vector3.up, Quaternion.identity) as Enemy;
		spawnedEnemy.OnDeath += OnEnemyDeath;
		spawnedEnemy.SetCharacteristics (currentWave.moveSpeed, currentWave.hitsToKillPlayer, currentWave.enemyHealth, currentWave.skinColor);
	}

	void ResetPlayerPosition(){
		if (playerT != null) {
			playerT.position = map.GetTileFromPosition (Vector3.zero).position + Vector3.up;
		}
	}

	void OnPlayerDeath(){
		isDisabled = true;
	}

	void OnEnemyDeath(){
		enemiesRemainingAlive--;
		if (enemiesRemainingAlive == 0) {
			NextWave ();
		}
	}

	void NextWave(){

		if (currentWaveNumber > 0) {
			AudioManager.instance.PlaySound2D ("Level Completed");
		}
		currentWaveNumber++;
		if (currentWaveNumber - 1 < waves.Length) {
			currentWave = waves [currentWaveNumber - 1];

			enemiesRemainingToSpawn = currentWave.enemyCount;
			enemiesRemainingAlive = enemiesRemainingToSpawn;

			if (OnNewWave != null) {
				OnNewWave (currentWaveNumber);
			}
			ResetPlayerPosition ();
		}
	}

	[System.Serializable]
	public class Wave{
		public bool infinite;
		public int enemyCount;
		public float timeBetweenSpawn;

		public float moveSpeed;
		public float enemyHealth;
		public int hitsToKillPlayer;
		public Color skinColor;
	}

}
