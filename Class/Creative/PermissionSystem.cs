// using System;
// using Terraria;
// using Terraria.GameContent.Creative;
// using Terraria.ModLoader;
// using CreativePowers = On.Terraria.GameContent.Creative.CreativePowers;
//
// namespace QOS.Class.Creative;
//
// public class PermissionSystem : ModSystem
// {
//     public override void Load()
//     {
//         On.Terraria.GameContent.Creative.CreativePowersHelper.IsAvailableForPlayer += On_IsAvailableForPlayer;
//     }
//
//     private static bool On_IsAvailableForPlayer(On.Terraria.GameContent.Creative.CreativePowersHelper.orig_IsAvailableForPlayer invoke,
//         ICreativePower power, int plr)
//     {
//         return true;
//     }
//
//     public override void Unload()
//     {
//         On.Terraria.GameContent.Creative.CreativePowersHelper.IsAvailableForPlayer -=
//             On_IsAvailableForPlayer;
//     }
// }

