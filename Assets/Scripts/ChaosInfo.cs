using UnityEngine;



public abstract class Info
{
    public abstract string Description { get; }
    public abstract string StatText { get; }
    public abstract Color PanelColor { get; }



    public Info()
    {

    }
}


public class HackBlockInfo : Info
{
    public override string Description => "시스템 해킹 차단 성공!!";
    public override string StatText => $"전등 OFF 무효화";
    public override Color PanelColor => new Color(0, 255/255f, 180/255f, 220/255f);



    public HackBlockInfo()
    {

    }
}



public class HackInfo : Info
{
    public override string Description => "시스템 해킹 발생!!";
    public override string StatText => $"전등 OFF";
    public override Color PanelColor => new Color(255/255f, 100/255f, 0, 220 / 255f);



    public HackInfo()
    {

    }
}




public abstract class MoneyInfo : Info
{
    protected int _money;
    public override string StatText => $"돈 +{_money.ToString("N0")}";
    public override Color PanelColor => new Color(50 / 255f, 255 / 255f, 0, 220 / 255f);



    public MoneyInfo(int money)
    {
        _money = money;
    }
}



public class ProjectMoneyInfo : MoneyInfo
{
    public override string Description => "프로젝트 완성!!";

    public ProjectMoneyInfo(int money) : base(money)
    {
    }
}



public class MutinyMoneyInfo : MoneyInfo
{
    public override string Description => "하극상 배상안 체결!!";

    public MutinyMoneyInfo(int money) : base(money)
    {
    }
}



public abstract class ChaosInfo : Info
{
    protected float _chaos;
    //public abstract string Description { get; }
    public override string StatText => $"혼란 +{_chaos.ToString("F0")}";
    public override Color PanelColor => new Color(255 / 255f, 0, 0, 220 / 255f);

    public ChaosInfo(float chaos)
    {
        _chaos = chaos;
    }
}



public class GunShotChaos : ChaosInfo
{
    public override string Description => "총기 발사!!";

    public GunShotChaos(float chaos) : base(chaos) { }
}



public class EscapedChaos : ChaosInfo
{
    public override string Description => "대학원생 탈출!!";

    public EscapedChaos(float chaos) : base(chaos) { }
}



public class InnocentKillChaos : ChaosInfo
{
    public override string Description => "무고한 대학원생 진압!!";

    public InnocentKillChaos(float chaos) : base(chaos) { }
}



public class NormalFoodRemovedChaos : ChaosInfo
{
    public override string Description => "맛있는 음식 약탈!!";

    public NormalFoodRemovedChaos(float chaos) : base(chaos) { }
}