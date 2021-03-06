﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace MainPower.Adms.Enricher
{
    public class IdfRegulator : IdfElement
    {
        private const string IdfRegulatorSymbol = "Symbol 7";
        private const string RegulatorDefaultType = "transformerType_regulator_default";
        private const string IdfTransformerType = "transformerType";

        private const string AdmsRegConnectionType = "ConnectionType";
        private const string AdmsRegName = "Name";
        private const string AdmsRegDesiredVoltage = "DesiredVoltage";
        private const string AdmsRegMaxTapLimit = "MaxTapLimit";
        private const string AdmsRegMinTapLimit = "MinTapLimit";
        private const string AdmsRegRatedAmps = "RatedAmps";
        private const string AdmsRegRegulationType = "RegulationType";
        private const string AdmsRegMaxVolts = "RegMaxVolts";
        private const string AdmsRegMinVolts = "RegMinVolts";
        private const string AdmsRegPosGradient = "RegPosGradient";
        private const string AdmsRegNegGradient = "RegNegGradient";
        private const string AdmsRegDeltaOpenPhase = "DeltaOpenPhase";
        private const string AdmsRegScadaId = "ScadaId";
        private const string AdmsRegBandwidth = "Bandwidth";
        private const string AdmsRegTxType = "TransformerType";
        private const string AdmsRegFirstHouseLimit = "FirstHouseLimit";
        private const string AdmsRegTapSide = "TapSide";

        private const string IdfRegFirstHouseLimit = "firstHouseLimit";
        private const string IdfRegConnectionType = "connectionType";
        private const string IdfRegBidirectional = "bidirectional";
        private const string IdfRegControlPhase = "controlPhase";
        private const string IdfRegDeltaOpenPhase = "deltaOpenPhase";
        private const string IdfRegDesiredVoltage = "desiredVoltage";
        private const string IdfRegRatedAmps = "ratedAmps";
        private const string IdfRegMaxTapControlLimit = "maxTapControlLimit";
        private const string IdfRegMinTapControlLimit = "minTapControlLimit";
        private const string IdfRegRegulatedNode = "regulatedNode";
        private const string IdfRegRegulationType = "regulationType";
        private const string IdfRegBandwidth = "bandwidth";
        private const string IdfRegTapSide = "tapSide";

        private const string IdfRegMaxVolts = "RegMaxVolts";
        private const string IdfRegMinVolts = "RegMinVolts";
        private const string IdfRegPosGradient = "RegPosGradient";
        private const string IdfRegNegGradient = "RegNegGradient";



        public IdfRegulator(XElement node, IdfGroup processor) : base(node, processor) { }


        public override void Process()
        {
            try
            {
                string scadaId = "";
                bool openDelta = false;

                Node.SetAttributeValue(IdfDeviceRatedkV, Node.Attribute(IdfDeviceBasekV).Value);
                Node.SetAttributeValue(IdfTransformerType, RegulatorDefaultType);
                Program.Enricher.Model.AddDevice(this, ParentGroup.Id, DeviceType.Regulator, IdfRegulatorSymbol);

                DataType asset = DataManager.I.RequestRecordById<AdmsRegulator>(Name);
                if (asset != null)
                {
                    if (!string.IsNullOrWhiteSpace(asset[AdmsRegConnectionType]))
                    {
                        Node.SetAttributeValue(IdfRegConnectionType, asset[AdmsRegConnectionType]);
                        if (asset[AdmsRegConnectionType] == "Delta open")
                        {
                            openDelta = true;
                            if (!string.IsNullOrWhiteSpace(asset[AdmsRegDeltaOpenPhase]))
                                Node.SetAttributeValue(IdfRegDeltaOpenPhase, asset[AdmsRegDeltaOpenPhase]);
                        }
                    }
                        
                    if (!string.IsNullOrWhiteSpace(asset[AdmsRegDesiredVoltage]))
                        Node.SetAttributeValue(IdfRegDesiredVoltage, asset[AdmsRegDesiredVoltage]);
                    if (!string.IsNullOrWhiteSpace(asset[AdmsRegMaxTapLimit]))
                        Node.SetAttributeValue(IdfRegMaxTapControlLimit, asset[AdmsRegMaxTapLimit]);
                    if (!string.IsNullOrWhiteSpace(asset[AdmsRegMinTapLimit]))
                        Node.SetAttributeValue(IdfRegMinTapControlLimit, asset[AdmsRegMinTapLimit]);
                    if (!string.IsNullOrWhiteSpace(asset[AdmsRegRatedAmps]))
                        Node.SetAttributeValue(IdfRegRatedAmps, asset[AdmsRegRatedAmps]);
                    if (!string.IsNullOrWhiteSpace(asset[AdmsRegRegulationType]))
                        Node.SetAttributeValue(IdfRegRegulationType, asset[AdmsRegRegulationType]);
                    if (!string.IsNullOrWhiteSpace(asset[AdmsRegMaxVolts]))
                        Node.SetAttributeValue(IdfRegMaxVolts, asset[AdmsRegMaxVolts]);
                    if (!string.IsNullOrWhiteSpace(asset[AdmsRegMinVolts]))
                        Node.SetAttributeValue(IdfRegMinVolts, asset[AdmsRegMinVolts]);
                    if (!string.IsNullOrWhiteSpace(asset[AdmsRegPosGradient]))
                        Node.SetAttributeValue(IdfRegPosGradient, asset[AdmsRegPosGradient]);
                    if (!string.IsNullOrWhiteSpace(asset[AdmsRegNegGradient]))
                        Node.SetAttributeValue(IdfRegNegGradient, asset[AdmsRegNegGradient]);
                    if (!string.IsNullOrWhiteSpace(asset[AdmsRegBandwidth]))
                        Node.SetAttributeValue(IdfRegBandwidth, asset[AdmsRegBandwidth]);
                    if (!string.IsNullOrWhiteSpace(asset[AdmsRegTxType]))
                        Node.SetAttributeValue(IdfTransformerType, asset[AdmsRegTxType]);
                    if (!string.IsNullOrWhiteSpace(asset[AdmsRegFirstHouseLimit]))
                        Node.SetAttributeValue(IdfRegFirstHouseLimit, asset[AdmsRegFirstHouseLimit]);
                    if (!string.IsNullOrWhiteSpace(asset[AdmsRegTapSide]))
                        Node.SetAttributeValue(IdfRegTapSide, asset[AdmsRegTapSide]);

                    //TODO: this should probably go in the database
                    Node.SetAttributeValue(IdfRegControlPhase, "1");
                    
                    scadaId = asset[AdmsRegScadaId];
                    if (!string.IsNullOrWhiteSpace(scadaId))
                        GenerateSCADALinking(scadaId, openDelta, asset[AdmsRegTapSide]);
                }
                else
                {
                    Err("Regulator was not in the ADMS database");
                }

                RemoveExtraAttributes();
            }
            catch (Exception ex)
            {
                Fatal($"Uncaught exception: {ex.Message}");
            }
        }

        private void RemoveExtraAttributes()
        {
            Node.SetAttributeValue(GisT1Asset, null);
        }


        private void GenerateSCADALinking(string scadaId, bool openDelta, string tapSide)
        {

            //Only support for open delta and wye configurations at the moment
            //for open delta, we assume that RegR is red to yellow phase, and RegB is yellow to blue phase, and that blue to red is open
            //if open delta is false, we assume that there is RegR, RegB, RegY on their respective phases to ground

            string us = tapSide == "1" ? "2" : "1";
            string ds = us == "1" ? "2" : "1";

            XElement x = new XElement("element");
            x.SetAttributeValue("type", "SCADA");
            x.SetAttributeValue("id", Id);

            x.SetAttributeValue("controlAllowState", "0");

            string refVoltage;

            if (openDelta)
            {
                refVoltage = Node.Attribute(IdfDeviceBasekV)?.Value;
            }
            else
            {
                refVoltage = Node.Attribute(IdfDeviceBasekV)?.Value;
                //refVoltage = (double.Parse(Node.Attribute(IdfDeviceBasekV)?.Value) / Math.Sqrt(3)).ToString("F3");
            }

            x.SetAttributeValue("s1VoltageReference", refVoltage);
            x.SetAttributeValue("s2VoltageReference", refVoltage);
            x.SetAttributeValue("controlVoltageReference", refVoltage);

            var tap = DataManager.I.RequestRecordByColumn<OsiScadaAnalog>(ScadaName, $"{scadaId} RegR Tap Position");
            if (tap != null)
                x.SetAttributeValue("p1TapPosition", tap.Key);
            var volts = DataManager.I.RequestRecordByColumn<OsiScadaAnalog>(ScadaName, $"{scadaId} RegR Volts (Source-L)");
            if (volts != null)
                x.SetAttributeValue($"s{us}p1KV", volts.Key);
            volts = DataManager.I.RequestRecordByColumn<OsiScadaAnalog>(ScadaName, $"{scadaId} RegR Volts (Load-L)");
            if (volts != null)
                x.SetAttributeValue($"s{ds}p1KV", volts.Key);
            var amps = DataManager.I.RequestRecordByColumn<OsiScadaAnalog>(ScadaName, $"{scadaId} RegR Amps");
            if (amps != null)
                x.SetAttributeValue($"s{us}p1Amps", volts.Key);



            //TODO: I don't think we actually need supervisory points here, just assume remote all the time
            /*
            var supervisory = DataManager.I.RequestRecordByColumn<OsiScadaStatus>(ScadaName, $"{scadaId} RegR Supervisory");
            if (supervisory != null)
                x.SetAttributeValue("p1RemoteLocalPoint", supervisory.Key);
            */
            x.SetAttributeValue("p1RemoteLocalPoint", "");

            if (openDelta)
            {
                x.SetAttributeValue("s1VoltageReference", Node.Attribute(IdfDeviceBasekV)?.Value);
                x.SetAttributeValue("s2VoltageReference", Node.Attribute(IdfDeviceBasekV)?.Value);


                tap = DataManager.I.RequestRecordByColumn<OsiScadaAnalog>(ScadaName, $"{scadaId} RegB Tap Position");
                if (tap != null)
                    x.SetAttributeValue("p2TapPosition", tap.Key);
                volts = DataManager.I.RequestRecordByColumn<OsiScadaAnalog>(ScadaName, $"{scadaId} RegB Volts (Source-L)");
                if (volts != null)
                    x.SetAttributeValue($"s{us}p2KV", volts.Key);
                volts = DataManager.I.RequestRecordByColumn<OsiScadaAnalog>(ScadaName, $"{scadaId} RegB Volts (Load-L)");
                if (volts != null)
                    x.SetAttributeValue($"s{ds}p2KV", volts.Key);

                amps = DataManager.I.RequestRecordByColumn<OsiScadaAnalog>(ScadaName, $"{scadaId} RegB Amps");
                if (amps != null)
                    x.SetAttributeValue($"s{us}p2Amps", volts.Key);
                /*
                 * TODO
                supervisory = DataManager.I.RequestRecordByColumn<OsiScadaStatus>(ScadaName, $"{scadaId} RegB Supervisory");
                if (supervisory != null)
                    x.SetAttributeValue("p2RemoteLocalPoint", supervisory.Key);
                */
                x.SetAttributeValue("p2RemoteLocalPoint", "");

                //in case we change from open delta to wye (seems unlikely) set the other parameters to null
                x.SetAttributeValue("p3TapPosition", "");
                x.SetAttributeValue($"s{us}p3KV", "");
                x.SetAttributeValue($"s{ds}p3KV", "");
                x.SetAttributeValue("p3RemoteLocalPoint", "");
            }
            else
            {
                tap = DataManager.I.RequestRecordByColumn<OsiScadaAnalog>(ScadaName, $"{scadaId} RegY Tap Position");
                if (tap != null)
                    x.SetAttributeValue("p2TapPosition", tap.Key);
                volts = DataManager.I.RequestRecordByColumn<OsiScadaAnalog>(ScadaName, $"{scadaId} RegY Volts (Source-L)");
                if (volts != null)
                    x.SetAttributeValue($"s{us}p2KV", volts.Key);
                volts = DataManager.I.RequestRecordByColumn<OsiScadaAnalog>(ScadaName, $"{scadaId} RegY Volts (Load-L)");
                if (volts != null)
                    x.SetAttributeValue($"s{ds}p2KV", volts.Key);

                amps = DataManager.I.RequestRecordByColumn<OsiScadaAnalog>(ScadaName, $"{scadaId} RegY Amps");
                if (amps != null)
                    x.SetAttributeValue($"s{us}p2Amps", volts.Key);

                /*
                 * TODO
                supervisory = DataManager.I.RequestRecordByColumn<OsiScadaStatus>(ScadaName, $"{scadaId} RegY Supervisory");
                if (supervisory != null)
                    x.SetAttributeValue("p2RemoteLocalPoint", supervisory.Key);
                */
                x.SetAttributeValue("p2RemoteLocalPoint", "");

                tap = DataManager.I.RequestRecordByColumn<OsiScadaAnalog>(ScadaName, $"{scadaId} RegB Tap Position");
                if (tap != null)
                    x.SetAttributeValue("p3TapPosition", tap.Key);
                volts = DataManager.I.RequestRecordByColumn<OsiScadaAnalog>(ScadaName, $"{scadaId} RegB Volts (Source-L)");
                if (volts != null)
                    x.SetAttributeValue($"s{us}p3KV", volts.Key);
                volts = DataManager.I.RequestRecordByColumn<OsiScadaAnalog>(ScadaName, $"{scadaId} RegB Volts (Load-L)");
                if (volts != null)
                    x.SetAttributeValue($"s{ds}p3KV", volts.Key);

                amps = DataManager.I.RequestRecordByColumn<OsiScadaAnalog>(ScadaName, $"{scadaId} RegB Amps");
                if (amps != null)
                    x.SetAttributeValue($"s{us}p3Amps", volts.Key);

                /*
                 * TODO
                supervisory = DataManager.I.RequestRecordByColumn<OsiScadaStatus>(ScadaName, $"{scadaId} RegB Supervisory");
                if (supervisory != null)
                    x.SetAttributeValue("p3RemoteLocalPoint", supervisory.Key);
                */
                x.SetAttributeValue("p3RemoteLocalPoint", "");
            }

            ParentGroup.AddGroupElement(x);
        }
    }
}
