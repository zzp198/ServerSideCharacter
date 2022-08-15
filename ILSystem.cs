﻿using System;
using System.Collections.Generic;
using System.IO;
using MonoMod.Cil;
using Terraria;
using Terraria.ID;
using Terraria.IO;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI;
using Terraria.Utilities;

namespace SSC;

public class ILSystem : ModSystem
{
    public int Cooldown;

    public override void Load()
    {
        // 此时已经选择玩家,并对其他玩家进行了初始化.通过IL挂载可确保仅被执行一次.
        IL.Terraria.Netplay.InnerClientLoop += ILHook1;
        // 隐藏其他无关紧要的UI,防止因为提前操作导致数据丢失.
        IL.Terraria.Main.DrawInterface += ILHook2;
        // 显示SSC是否启用
        IL.Terraria.Main.DrawInventory += ILHook3;
        // 用于显示UI,但是不用TML提供的接口以免被重复调用.并在应用SSC后重复调用一遍TML的接口确保新角色数据正常.
        IL.Terraria.Player.Hooks.EnterWorld += ILHook4;
        // 每当多人模式客户端游戏中请求保存时触发.(其他mod请求此方法时也可触发保存,完美)
        IL.Terraria.Player.SavePlayer += ILHook5;
        // 缩短自动保存的时间间隔
        IL.Terraria.Main.DoUpdate_AutoSave += ILHook6;
    }

    public override void Unload()
    {
        IL.Terraria.Netplay.InnerClientLoop -= ILHook1;
        IL.Terraria.Main.DrawInterface -= ILHook2;
        IL.Terraria.Main.DrawInventory -= ILHook3;
        IL.Terraria.Player.Hooks.EnterWorld -= ILHook4;
        IL.Terraria.WorldGen.saveToonWhilePlayingCallBack -= ILHook5;
        IL.Terraria.Main.DoUpdate_AutoSave -= ILHook6;
    }

    private static void ILHook1(ILContext il)
    {
        var c = new ILCursor(il);
        c.GotoNext(i => i.MatchLdstr("Net.FoundServer"));
        c.EmitDelegate(() =>
        {
            Main.statusText = "SSC Hooking...";
            var data = new PlayerFileData(Path.Combine(SSC.SavePath, $"{SSC.SteamID}.plr"), false)
            {
                Metadata = FileMetadata.FromCurrentSettings(FileType.Player),
                Player = new Player
                {
                    name = DateTime.UtcNow.Ticks.ToString(),
                    // name = SSC.SteamID.ToString(), TODO 
                    difficulty = Main.LocalPlayer.difficulty,
                    savedPerPlayerFieldsThatArentInThePlayerClass = new Player.SavedPlayerDataWithAnnoyingRules(),
                }
            };
            data.Player.AddBuff(ModContent.BuffType<Content.Spooky>(), 198); // 幽灵化
            data.MarkAsServerSide();
            data.SetAsActive();
        });
    }

    private static void ILHook2(ILContext il)
    {
        var c = new ILCursor(il);
        c.GotoNext(MoveType.After, i => i.MatchCall(typeof(SystemLoader), nameof(SystemLoader.ModifyInterfaceLayers)));
        c.EmitDelegate<Func<List<GameInterfaceLayer>, List<GameInterfaceLayer>>>(layers =>
        {
            if (Main.LocalPlayer.HasBuff<Content.Spooky>())
            {
                foreach (var layer in layers)
                {
                    switch (layer.Name)
                    {
                        case "Vanilla: Map / Minimap":
                        case "Vanilla: Resource Bars":
                            layer.Active = false;
                            break;
                        default:
                            layer.Active = layer.Name.StartsWith("Vanilla");
                            break;
                    }
                }
            }

            return layers;
        });
    }

    private static void ILHook3(ILContext il)
    {
        var c = new ILCursor(il);
        c.GotoNext(MoveType.After, i => i.MatchCallvirt(typeof(LocalizedText), "get_Value"));
        c.EmitDelegate<Func<string, string>>(i => $"{i} (SSC)");
    }

    private static void ILHook4(ILContext il)
    {
        var c = new ILCursor(il);
        c.GotoNext(MoveType.After, i => i.MatchCall(typeof(PlayerLoader), nameof(PlayerLoader.OnEnterWorld)));
        c.EmitDelegate(() =>
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                UISystem.UI.SetState(UISystem.View);
            }
        });
    }

    private static void ILHook5(ILContext il)
    {
        // IL_002e: ldloc.0      // V_0
        // IL_002f: ldftn        instance void Terraria.Player/'<>c__DisplayClass1757_0'::'<SavePlayer>b__0'()
        // IL_0035: newobj       instance void [System.Runtime]System.Action::.ctor(object, native int)
        // IL_003a: call         void Terraria.Utilities.FileUtilities::ProtectedInvoke(class [System.Runtime]System.Action)

        // 原方法会成功保存Map,剩下的内容由此Hook继续下去.
        var c = new ILCursor(il);
        c.GotoNext(MoveType.After, i => i.MatchCall(typeof(FileUtilities), nameof(FileUtilities.ProtectedInvoke)));
        c.EmitDelegate(() =>
        {
            if (Main.netMode == NetmodeID.MultiplayerClient && !Main.LocalPlayer.HasBuff<Content.Spooky>())
            {
                var id = SSC.SteamID;
                var data = SSCUtils.Player2ByteArray(id, Main.LocalPlayer);

                var mp = SSCUtils.GetPacket(SSC.ID.SSCBinary);
                mp.Write(id);
                mp.Write(Main.LocalPlayer.name);
                mp.Write(data.Length);
                mp.Write(data);
                mp.Send();
            }
        });
    }

    private static void ILHook6(ILContext il)
    {
        var c = new ILCursor(il);
        c.GotoNext(i => i.MatchLdcI4(300000));
        c.EmitDelegate<Func<long, long>>(_ => 60000);
    }
}