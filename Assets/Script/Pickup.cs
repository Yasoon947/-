using UnityEngine;

public enum PickupType
{
    StunAI,
    DoubleJump,
    Shield,
}

[RequireComponent(typeof(Collider2D))]
public class Pickup : MonoBehaviour
{
    public PickupType type = PickupType.StunAI;
    public float duration = 5f;

    private void Reset()
    {
        var col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (GameStateManager.Instance != null && !GameStateManager.Instance.IsPlaying)
            return;

        var player = other.GetComponent<PlayerMove>();
        if (player == null) return;

        ApplyEffect(player);
        AudioManager.Instance?.PlayPickup();
        ReturnToPool();
    }

    private void ApplyEffect(PlayerMove player)
    {
        switch (type)
        {
            case PickupType.StunAI:
                foreach (var ai in FindObjectsOfType<ChaserAI>())
                    ai.ApplyStun(duration);
                break;
            case PickupType.DoubleJump:
                player.ActivateDoubleJump(duration);
                break;
            case PickupType.Shield:
                player.ActivateShield();
                break;
        }
    }

    private void ReturnToPool()
    {
        if (PickupPoolManager.Instance != null)
            PickupPoolManager.Instance.Return(type, gameObject);
        else
            Destroy(gameObject);
    }
}