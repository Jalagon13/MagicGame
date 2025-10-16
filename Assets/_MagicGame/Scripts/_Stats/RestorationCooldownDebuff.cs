using UnityEngine;

namespace ProjectTinker
{
    /// <summary>
    /// Special debuff that prevents health restoration from consumables
    /// </summary>
    public class RestorationCooldownDebuff : Buff
    {
        public const string RESTORATION_COOLDOWN_SOURCE = "RestorationCooldown";

        public RestorationCooldownDebuff(Stat stat, float duration)
            : base(stat, new StatModifier(0f, StatModifierType.Flat, RESTORATION_COOLDOWN_SOURCE), "Restoration Cooldown Debuff", duration)
        {
        }

        /// <summary>
        /// Creates a restoration cooldown debuff that doesn't modify any stats
        /// but serves as a flag to prevent health restoration
        /// </summary>
        public static RestorationCooldownDebuff CreateCooldownDebuff(float duration)
        {
            // Create a dummy stat just for the cooldown mechanism
            var dummyStat = new Stat(0f);
            return new RestorationCooldownDebuff(dummyStat, duration);
        }
    }
}
