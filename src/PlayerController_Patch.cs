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

namespace RailroaderRD;

[HarmonyPatch(typeof(PlayerController))]
[HarmonyPatch("HandleCameraInput")]
public class PlayerController_HandleCameraInput_Patch
{
    static AccessTools.FieldRef<PlayerController, float> _inputLookPitchRef = AccessTools.FieldRefAccess<PlayerController, float>("_inputLookPitch");
    static AccessTools.FieldRef<PlayerController, float> _inputLookYawRef = AccessTools.FieldRefAccess<PlayerController, float>("_inputLookYaw");
    static AccessTools.FieldRef<PlayerController, bool> _isSelectedRef = AccessTools.FieldRefAccess<PlayerController, bool>("_isSelected");

    static void Prefix(PlayerController __instance)
    {
        bool isSelected = _isSelectedRef(__instance);
        if (!isSelected)
            return;

        float pitch = _inputLookPitchRef(__instance);
        float yaw = _inputLookYawRef(__instance);

        ButtonMask buttons = RailroaderRD.Instance.Buttons;

        const float cameraSpeed = 0.5f;

        if (buttons.HasFlag(ButtonMask.DPadDown))
            pitch -= cameraSpeed;
        if (buttons.HasFlag(ButtonMask.DPadUp))
            pitch += cameraSpeed;
        if (buttons.HasFlag(ButtonMask.DPadLeft))
            yaw -= cameraSpeed;
        if (buttons.HasFlag(ButtonMask.DPadRight))
            yaw += cameraSpeed;

        _inputLookPitchRef(__instance) = pitch;
        _inputLookYawRef(__instance) = yaw;

        // _trainControllerRef(__instance) = true;
    }
}