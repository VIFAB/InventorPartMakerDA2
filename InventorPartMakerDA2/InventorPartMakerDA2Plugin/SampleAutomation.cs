/////////////////////////////////////////////////////////////////////
// Copyright (c) Autodesk, Inc. All rights reserved
// Written by Forge Partner Development
//
// Permission to use, copy, modify, and distribute this software in
// object code form for any purpose and without fee is hereby granted,
// provided that the above copyright notice appears in all copies and
// that both that copyright notice and the limited warranty and
// restricted rights notice below appear in all supporting
// documentation.
//
// AUTODESK PROVIDES THIS PROGRAM "AS IS" AND WITH ALL FAULTS.
// AUTODESK SPECIFICALLY DISCLAIMS ANY IMPLIED WARRANTY OF
// MERCHANTABILITY OR FITNESS FOR A PARTICULAR USE.  AUTODESK, INC.
// DOES NOT WARRANT THAT THE OPERATION OF THE PROGRAM WILL BE
// UNINTERRUPTED OR ERROR FREE.
/////////////////////////////////////////////////////////////////////

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

using Inventor;
using Autodesk.Forge.DesignAutomation.Inventor.Utils;
using System.Xml;
using System.IO;

namespace InventorPartMakerDA2Plugin
{
    [ComVisible(true)]
    public class SampleAutomation
    {
        private readonly InventorServer inventorApplication;

        public SampleAutomation(InventorServer inventorApp)
        {
            inventorApplication = inventorApp;
        }

        public void Run(Document doc)
        {
            LogTrace("Run called");
            RunWithArguments(doc, null);
        }

        public void RunWithArguments(Document doc, NameValueMap map)
        {
            //LogTrace("Processing " + doc.FullFileName);

            //try
            //{
            //    if (doc.DocumentType == DocumentTypeEnum.kPartDocumentObject)
            //    {
            //        using (new HeartBeat())
            //        {
            //            // TODO: handle the Inventor part here
            //        }
            //    }
            //    else if (doc.DocumentType == DocumentTypeEnum.kAssemblyDocumentObject) // Assembly.
            //    {
            //        using (new HeartBeat())
            //        {
            //            // TODO: handle the Inventor assembly here
            //        }
            //    }
            //}
            //catch (Exception e)
            //{
            //    LogError("Processing failed. " + e.ToString());
            //}
            LogTrace("Initialiting");
            PartDocument oPartDoc = (PartDocument)inventorApplication.Documents.Add(DocumentTypeEnum.kPartDocumentObject, inventorApplication.FileManager.GetTemplateFile(DocumentTypeEnum.kPartDocumentObject), true);
            LogTrace("Part template opened");
            TransientGeometry oTG = inventorApplication.TransientGeometry;
            PartComponentDefinition oPartComDef = oPartDoc.ComponentDefinition;
            UserParameters oParams = oPartComDef.Parameters.UserParameters;
            oParams.AddByExpression("width", "50", UnitsTypeEnum.kMillimeterLengthUnits);
            oParams.AddByExpression("height", "10", UnitsTypeEnum.kMillimeterLengthUnits);
            oParams.AddByExpression("length", "10", UnitsTypeEnum.kMillimeterLengthUnits);
            LogTrace("Standard parameters created");


            Point2d[] oPoints = new Point2d[4];


            oPoints[0] = oTG.CreatePoint2d(0, 0);
            oPoints[1] = oTG.CreatePoint2d(0, oPartComDef.Parameters.GetValueFromExpression("width", UnitsTypeEnum.kMillimeterLengthUnits));
            oPoints[2] = oTG.CreatePoint2d(oPartComDef.Parameters.GetValueFromExpression("height", UnitsTypeEnum.kMillimeterLengthUnits), 0);
            oPoints[3] = oTG.CreatePoint2d(oPartComDef.Parameters.GetValueFromExpression("height", UnitsTypeEnum.kMillimeterLengthUnits), oPartComDef.Parameters.GetValueFromExpression("width", UnitsTypeEnum.kMillimeterLengthUnits));
            LogTrace("Inventor points created");
            LogTrace("Initiating sketch creation...");
            PlanarSketch oSketch = oPartComDef.Sketches.Add(oPartComDef.WorkPlanes[1]);
            SketchPoint[] osPoints = new SketchPoint[4];
            SketchLine[] oLines = new SketchLine[4];

            osPoints[0] = oSketch.SketchPoints.Add(oPoints[0]);
            osPoints[1] = oSketch.SketchPoints.Add(oPoints[1]);
            osPoints[2] = oSketch.SketchPoints.Add(oPoints[2]);
            osPoints[3] = oSketch.SketchPoints.Add(oPoints[3]);

            oLines[0] = oSketch.SketchLines.AddByTwoPoints(osPoints[0], osPoints[1]);
            oLines[1] = oSketch.SketchLines.AddByTwoPoints(oLines[0].EndSketchPoint, osPoints[3]);
            oLines[2] = oSketch.SketchLines.AddByTwoPoints(oLines[1].EndSketchPoint, osPoints[2]);
            oLines[3] = oSketch.SketchLines.AddByTwoPoints(oLines[2].EndSketchPoint, oLines[0].StartSketchPoint);
            LogTrace("Sketch created, adding dimensions");
            oSketch.DimensionConstraints.AddTwoPointDistance(osPoints[0], osPoints[1], DimensionOrientationEnum.kAlignedDim, oPoints[1]); //d0//
            oSketch.DimensionConstraints.AddTwoPointDistance(osPoints[1], osPoints[3], DimensionOrientationEnum.kAlignedDim, oPoints[3]); //d1//
            LogTrace("Dimensions added to the sketch, changing standard values for User parameter values");

            var inventorMParams = oPartComDef.Parameters.ModelParameters;
            foreach (ModelParameter mParam in inventorMParams)
            {
                if (mParam.Name.Contains("d0"))
                {
                    mParam.Expression = "width";
                }
                else if (mParam.Name.Contains("d1"))
                {
                    mParam.Expression = "height";
                }

            }

            LogTrace("Dimensions with user parameter values created, starting extrude operation");

            //oSketch.DimensionConstraints.AddTwoPointDistance(oPoints[0], oPoints[1], DimensionOrientationEnum.kAlignedDim, "width", true);
            Profile oProfile = oSketch.Profiles.AddForSolid();

            ExtrudeDefinition oExtrudeDef = oPartComDef.Features.ExtrudeFeatures.CreateExtrudeDefinition(oProfile, PartFeatureOperationEnum.kJoinOperation);
            oExtrudeDef.SetDistanceExtent(oPartComDef.Parameters.UserParameters.AddByExpression("length", "length", UnitsTypeEnum.kMillimeterLengthUnits), PartFeatureExtentDirectionEnum.kPositiveExtentDirection);

            ExtrudeFeature oExtrude = oPartComDef.Features.ExtrudeFeatures.Add(oExtrudeDef);
            //oExtrude.FeatureDimensions[1].Parameter.Name = "length";
            LogTrace("Extrude operation finished");
            XmlDocument xmlDoc = new XmlDocument();
            string currentDir = System.IO.Directory.GetCurrentDirectory();
            string projectDir = Directory.GetParent(currentDir).Parent.FullName;
            LogTrace("Reading XML input file from " + projectDir);
            xmlDoc.Load(System.IO.Path.Combine(projectDir, "React-BIM-output.xml"));
            //xmlDoc.Load("C:\\webapps\\IpartCreator\\React-BIM-output.xml");
            XmlNodeList memberNodeList = xmlDoc.GetElementsByTagName("Member");
            XmlNodeList partNumberNodeList = xmlDoc.GetElementsByTagName("PartNumber");
            XmlNodeList widthNodeList = xmlDoc.GetElementsByTagName("width");
            XmlNodeList heightNodeList = xmlDoc.GetElementsByTagName("height");
            XmlNodeList lengthNodeList = xmlDoc.GetElementsByTagName("length");
            LogTrace("XML tag values imported");
            LogTrace("Creating Part");
            
            foreach(UserParameter oParam in oParams)
            {
                if (oParam.Name.Contains("width"))
                {
                    oParam.Expression = widthNodeList.;
                }
            }
            



          
            LogTrace("Excel created");
            oPartDoc.Update();
            string iPartPath = projectDir + "/results/bimipart.ipt";
            int iNumRows = oFactory.TableRows.Count;
            LogTrace("Saving iPart to " + iPartPath);
            oPartDoc.SaveAs(iPartPath, false);

            LogTrace("Opening new assembly template");
            AssemblyDocument oAssyDoc = (AssemblyDocument)inventorApplication.Documents.Add(DocumentTypeEnum.kAssemblyDocumentObject, inventorApplication.FileManager.GetTemplateFile(DocumentTypeEnum.kAssemblyDocumentObject), true);
            AssemblyComponentDefinition oAssyComDef = oAssyDoc.ComponentDefinition;
            ComponentOccurrences oAssyCompOccs = oAssyComDef.Occurrences;
            LogTrace("Creating Matrix");
            Matrix oPos = oTG.CreateMatrix();
            int oStep = 0;
            int iRow;
            LogTrace("Placing iPart members to assembly");
            for (iRow = 1; iRow <= iNumRows; iRow++)
            {
                oStep = oStep + 150;

                oPos.SetTranslation(oTG.CreateVector(oStep, oStep, 0), false);
                ComponentOccurrence oOcc = oAssyCompOccs.AddiPartMember(iPartPath, oPos, iRow);
            }
            string assyPath = projectDir + "/results/bimassy.iam";
            LogTrace("Saving Assembly file to " + assyPath);
            oAssyDoc.SaveAs(assyPath, false);

        }

        #region Logging utilities

        /// <summary>
        /// Log message with 'trace' log level.
        /// </summary>
        private static void LogTrace(string format, params object[] args)
        {
            Trace.TraceInformation(format, args);
        }

        /// <summary>
        /// Log message with 'trace' log level.
        /// </summary>
        private static void LogTrace(string message)
        {
            Trace.TraceInformation(message);
        }

        /// <summary>
        /// Log message with 'error' log level.
        /// </summary>
        private static void LogError(string format, params object[] args)
        {
            Trace.TraceError(format, args);
        }

        /// <summary>
        /// Log message with 'error' log level.
        /// </summary>
        private static void LogError(string message)
        {
            Trace.TraceError(message);
        }

        #endregion
    }
}