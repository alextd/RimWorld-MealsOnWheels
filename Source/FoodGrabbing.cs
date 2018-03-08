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
		public static void Postfix(ref bool __result, Pawn getter, Pawn eater, ref Thing foodSource, ref ThingDef foodDef, bool canUseInventory = true)
		{

			if (__result == false && canUseInventory &&
				getter.RaceProps.ToolUser && getter.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation))
			{
				Log.Message("There be no food for " + eater);
				List<Pawn> pawns = eater.Map.mapPawns.SpawnedPawnsInFaction(Faction.OfPlayer).FindAll(p => p != getter);
				foreach (Pawn p in pawns)
				{
					Log.Message("Food soon rotten on " + p + "?");
					Thing thing = FoodUtility.BestFoodInInventory(p, null, FoodPreferability.MealAwful);
					if (thing != null && thing.TryGetComp<CompRottable>() is CompRottable compRottable &&
						compRottable != null && compRottable.Stage == RotStage.Fresh && compRottable.TicksUntilRotAtCurrentTemp < GenDate.TicksPerDay / 2)
					{
						Log.Message("Food is " + thing);
						foodSource = thing;
						foodDef = FoodUtility.GetFinalIngestibleDef(foodSource, false);
						__result = true;
					}
				}
				foreach (Pawn p in pawns)
				{
					Log.Message("Food on " + p + "?");
					Thing thing = FoodUtility.BestFoodInInventory(p, null, FoodPreferability.DesperateOnly, FoodPreferability.MealLavish, 0f, !eater.IsTeetotaler());
					if (thing != null)
					{
						Log.Message("Food is " + thing);
						foodSource = thing;
						foodDef = FoodUtility.GetFinalIngestibleDef(foodSource, false);
						__result = true;
					}
				}
			}
		}
	}


	[HarmonyPatch(typeof(JobDriver_Ingest))]
	[HarmonyPatch("TryMakePreToilReservations")]
	static class Ingest_TryMakePreToilReservations_Patch
	{
		public static bool Prefix(JobDriver_Ingest __instance, ref bool __result)
		{
			Job job = __instance.job;
			Pawn eater = __instance.pawn;
			Thing ingestibleSource = job.targetA.Thing;

			Log.Message(eater + " TryMakePreToilReservations " + ingestibleSource);

			if (ingestibleSource == null || ingestibleSource.holdingOwner == null) return true;

			//job.count is not set properly so here we go again:
			int dropCount = FoodUtility.WillIngestStackCountOf(eater, ingestibleSource.def);
			Thing droppedFood = null;
			if (ingestibleSource.holdingOwner.Owner is Pawn_InventoryTracker holder)
			{
				Log.Message(holder.pawn + " dropping " + ingestibleSource);
				holder.innerContainer.TryDrop(ingestibleSource, ThingPlaceMode.Direct, dropCount, out droppedFood);
			}
			else if (ingestibleSource.holdingOwner.Owner is Pawn_CarryTracker carrier)
			{
				Log.Message(carrier.pawn + " dropping " + ingestibleSource);
				carrier.innerContainer.TryDrop(ingestibleSource, ThingPlaceMode.Direct, dropCount, out droppedFood);
			}
			else return true;

			Log.Message(eater + " now eating with " + droppedFood);
			job.targetA = droppedFood;
			if (droppedFood.IsForbidden(eater))
			{
				Log.Message(droppedFood + " is Forbidden, job will restart");
				//Whoops dropped onto forbidden / reserved stack
				__result = true;  //Job will fail on forbidden naturally
				return false;
			}
			if (!eater.CanReserve(droppedFood))
			{
				Verse.Log.Warning("Food " + droppedFood + " for " + eater + " was dropped onto a reserved stack. Job will fail and try again, so ignore the error please.");
			}
			return true;
		}
	}


}
