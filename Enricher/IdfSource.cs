using System;
using System.Diagnostics;
using System.Xml.Linq;

namespace MainPower.Adms.Enricher
{
    /// <summary>
    /// Represents an IDF Source element
    /// </summary>
    public class IdfSource : IdfElement
    {
        //constants used in the IDF
        private const string IdfSourcePosSeqX = "positiveSequenceReactance";
        private const string IdfSourcePosSeqR = "positiveSequenceResistance";
        private const string IdfSourceZeroSeqX = "zeroSequenceReactance";
        private const string IdfSourceZeroSeqR = "zeroSequenceResistance";
        private const string IdfSourcePhase1Angle = "phase1Angle";
        private const string IdfSourcePhase2Angle = "phase2Angle";
        private const string IdfSourcePhase3Angle = "phase3Angle";
        private const string IdfSourceVoltageTypeLL = "L-L";
       
        //constants used by the ADMS database
        private const string AdmsSourcePosSeqX = "PositiveSequenceReactance";
        private const string AdmsSourcePosSeqR = "PositiveSequenceResistance";
        private const string AdmsSourceZeroSeqX = "ZeroSequenceReactance";
        private const string AdmsSourceZeroSeqR = "ZeroSequenceResistance";
        private const string AdmsSourcePhase1Angle = "Phase1Angle";
        private const string AdmsSourcePhase2Angle = "Phase2Angle";
        private const string AdmsSourcePhase3Angle = "Phase3Angle";
        private const string AdmsSourcePhase1VoltageRef = "Phase1Voltage";
        private const string AdmsSourcePhase2VoltageRef = "Phase2Voltage";
        private const string AdmsSourcePhase3VoltageRef = "Phase3Voltage";

        public IdfSource(XElement node, IdfGroup processor) : base(node, processor) { }

        public override void Process()
        {
            try
            {
                var source = DataManager.I.RequestRecordById<AdmsSource>(Name);
                if (source == null)
                {
                    Warn("Source does not exist in ADMS db");
                }
                else
                {
                    string basekV = Node.Attribute(IdfDeviceBasekV).Value;

                    Node.SetAttributeValue(IdfSourcePosSeqX, source[AdmsSourcePosSeqX]);
                    Node.SetAttributeValue(IdfSourcePosSeqR, source[AdmsSourcePosSeqR]);
                    Node.SetAttributeValue(IdfSourceZeroSeqX, source[AdmsSourceZeroSeqX]);
                    Node.SetAttributeValue(IdfSourceZeroSeqR, source[AdmsSourceZeroSeqR]);
                    Node.SetAttributeValue(IdfSourcePhase1Angle, source[AdmsSourcePhase1Angle]);
                    Node.SetAttributeValue(IdfSourcePhase2Angle, source[AdmsSourcePhase2Angle]);
                    Node.SetAttributeValue(IdfSourcePhase3Angle, source[AdmsSourcePhase3Angle]);
                    Node.SetAttributeValue(IdfDeviceVoltageType, IdfSourceVoltageTypeLL);
                    
                    XElement x = new XElement(IdfEl);
                    x.SetAttributeValue(IdfElementType, IdfElementTypeScada);
                    x.SetAttributeValue(IdfElementId, Id);
                    x.SetAttributeValue(IdfDeviceVoltageReference, basekV);
                    
                    var p1Voltage = DataManager.I.RequestRecordByColumn<OsiScadaAnalog>(ScadaName, source[AdmsSourcePhase1VoltageRef],SearchMode.Exact);
                    if (p1Voltage != null)
                    {
                        x.SetAttributeValue(IdfDeviceP1kV, p1Voltage.Key);
                    }
                    var p2Voltage = DataManager.I.RequestRecordByColumn<OsiScadaAnalog>(ScadaName, source[AdmsSourcePhase2VoltageRef], SearchMode.Exact);
                    if (p2Voltage != null)
                    {
                        x.SetAttributeValue(IdfDeviceP2kV, p2Voltage.Key);
                    }
                    var p3Voltage = DataManager.I.RequestRecordByColumn<OsiScadaAnalog>(ScadaName, source[AdmsSourcePhase3VoltageRef], SearchMode.Exact);
                    if (p3Voltage != null)
                    {
                        x.SetAttributeValue(IdfDeviceP3kV, p3Voltage.Key);
                    }
                    ParentGroup.AddGroupElement(x);
                }
                Program.Enricher.Model.AddSource(Node, ParentGroup.Id);
            }
            catch (Exception ex)
            {
                Fatal($"Uncaught exception: {ex.Message}");
            }
        }
    }
}

