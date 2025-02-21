using System.Collections.Generic;
using UnityEngine;
using System.Linq;

using TownOfHost.Roles.Core;
using static TownOfHost.Options;

namespace TownOfHost.Roles.Ghost
{
    public class AsistingAngel
    {
        private static readonly int Id = 60400;
        public static List<byte> playerIdList = new();
        public static OptionItem CoolDown;
        public static OptionItem AddClowDown;
        public static OptionItem Guardtime;
        public static OptionItem LimitDay;
        public static PlayerControl Asist;
        public static float Count;
        public static byte Track;
        public static bool Guard;
        public static int Limit;
        public static Vector3 pos;
        public static void SetupCustomOption()
        {
            SetupRoleOptions(Id, TabGroup.GhostRoles, CustomRoles.AsistingAngel, new(1, 1, 1));
            GhostRoleAssingData.Create(Id + 1, CustomRoles.AsistingAngel, CustomRoleTypes.Crewmate, CustomRoleTypes.Neutral);
            CoolDown = FloatOptionItem.Create(Id + 2, "AsistingAngelCoolDown", new(0f, 180f, 0.5f), 25f, TabGroup.GhostRoles, false)
                .SetValueFormat(OptionFormat.Seconds).SetParent(CustomRoleSpawnChances[CustomRoles.AsistingAngel]);
            AddClowDown = FloatOptionItem.Create(Id + 3, "AsistingAngelAddClowDown", new(0f, 30f, 0.5f), 5f, TabGroup.GhostRoles, false)
            .SetValueFormat(OptionFormat.Seconds).SetParent(CustomRoleSpawnChances[CustomRoles.AsistingAngel]);
            Guardtime = FloatOptionItem.Create(Id + 4, "AsistingAngelGuardtime", new(1f, 30f, 1f), 5f, TabGroup.GhostRoles, false)
                .SetValueFormat(OptionFormat.Seconds).SetParent(CustomRoleSpawnChances[CustomRoles.AsistingAngel]);
            LimitDay = FloatOptionItem.Create(Id + 5, "AsistingAngelLimitDay", new(0f, 5f, 1f), 3f, TabGroup.GhostRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.AsistingAngel]);
        }
        public static void Init()
        {
            playerIdList = new();
            Asist = null;
            Track = byte.MaxValue;
            Count = 0;
            Limit = 0;
            Guard = false;
            pos = new Vector3(999f, 999f);
            CustomRoleManager.MarkOthers.Add(OtherMark);
        }
        public static void Add(byte playerId)
        {
            playerIdList.Add(playerId);
        }

        public static bool ch()
        {
            //アシスト先が決まってるなら～
            if (Asist != null) return false;
            foreach (var pc in PlayerCatch.AllPlayerControls.Where(x => x.IsGhostRole()))
            {
                if (pc.Is(CustomRoles.AsistingAngel)) return true;
            }
            return false;
        }

        public static void UseAbility(PlayerControl pc, PlayerControl target)
        {
            if (pc.Is(CustomRoles.AsistingAngel))
            {
                //アシスト先が決まってない場合
                if (Limit > LimitDay.GetFloat() && Asist == null) return;//経過日数が設定を超えたらもう負け。

                if (Asist == null)
                {
                    Asist = target;
                    pc.RpcResetAbilityCooldown();
                    UtilsNotifyRoles.NotifyRoles(SpecifySeer: [target, pc]);
                }
                else
                {
                    //どっちかは知らんが回数とリセットは入れるで～
                    Count++;
                    pc.RpcResetAbilityCooldown(kousin: true);

                    if (!Asist.IsAlive()) return;//アシスト対象が死んでるならでしゃばるな。

                    if (target == Asist)//アシスト対象なら一定時間ばりあー
                    {
                        Guard = true;
                        _ = new LateTask(() =>
                        {
                            Guard = false;
                            pc.RpcResetAbilityCooldown(kousin: true);//成功の有無にかかわらずリセットさせる。
                        }, Guardtime.GetFloat(), "", true);
                    }
                    else//違うなら対象の位置を矢印で教えないとねっ
                    {
                        //リセット処理
                        GetArrow.Remove(Asist.PlayerId, pos);
                        Track = target.PlayerId;
                        pos = target.transform.position;
                        GetArrow.Add(Asist.PlayerId, target.transform.position);
                        UtilsNotifyRoles.NotifyRoles(SpecifySeer: [target, pc]);
                        pc.RpcResetAbilityCooldown(kousin: true);
                    }
                }
            }
        }
        public static string OtherMark(PlayerControl seer, PlayerControl seen, bool isForMeeting = false)
        {
            seen ??= seer;

            //タゲ未設定時
            if (Limit > LimitDay.GetFloat() && Asist == null) return "";//過ぎたなら後は後悔しなっ。
            if (Asist == null && seer == seen && seer.Is(CustomRoles.AsistingAngel)) return $"<color=#8da0d6> ({Limit}/{LimitDay.GetFloat()})</color>";
            if (Asist == null) return "";

            //タゲがいる時
            var r = "";
            if (seer == seen)
                if (seer == Asist || seer.Is(CustomRoles.AsistingAngel))//対象 or アシストの場合
                {
                    r = "<color=#8da0b6>＠</color>";
                    if (Track is not byte.MaxValue && !isForMeeting)
                    {
                        return r + $"  <color=#8da0b6>{GetArrow.GetArrows(seer, pos)}</color>";
                    }
                    else
                        return r;
                }

            if (!seer.IsAlive())//霊界の場合
            {
                if (seen == Asist || seen.Is(CustomRoles.AsistingAngel))
                    return "<color=#8da0b6>＠</color>";
            }

            return "";
        }
    }
}