using UnityEngine;

public class AttributeSystem : SceneSingleton<AttributeSystem>
{
    public AttributeModifier StudMoveSpeedMod { private set; get; } = new AttributeModifier();
    public AttributeModifier ProfMoveSpeedMod { private set; get; } = new AttributeModifier();
    public AttributeModifier TaskEfficiencyMod { private set; get; } = new AttributeModifier();
    public AttributeModifier BoostTaskChanceMod { private set; get; } = new AttributeModifier();
    public AttributeModifier BarricadeInstallSpeedMod { private set; get; } = new AttributeModifier();
    public AttributeModifier HackRepairSpeedMod { private set; get; } = new AttributeModifier();
    public AttributeModifier WeaponSupplySpeedMod { private set; get; } = new AttributeModifier();
    public AttributeModifier HackBlockChanceMod { private set; get; } = new AttributeModifier();
    public AttributeModifier StudStomachScaleMod { private set; get; } = new AttributeModifier();
    public AttributeModifier StudHeadScaleMod { private set; get; } = new AttributeModifier();
    public AttributeModifier JumpDamageMod { private set; get; } = new AttributeModifier();
    public AttributeModifier TurtleNeckDistanceMod { private set; get; } = new AttributeModifier();
    public AttributeModifier StudEscapeChanceMod { private set; get; } = new AttributeModifier();
    public AttributeModifier StudDamageMod { private set; get; } = new AttributeModifier();
    public AttributeModifier ChaosDecreaseMod { private set; get; } = new AttributeModifier();
    public AttributeModifier HealDelaySpeedMod { private set; get; } = new AttributeModifier();
    public AttributeModifier StaminaCostMod { private set; get; } = new AttributeModifier();
    public AttributeModifier ShotSpreadMod { private set; get; } = new AttributeModifier();
    public AttributeModifier MutinyMoneyMod { private set; get; } = new AttributeModifier();
    public AttributeModifier StudHairScaleMod { private set; get; } = new AttributeModifier();
    public AttributeModifier BarricadeHitAmountMod { private set; get; } = new AttributeModifier();
    public AttributeModifier JumpStaminaMod { private set; get; } = new AttributeModifier();

    public AttributeModifier ThrowVelocityMod { private set; get; } = new AttributeModifier();
    public AttributeModifier ThrowDamageMod { private set; get; } = new AttributeModifier();
    public AttributeModifier MeleeAttackSpeedMod { private set; get; } = new AttributeModifier();
    public AttributeModifier MeleeDamageMod { private set; get; } = new AttributeModifier();


    public bool IsDeskCoffee { set; get; }
    public bool IsStudBald { set; get; }
    public bool IsStudOutline { set; get; }
    public bool IsDeskFood { set; get; }
    public bool IsExitAlarm { set; get; }
    public bool IsStudShackle { set; get; }
    public bool IsOtakuPoster { set; get; }
    public bool IsMetalBarricade { set; get; }
}
