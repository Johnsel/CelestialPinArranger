﻿using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Linq;
using SymbolBuilder.Model;

namespace SymbolBuilder.Readers
{
    internal class TiMux
    {
        public string DevicePinId { get; set; }
        public List<TiPeripheralPin> PeripheralPins { get; set; }
    }
    internal class TiPeripheralPin
    {
        public string PeripheralPinId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }

        public string InterfacePinId { get; set; }
        public string InterfaceName { get; set; }
        public string InterfaceDescription { get; set; }
    }

    public class TiSysConfigPinReader : PinDataReader
    {
        public override string Name => "TI SysConfig";

        public override string Filter => "Ti SysConfig (*.json)|*.json";

        public override string FileType => "*.json";

        public override bool CanRead(string fileName)
        {
            return File.Exists(fileName) && Path.GetExtension(fileName) == ".json";
        }

        public override List<SymbolDefinition> LoadFromStream(Stream stream, string fileName)
        {
            var ret = new List<SymbolDefinition>();

            var json = JsonDocument.Parse(stream);

            List<TiMux> muxes = new List<TiMux>();

            foreach (var jsonMux in json.RootElement.GetProperty("muxes").EnumerateArray())
            {
                var mux = new TiMux()
                {
                    DevicePinId = jsonMux.GetProperty("devicePinID").GetString(),
                    PeripheralPins = new List<TiPeripheralPin>()
                };

                foreach (var muxSetting in jsonMux.GetProperty("muxSetting").EnumerateArray())
                {
                    TiPeripheralPin pin = new TiPeripheralPin()
                    {
                        PeripheralPinId = muxSetting.GetProperty("peripheralPinID").GetString()
                    };

                    var jsonPp = json.RootElement.GetProperty("peripheralPins").GetProperty(pin.PeripheralPinId);
                    pin.Name = jsonPp.GetProperty("name").GetString();
                    pin.Description = jsonPp.GetProperty("description").GetString();
                    
                    pin.InterfacePinId = jsonPp.GetProperty("interfacePinID").GetString();
                    var ifPin = json.RootElement.GetProperty("interfacePins").GetProperty(pin.InterfacePinId);
                    pin.InterfaceName = ifPin.GetProperty("name").GetString();
                    pin.InterfaceDescription = ifPin.GetProperty("description").GetString();

                    mux.PeripheralPins.Add(pin);
                }

                muxes.Add(mux);
            }

            foreach (var part in json.RootElement.GetProperty("parts").EnumerateObject())
            {
                string partName = part.Value.GetProperty("name").GetString();
                var packageId = part.Value.GetProperty("packageIDWrapper")[0].GetProperty("packageID").GetString();

                var package = json.RootElement.GetProperty("packages").GetProperty(packageId);

                var packageName = package.GetProperty("name").GetString();
                var pins = package.GetProperty("packagePin");
                if (pins.ValueKind != JsonValueKind.Array)
                    continue;

                SymbolDefinition pack = new SymbolDefinition(partName, "TI", packageName);

                /*
                foreach (var peripheralId in part.Value.GetProperty("peripheralWrapper").EnumerateArray())
                {
                    var peripheral = json.RootElement.GetProperty("peripherals").GetProperty(peripheralId.GetString());
                    var pName = peripheral.GetProperty("name").GetString();
                    var intId = peripheral.GetProperty("interfaceID").GetString();

                    var iface = json.RootElement.GetProperty("interfaces").GetProperty(intId);
                    string ifaceName = iface.GetProperty("name").GetString();

                }
                */
                foreach (var pin in pins.EnumerateArray())
                {
                    var pinId = pin.GetProperty("devicePinID").GetString();
                    var ball = pin.GetProperty("ball").GetString();

                    var pinName = json.RootElement.GetProperty("devicePins").GetProperty(pinId).GetProperty("name").GetString();

                    List<PinSignal> altFunctions = new List<PinSignal>();

                    var muxPin = muxes.FirstOrDefault(m => m.DevicePinId == pinId);
                    if (muxPin != null)
                    {
                        altFunctions.AddRange(muxPin.PeripheralPins.Select(p => new PinSignal(p.Name)));
                    }

                    pack.SymbolBlocks.FirstOrDefault().Pins.Add(new PinDefinition(ball, pinName) { AlternativeSignals = altFunctions });
                }

                pack.CheckPinNames();
                ret.Add(pack);
            }

            return ret;
        }
    }
}
