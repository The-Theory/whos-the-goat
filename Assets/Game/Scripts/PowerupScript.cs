using UnityEngine;
using System;
using System.Collections.Generic;

//////////////////////////////////////////////////////////////////////////////
/// Powerup class
//////////////////////////////////////////////////////////////////////////////
public class Powerup {
    public static IReadOnlyDictionary<string, Powerup> Registry { get; } = RegisterPowerups();
    public Action<PlayerScript> OnCollected { get; }
    public Action<PlayerScript> OnExpired { get; }
    public float Duration { get; }

    public Powerup(Action<PlayerScript> onCollected, Action<PlayerScript> onExpired, float duration = 10f) {
        OnCollected = onCollected;
        OnExpired = onExpired;
        Duration = duration;
    }

    private static Dictionary<string, Powerup> RegisterPowerups() {
        var dict = new Dictionary<string, Powerup>();

        dict["Speed"] = new Powerup(
            onCollected: player => player.speedMultiplier = 2f,
            onExpired: player => player.speedMultiplier = 1f
        );

        dict["Expand"] = new Powerup(
            onCollected: player => player.scaleMultiplier = 2f,
            onExpired: player => player.scaleMultiplier = 1f
        );

        dict["Shrink"] = new Powerup(
            onCollected: player => player.scaleMultiplier = 0.5f,
            onExpired: player => player.scaleMultiplier = 1f
        );

        dict["Jump"] = new Powerup(
            onCollected: player => player.jumpMultiplier = 1.5f,
            onExpired: player => player.jumpMultiplier = 1f
        );

        return dict;
    }
}

public class PowerupScript : MonoBehaviour {
    ////////////////////////////////////////////////////////////////////////////////
    /// Collision detection
    ////////////////////////////////////////////////////////////////////////////////
    private void OnTriggerEnter2D(Collider2D other) {
        if (!other.gameObject.CompareTag("Player")) return;

        var player = other.gameObject.GetComponent<PlayerScript>();
        var sr = GetComponent<SpriteRenderer>();
        var powerupType = sr.sprite.name;

        if (!Powerup.Registry.TryGetValue(powerupType, out var powerup)) {
            Debug.LogError($"Powerup type {powerupType} not found in registry");
            DestroyPowerup();
        }
            
        powerup.OnCollected(player);

        if (powerup.Duration > 0f && powerup.OnExpired != null)
            player.RunPowerupExpiry(powerupType, powerup.Duration, powerup.OnExpired);

        DestroyPowerup();
    }

    private void DestroyPowerup() {
        gameObject.SetActive(false);
        Destroy(gameObject);
    }
}
