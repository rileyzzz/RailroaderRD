//using System;
//using Game.Messages;
//using Game.State;
//using HarmonyLib;
//using Model;
//using RollingStock;
//using RollingStock.Controls;
//using UI;
//using UnityEngine;
//using Serilog;

//namespace RailroaderRD;

//[HarmonyPatch(typeof(TrainInput))]
//[HarmonyPatch("Update")]
//public class TrainInput_Update_Patch
//{
//    static AccessTools.FieldRef<TrainInput, TrainController> _trainControllerRef = AccessTools.FieldRefAccess<TrainInput, TrainController>("_trainController");

//    static void Prefix(TrainInput __instance)
//    {
//        // _trainControllerRef(__instance) = true;
//        Log.Information("input patch!");
//    }
//}