using System;
using System.Xml.Linq;

namespace MainPower.Osi.Enricher
{
    public class IdfSource : IdfElement
    {
        public IdfSource(XElement node, IdfGroup processor) : base(node, processor) { }

        public override void Process()
        {
            try
            {
                //TODO: this ought to be in a database somewhere
                switch (Name)
                {
                    case "SBK_52":
                    case "SBK_92":
                        Node.SetAttributeValue("positiveSequenceReactance", "9.8"); //4.9
                        Node.SetAttributeValue("positiveSequenceResistance", "5.6"); //2.8
                        Node.SetAttributeValue("zeroSequenceReactance", "36.2"); //18.1
                        Node.SetAttributeValue("zeroSequenceResistance", "5.2"); //2.6
                        Node.SetAttributeValue("phase1Angle", "0");
                        Node.SetAttributeValue("phase2Angle", "120");
                        Node.SetAttributeValue("phase3Angle", "-120");
                        break;
                    case "WPR_172":
                    case "WPR_92":
                        Node.SetAttributeValue("positiveSequenceReactance", "10.06");//5.030061
                        Node.SetAttributeValue("positiveSequenceResistance", "1.32");//0.659208
                        Node.SetAttributeValue("zeroSequenceReactance", "12.906348");//6.453174
                        Node.SetAttributeValue("zeroSequenceResistance", "1.17");//0.585
                        Node.SetAttributeValue("phase1Angle", "0");
                        Node.SetAttributeValue("phase2Angle", "120");
                        Node.SetAttributeValue("phase3Angle", "-120");
                        break;
                    case "CUL_1252":
                    case "CUL_1212":
                        Node.SetAttributeValue("positiveSequenceReactance", "5.89");//2.943421
                        Node.SetAttributeValue("positiveSequenceResistance", "0.391");//0.195689
                        Node.SetAttributeValue("zeroSequenceReactance", "4.348704");//2.174352
                        Node.SetAttributeValue("zeroSequenceResistance", "114.17");//57.08497
                        Node.SetAttributeValue("phase1Angle", "90");
                        Node.SetAttributeValue("phase2Angle", "-150");
                        Node.SetAttributeValue("phase3Angle", "-30");
                        break;
                }


            }
            catch (Exception ex)
            {
                Fatal($"Uncaught exception: {ex.Message}");
            }
        }
    }
}

