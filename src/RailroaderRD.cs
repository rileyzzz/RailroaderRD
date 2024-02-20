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
using UnityEngine;
using Effects;

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

    private int HeadlightState = 0;
    private int WiperState = 0;
    public void Update()
    {
        // logger.Verbose("UPDATE()");
        if (raildriver != null)
        {
            var controller = TrainController.Shared;
            if (controller != null && controller.SelectedLocomotive is BaseLocomotive loco)
            {
                loco.ControlHelper.Reverser = -raildriver.Reverser;
                loco.ControlHelper.Throttle = raildriver.Throttle;
                loco.ControlHelper.LocomotiveBrake = 1.0f - raildriver.AutoBrake;
                loco.ControlHelper.TrainBrake = 1.0f - raildriver.IndBrake;
                
                if (raildriver.BailOff > 0.7f)
                    loco.ControlHelper.BailOff();

                int headlight = (int)Math.Round(raildriver.Lights * 2.0f);
                if (HeadlightState != headlight)
                {
                    HeadlightState = headlight;
                    int bits = HeadlightStateLogic.IntFromStates((HeadlightController.State)headlight, (HeadlightController.State)headlight);
                    
                    ChangeValue(loco, PropertyChange.Control.Headlight, bits);
                }
            }
        }
    }

    public void ModTabDidOpen(UIPanelBuilder builder)
    {
        logger.Information("Daytime!");


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
    }

    public void ModTabDidClose()
    {
        //  SaveConfig();
    }
}

