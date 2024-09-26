using BepInEx;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SmarterPigmentAutoselect
{
    [BepInPlugin(GUID, "Smarter Pigment Autoselect", "1.0.0")]
    [HarmonyPatch]
    public class Plugin : BaseUnityPlugin
    {
        public const string GUID = "SpecialAPI.SmarterPigmentAutoselect";

        public void Awake()
        {
            new Harmony(GUID).PatchAll();
        }

        [HarmonyPatch(typeof(CombatVisualizationController), nameof(CombatVisualizationController.TryConnectCostSlots))]
        [HarmonyPrefix]
        public static bool Smart(CombatVisualizationController __instance, out bool __result, ManaBarType type, bool matchColor, out bool didConnectAny)
        {
            didConnectAny = false;
            __result = true;

            var info = __instance._costBarInfo;
            var options = new List<List<int>>(info.Length);
            var slots = new List<int>(info.Length);
            
            var bar = __instance.GetManaBarInfo(type);
            var barUI = __instance.GetManaBar(type);

            ManaSlotUIInfo[] secondBar = null;
            ManaBarLayout secondBarUI = null;

            if(type == ManaBarType.Main)
            {
                secondBar = __instance.GetManaBarInfo(ManaBarType.Yellow);
                secondBarUI = __instance.GetManaBar(ManaBarType.Yellow);
            }

            for (int i = 0; i < info.Length; i++)
            {
                var costStuff = info[i];
                options.Add([]);
                slots.Add(i);

                if (!costStuff.HasManaSlot || costStuff.IsConnected)
                    continue;

                for (int j = bar.Length - 1; j >= 0; j--)
                {
                    var stuffInBar = bar[j];

                    if(!stuffInBar.IsEmpty && !stuffInBar.IsJumping && !stuffInBar.InUse && !stuffInBar.BeingGrabbed && (!matchColor || !costStuff.ManaSlot.DealsCostDamage(stuffInBar.Mana)))
                        options[i].Add(j);
                }

                if (secondBar == null)
                    continue;

                for(int j = secondBar.Length - 1; j >= 0; j--)
                {
                    var stuffInBar = secondBar[j];

                    if (!stuffInBar.IsEmpty && !stuffInBar.IsJumping && !stuffInBar.InUse && !stuffInBar.BeingGrabbed && (!matchColor || !costStuff.ManaSlot.DealsCostDamage(stuffInBar.Mana)))
                        options[i].Add(j - secondBar.Length);
                }
            }

            var usedNumbers = new HashSet<int>();

            slots.Sort((a, b) => options[a].Count.CompareTo(options[b].Count));
            options.Sort((a, b) => a.Count.CompareTo(b.Count));

            for(int i = 0; i < slots.Count; i++)
            {
                var optionsForSlot = options[i];
                var availableNumbers = optionsForSlot.Except(usedNumbers);

                if (availableNumbers.Any())
                {
                    var selectedNumber = availableNumbers.Max();

                    var actualIndex = selectedNumber < 0 ? selectedNumber + secondBar.Length : selectedNumber;
                    var actualBar = selectedNumber < 0 ? secondBar : bar;
                    var actualBarUI = selectedNumber < 0 ? secondBarUI : barUI;

                    var barstuff = actualBar[actualIndex];
                    barstuff.ConnectSlot(info[slots[i]]);
                    actualBarUI.SlotStatesUpdate(actualIndex, barstuff.InUse, barstuff.BeingGrabbed);

                    usedNumbers.Add(selectedNumber);
                    didConnectAny = true;
                }

                else
                    __result = false;
            }

            return false;
        }
    }
}
