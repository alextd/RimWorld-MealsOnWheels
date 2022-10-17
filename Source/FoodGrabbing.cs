using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace Meals_On_Wheels
{
	[HarmonyPatch(typeof(FoodUtility),nameof(FoodUtility.TryFindBestFoodSourceFor))]
	class FoodGrabbing
	{
		//public static bool TryFindBestFoodSourceFor(Pawn getter, Pawn eater, bool desperate, out Thing foodSource, out ThingDef foodDef, bool canRefillDispenser = true, bool canUseInventory = true, bool canUsePackAnimalInventory = false, bool allowForbidden = false, bool allowCorpse = true, bool allowSociallyImproper = false, bool allowHarvest = false, bool forceScanWholeMap = false, bool ignoreReservations = false, bool calculateWantedStackCount = false, FoodPreferability minPrefOverride = FoodPreferability.Undefined)
		[HarmonyPriority(Priority.Low)]
		public static void Postfix(ref bool __result, Pawn getter, Pawn eater, ref Thing foodSource, ref ThingDef foodDef, bool canUseInventory, bool canUsePackAnimalInventory)
		{
			if (eater.IsFreeColonist && __result == false && canUseInventory && canUsePackAnimalInventory &&
				getter.RaceProps.ToolUser && getter.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation))
			{
				Log.Message($"There be no food for " + eater);
				List<Pawn> pawns = eater.Map.mapPawns.SpawnedPawnsInFaction(Faction.OfPlayer).FindAll(
					p => p != getter &&
					!p.Position.IsForbidden(getter) && 
					getter.CanReach(p, PathEndMode.OnCell, Danger.Some)
				);
				foreach (Pawn p in pawns)
				{
					Log.Message($"Food soon rotten on " + p + "?");
					Thing thing = FoodUtility.BestFoodInInventory(p, eater, FoodPreferability.MealAwful);
					if (thing != null && thing.TryGetComp<CompRottable>() is CompRottable compRottable &&
						compRottable != null && compRottable.Stage == RotStage.Fresh && compRottable.TicksUntilRotAtCurrentTemp < GenDate.TicksPerDay / 2)
					{
						Log.Message($"Food is " + thing);
						foodSource = thing;
						foodDef = FoodUtility.GetFinalIngestibleDef(foodSource, false);
						__result = true;
						return;
					}
				}
				foreach (Pawn p in pawns)
				{
					Log.Message($"Food on " + p + "?");
					Thing thing = FoodUtility.BestFoodInInventory(p, eater, FoodPreferability.DesperateOnly, FoodPreferability.MealLavish, 0f, !eater.IsTeetotaler());
					if (thing != null)
					{
						Log.Message($"Food is " + thing);
						foodSource = thing;
						foodDef = FoodUtility.GetFinalIngestibleDef(foodSource, false);
						__result = true;
						return;
					}
				}
			}
		}
	}
}
