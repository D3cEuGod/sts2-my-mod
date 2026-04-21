namespace Sts2DpsPrototype;

internal static class DamageEventBridge
{
    internal static void RecordDamage(string playerId, string playerName, float damage)
    {
        DpsTracker.RecordDamage(playerId, playerName, damage);
    }

    internal static void ResetCombat()
    {
        DpsTracker.Reset();
    }
}
