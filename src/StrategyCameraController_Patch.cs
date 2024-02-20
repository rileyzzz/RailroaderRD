using System;
using Game.Messages;
using Game.State;
using HarmonyLib;
using Model;
using RollingStock;
using RollingStock.Controls;
using UI;
using UnityEngine;
using Serilog;
using Character;
using Cameras;

namespace RailroaderRD;

[HarmonyPatch(typeof(StrategyCameraController))]
[HarmonyPatch("UpdateInput")]
public class StrategyCameraController_UpdateInput_Patch
{
    static AccessTools.FieldRef<StrategyCameraController, float> _distanceInputRef = AccessTools.FieldRefAccess<StrategyCameraController, float>("_distanceInput");
    static AccessTools.FieldRef<StrategyCameraController, float> _angleXInputRef = AccessTools.FieldRefAccess<StrategyCameraController, float>("_angleXInput");
    static AccessTools.FieldRef<StrategyCameraController, float> _angleYInputRef = AccessTools.FieldRefAccess<StrategyCameraController, float>("_angleYInput");

    static void Postfix(StrategyCameraController __instance)
    {
        //bool isSelected = _isSelectedRef(__instance);
        //if (!isSelected)
        //    return;

        float distance = _distanceInputRef(__instance);
        float angleX = _angleXInputRef(__instance);
        float angleY = _angleYInputRef(__instance);

        ButtonMask buttons = RailroaderRD.Instance.Buttons;

        const float zoomSpeed = 0.2f;
        const float cameraSpeed = 10.0f;

        if (buttons.HasFlag(ButtonMask.Up))
            distance -= zoomSpeed;
        if (buttons.HasFlag(ButtonMask.Down))
            distance += zoomSpeed;

        if (buttons.HasFlag(ButtonMask.DPadDown))
            angleX -= cameraSpeed;
        if (buttons.HasFlag(ButtonMask.DPadUp))
            angleX += cameraSpeed;
        if (buttons.HasFlag(ButtonMask.DPadLeft))
            angleY += cameraSpeed;
        if (buttons.HasFlag(ButtonMask.DPadRight))
            angleY -= cameraSpeed;

        _distanceInputRef(__instance) = distance;
        _angleXInputRef(__instance) = angleX;
        _angleYInputRef(__instance) = angleY;
    }
}