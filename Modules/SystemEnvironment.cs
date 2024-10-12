using System;

namespace TownOfHost.Modules;

public static class SystemEnvironment
{
    public static void SetEnvironmentVariables()
    {
        // ユーザ環境変数にログフォルダのパスを設定
        Environment.SetEnvironmentVariable("TOWN_OF_HOST_K_DIR_LOGS", Utils.GetLogFolder().FullName, EnvironmentVariableTarget.User);
    }
}
