using Character;
using Model;
using Railloader;
using Serilog;
using System;
using System.Collections;
using System.Threading;
using UI;
using UI.Builder;
using HarmonyLib;
using Game.Messages;
using Game.State;
//using UnityEngine;
using Effects;
using KeyValue.Runtime;

namespace RailroaderRD;

public class RailroaderRD : PluginBase, IUpdateHandler, IModTabHandler
{
    private const string SettingsID = "RailroaderRD";

    ILogger logger = Log.ForContext<RailroaderRD>();
    RaildriverInterface raildriver = null;
    IModdingContext modContext;

    public static RailroaderRD Instance { get; set; }

    static RailroaderRD()
    {
        Log.Information("Hello! Static Constructor was called!");
    }

    public RailroaderRD(IModdingContext moddingContext, IModDefinition self)
    {
        var harmony = new Harmony("com.rileyzzz.railroaderRD");
        harmony.PatchAll();

        modContext = moddingContext;
        LoadConfig();

        moddingContext.RegisterConsoleCommand(new EchoCommand());
    }

    public void LoadConfig()
    {
        RDConfig.Current = modContext.LoadSettingsData<RDConfig>(SettingsID);
        if (RDConfig.Current == null)
            RDConfig.Current = new();
    }

    public void SaveConfig()
    {
        modContext.SaveSettingsData<RDConfig>(SettingsID, RDConfig.Current);
    }

    public override void OnEnable()
    {
        logger.Information("OnEnable() was called!");

        Instance = this;

        raildriver = new RaildriverInterface();
        raildriver.Connect();
    }

    public override void OnDisable()
    {
        raildriver?.Disconnect();
        raildriver = null;

        Instance = null;
    }

    private void ChangeValue(BaseLocomotive loco, PropertyChange.Control control, int value)
    {
        StateManager.ApplyLocal(new PropertyChange(loco.id, control, value));
    }

    private Value GetValue(BaseLocomotive loco, PropertyChange.Control control)
    {
        return loco.KeyValueObject[PropertyChange.KeyForControl(control)];
    }

    private ButtonMask prevMask = 0;

    public ButtonMask Buttons => raildriver?.Buttons ?? 0;
    public void Update()
    {
        // logger.Verbose("UPDATE()");
        if (raildriver != null && raildriver.Connected)
        {
            var controller = TrainController.Shared;
            if (controller != null && controller.SelectedLocomotive is BaseLocomotive loco)
            {
                raildriver.UpdateVelocityDisplay(Math.Abs(loco.velocity));

                loco.ControlHelper.Reverser = -raildriver.Reverser;
                loco.ControlHelper.Throttle = raildriver.Throttle;
                loco.ControlHelper.TrainBrake = 1.0f - raildriver.AutoBrake;
                loco.ControlHelper.LocomotiveBrake = 1.0f - raildriver.IndBrake;
                
                if (raildriver.BailOff > 0.7f)
                    loco.ControlHelper.BailOff();

                int headlight = (int)Math.Round(raildriver.Lights * 2.0f);
                int bits = HeadlightStateLogic.IntFromStates((HeadlightController.State)headlight, (HeadlightController.State)headlight);
                if (bits != GetValue(loco, PropertyChange.Control.Headlight).IntValue)
                {
                    ChangeValue(loco, PropertyChange.Control.Headlight, bits);
                }

                ButtonMask downMask = raildriver.Buttons;
                ButtonMask pressed = downMask & (downMask ^ prevMask);

                if (pressed.HasFlag(ButtonMask.Bell))
                {
                    loco.ControlHelper.Bell = !loco.ControlHelper.Bell;
                }

                float whistle = 0.0f;
                if (downMask.HasFlag(ButtonMask.WhistleUp))
                    whistle += 1.0f;
                if (downMask.HasFlag(ButtonMask.WhistleDown))
                    whistle += 0.5f;

                loco.ControlHelper.Horn = whistle;

                prevMask = downMask;
            }
            else
            {
                raildriver.UpdateVelocityDisplay(0);
            }
        }
    }

    public void ModTabDidOpen(UIPanelBuilder builder)
    {
        if (raildriver == null || !raildriver.Connected)
        {
            builder.AddLabel("No RailDriver connected.");
            builder.AddButton("Connect", () => { raildriver?.Connect(); builder.Rebuild(); });
            return;
        }

        builder.AddLabel($"RailDriver {raildriver.Pid} connected.");
        builder.AddButton("Disconnect", () => { raildriver?.Disconnect(); builder.Rebuild(); });

        builder.AddSection("Calibration");

        builder.AddLabel("Reverser");
        builder.HStack(delegate (UIPanelBuilder hstack) {
            hstack.AddButtonCompact("F", () => { raildriver.CalibrationData.ReverserMin = raildriver.RawReverser; SaveConfig(); });
            hstack.AddButtonCompact("N", () => { raildriver.CalibrationData.ReverserCenter = raildriver.RawReverser; SaveConfig(); });
            hstack.AddButtonCompact("R", () => { raildriver.CalibrationData.ReverserMax = raildriver.RawReverser; SaveConfig(); });
        });
        builder.AddSlider(() => raildriver.Reverser, () => "", (x) => { }, -1.0f, 1.0f);

        builder.AddLabel("Dynamic Brake/Throttle");
        builder.HStack(delegate (UIPanelBuilder hstack) {
            hstack.AddButtonCompact("Brk", () => { raildriver.CalibrationData.ThrottleMin = raildriver.RawThrottle; SaveConfig(); });
            hstack.AddButtonCompact("0", () => { raildriver.CalibrationData.ThrottleCenter = raildriver.RawThrottle; SaveConfig(); });
            hstack.AddButtonCompact("Throttle", () => { raildriver.CalibrationData.ThrottleMax = raildriver.RawThrottle; SaveConfig(); });
        });
        builder.AddSlider(() => raildriver.Throttle, () => "", (x) => { }, -1.0f, 1.0f);

        builder.AddLabel("Auto Brake");
        builder.HStack(delegate (UIPanelBuilder hstack) {
            hstack.AddButtonCompact("Emg", () => { raildriver.CalibrationData.AutoBrakeMin = raildriver.RawAutoBrake; SaveConfig(); });
            hstack.AddButtonCompact("Full", () => { raildriver.CalibrationData.AutoBrakeEmg = raildriver.RawAutoBrake; SaveConfig(); });
            hstack.AddButtonCompact("Release", () => { raildriver.CalibrationData.AutoBrakeMax = raildriver.RawAutoBrake; SaveConfig(); });
        });
        builder.AddSlider(() => raildriver.AutoBrake, () => "", (x) => { }, 0.0f, 1.0f);

        builder.AddLabel("Ind Brake");
        builder.HStack(delegate (UIPanelBuilder hstack) {
            hstack.AddButtonCompact("Full", () => { raildriver.CalibrationData.IndBrakeMin = raildriver.RawIndBrake; SaveConfig(); });
            hstack.AddButtonCompact("Release", () => { raildriver.CalibrationData.IndBrakeMax = raildriver.RawIndBrake; SaveConfig(); });
        });
        builder.AddSlider(() => raildriver.IndBrake, () => "", (x) => { }, 0.0f, 1.0f);

        builder.AddLabel("Bail Off");
        builder.HStack(delegate (UIPanelBuilder hstack) {
            hstack.AddButtonCompact("0", () => { raildriver.CalibrationData.BailOffMin = raildriver.RawBailOff; SaveConfig(); });
            hstack.AddButtonCompact("1", () => { raildriver.CalibrationData.BailOffMax = raildriver.RawBailOff; SaveConfig(); });
        });
        builder.AddSlider(() => raildriver.BailOff, () => "", (x) => { }, 0.0f, 1.0f);

        builder.AddLabel("Lights");
        builder.HStack(delegate (UIPanelBuilder hstack) {
            hstack.AddButtonCompact("Off", () => { raildriver.CalibrationData.LightsMin = raildriver.RawLights; SaveConfig(); });
            hstack.AddButtonCompact("Dim", () => { raildriver.CalibrationData.LightsCenter = raildriver.RawLights; SaveConfig(); });
            hstack.AddButtonCompact("Full", () => { raildriver.CalibrationData.LightsMax = raildriver.RawLights; SaveConfig(); });
        });
        builder.AddSlider(() => raildriver.Lights, () => "", (x) => { }, 0.0f, 1.0f);


    }

    public void ModTabDidClose()
    {
        //  SaveConfig();
    }
}

