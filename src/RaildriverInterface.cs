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

    //Reverser = rdata[1]
    //Throttle = rdata[2]
    //AutoBrake = rdata[3]
    //Ind Brake = rdata[4]
    //Bail Off = rdata[5]
    //Wiper = rdata[6]
    //Lights = rdata[7]
    //buttons = rdata[8] to rdata[13]
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
    public byte LightsMax = 0xff;

    public CalibrationData()
    {
    }
}


internal class RaildriverInterface : PIEDataHandler, PIEErrorHandler
{
    PIEDevice device;

    PIEState deviceState;
    public CalibrationData CalibrationData { get; private set; }

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


    byte[] wData = null;

    public RaildriverInterface()
    {
        // Load calibration data from JSON.
        CalibrationData = new();
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
