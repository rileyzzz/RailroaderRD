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

namespace RailroaderRD;

public class RailroaderRD : PluginBase, IUpdateHandler, IModTabHandler
{
    ILogger logger = Log.ForContext<RailroaderRD>();
    RaildriverInterface raildriver = null;

    public static RailroaderRD Instance { get; set; }

    static RailroaderRD()
    {
        Log.Information("Hello! Static Constructor was called!");
    }

    public RailroaderRD(IModdingContext moddingContext, IModDefinition self)
    {
        logger.Information("Hello! Constructor was called for {modId}/{modVersion}!", self.Id, self.Version);

        moddingContext.RegisterConsoleCommand(new EchoCommand());
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

            }
        }
    }

    public void ModTabDidOpen(UIPanelBuilder builder)
    {
        logger.Information("Daytime!");


        builder.AddSection("Calibration");

        builder.AddLabel("Reverser");
        builder.HStack(delegate (UIPanelBuilder hstack) {
            hstack.AddButtonCompact("F", () => { raildriver.CalibrationData.ReverserMin = raildriver.RawReverser; });
            hstack.AddButtonCompact("N", () => { raildriver.CalibrationData.ReverserCenter = raildriver.RawReverser; });
            hstack.AddButtonCompact("R", () => { raildriver.CalibrationData.ReverserMax = raildriver.RawReverser; });
        });
        builder.AddSlider(() => raildriver.Reverser, () => "", (x) => { }, -1.0f, 1.0f);

        builder.AddLabel("Dynamic Brake/Throttle");
        builder.HStack(delegate (UIPanelBuilder hstack) {
            hstack.AddButtonCompact("Brk", () => { raildriver.CalibrationData.ThrottleMin = raildriver.RawThrottle; });
            hstack.AddButtonCompact("0", () => { raildriver.CalibrationData.ThrottleCenter = raildriver.RawThrottle; });
            hstack.AddButtonCompact("Throttle", () => { raildriver.CalibrationData.ThrottleMax = raildriver.RawThrottle; });
        });
        builder.AddSlider(() => raildriver.Throttle, () => "", (x) => { }, -1.0f, 1.0f);

        builder.AddLabel("Auto Brake");
        builder.HStack(delegate (UIPanelBuilder hstack) {
            hstack.AddButtonCompact("Emg", () => { raildriver.CalibrationData.AutoBrakeMin = raildriver.RawAutoBrake; });
            hstack.AddButtonCompact("Full", () => { raildriver.CalibrationData.AutoBrakeEmg = raildriver.RawAutoBrake; });
            hstack.AddButtonCompact("Release", () => { raildriver.CalibrationData.AutoBrakeMax = raildriver.RawAutoBrake; });
        });
        builder.AddSlider(() => raildriver.AutoBrake, () => "", (x) => { }, 0.0f, 1.0f);

        builder.AddLabel("Ind Brake");
        builder.HStack(delegate (UIPanelBuilder hstack) {
            hstack.AddButtonCompact("Full", () => { raildriver.CalibrationData.IndBrakeMin = raildriver.RawIndBrake; });
            hstack.AddButtonCompact("Release", () => { raildriver.CalibrationData.IndBrakeMax = raildriver.RawIndBrake; });
        });
        builder.AddSlider(() => raildriver.IndBrake, () => "", (x) => { }, 0.0f, 1.0f);

        builder.AddLabel("Bail Off");
        builder.HStack(delegate (UIPanelBuilder hstack) {
            hstack.AddButtonCompact("0", () => { raildriver.CalibrationData.BailOffMin = raildriver.RawBailOff; });
            hstack.AddButtonCompact("1", () => { raildriver.CalibrationData.BailOffMax = raildriver.RawBailOff; });
        });
        builder.AddSlider(() => raildriver.BailOff, () => "", (x) => { }, 0.0f, 1.0f);
    }

    public void ModTabDidClose()
    {
        logger.Information("Nighttime...");
    }
}

