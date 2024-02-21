using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PIEHid64Net;
using Serilog;

namespace RailroaderRD;

internal struct PIEState
{
    public byte Reverser;
    public byte Throttle;
    public byte AutoBrake;
    public byte IndBrake;
    public byte BailOff;
    public byte Wiper;
    public byte Lights;

    public ButtonMask Buttons;
    //Reverser = rdata[1]
    //Throttle = rdata[2]
    //AutoBrake = rdata[3]
    //Ind Brake = rdata[4]
    //Bail Off = rdata[5]
    //Wiper = rdata[6]
    //Lights = rdata[7]
    //buttons = rdata[8] to rdata[13]
}

[Flags]
public enum ButtonMask : ulong
{
    Buttons0    = 0x0000000000FF,
    Buttons1    = 0x00000000FF00,
    Buttons2    = 0x000000FF0000,
    Buttons3    = 0x00000F000000,
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


internal class RaildriverInterface : PIEDataHandler, PIEErrorHandler
{
    PIEDevice device;

    public bool Connected => device != null;
    public long Pid => device?.Pid ?? 0;


    PIEState deviceState;
    public CalibrationData CalibrationData => RDConfig.Current.CalibrationData;

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

    public void SaveConfig()
    {

    }

    public void LoadConfig()
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
        //check the sourceDevice and make sure it is the same device as selected in CboDevice   
        if (sourceDevice == device)
        {

            //write raw data to listbox1
            //String output = "Callback: " + sourceDevice.Pid + ", ID: " + selecteddevice.ToString() + ", data=";
            //for (int i = 0; i < sourceDevice.ReadLength; i++)
            //{
            //    output = output + data[i].ToString() + "  ";
            //}

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

            //Reverser = rdata[1]
            //Throttle = rdata[2]
            //AutoBrake = rdata[3]
            //Ind Brake = rdata[4]
            //Bail Off = rdata[5]
            //Wiper = rdata[6]
            //Lights = rdata[7]
            //buttons = rdata[8] to rdata[13]
            // this.SetListBox(output);
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
