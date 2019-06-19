using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Harmony;
using RimWorld;
using Verse;
using Verse.AI;

namespace Meals_On_Wheels
{
	[HarmonyPatch(typeof(FoodUtility))]
	[HarmonyPatch("TryFindBestFoodSourceFor")]
	class FoodGrabbing
	{
		[HarmonyPriority(Priority.Low)]
		public static void Postfix(ref bool __result, Pawn getter, Pawn eater, ref Thing foodSource, ref ThingDef foodDef, bool canUseInventory = true)
		{
			if (eater.IsFreeColonist && __result == false && canUseInventory &&
				getter.RaceProps.ToolUser && getter.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation))
			{
				Log.Message($"There be no food for " + eater);
				List<Pawn> pawns = eater.Map.mapPawns.SpawnedPawnsInFaction(Faction.OfPlayer).FindAll(
					p => p != getter &&
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


	[HarmonyPatch(typeof(JobDriver_FoodFeedPatient), "TryMakePreToilReservations")]
	static class Food_TryMakePreToilReservations_Patch3
	{
		public static bool Prefix(JobDriver __instance, ref bool __result)
		{
			return Food_TryMakePreToilReservations_Patch.Prefix(__instance, ref __result);
		}
	}
	[HarmonyPatch(typeof(JobDriver_Ingest), "TryMakePreToilReservations")]
	static class Food_TryMakePreToilReservations_Patch2
	{
		public static bool Prefix(JobDriver __instance, ref bool __result)
		{
			return Food_TryMakePreToilReservations_Patch.Prefix(__instance, ref __result);
		}
	}
	[HarmonyPatch(typeof(JobDriver_FoodDeliver), "TryMakePreToilReservations")]
	static class Food_TryMakePreToilReservations_Patch
	{
		public static bool Prefix(JobDriver __instance, ref bool __result)
		{
			Job job = __instance.job;
			Pawn getter = __instance.pawn;

			if (!getter.IsFreeColonist) return true;

			Thing ingestibleSource = job.targetA.Thing;
			Log.Message(getter + " TryMakePreToilReservations for job " + job + " with food " + ingestibleSource);

			if (ingestibleSource == null || ingestibleSource.holdingOwner == null) return true;
			if (getter.Faction == null || ingestibleSource is Building_NutrientPasteDispenser) return true;

			//Well apparently inventory items can be forbidden
			ingestibleSource.SetForbidden(false, false);

			//job.count is not set properly so here we go again:
			float nutrition = FoodUtility.GetNutrition(ingestibleSource, ingestibleSource.def);
			int dropCount = FoodUtility.WillIngestStackCountOf(getter, ingestibleSource.def, nutrition);
			dropCount = Math.Min(dropCount, ingestibleSource.stackCount);
			Thing droppedFood = null;
			if (ingestibleSource.holdingOwner.Owner is Pawn_InventoryTracker holder)
			{
				if (holder.pawn == getter) return true;
				Log.Message(holder.pawn + " dropping " + ingestibleSource);
				holder.innerContainer.TryDrop(ingestibleSource, ThingPlaceMode.Direct, dropCount, out droppedFood);
			}
			else if (ingestibleSource.holdingOwner.Owner is Pawn_CarryTracker carrier)
			{
				if (carrier.pawn == getter) return true;
				Log.Message(carrier.pawn + " dropping " + ingestibleSource);
				carrier.innerContainer.TryDrop(ingestibleSource, ThingPlaceMode.Direct, dropCount, out droppedFood);
			}
			else return true;

			if (droppedFood == null) return true;	//I don't think this should happen but if it does this won't work

			Log.Message(getter + " now getting " + droppedFood);
			job.targetA = droppedFood;
			if (droppedFood.IsForbidden(getter))
			{
				Log.Message(droppedFood + " is Forbidden, job will restart");
				//Whoops dropped onto forbidden / reserved stack
				__result = true;  //Job will fail on forbidden naturally
				return false;
			}
			if (!getter.CanReserve(droppedFood))
			{
				Verse.Log.Warning($"Food " + droppedFood + " for " + getter + " was dropped onto a reserved stack. Job will fail and try again, so ignore the error please.");
			}
			return true;
		}
	}
}
