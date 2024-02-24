using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using PIEHid64Net;
using Serilog;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Utilities;

namespace RailroaderRD;

[StructLayout(LayoutKind.Explicit, Size = 32)]
public struct PIEState : IInputStateTypeInfo
{
    public FourCC format => new FourCC('R', 'D', 'R', 'V');

    [InputControl(layout = "Axis"), FieldOffset(0)]
    public byte Reverser;
    [InputControl(layout = "Axis"), FieldOffset(1)]
    public byte Throttle;
    [InputControl(layout = "Axis"), FieldOffset(2)]
    public byte AutoBrake;
    [InputControl(layout = "Axis"), FieldOffset(3)]
    public byte IndBrake;
    [InputControl(layout = "Axis"), FieldOffset(4)]
    public byte BailOff;
    [InputControl(layout = "Axis"), FieldOffset(5)]
    public byte Wiper;
    [InputControl(layout = "Axis"), FieldOffset(6)]
    public byte Lights;

    [FieldOffset(8),
    InputControl(name = "RD Button 0", layout = "Button", bit = 0),
    InputControl(name = "RD Button 1", layout = "Button", bit = 1),
    InputControl(name = "RD Button 2", layout = "Button", bit = 2),
    InputControl(name = "RD Button 3", layout = "Button", bit = 3),
    InputControl(name = "RD Button 4", layout = "Button", bit = 4),
    InputControl(name = "RD Button 5", layout = "Button", bit = 5),
    InputControl(name = "RD Button 6", layout = "Button", bit = 6),
    InputControl(name = "RD Button 7", layout = "Button", bit = 7),
    InputControl(name = "RD Button 8", layout = "Button", bit = 8),
    InputControl(name = "RD Button 9", layout = "Button", bit = 9),
    InputControl(name = "RD Button 10", layout = "Button", bit = 10),
    InputControl(name = "RD Button 11", layout = "Button", bit = 11),
    InputControl(name = "RD Button 12", layout = "Button", bit = 12),
    InputControl(name = "RD Button 13", layout = "Button", bit = 13),
    InputControl(name = "RD Button 14", layout = "Button", bit = 14),
    InputControl(name = "RD Button 15", layout = "Button", bit = 15),
    InputControl(name = "RD Button 16", layout = "Button", bit = 16),
    InputControl(name = "RD Button 17", layout = "Button", bit = 17),
    InputControl(name = "RD Button 18", layout = "Button", bit = 18),
    InputControl(name = "RD Button 19", layout = "Button", bit = 19),
    InputControl(name = "RD Button 20", layout = "Button", bit = 20),
    InputControl(name = "RD Button 21", layout = "Button", bit = 21),
    InputControl(name = "RD Button 22", layout = "Button", bit = 22),
    InputControl(name = "RD Button 23", layout = "Button", bit = 23),
    InputControl(name = "RD Button 24", layout = "Button", bit = 24),
    InputControl(name = "RD Button 25", layout = "Button", bit = 25),
    InputControl(name = "RD Button 26", layout = "Button", bit = 26),
    InputControl(name = "RD Button 27", layout = "Button", bit = 27),

    InputControl(name = "RD Up",            layout = "Button", bit = 28),
    InputControl(name = "RD Down",          layout = "Button", bit = 29),
    InputControl(name = "RD DPad Up",       layout = "Button", bit = 30),
    InputControl(name = "RD DPad Right",    layout = "Button", bit = 31),
    InputControl(name = "RD DPad Down",     layout = "Button", bit = 32),
    InputControl(name = "RD DPad Left",     layout = "Button", bit = 33),
    InputControl(name = "RD Range Up",      layout = "Button", bit = 34),
    InputControl(name = "RD Range Down",    layout = "Button", bit = 35),
    InputControl(name = "RD EStop Up",      layout = "Button", bit = 36),
    InputControl(name = "RD EStop Down",    layout = "Button", bit = 37),
    InputControl(name = "RD Alert",         layout = "Button", bit = 38),
    InputControl(name = "RD Sand",          layout = "Button", bit = 39),
    InputControl(name = "RD Pantograph",    layout = "Button", bit = 40),
    InputControl(name = "RD Bell",          layout = "Button", bit = 41),
    InputControl(name = "RD Horn Up",       layout = "Button", bit = 42),
    InputControl(name = "RD Horn Down",     layout = "Button", bit = 43),
        ]
    public ButtonMask Buttons;
}

[Flags]
public enum ButtonMask : ulong
{
    /*
    Buttons0    = 0x0000000000FF,
    Buttons1    = 0x00000000FF00,
    Buttons2    = 0x000000FF0000,
    Buttons3    = 0x00000F000000,
    */
    Buttons     = 0x00000FFFFFFF,
    NumButtonBits = 28,
    Up          = 0x000010000000,
    Down        = 0x000020000000,
    DPadUp      = 0x000040000000,
    DPadRight   = 0x000080000000,
    DPadDown    = 0x000100000000,
    DPadLeft    = 0x000200000000,
    RangeUp     = 0x000400000000,
    RangeDown   = 0x000800000000,
    EStopUp     = 0x001000000000,
    EStopDown   = 0x002000000000,
    Alert       = 0x004000000000,
    Sand        = 0x008000000000,
    Pantograph  = 0x010000000000,
    Bell        = 0x020000000000,
    WhistleUp   = 0x040000000000,
    WhistleDown = 0x080000000000
}

internal class CalibrationData
{
    public byte ReverserMin = 0x00;
    public byte ReverserCenter = 0x7f;
    public byte ReverserMax = 0xff;

    public byte ThrottleMin = 0x00;
    public byte ThrottleCenter = 0x7f;
    public byte ThrottleMax = 0xff;

    public byte AutoBrakeMin = 0x00;
    public byte AutoBrakeEmg = 0x10;
    public byte AutoBrakeMax = 0xff;

    public byte IndBrakeMin = 0x00;
    public byte IndBrakeMax = 0xff;

    public byte BailOffMin = 0x00;
    public byte BailOffMax = 0xff;

    public byte WiperMin = 0x00;
    public byte WiperMax = 0xff;

    public byte LightsMin = 0x00;
    public byte LightsCenter = 0x7f;
    public byte LightsMax = 0xff;

    public CalibrationData()
    {
    }
}

[InputControlLayout(displayName = "RailDriver", stateType = typeof(PIEState))]
internal class RaildriverInterface : InputDevice, PIEDataHandler, PIEErrorHandler
{
    private new PIEDevice device;

    public bool Connected => device != null;
    public long Pid => device?.Pid ?? 0;


    PIEState deviceState;
    public CalibrationData CalibrationData => RDConfig.Current.CalibrationData;
    static RaildriverInterface()
    {
        // RegisterLayout() adds a "Control layout" to the system.
        // These can be layouts for individual Controls (like sticks)
        // or layouts for entire Devices (which are themselves
        // Controls) like in our case.
        InputSystem.RegisterLayout<RaildriverInterface>();
    }

    public byte RawReverser => deviceState.Reverser;
    public float Reverser
    {
        get
        {
            if (CalibrationData.ReverserMin == CalibrationData.ReverserCenter || CalibrationData.ReverserCenter == CalibrationData.ReverserMax)
                return 0.0f;

            if (RawReverser < CalibrationData.ReverserCenter)
            {
                return ((float)(RawReverser - CalibrationData.ReverserMin) / (float)(CalibrationData.ReverserCenter - CalibrationData.ReverserMin)) - 1.0f;
            }
            else
            {
                return ((float)(RawReverser - CalibrationData.ReverserCenter) / (float)(CalibrationData.ReverserMax - CalibrationData.ReverserCenter));
            }
        }
    }

    public byte RawThrottle => deviceState.Throttle;
    public float Throttle
    {
        get
        {
            if (CalibrationData.ThrottleMin == CalibrationData.ThrottleCenter || CalibrationData.ThrottleCenter == CalibrationData.ThrottleMax)
                return 0.0f;

            if (RawThrottle < CalibrationData.ThrottleCenter)
            {
                return ((float)(RawThrottle - CalibrationData.ThrottleMin) / (float)(CalibrationData.ThrottleCenter - CalibrationData.ThrottleMin)) - 1.0f;
            }
            else
            {
                return ((float)(RawThrottle - CalibrationData.ThrottleCenter) / (float)(CalibrationData.ThrottleMax - CalibrationData.ThrottleCenter));
            }
        }
    }

    public byte RawAutoBrake => deviceState.AutoBrake;

    public float AutoBrake
    {
        get
        {
            if (CalibrationData.AutoBrakeEmg == CalibrationData.AutoBrakeMax)
                return 0.0f;

            return ((float)(RawAutoBrake - CalibrationData.AutoBrakeEmg) / (float)(CalibrationData.AutoBrakeMax - CalibrationData.AutoBrakeEmg));
        }
    }

    public bool EmergencyBrake => RawAutoBrake < CalibrationData.AutoBrakeEmg;

    public byte RawIndBrake => deviceState.IndBrake;
    public float IndBrake
    {
        get
        {
            if (CalibrationData.IndBrakeMin == CalibrationData.IndBrakeMax)
                return 0.0f;

            return ((float)(RawIndBrake - CalibrationData.IndBrakeMin) / (float)(CalibrationData.IndBrakeMax - CalibrationData.IndBrakeMin));
        }
    }

    public byte RawBailOff => deviceState.BailOff;
    public float BailOff
    {
        get
        {
            if (CalibrationData.BailOffMin == CalibrationData.BailOffMax)
                return 0.0f;

            return ((float)(RawBailOff - CalibrationData.BailOffMin) / (float)(CalibrationData.BailOffMax - CalibrationData.BailOffMin));
        }
    }

    public byte RawWipers => deviceState.Wiper;
    public float Wipers
    {
        get
        {
            if (CalibrationData.WiperMin == CalibrationData.WiperMax)
                return 0.0f;

            return ((float)(RawWipers - CalibrationData.WiperMin) / (float)(CalibrationData.WiperMax - CalibrationData.WiperMin));
        }
    }

    public byte RawLights => deviceState.Lights;
    public float Lights
    {
        get
        {
            if (CalibrationData.LightsMin == CalibrationData.LightsCenter || CalibrationData.LightsCenter == CalibrationData.LightsMax)
                return 0.0f;

            if (RawLights < CalibrationData.LightsCenter)
            {
                return ((float)(RawLights - CalibrationData.LightsMin) / (float)(CalibrationData.LightsCenter - CalibrationData.LightsMin)) * 0.5f;
            }
            else
            {
                return ((float)(RawLights - CalibrationData.LightsCenter) / (float)(CalibrationData.LightsMax - CalibrationData.LightsCenter)) * 0.5f + 0.5f;
            }
        }
    }

    public ButtonMask Buttons => deviceState.Buttons;


    byte[] wData = null;

    public RaildriverInterface()
    {
    }

    public void Connect()
    {
        var cbotodevice = new int[128];
        Log.Information($"Enumerating RD devices.");
        var devices = PIEDevice.EnumeratePIE();
        Log.Information($"Found {devices.Length} devices.");
        if (devices.Length == 0)
        {
            Log.Information("No RailDriver devices found.");
        }
        else
        {
            //System.Media.SystemSounds.Beep.Play(); 
            int cbocount = 0; //keeps track of how many valid devices were added to the CboDevice box
            for (int i = 0; i < devices.Length; i++)
            {
                //information about device
                //PID = devices[i].Pid);
                //HID Usage = devices[i].HidUsage);
                //HID Usage Page = devices[i].HidUsagePage);
                //HID Version = devices[i].Version);
                if (devices[i].HidUsagePage == 0xc)
                {
                    switch (devices[i].Pid)
                    {
                        case 210:
                            // CboDevices.Items.Add("RailDriver (" + devices[i].Pid + "), ID: " + i);
                            cbotodevice[cbocount] = i;
                            cbocount++;
                            break;

                        default:
                            // CboDevices.Items.Add("Unknown Device (" + devices[i].Pid + "), ID: " + i);
                            cbotodevice[cbocount] = i;
                            cbocount++;
                            break;
                    }
                }
            }
        }
        if (devices.Length > 0)
        {
            device = devices[cbotodevice[0]];
            device.SetupInterface();
            wData = new byte[device.WriteLength];

            SetupCallback();
        }
    }

    private void SetupCallback()
    {
        device.SetErrorCallback(this);
        device.SetDataCallback(this);
        device.callNever = false;
    }

    public void Disconnect()
    {
        device?.CloseInterface();
        device = null;
    }


    private byte SevenSegment(char c)
    {
        //   1
        // 6   2
        //   7
        // 5   3
        //   4   8

        switch (c)
        {
            case '0': return 0b00111111;
            case '1': return 0b00000110;
            case '2': return 0b01011011;
            case '3': return 0b01001111;
            case '4': return 0b01100110;
            case '5': return 0b01101101;
            case '6': return 0b01111101;
            case '7': return 0b00000111;
            case '8': return 0b01111111;
            case '9': return 0b01101111;
            default: return 0;
        }
    }


    byte[] buf = new byte[3];
    public void UpdateVelocityDisplay(float velocity)
    {
        if (device == null)
            return;

        // m/s to mph
        string v = (velocity * 2.23694f).ToString("0.0").PadLeft(3);
        for (int j = 0; j < device.WriteLength; j++)
        {
            wData[j] = 0;
        }

        int nBuf = 0;
        int iStr = 0;
        while (nBuf < 3 && iStr < v.Length)
        {
            char c = v[iStr++];
            byte data = SevenSegment(c);
            if (nBuf != 2 && iStr < v.Length && v[iStr] == '.')
            {
                iStr++;
                data |= 0x80;
            }

            buf[nBuf++] = data;
        }

        int o = 1;
        wData[o++] = 134;

        for (int j = nBuf - 1; j >= 0; --j)
        {
            wData[o++] = buf[j];
        }



        //wData[2] = (byte)SevenSegment(v[0]);
        //wData[3] = (byte)SevenSegment(v[1]);
        //wData[4] = (byte)SevenSegment(v[2]);

        int result = 404;
        while (result == 404) { result = device.WriteData(wData); }
    }

    void PIEDataHandler.HandlePIEHidData(Byte[] data, PIEDevice sourceDevice, int error)
    {
        if (sourceDevice == device)
        {
            deviceState = new PIEState() {
                Reverser = data[1],
                Throttle = data[2],
                AutoBrake = data[3],
                IndBrake = data[4],
                BailOff = data[5],
                Wiper = data[6],
                Lights = data[7],
            };

            uint buttons0 = BitConverter.ToUInt32(data, 8);
            uint buttons1 = BitConverter.ToUInt16(data, 12);

            deviceState.Buttons = (ButtonMask)(((ulong)buttons1 << 32) | (ulong)buttons0);

            // Seems like Unity tends to throw an exception here every once in a while.
            // Not sure if it's something i've done wrong or just a bug in 2021.3.
            try
            {
                InputSystem.QueueStateEvent(this, deviceState);
            }
            catch (Exception e)
            {
                Log.Error($"RD queue event error! {e.Message}");
            }

            // Stack trace for context:
            //
            // [01:53:39.68 ERR Unity.InputSystem] An uncaught exception occured.
            // System.ArgumentNullException: Value cannot be null.
            // Parameter name: destination
            //   at (wrapper managed-to-native) Unity.Collections.LowLevel.Unsafe.UnsafeUtility.MemSet(void*,byte,long)
            //   at Unity.Collections.LowLevel.Unsafe.UnsafeUtility.MemClear (System.Void* destination, System.Int64 size) [0x00001] in <ac95ed96621641249c5b2cf74e341b4b>:0 
            //   at Unity.Collections.NativeArray`1[T]..ctor (System.Int32 length, Unity.Collections.Allocator allocator, Unity.Collections.NativeArrayOptions options) [0x00026] in <ac95ed96621641249c5b2cf74e341b4b>:0 
            //   at UnityEngine.InputSystem.LowLevel.InputEventBuffer.AllocateEvent (System.Int32 sizeInBytes, System.Int32 capacityIncrementInBytes, Unity.Collections.Allocator allocator) [0x0005f] in <92d7a21b943a423cb1579d8b653e65b5>:0 
            //   at UnityEngine.InputSystem.LowLevel.InputEventBuffer.AppendEvent (UnityEngine.InputSystem.LowLevel.InputEvent* eventPtr, System.Int32 capacityIncrementInBytes, Unity.Collections.Allocator allocator) [0x00017] in <92d7a21b943a423cb1579d8b653e65b5>:0 
            //   at UnityEngine.InputSystem.LowLevel.InputEventStream.Write (UnityEngine.InputSystem.LowLevel.InputEvent* eventPtr) [0x0005e] in <92d7a21b943a423cb1579d8b653e65b5>:0 
            //   at UnityEngine.InputSystem.InputManager.QueueEvent (UnityEngine.InputSystem.LowLevel.InputEvent* eventPtr) [0x0000d] in <92d7a21b943a423cb1579d8b653e65b5>:0 
            //   at UnityEngine.InputSystem.InputManager.QueueEvent[TEvent] (TEvent& inputEvent) [0x00007] in <92d7a21b943a423cb1579d8b653e65b5>:0 
            //   at UnityEngine.InputSystem.InputSystem.QueueStateEvent[TState] (UnityEngine.InputSystem.InputDevice device, TState state, System.Double time) [0x000f7] in <92d7a21b943a423cb1579d8b653e65b5>:0 
            //   at RailroaderRD.RaildriverInterface.PIEHid64Net.PIEDataHandler.HandlePIEHidData (System.Byte[] data, PIEHid64Net.PIEDevice sourceDevice, System.Int32 error) [0x0008b] in C:\Users\riley\Desktop\Code\projects\Railroader\RailroaderRD\src\RaildriverInterface.cs:438 
            //   at PIEHid64Net.PIEDevice.DataEventThread () [0x00079] in <a8866dfa32594b51a93b04420fce114f>:0 
            //   at System.Threading.ThreadHelper.ThreadStart_Context (System.Object state) [0x00014] in <7e05db41a20b45108859fa03b97088d4>:0 
            //   at System.Threading.ExecutionContext.RunInternal (System.Threading.ExecutionContext executionContext, System.Threading.ContextCallback callback, System.Object state, System.Boolean preserveSyncCtx) [0x00071] in <7e05db41a20b45108859fa03b97088d4>:0 
            //   at System.Threading.ExecutionContext.Run (System.Threading.ExecutionContext executionContext, System.Threading.ContextCallback callback, System.Object state, System.Boolean preserveSyncCtx) [0x00000] in <7e05db41a20b45108859fa03b97088d4>:0 
            //   at System.Threading.ExecutionContext.Run (System.Threading.ExecutionContext executionContext, System.Threading.ContextCallback callback, System.Object state) [0x0002b] in <7e05db41a20b45108859fa03b97088d4>:0 
            //   at System.Threading.ThreadHelper.ThreadStart () [0x00008] in <7e05db41a20b45108859fa03b97088d4>:0 
        }
    }

    public PIEState GetDeviceState()
    {
        return deviceState;
    }

    void PIEErrorHandler.HandlePIEHidError(PIEDevice sourceDevice, long error)
    {
        Log.Error($"RailDriver Error: {error.ToString()}");
    }
}
