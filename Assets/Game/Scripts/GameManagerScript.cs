using UnityEngine;
using System.Collections.Generic;

public class GameManagerScript : MonoBehaviour {
    ////////////////////////////////////////////////////////////////////////////////
    /// Variables
    ////////////////////////////////////////////////////////////////////////////////
    // Inputs
    [SerializeField] private Sprite[] powerupSprites;
    [SerializeField] private GameObject powerupPrefab;
    [SerializeField] private GameObject powerupFolder;

    // Spawn variables
    private float powerupInterval = 2f;
    private float powerupTimer = 0f;
    private Vector2 powerupSpawnRange = new Vector2(13f, 7f);



    ////////////////////////////////////////////////////////////////////////////////
    /// Update
    ////////////////////////////////////////////////////////////////////////////////
    private void Update() {
        /////////////// Spawn powerup logic ///////////////
        powerupTimer += Time.deltaTime;
        if (powerupTimer >= powerupInterval) {
            powerupTimer = 0f;

            SpawnPowerup();
        }
    }

    public void SpawnPowerup() {
        var platformLayer = LayerMask.GetMask("Platform");
        float x, y;
        const int maxAttempts = 20;
        int attempts = 0;

        // Try to spawn powerup at random pos
        // Retry if collision with platform
        do {
            x = Random.Range(-powerupSpawnRange.x, powerupSpawnRange.x);
            y = Random.Range(-powerupSpawnRange.y, powerupSpawnRange.y);
            attempts++;
        } while (Physics2D.OverlapCircle(new Vector2(x, y), 0.5f, platformLayer) != null && attempts < maxAttempts);

        // Give up if no space found
        if (attempts >= maxAttempts)
            return;

        // Load data
        var position = new Vector3(x, y, 0f);
        var sprite = powerupSprites[Random.Range(0, powerupSprites.Length)];

        // Load powerup
        var powerup = Instantiate(powerupPrefab, position, Quaternion.identity);

        // Assign data 
        powerup.transform.SetParent(powerupFolder.transform);
        powerup.GetComponent<SpriteRenderer>().sprite = sprite;
    }
}
