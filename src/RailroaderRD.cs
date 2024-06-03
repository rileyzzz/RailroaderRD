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
using UnityEngine.InputSystem;
using static UnityEngine.InputSystem.InputActionRebindingExtensions;
using Model.AI;

namespace RailroaderRD;

public class RailroaderRD : PluginBase, IUpdateHandler, IModTabHandler
{
    private const string SettingsID = "RailroaderRD";

    ILogger logger = Log.ForContext<RailroaderRD>();
    RaildriverInterface raildriver = null;
    IModdingContext modContext;

    static AccessTools.FieldRef<AutoEngineerPlanner, Orders> _ordersRef = AccessTools.FieldRefAccess<AutoEngineerPlanner, Orders>("_orders");

    public static RailroaderRD Instance { get; set; }

    static RailroaderRD()
    {
    }

    public RailroaderRD(IModdingContext moddingContext, IModDefinition self)
    {
        var harmony = new Harmony("com.rileyzzz.railroaderRD");
        harmony.PatchAll();

        modContext = moddingContext;
        LoadConfig();

        // moddingContext.RegisterConsoleCommand(new EchoCommand());
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
        Instance = this;

        //raildriver = new RaildriverInterface();
        raildriver = InputSystem.AddDevice<RaildriverInterface>();

        if (RDConfig.Current.AutoConnectOnStart)
        {
            raildriver.Connect();
        }
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

                Orders orders = _ordersRef(controller.SelectedLocomotive.AutoEngineerPlanner);

                if (orders.Enabled == false)
                {
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
            }
            else
            {
                raildriver.UpdateVelocityDisplay(0);
            }
        }
    }

    private readonly UIState<string> _selectedTabState = new UIState<string>(null);

    public void ModTabDidOpen(UIPanelBuilder builder)
    {
        bool isConnected = raildriver != null && raildriver.Connected;

        if (isConnected)
        {
            builder.AddLabel($"RailDriver {raildriver.Pid} connected.");
            builder.AddButton("Disconnect", () => { raildriver?.Disconnect(); builder.Rebuild(); });
        }
        else
        {
            builder.AddLabel("No RailDriver connected.");
            builder.AddButton("Connect", () => { raildriver?.Connect(); builder.Rebuild(); });
        }

        builder.AddTabbedPanels(_selectedTabState, delegate (UITabbedPanelBuilder tabBuilder) {
            tabBuilder.AddTab("Options", "Options", OptionsMenu);
            if (isConnected)
            {
                tabBuilder.AddTab("Calibration", "Calibration", CalibrationMenu);
            }
        });
    }

    private void OptionsMenu(UIPanelBuilder builder)
    {
        builder.HStack(delegate (UIPanelBuilder hstack) {
            hstack.AddLabel("Auto Connect on Game Start");
            hstack.AddToggle(() => RDConfig.Current.AutoConnectOnStart, (value) => { RDConfig.Current.AutoConnectOnStart = value; SaveConfig(); });
        });
    }

    // TODO: Alternate control bind system?
    //private void ControlsMenu(UIPanelBuilder builder)
    //{
    //    var rebindableActions = GameInput.shared.RebindableActions;

    //    foreach (var actions in rebindableActions)
    //    {
    //        builder.AddSection(actions.title, delegate (UIPanelBuilder builder) {
    //            foreach (var action in actions.actions)
    //            {
    //                ButtonRebind(builder, action);
    //            }
    //        });
    //    }
    //}

    //private InputAction _bindingControl;
    //private RebindingOperation _rebindOp;

    //private void ButtonRebind(UIPanelBuilder builder, InputAction action)
    //{
    //    // var cfg = RDConfig.Current.ControlBindings;

    //    builder.HStack(delegate (UIPanelBuilder hstack) {
    //        string controlName = action.name;
    //        Guid controlId = action.id;
    //        hstack.AddLabel(controlName);

    //        string bound = "None";
    //        if (_bindingControl == action)
    //            bound = "<Waiting...>";

    //        //if (cfg.TryGetValue(controlId, out int value))
    //        //    bound = $"Button {value}";

    //        hstack.AddButton(bound, () => {
    //            if (_rebindOp != null)
    //                _rebindOp.Cancel();

    //            bool actionWasEnabled = action.enabled;
    //            if (actionWasEnabled)
    //            {
    //                action.Disable();
    //            }

    //            _bindingControl = action;
    //            _rebindOp = action.PerformInteractiveRebinding()
    //                // .WithBindingGroup("Gamepad")
    //                .WithControlsExcluding("<Mouse>/leftButton").WithControlsExcluding("<Mouse>/rightButton").WithControlsExcluding("<Mouse>/press")
    //                .WithCancelingThrough("<Keyboard>/escape")
    //                .OnCancel((x) => {
    //                    _bindingControl = null;
    //                    _rebindOp = null;
    //                    builder.Rebuild();
    //                })
    //                .OnComplete((x) => {
    //                    _bindingControl = null;
    //                    _rebindOp = null;
    //                    builder.Rebuild();
    //                });

    //            _rebindOp.Start();
    //            builder.Rebuild();
    //        });
    //    });
    //}

    private void CalibrationMenu(UIPanelBuilder builder)
    {
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

