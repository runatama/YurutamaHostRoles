using AmongUs.GameOptions;
using TownOfHost.Modules;
using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;

namespace TownOfHost.Roles.Impostor;
public sealed class Curser : RoleBase, IImpostor
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Curser),
            player => new Curser(player),
            CustomRoles.Curser,
            () => RoleTypes.Shapeshifter,
            CustomRoleTypes.Impostor,
            6700,
            SetupCustomOption,
            "cs"
        );
    public Curser(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        TKillCooldown = OptionTKillCooldown.GetFloat();
        KillCooldown = OptionKillCooldown.GetFloat();
        Cooldown = OptionCooldown.GetFloat();
        NroiCunt = OptionNroiCunt.GetInt();

        TargetId = byte.MaxValue;
    }
    private static OptionItem OptionTKillCooldown;
    private static OptionItem OptionKillCooldown;
    private static OptionItem OptionNroiCunt;
    private static OptionItem OptionCooldown;
    private static float TKillCooldown;
    private static float KillCooldown;
    private static float Cooldown;
    int NroiCunt;
    byte TargetId;
    enum OptionName
    {
        CurserTKillCooldown,
        CurserNroiCunt
    }
    private static void SetupCustomOption()
    {
        OptionTKillCooldown = FloatOptionItem.Create(RoleInfo, 9, OptionName.CurserTKillCooldown, OptionBaseCoolTime, 20f, false)
            .SetValueFormat(OptionFormat.Seconds);
        OptionKillCooldown = FloatOptionItem.Create(RoleInfo, 10, GeneralOption.KillCooldown, OptionBaseCoolTime, 30f, false)
            .SetValueFormat(OptionFormat.Seconds);
        OptionCooldown = FloatOptionItem.Create(RoleInfo, 11, GeneralOption.Cooldown, OptionBaseCoolTime, 30f, false)
            .SetValueFormat(OptionFormat.Seconds);
        OptionNroiCunt = FloatOptionItem.Create(RoleInfo, 12, OptionName.CurserNroiCunt, new(1, 15, 1), 2, false);
    }
    private void SetTarget(byte targetId)
    {
        TargetId = targetId;
        if (AmongUsClient.Instance.AmHost)
        {
            using var sender = CreateSender();
            sender.Writer.Write(targetId);
        }
    }
    public override bool CheckShapeshift(PlayerControl target, ref bool animate)
    {
        if ((target.Is(CustomRoleTypes.Impostor) && !SuddenDeathMode.NowSuddenDeathMode) || NroiCunt == 0 || target.PlayerId == TargetId) return false;

        SetTarget(target.PlayerId);
        Logger.Info($"{Player.GetNameWithRole()}のターゲットを{target.GetNameWithRole()}に設定", "CurserTarget");
        Player.MarkDirtySettings();
        Player.RpcResetAbilityCooldown();
        UtilsNotifyRoles.NotifyRoles(SpecifySeer: Player);
        NroiCunt--; // ターゲット設定したらNroiCuntを1減らす
        return false;
    }
    public void OnCheckMurderAsKiller(MurderInfo info)
    {
        if (!info.IsSuicide)
        {
            (var killer, var target) = info.AttemptTuple;
            // ターゲットを設定したプレイヤーを切ったらTKillcooldownにする処理
            if (TargetId == target.PlayerId)
            {
                Logger.Info($"{Player.GetNameWithRole()}が呪い殺しました", "CurserShapeshift");
                Main.AllPlayerKillCooldown[killer.PlayerId] = TKillCooldown;
                TargetId = byte.MaxValue;
                Player.SyncSettings(); // キルクールダウンを同期
            }
            else
            {
                Logger.Info($"{Player.GetNameWithRole()}が通常キルしました", "CurserNormalKillCooldown");
                Main.AllPlayerKillCooldown[killer.PlayerId] = KillCooldown;
                Player.SyncSettings(); // 通常のキルクールダウンを同期
            }
        }
    }
    public override string GetLowerText(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false, bool isForHud = false)
    {
        seen ??= seer;
        if (isForMeeting || !Player.IsAlive()) return "";
        if (!Is(seer) || !Is(seen)) return "";

        var target = PlayerCatch.GetPlayerById(TargetId);
        return target != null ? $"Target:{Utils.GetPlayerColor(target.PlayerId)}" : "";
    }
    public override string GetMark(PlayerControl seer, PlayerControl seen = null, bool _ = false)
    {
        seen ??= seer;
        return TargetId == seen.PlayerId ? Utils.ColorString(Palette.ImpostorRed, "θ") : "";
    }
    public override string GetProgressText(bool comms = false, bool gamelog = false) => Utils.ColorString(RoleInfo.RoleColor, $"({NroiCunt})");
    public override void ApplyGameOptions(IGameOptions opt) => AURoleOptions.ShapeshifterCooldown = Cooldown;
    public float CalculateKillCooldown() => KillCooldown;
    public override string GetAbilityButtonText() => GetString("MadChanger_Targetset");
}
