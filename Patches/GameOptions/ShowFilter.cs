using TownOfHost.Roles.Core;
using static TownOfHost.Translator;

namespace TownOfHost;

class ShowFilter
{
    public static CustomRoles NowSettingRole;
    public static OptionItem NosetOptin;

    public static void SetRoleAndReset(CustomRoles role)
    {
        var oldsettingrole = NowSettingRole;
        var oldOption = NosetOptin;
        if (NosetOptin is FilterOptionItem filterOptionItem) filterOptionItem.SetRoleValue(role);

        NowSettingRole = CustomRoles.NotAssigned;
        NosetOptin = null;

        _ = new LateTask(() =>
        {
            GameSettingMenuStartPatch.tabButtons[(int)oldOption.Tab]?.OnClick?.Invoke();
            _ = new LateTask(() =>
            {
                if (GameSettingMenuStartPatch.rolebutton.TryGetValue(oldsettingrole, out var button))
                {
                    button?.OnClick?.Invoke();

                    var rand = IRandom.Instance;
                    int rect = IRandom.Instance.Next(1, 101);
                    if (rect < 40)
                        GameSettingMenuChangeTabPatch.meg = GetString("ModSettingInfo0");
                    else if (rect < 50)
                        GameSettingMenuChangeTabPatch.meg = GetString("ModSettingInfo10");
                    else if (rect < 60)
                        GameSettingMenuChangeTabPatch.meg = GetString("ModSettingInfo1");
                    else if (rect < 70)
                        GameSettingMenuChangeTabPatch.meg = GetString("ModSettingInfo2");
                    else if (rect < 80)
                        GameSettingMenuChangeTabPatch.meg = GetString("ModSettingInfo3");
                    else if (rect < 90)
                        GameSettingMenuChangeTabPatch.meg = GetString("ModSettingInfo4");
                    else if (rect < 95)
                        GameSettingMenuChangeTabPatch.meg = GetString("ModSettingInfo5");
                    else if (rect < 99)
                        GameSettingMenuChangeTabPatch.meg = GetString("ModSettingInfo6");
                    else
                        GameSettingMenuChangeTabPatch.meg = GetString("ModSettingInfo7");
                }
            }, 0.4f, "ResetTabandopt", true);
        }, 0.2f, "ResetOption", true);
    }

    public static void CheckAndReset()
    {
        var olda = NowSettingRole;
        var oldb = NosetOptin;
        NowSettingRole = CustomRoles.NotAssigned;
        NosetOptin = null;

        if (olda is CustomRoles.NotAssigned) return;
        if (oldb == null) return;

        var oldsettingrole = NowSettingRole;
        if (NosetOptin is FilterOptionItem filterOptionItem) filterOptionItem.SetRoleValue(0);

    }
}