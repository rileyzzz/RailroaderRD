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

[HarmonyPatch(typeof(CharacterCameraController))]
[HarmonyPatch("UpdateWithInput")]
public class CharacterCameraController_UpdateWithInput_Patch
{
    static AccessTools.FieldRef<CharacterCameraController, float> _targetPitchRef = AccessTools.FieldRefAccess<CharacterCameraController, float>("_targetPitch");
    static AccessTools.FieldRef<CharacterCameraController, float> _targetYawRef = AccessTools.FieldRefAccess<CharacterCameraController, float>("_targetYaw");
    //static AccessTools.FieldRef<PlayerController, bool> _isSelectedRef = AccessTools.FieldRefAccess<PlayerController, bool>("_isSelected");

    static void Prefix(CharacterCameraController __instance)
    {
        float pitch = _targetPitchRef(__instance);
        float yaw = _targetYawRef(__instance);

        ButtonMask buttons = RailroaderRD.Instance.Buttons;

        const float cameraSpeed = 0.5f;

        if (buttons.HasFlag(ButtonMask.DPadDown))
            pitch += cameraSpeed;
        if (buttons.HasFlag(ButtonMask.DPadUp))
            pitch -= cameraSpeed;
        if (buttons.HasFlag(ButtonMask.DPadLeft))
            yaw -= cameraSpeed;
        if (buttons.HasFlag(ButtonMask.DPadRight))
            yaw += cameraSpeed;

        _targetPitchRef(__instance) = pitch;
        _targetYawRef(__instance) = yaw;
    }
}