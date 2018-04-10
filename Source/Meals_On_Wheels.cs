using System.Reflection;
using Verse;
using UnityEngine;
using Harmony;
using RimWorld;

namespace Meals_On_Wheels
{
	public class Mod : Verse.Mod
	{
		public Mod(ModContentPack content) : base(content)
		{
			// initialize settings
			//GetSettings<Settings>();
#if DEBUG
			HarmonyInstance.DEBUG = true;
#endif
			HarmonyInstance harmony = HarmonyInstance.Create("uuugggg.rimworld.Meals_On_Wheels.main");
			harmony.PatchAll(Assembly.GetExecutingAssembly());


			harmony.Patch(AccessTools.Method(typeof(JobDriver_FoodFeedPatient), "TryMakePreToilReservations"),
				new HarmonyMethod(typeof(Food_TryMakePreToilReservations_Patch), "Prefix"), null);
			harmony.Patch(AccessTools.Method(typeof(JobDriver_Ingest), "TryMakePreToilReservations"),
				new HarmonyMethod(typeof(Food_TryMakePreToilReservations_Patch), "Prefix"), null);
			harmony.Patch(AccessTools.Method(typeof(JobDriver_FoodDeliver), "TryMakePreToilReservations"),
				new HarmonyMethod(typeof(Food_TryMakePreToilReservations_Patch), "Prefix"), null);

		}

		//public override void DoSettingsWindowContents(Rect inRect)
		//{
		//	base.DoSettingsWindowContents(inRect);
		//	GetSettings<Settings>().DoWindowContents(inRect);
		//}

		//public override string SettingsCategory()
		//{
		//	return "TD.MealsOnWheels".Translate();
		//}
	}
}