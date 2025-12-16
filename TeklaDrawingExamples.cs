// start ## ----------- Updated on 2024.04.04-------- By Hugo Velasco ##



#pragma warning disable 1633 // Unrecognized #pragma directive
#pragma reference "Tekla.Macros.Akit"
#pragma reference "Tekla.Macros.Wpf.Runtime"
#pragma reference "Tekla.Macros.Runtime"
#pragma warning restore 1633 // Unrecognized #pragma directive

namespace UserMacros {
}




using System;
using Tekla.Structures.Drawing;
using TSD = Tekla.Structures.Drawing;
using Tekla.Structures.Drawing.UI;
using Tekla.Structures.Model;
using TSM = Tekla.Structures.Model;

namespace FromDrawingToModel
{
    #region Drawing and Model Interaction Class

    public class DrawingModelHelper
    {
        private readonly DrawingHandler _drawingHandler;
        private readonly Model _model;

        public DrawingModelHelper()
        {
            _drawingHandler = new DrawingHandler();
            _model = new Model();
        }

        public bool Initialize()
        {
            return _model.GetConnectionStatus() && _drawingHandler.GetConnectionStatus();
        }

        public void PickObjectInDrawing()
        {
            try
            {
                Drawing drawing = _drawingHandler.GetActiveDrawing();
                if (drawing != null)
                {
                    DrawingObject pickedObject;
                    ViewBase pickedView;
                    Tekla.Structures.Geometry3d.Point pickedPoint;

                    // Pick a part in the model 
                    Picker myPicker = _drawingHandler.GetPicker();
                    myPicker.PickObject("Pick a model object in the drawing", out pickedObject, out pickedView, out pickedPoint);

                    GetInfoFromDrawing(pickedObject);
                }
                else
                {
                    Console.WriteLine("A drawing must be opened in Tekla Structures model!");
                }
            }
            catch (Exception exc)
            {
                Console.WriteLine(exc.ToString());
            }
        }

        private void GetInfoFromDrawing(DrawingObject pickedObject)
        {
            if (pickedObject != null)
            {
                TSD.ModelObject modelObjectInDrawing = pickedObject as TSD.ModelObject;
                if (modelObjectInDrawing != null)
                {
                    TSM.ModelObject modelObjectInModel = _model.SelectModelObject(modelObjectInDrawing.ModelIdentifier);
                    if (modelObjectInModel is TSM.Part)
                    {
                        Beam beam = modelObjectInModel as Beam;
                        if (beam != null)
                        {
                            GetBeamInfo(beam);
                        }
                        PolyBeam polyBeam = modelObjectInModel as PolyBeam;
                        if (polyBeam != null)
                        {
                            GetPolyBeamInfo(polyBeam);
                        }
                        ContourPlate contourPlate = modelObjectInModel as ContourPlate;
                        if (contourPlate != null)
                        {
                            GetContourPlateInfo(contourPlate);
                        }
                    }
                }
            }
        }

        #region Info Display Methods

        private void GetBeamInfo(Beam beam)
        {
            Console.WriteLine("Beam Info:" + Environment.NewLine +
                "Name: " + beam.Name + Environment.NewLine +
                "Id: " + beam.Identifier.ID.ToString() + Environment.NewLine +
                "Material: " + beam.Material.MaterialString + Environment.NewLine +
                "Profile: " + beam.Profile.ProfileString + Environment.NewLine +
                "Start point: " + beam.StartPoint.ToString() + Environment.NewLine +
                "End point: " + beam.EndPoint.ToString() + Environment.NewLine +
                "Class: " + beam.Class + Environment.NewLine +
                "Finish: " + beam.Finish + Environment.NewLine +
                "Position depth: " + beam.Position.Depth.ToString() + Environment.NewLine +
                "Position plane: " + beam.Position.Plane.ToString() + Environment.NewLine +
                "Position rotation: " + beam.Position.Rotation.ToString() + Environment.NewLine +
                "Father assembly: " + beam.GetAssembly().Name.ToString());
        }

        private void GetPolyBeamInfo(PolyBeam polyBeam)
        {
            Console.WriteLine("PolyBeam Info:" + Environment.NewLine +
                "Name: " + polyBeam.Name + Environment.NewLine +
                "Id: " + polyBeam.Identifier.ID.ToString() + Environment.NewLine +
                "Material: " + polyBeam.Material.MaterialString + Environment.NewLine +
                "Profile: " + polyBeam.Profile.ProfileString + Environment.NewLine +
                "Contour points: " + polyBeam.Contour.ContourPoints.ToString() + Environment.NewLine +
                "Class: " + polyBeam.Class + Environment.NewLine +
                "Finish: " + polyBeam.Finish + Environment.NewLine +
                "Position depth: " + polyBeam.Position.Depth.ToString() + Environment.NewLine +
                "Position plane: " + polyBeam.Position.Plane.ToString() + Environment.NewLine +
                "Position rotation: " + polyBeam.Position.Rotation.ToString() + Environment.NewLine +
                "Father assembly: " + polyBeam.GetAssembly().Name.ToString());
        }

        private void GetContourPlateInfo(ContourPlate contourPlate)
        {
            Console.WriteLine("ContourPlate Info:" + Environment.NewLine +
                "Name: " + contourPlate.Name + Environment.NewLine +
                "Id: " + contourPlate.Identifier.ID.ToString() + Environment.NewLine +
                "Material: " + contourPlate.Material.MaterialString + Environment.NewLine +
                "Profile: " + contourPlate.Profile.ProfileString + Environment.NewLine +
                "Contour points: " + contourPlate.Contour.ContourPoints.ToString() + Environment.NewLine +
                "Class: " + contourPlate.Class + Environment.NewLine +
                "Finish: " + contourPlate.Finish + Environment.NewLine +
                "Position depth: " + contourPlate.Position.Depth.ToString() + Environment.NewLine +
                "Position plane: " + contourPlate.Position.Plane.ToString() + Environment.NewLine +
                "Position rotation: " + contourPlate.Position.Rotation.ToString() + Environment.NewLine +
                "Father assembly: " + contourPlate.GetAssembly().Name.ToString());
        }

        #endregion
    }

    #endregion

    #region Main Execution Class

    class Program
    {
        static void Main()
        {
            DrawingModelHelper helper = new DrawingModelHelper();

            if (helper.Initialize())
            {
                helper.PickObjectInDrawing();
            }
            else
            {
                Console.WriteLine("Failed to connect to Tekla Structures.");
            }
        }
    }

    #endregion
}



using System;
using TSM = Tekla.Structures.Model;
using Tekla.Structures.Drawing;
using Tekla.Structures;
using Tekla.Structures.Drawing.UI;
using System.Collections;
using Tekla.Structures.Solid;
using Point = Tekla.Structures.Geometry3d.Point;
using Color = Tekla.Structures.Model.UI.Color;
using TSDT = Tekla.Structures.Drawing.Tools;

////Hint: More information: User Manual > Tab2
////Hint: App/plug-in samples:  Library > Tab2


class MyClass
{
	private static TSM.Model MyModel = new TSM.Model();
	
	static void Main()
	{
		try
		{
			//Your code
			//PickPointExample();
			PickDrawingObject();
		}
		catch (Exception)
		{	
		}
	}

	static void PickPointExample()
	{
		var drawingHandler = new DrawingHandler();

		Picker picker = drawingHandler.GetPicker();

		try
		{
			var pick = picker.PickPoint("Pick_point");
			var point = pick.Item1;
			var view = pick.Item2;
			// Do something with the picked point and view.
		}
		catch (PickerInterruptedException)
		{
			// No pick happened
		}
	}
	
	static void GetEquivalentModelObject()
	{
		
	
	}

	static void PickDrawingObject()
	{
		var drawingHandler = new DrawingHandler();
		Picker picker = drawingHandler.GetPicker();

		try
		{
			Picker DrawingObjectPicker = drawingHandler.GetPicker(); 
			DrawingObject pickedDrawingObject = null;
			ViewBase view = null;
			Point pickedPoint = null;
			// Allow picking of parts and bolts.
			Type [] myTypeFilter = new Type[] { typeof(Part), typeof(Bolt) };
			string prompt = "Pick a Part from the drawing";
			DrawingObjectPicker.PickObject(prompt, myTypeFilter , out pickedDrawingObject, out view, out pickedPoint);
			// Do something with the picked objet and view.
			if (pickedDrawingObject != null)
				{
					// Convert to Drawing Model Object to get the ID
					ModelObject pickedModelObject = pickedDrawingObject as ModelObject;
					Identifier modelObjectID = pickedModelObject.ModelIdentifier; 
					

				
				
					if (pickedDrawingObject is Part)
					{
						// Get the model side object						
						TSM.Part equivalentModelPart =  MyModel.SelectModelObject(modelObjectID) as TSM.Part;
						// Convert to part
						Part pickedPart = pickedDrawingObject as Part;
						pickedPart.Attributes.VisibleLines.Color = DrawingColors.Green;
						pickedPart.Attributes.VisibleLines.Type = LineTypes.DashedLine;
						pickedPart.Modify();
					}			
				}

			
		}
		catch (PickerInterruptedException)
		{
			// No pick happened
		}
	}
}




using System;
using TSM = Tekla.Structures.Model;
using Tekla.Structures.Drawing;
using TSD = Tekla.Structures.Drawing;
using Tekla.Structures;
using Tekla.Structures.Drawing.UI;
using System.Collections;
using Tekla.Structures.Solid;
using Point = Tekla.Structures.Geometry3d.Point;
using Tekla.Structures.Geometry3d;
using Color = Tekla.Structures.Model.UI.Color;
using TSDT = Tekla.Structures.Drawing.Tools;

////Hint: More information: User Manual > Tab2
////Hint: App/plug-in samples:  Library > Tab2

namespace FromDrawingToModel
{
	#region Drawing and Model Interaction Class

	public class DrawingModelHelper
	{
		private DrawingHandler _drawingHandler;
		private TSM.Model _model;

		public DrawingModelHelper()
		{
			_drawingHandler = new DrawingHandler();
			_model = new TSM.Model();
		}

		public bool Initialize()
		{
			return _model.GetConnectionStatus() && _drawingHandler.GetConnectionStatus();
		}
		
		#region Original Functions
		// Functions from the API example
		public void PickObjectInDrawing()
		{
			try
			{
				Drawing drawing = _drawingHandler.GetActiveDrawing();
				if (drawing != null)
				{
					DrawingObject pickedObject;
					ViewBase pickedView;
					Tekla.Structures.Geometry3d.Point pickedPoint;

					// Pick a part in the model 
					Picker myPicker = _drawingHandler.GetPicker();
					myPicker.PickObject("Pick a model object in the drawing", out pickedObject, out pickedView, out pickedPoint);

					GetInfoFromDrawing(pickedObject);
				}
				else
				{
					Console.WriteLine("A drawing must be opened in Tekla Structures model!");
				}
			}
			catch (Exception exc)
			{
				Console.WriteLine(exc.ToString());
			}
		}

		private void GetInfoFromDrawing(DrawingObject pickedObject)
		{
			if (pickedObject != null)
			{
				TSD.ModelObject modelObjectInDrawing = pickedObject as TSD.ModelObject;
				if (modelObjectInDrawing != null)
				{
					TSM.ModelObject modelObjectInModel = _model.SelectModelObject(modelObjectInDrawing.ModelIdentifier);
					if (modelObjectInModel is TSM.Part)
					{
						TSM.Beam beam = modelObjectInModel as TSM.Beam;
						if (beam != null)
						{
							GetBeamInfo(beam);
						}
						TSM.PolyBeam polyBeam = modelObjectInModel as TSM.PolyBeam;
						if (polyBeam != null)
						{
							GetPolyBeamInfo(polyBeam);
						}
						TSM.ContourPlate contourPlate = modelObjectInModel as TSM.ContourPlate;
						if (contourPlate != null)
						{
							GetContourPlateInfo(contourPlate);
						}
					}
				}
			}
		}

		private void GetBeamInfo(TSM.Beam beam)
		{
			Console.WriteLine("Beam Info:" + Environment.NewLine +
				"Name: " + beam.Name + Environment.NewLine +
				"Id: " + beam.Identifier.ID.ToString() + Environment.NewLine +
				"Material: " + beam.Material.MaterialString + Environment.NewLine +
				"Profile: " + beam.Profile.ProfileString + Environment.NewLine +
				"Start point: " + beam.StartPoint.ToString() + Environment.NewLine +
				"End point: " + beam.EndPoint.ToString() + Environment.NewLine +
				"Class: " + beam.Class + Environment.NewLine +
				"Finish: " + beam.Finish + Environment.NewLine +
				"Position depth: " + beam.Position.Depth.ToString() + Environment.NewLine +
				"Position plane: " + beam.Position.Plane.ToString() + Environment.NewLine +
				"Position rotation: " + beam.Position.Rotation.ToString() + Environment.NewLine +
				"Father assembly: " + beam.GetAssembly().Name.ToString());
		}

		private void GetPolyBeamInfo(TSM.PolyBeam polyBeam)
		{
			Console.WriteLine("PolyBeam Info:" + Environment.NewLine +
				"Name: " + polyBeam.Name + Environment.NewLine +
				"Id: " + polyBeam.Identifier.ID.ToString() + Environment.NewLine +
				"Material: " + polyBeam.Material.MaterialString + Environment.NewLine +
				"Profile: " + polyBeam.Profile.ProfileString + Environment.NewLine +
				"Contour points: " + polyBeam.Contour.ContourPoints.ToString() + Environment.NewLine +
				"Class: " + polyBeam.Class + Environment.NewLine +
				"Finish: " + polyBeam.Finish + Environment.NewLine +
				"Position depth: " + polyBeam.Position.Depth.ToString() + Environment.NewLine +
				"Position plane: " + polyBeam.Position.Plane.ToString() + Environment.NewLine +
				"Position rotation: " + polyBeam.Position.Rotation.ToString() + Environment.NewLine +
				"Father assembly: " + polyBeam.GetAssembly().Name.ToString());
		}

		private void GetContourPlateInfo(TSM.ContourPlate contourPlate)
		{
			Console.WriteLine("ContourPlate Info:" + Environment.NewLine +
				"Name: " + contourPlate.Name + Environment.NewLine +
				"Id: " + contourPlate.Identifier.ID.ToString() + Environment.NewLine +
				"Material: " + contourPlate.Material.MaterialString + Environment.NewLine +
				"Profile: " + contourPlate.Profile.ProfileString + Environment.NewLine +
				"Contour points: " + contourPlate.Contour.ContourPoints.ToString() + Environment.NewLine +
				"Class: " + contourPlate.Class + Environment.NewLine +
				"Finish: " + contourPlate.Finish + Environment.NewLine +
				"Position depth: " + contourPlate.Position.Depth.ToString() + Environment.NewLine +
				"Position plane: " + contourPlate.Position.Plane.ToString() + Environment.NewLine +
				"Position rotation: " + contourPlate.Position.Rotation.ToString() + Environment.NewLine +
				"Father assembly: " + contourPlate.GetAssembly().Name.ToString());
		}

		#endregion

		#region Custom Functions
		// Functions to meet our objective
		
		private TSM.ModelObject PickDrawingPartOrBolt(out ViewBase view)
		{
			Picker DrawingObjectPicker = _drawingHandler.GetPicker();

			// Variables:
			DrawingObject pickedDrawingObject = null;
			view = null;
			Point pickedPoint = null;
			Type[] myTypeFilter = new Type[] { typeof(Part), typeof(Bolt)}; // Allow picking of parts or bolts
			string prompt = "Pick an Part or a Bolt from the drawing";
			TSM.ModelObject equivalentModelObject = null;

			try
			{			
				// Pick a part from drawing
				DrawingObjectPicker.PickObject(prompt, myTypeFilter, out pickedDrawingObject, out view, out pickedPoint);
				
				if (pickedDrawingObject != null)
				{
					// Convert to Drawing Model Object to get the ID
					ModelObject pickedModelObject = pickedDrawingObject as ModelObject;
					Identifier modelObjectID = pickedModelObject.ModelIdentifier;

					// Get the model side object						
					equivalentModelObject = _model.SelectModelObject(modelObjectID);					
				}


			}
			catch (PickerInterruptedException)
			{
				// No pick happened
			}

			return equivalentModelObject;
		}
		
		public ArrayList GetPolyBeamContour()
		{
			try
			{					
				ViewBase currentView;
				
				TSM.PolyBeam modelPolybeam = PickDrawingPartOrBolt(out currentView) as TSM.PolyBeam;
				ArrayList contourPoints = modelPolybeam.Contour.ContourPoints;
				
				
				// Transformation plane to draw Circles
				TSM.TransformationPlane CurrPlane = _model.GetWorkPlaneHandler().GetCurrentTransformationPlane();
				_model.GetWorkPlaneHandler().SetCurrentTransformationPlane(new TSM.TransformationPlane((currentView as View).DisplayCoordinateSystem));

				// Draw Circles at contour Points
				Circle MyCircle;
				
				foreach (TSM.ContourPoint cPoint in contourPoints)
				{
					MyCircle = new Circle(currentView, cPoint, 25.4);
					MyCircle.Insert();
				}
				
				// Update Drawing
				_drawingHandler.GetActiveDrawing().CommitChanges();
				
				// Restore Plane
				_model.GetWorkPlaneHandler().SetCurrentTransformationPlane(CurrPlane);

				return contourPoints;
			}
			catch (System.Exception)
			{

				throw;
			}

		}
			
			
		#endregion
	}

	#endregion

	#region draw objects

	class DrawingGraphicObjects
	{
		public static void CircleAtPoint(Point point, ViewBase viewBase, double radius = 10)
		{
			Circle MyCircle;
			MyCircle = new Circle(viewBase, point, radius);
			MyCircle.Insert();		
		}
	}
	
	
	#endregion

	#region Main Execution Class

	class Program
	{
		static void Main()
		{
			DrawingModelHelper helper = new DrawingModelHelper();
			DrawingHandler drawingHandler = new DrawingHandler();
			Picker picker = drawingHandler.GetPicker();

			if (helper.Initialize())
			{
				// helper.PickObjectInDrawing();
				helper.GetPolyBeamContour();
			}
			else
			{
				Console.WriteLine("Failed to connect to Tekla Structures.");
			}


			Console.WriteLine("Press Enter to close...");
			Console.ReadLine();  // Wait for user input before closing the console
		}
	}

	#endregion
}


//////////
///
using System;
using TSM = Tekla.Structures.Model;
using Tekla.Structures.Drawing;
using TSD = Tekla.Structures.Drawing;
using Tekla.Structures;
using Tekla.Structures.Drawing.UI;
using System.Collections;
using Tekla.Structures.Solid;
using Point = Tekla.Structures.Geometry3d.Point;
using Tekla.Structures.Geometry3d;
using Color = Tekla.Structures.Model.UI.Color;
using TSDT = Tekla.Structures.Drawing.Tools;
using System.Reflection;

////Hint: More information: User Manual > Tab2
////Hint: App/plug-in samples:  Library > Tab2

namespace FromDrawingToModel
{
	#region Drawing and Model Interaction Class

	public class DrawingModelHelper
	{
		private DrawingHandler _drawingHandler;
		private TSM.Model _model;
		ViewBase _currentView = null;

		public DrawingModelHelper()
		{
			_drawingHandler = new DrawingHandler();
			_model = new TSM.Model();
		}

		public bool Initialize()
		{
			return _model.GetConnectionStatus() && _drawingHandler.GetConnectionStatus();
		}

		#region Original Functions
		// Functions from the API example
		public void PickObjectInDrawing()
		{
			try
			{
				Drawing drawing = _drawingHandler.GetActiveDrawing();
				if (drawing != null)
				{
					DrawingObject pickedObject;
					ViewBase pickedView;
					Tekla.Structures.Geometry3d.Point pickedPoint;

					// Pick a part in the model 
					Picker myPicker = _drawingHandler.GetPicker();
					myPicker.PickObject("Pick a model object in the drawing", out pickedObject, out pickedView, out pickedPoint);

					GetInfoFromDrawing(pickedObject);
				}
				else
				{
					Console.WriteLine("A drawing must be opened in Tekla Structures model!");
				}
			}
			catch (Exception exc)
			{
				Console.WriteLine(exc.ToString());
			}
		}

		private void GetInfoFromDrawing(DrawingObject pickedObject)
		{
			if (pickedObject != null)
			{
				TSD.ModelObject modelObjectInDrawing = pickedObject as TSD.ModelObject;
				if (modelObjectInDrawing != null)
				{
					TSM.ModelObject modelObjectInModel = _model.SelectModelObject(modelObjectInDrawing.ModelIdentifier);
					if (modelObjectInModel is TSM.Part)
					{
						TSM.Beam beam = modelObjectInModel as TSM.Beam;
						if (beam != null)
						{
							GetBeamInfo(beam);
						}
						TSM.PolyBeam polyBeam = modelObjectInModel as TSM.PolyBeam;
						if (polyBeam != null)
						{
							GetPolyBeamInfo(polyBeam);
						}
						TSM.ContourPlate contourPlate = modelObjectInModel as TSM.ContourPlate;
						if (contourPlate != null)
						{
							GetContourPlateInfo(contourPlate);
						}
					}
				}
			}
		}

		private void GetBeamInfo(TSM.Beam beam)
		{
			Console.WriteLine("Beam Info:" + Environment.NewLine +
				"Name: " + beam.Name + Environment.NewLine +
				"Id: " + beam.Identifier.ID.ToString() + Environment.NewLine +
				"Material: " + beam.Material.MaterialString + Environment.NewLine +
				"Profile: " + beam.Profile.ProfileString + Environment.NewLine +
				"Start point: " + beam.StartPoint.ToString() + Environment.NewLine +
				"End point: " + beam.EndPoint.ToString() + Environment.NewLine +
				"Class: " + beam.Class + Environment.NewLine +
				"Finish: " + beam.Finish + Environment.NewLine +
				"Position depth: " + beam.Position.Depth.ToString() + Environment.NewLine +
				"Position plane: " + beam.Position.Plane.ToString() + Environment.NewLine +
				"Position rotation: " + beam.Position.Rotation.ToString() + Environment.NewLine +
				"Father assembly: " + beam.GetAssembly().Name.ToString());
		}

		private void GetPolyBeamInfo(TSM.PolyBeam polyBeam)
		{
			Console.WriteLine("PolyBeam Info:" + Environment.NewLine +
				"Name: " + polyBeam.Name + Environment.NewLine +
				"Id: " + polyBeam.Identifier.ID.ToString() + Environment.NewLine +
				"Material: " + polyBeam.Material.MaterialString + Environment.NewLine +
				"Profile: " + polyBeam.Profile.ProfileString + Environment.NewLine +
				"Contour points: " + polyBeam.Contour.ContourPoints.ToString() + Environment.NewLine +
				"Class: " + polyBeam.Class + Environment.NewLine +
				"Finish: " + polyBeam.Finish + Environment.NewLine +
				"Position depth: " + polyBeam.Position.Depth.ToString() + Environment.NewLine +
				"Position plane: " + polyBeam.Position.Plane.ToString() + Environment.NewLine +
				"Position rotation: " + polyBeam.Position.Rotation.ToString() + Environment.NewLine +
				"Father assembly: " + polyBeam.GetAssembly().Name.ToString());
		}

		private void GetContourPlateInfo(TSM.ContourPlate contourPlate)
		{
			Console.WriteLine("ContourPlate Info:" + Environment.NewLine +
				"Name: " + contourPlate.Name + Environment.NewLine +
				"Id: " + contourPlate.Identifier.ID.ToString() + Environment.NewLine +
				"Material: " + contourPlate.Material.MaterialString + Environment.NewLine +
				"Profile: " + contourPlate.Profile.ProfileString + Environment.NewLine +
				"Contour points: " + contourPlate.Contour.ContourPoints.ToString() + Environment.NewLine +
				"Class: " + contourPlate.Class + Environment.NewLine +
				"Finish: " + contourPlate.Finish + Environment.NewLine +
				"Position depth: " + contourPlate.Position.Depth.ToString() + Environment.NewLine +
				"Position plane: " + contourPlate.Position.Plane.ToString() + Environment.NewLine +
				"Position rotation: " + contourPlate.Position.Rotation.ToString() + Environment.NewLine +
				"Father assembly: " + contourPlate.GetAssembly().Name.ToString());
		}
		

		#endregion

		#region Custom Functions
		// Functions to meet our objective

		public TSM.ModelObject PickDrawingPartOrBolt(out ViewBase MyView, string prompt = "Pick a Part or a Bolt from the drawing")
		{
			Picker DrawingObjectPicker = _drawingHandler.GetPicker();

			// Variables:
			DrawingObject pickedDrawingObject = null;
			Point pickedPoint = null;
			Type[] myTypeFilter = new Type[] { typeof(Part), typeof(Bolt) }; // Allow picking of parts or bolts
			TSM.ModelObject equivalentModelObject = null;
			MyView = null; // assigned to avoid errors, TODO: fix later, maybe putting the try on a higher level

			try
			{
				// Pick a part from drawing
				DrawingObjectPicker.PickObject(prompt, myTypeFilter, out pickedDrawingObject, out MyView, out pickedPoint);

				// Draw BB's
				DrawingGraphicObjects.DrawBBForObject(pickedDrawingObject);
				DrawingGraphicObjects.DrawBBForObjectFrame(pickedDrawingObject);
				
				
				// Assign Global view if unassigned
				if (_currentView == null){_currentView = MyView; }
	

				if (pickedDrawingObject != null)
				{					
					
					// Store View Plane
					TSM.TransformationPlane viewPlane = new TSM.TransformationPlane((MyView as View).ViewCoordinateSystem);					

					// Store Model work plane
					TSM.WorkPlaneHandler workPlaneHandler = _model.GetWorkPlaneHandler();
					TSM.TransformationPlane currentPlane = workPlaneHandler.GetCurrentTransformationPlane();

					//Clear workplane to new blank global
					workPlaneHandler.SetCurrentTransformationPlane(new TSM.TransformationPlane());
					//Set model everything to the view
					workPlaneHandler.SetCurrentTransformationPlane(viewPlane);
					
					// Now that we are working on the view Plane,
					// Convert to Drawing Model Object and get the ID
					ModelObject pickedModelObject = pickedDrawingObject as ModelObject;				

					// Get the model side object						
					equivalentModelObject = _model.SelectModelObject(pickedModelObject.ModelIdentifier);					
					

					// Return the original Model Plane
					workPlaneHandler.SetCurrentTransformationPlane(currentPlane);
					
				}


			}
			catch (PickerInterruptedException)
			{
				// No pick happened
			}
			
			

			return equivalentModelObject;
		}

		public ArrayList GetPolyBeamContourPoints(TSM.PolyBeam modelPolybeam, bool drawCircles = false)
		{

			try
			{	
				// Get the contourPoints in the view coordinates
				ArrayList contourPoints = modelPolybeam.Contour.ContourPoints;


				// Option to visualize the points
				if (drawCircles)
				{										
					DrawPoints(contourPoints);
				}
				
				// Update Drawing
				_drawingHandler.GetActiveDrawing().CommitChanges();

				return contourPoints;
			}
			catch (System.Exception)
			{

				throw;
			}

		}


		public ArrayList GetSolidPointsFromPart(TSM.Part MyPart, bool drawCircles = false, int initialPointNumber = 0, bool reverseList = false)
		{
			ArrayList MyList = new ArrayList();
			ArrayList MyFaceNormalList = new ArrayList();

			// Store View Plane
			TSM.TransformationPlane viewPlane = new TSM.TransformationPlane((_currentView as View).ViewCoordinateSystem);

			// Store Model work plane
			TSM.WorkPlaneHandler workPlaneHandler = _model.GetWorkPlaneHandler();
			TSM.TransformationPlane currentPlane = workPlaneHandler.GetCurrentTransformationPlane();

			// Clear workplane to new blank global
			workPlaneHandler.SetCurrentTransformationPlane(new TSM.TransformationPlane());
			// Set model everything to the view
			workPlaneHandler.SetCurrentTransformationPlane(viewPlane);

			// Now that we are working on the view Plane,    

			TSM.Solid mySolid = MyPart.GetSolid();
			FaceEnumerator MyFaceEnum = mySolid.GetFaceEnumerator();
			while (MyFaceEnum.MoveNext())
			{
				Face MyFace = MyFaceEnum.Current as Face;
				if (MyFace != null)
				{
					MyFaceNormalList.Add(MyFace.Normal);

					LoopEnumerator MyLoopEnum = MyFace.GetLoopEnumerator();
					while (MyLoopEnum.MoveNext())
					{
						Loop MyLoop = MyLoopEnum.Current as Loop;
						if (MyLoop != null)
						{
							VertexEnumerator MyVertexEnum = MyLoop.GetVertexEnumerator() as VertexEnumerator;
							while (MyVertexEnum.MoveNext())
							{
								Point MyVertex = MyVertexEnum.Current as Point;
								if (MyVertex != null)
								{
									// Remove duplicates based on a tolerance in Z-axis (or 2D comparison in XY plane)
									bool isDuplicate = false;
									foreach (Point existingPoint in MyList)
									{
										if (Math.Abs(existingPoint.X - MyVertex.X) < 0.01 &&
											Math.Abs(existingPoint.Y - MyVertex.Y) < 0.01)
										{
											isDuplicate = true;
											break;
										}
									}
									if (!isDuplicate)
									{
										MyList.Add(MyVertex);
									}
								}
							}
						}
					}
				}
			}
			
			// Option to reverse the order of points
			if (reverseList)
			{				
				MyList.Reverse();
			}


			// Option to set the initial point by point number
			if (initialPointNumber != 0)
			{
				SetInitialPointByNumber(MyList, initialPointNumber);
			}
			
			
			// Option to visualize the points
			if (drawCircles)
			{
				DrawPoints(MyList);
			}  




			// Return the original Model Plane
			workPlaneHandler.SetCurrentTransformationPlane(currentPlane);

			return MyList;
		}

		private void DrawPoints(ArrayList points)
		{
			int i = 0;
			foreach (Point point in points)
			{
				Point unfoldedPoint = TSDT.DrawingCoordinateConverter.Convert(_currentView, _currentView, point); // Point Converted to local
				Circle MyCircle = new Circle(_currentView, unfoldedPoint, 10);
				MyCircle.Insert();

				Text myText = new Text(_currentView, unfoldedPoint, i.ToString());
				myText.Insert();
				i++;
			}
		}

		private void SetInitialPointByNumber(ArrayList points, int pointNumber)
		{
			if (pointNumber >= 0 && pointNumber < points.Count)
			{
				ArrayList reorderedList = new ArrayList();
				for (int i = pointNumber; i < points.Count; i++)
				{
					reorderedList.Add(points[i]);
				}
				for (int i = 0; i < pointNumber; i++)
				{
					reorderedList.Add(points[i]);
				}
				points.Clear();
				points.AddRange(reorderedList);
			}
		}




		public PointList PickNPoints(int numberOfPoints, bool drawCircles = false, string prefix = "Pick Point number: ")
		{
			Picker myPicker = _drawingHandler.GetPicker();
			StringList stringList = new StringList();
			int i = 0;

			foreach (var s in stringList)
			{
				stringList.Add(prefix + i.ToString());
			}


			var pickedPoints = myPicker.PickPoints(numberOfPoints, stringList);
			// Assign Global view if unassigned
			if (_currentView == null) { _currentView = pickedPoints.Item2; }

			return pickedPoints.Item1;
			
		}


		#endregion
	}

	#endregion

	#region draw objects

	class DrawingGraphicObjects
	{
		public static void CircleAtPoint(Point point, ViewBase viewBase, double radius = 10)
		{
			Circle MyCircle;
			MyCircle = new Circle(viewBase, point, radius);
			MyCircle.Insert();
		}

		public static void DrawBBForObject(DrawingObject drawingObject)
		{
			if (drawingObject is IAxisAlignedBoundingBox)
			{
				drawingObject.Select();
				RectangleBoundingBox currentBB = (drawingObject as IAxisAlignedBoundingBox).GetAxisAlignedBoundingBox();
				Console.WriteLine("Drawing AABB for {0}", drawingObject.GetType().Name);

				DrawBB(drawingObject.GetView(), currentBB, DrawingColors.Red);
			}

			if (drawingObject is IObjectAlignedBoundingBox)
			{
				drawingObject.Select();
				RectangleBoundingBox currentBB = (drawingObject as IObjectAlignedBoundingBox).GetObjectAlignedBoundingBox();
				Console.WriteLine("Drawing OABB for {0}: Width: {1}, Height: {2}, Angle: {3}",
					drawingObject.GetType().Name,
					Distance.PointToPoint(currentBB.LowerRight, currentBB.LowerLeft),
					Distance.PointToPoint(currentBB.UpperLeft, currentBB.LowerLeft),
					currentBB.AngleToAxis);

				DrawBB(drawingObject.GetView(), currentBB, DrawingColors.Cyan);
			}
		}

		public static void DrawBBForObjectFrame(DrawingObject drawingObject)
		{
			try
			{
				// Will throw System.Reflection.AmbiguousMatchException for Mark(Base)
				PropertyInfo AttributesInfo = drawingObject.GetType().GetProperty("Attributes");
				object Attributes = AttributesInfo.GetValue(drawingObject, null);
				object Frame = AttributesInfo.PropertyType.GetProperty("Frame").GetValue(Attributes, null);
				if (Frame is IObjectAlignedBoundingBox)
				{
					Console.WriteLine("Drawing OABB for {0}.Attributes.Frame", drawingObject.GetType().Name);
					DrawBB(drawingObject.GetView(),
						   (Frame as IObjectAlignedBoundingBox).GetObjectAlignedBoundingBox(),
						   DrawingColors.Cyan);
				}

				if (Frame is IAxisAlignedBoundingBox)
				{
					Console.WriteLine("Drawing AABB for {0}.Attributes.Frame", drawingObject.GetType().Name);
					DrawBB(drawingObject.GetView(),
						   (Frame as IAxisAlignedBoundingBox).GetAxisAlignedBoundingBox(),
						   DrawingColors.Red);
				}
			}
			catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine(ex.ToString());
			}
		}

		static void DrawBB(ViewBase DrawView, RectangleBoundingBox BB, DrawingColors Color)
		{
			PointList polygonPoints = BBToPointList(BB);
			Polygon MyPolygon = new Polygon(DrawView, polygonPoints);
			MyPolygon.Attributes.Line.Color = Color;
			MyPolygon.Insert();
			MyPolygon.SetUserProperty("__MyBBPolygon__", 1);
		}

		static PointList BBToPointList(RectangleBoundingBox BB)
		{
			PointList polygonPoints = new PointList();
			polygonPoints.Add(BB.LowerLeft);
			polygonPoints.Add(BB.UpperLeft);
			polygonPoints.Add(BB.UpperRight);
			polygonPoints.Add(BB.LowerRight);
			return polygonPoints;
		}
	}


	#endregion

	#region Main Execution Class

	class Program
	{
		static void Main()
		{

			DrawingModelHelper helper = new DrawingModelHelper();


			if (helper.Initialize())
			{
				ViewBase currentView = null;

				// Pick Polybeam and process the points (number them and classify them) , draw circles to identify them, we need the view to be defined here
				//TSM.PolyBeam modelPolybeam = helper.PickDrawingPartOrBolt(out currentView, "Pick the polybeam") as TSM.PolyBeam; //_currentView is updated here
				TSM.PolyBeam modelPolybeam = helper.PickDrawingPartOrBolt(out currentView, "Pick the polybeam - -") as TSM.PolyBeam;
				ArrayList polybeamContourPoints = helper.GetPolyBeamContourPoints(modelPolybeam, false);
				ArrayList PolybeamSolidPoints =  helper.GetSolidPointsFromPart(modelPolybeam as TSM.Part, true, 68, true);
			

				// Pick secondary points and process them
				//helper.PickNPoints(3, true);
				

				// Pick Bolts to be included, or turn them on and off by type

				// Create Dims

				// finish
			}
			else
			{
				Console.WriteLine("Failed to connect to Tekla Structures.");
			}


			//Console.WriteLine("Press Enter to close...");
			//Console.ReadLine();  // Wait for user input before closing the console
		}
	}

	#endregion
}


using System;
using Tekla.Structures.Model;
using TSM = Tekla.Structures.Model;
using Tekla.Structures.Drawing;
using TSD = Tekla.Structures.Drawing;
using Tekla.Structures.Geometry3d;
using System.Collections.Generic;

namespace TeklaDimensioning
{
	class Program
	{
		static void Main(string[] args)
		{
			// Initial setup: Verify Tekla Structures API connection
			if (!IsTeklaConnected())
			{
				Console.WriteLine("Failed to connect to Tekla Structures.");
				return;
			}

			Console.WriteLine("Connected to Tekla Structures.");

			// Basic dimensioning function: Select parts and add dimensions
			Console.WriteLine("Starting the dimensioning process...");

			// Select parts from the model (for simplicity, selecting all parts)
			var parts = SelectParts();
			if (parts == null || !parts.MoveNext())
			{
				Console.WriteLine("No parts found in the model.");
				return;
			}

			// Add dimensions to the selected parts
			AddDimensions(parts);

			Console.WriteLine("Dimensioning process completed.");
		}

		// Function to check if Tekla Structures is connected
		static bool IsTeklaConnected()
		{
			try
			{
				Model model = new Model();
				return model.GetConnectionStatus();
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error connecting to Tekla Structures: {ex.Message}");
				return false;
			}
		}

		// Function to select parts from the model
		static ModelObjectEnumerator SelectParts()
		{
			try
			{
				DrawingObjectEnumerator drawingParts = SelectDrawingParts();

				// Initialize a list to hold the model object identifiers
				List<Tekla.Structures.Identifier> modelIdentifiers = new List<Tekla.Structures.Identifier>();				// Collect model identifiers from drawing parts
				while (drawingParts.MoveNext())
				{
					TSD.Part drawingPart = drawingParts.Current as TSD.Part;
					if (drawingPart != null)
					{
						// Add the model identifier to the list
						modelIdentifiers.Add(drawingPart.ModelIdentifier);
					}
				}

				// Use the model identifiers to get the corresponding model objects
				Model model = new Model();
				ModelObjectSelector selector = model.GetModelObjectSelector();
				ModelObjectEnumerator modelParts = selector. GetObjectsByIdentifier(modelIdentifiers);

				return parts;
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error selecting parts: {ex.Message}");
				return null;
			}
		}
		
		// Function to select parts from the drawing
		static DrawingObjectEnumerator SelectDrawingParts()
		{
			try
			{
				DrawingHandler drawingHandler = new DrawingHandler();
				if (!drawingHandler.GetConnectionStatus())
				{
					Console.WriteLine("Failed to connect to the Tekla Drawing Handler.");
					return null;
				}

				// Assuming we're working with the first open drawing (for MVP simplicity)
				Drawing currentDrawing = drawingHandler.GetActiveDrawing();
				if (currentDrawing == null)
				{
					Console.WriteLine("No active drawing found.");
					return null;
				}
				

				// Select all parts (like beams, polybeams, plates) from the drawing view
				DrawingObjectEnumerator.AutoFetch = true;
				DrawingObjectEnumerator drawingParts = currentDrawing.GetSheet(). GetAllObjects(typeof(TSD.Part)); 


				return drawingParts;
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error selecting parts from drawing: {ex.Message}");
				return null;
			}
		}


		// Function to add dimensions to selected parts
		static void AddDimensions(ModelObjectEnumerator parts)
		{
			try
			{
				Model model = new Model();
				DrawingHandler drawingHandler = new DrawingHandler();

				if (!drawingHandler.GetConnectionStatus())
				{
					Console.WriteLine("Failed to connect to the Tekla Drawing Handler.");
					return;
				}

				// Assuming we're working with the first open drawing (for MVP simplicity)
				Drawing currentDrawing = drawingHandler.GetActiveDrawing();
				if (currentDrawing == null)
				{
					Console.WriteLine("No active drawing found.");
					return;
				}

				// Loop through each part and add a simple dimension
				foreach (Part part in parts)
				{
					if (part != null)
					{
						Console.WriteLine($"Adding dimension to part: {part.Identifier.ID}");

						// Create a simple straight dimension for the part (example: from its bounding box)
						var boundingBox = part.GetSolid().MinimumPoint; // Simplified example
						var dimensionPoint1 = new Point(boundingBox.X, boundingBox.Y, boundingBox.Z);
						var dimensionPoint2 = new Point(boundingBox.X + 1000, boundingBox.Y + 1000, boundingBox.Z); // Example dimension points

						StraightDimensionSet dimensionSet = new StraightDimensionSet(currentDrawing, dimensionPoint1, dimensionPoint2);
						dimensionSet.Insert();
					}
				}

				Console.WriteLine("Dimensions added successfully.");
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error adding dimensions: {ex.Message}");
			}
		}
	}

	// Placeholder class for StraightDimensionSet
	// In a real implementation, you would create the necessary methods to work with dimensions
	public class StraightDimensionSet
	{
		public StraightDimensionSet(Drawing drawing, Point startPoint, Point endPoint)
		{
			// Placeholder constructor
		}

		public void Insert()
		{
			// Placeholder method for inserting a dimension
		}
	}
}


using System;
using Tekla.Structures;
using System.Windows.Forms;
using Tekla.Structures.Model;
using TSM = Tekla.Structures.Model;
using Tekla.Structures.Drawing;
using TSD = Tekla.Structures.Drawing;
using Tekla.Structures.Geometry3d;
using TSDT = Tekla.Structures.Drawing.Tools;
using System.Collections.Generic;
using Tekla.Structures.Drawing.UI;
using System.Reflection;
using System.Collections;

namespace TeklaDimensioning
{
	class Program
	{
		static void Main(string[] args)
		{
			// Initial setup: Verify Tekla Structures API connection
			if (!IsTeklaConnected())
			{
				Console.WriteLine("Failed to connect to Tekla Structures.");
				return;
			}

			Console.WriteLine("Connected to Tekla Structures.");

			DrawingObjectProcessor processor = new DrawingObjectProcessor();
			processor.ProcessSelectedObjects();


			//			List<TSM.ModelObject> modelPartList = null;
			//			List< TSM.ModelObject> modelBoltList = null;
			//			GetEquivalentModelObjectsFromSelected(out modelPartList, out modelBoltList);
			//			DrawingHandler _drawingHandler = new DrawingHandler();
			//			BoundingBoxDrawer.DrawBBs(_drawingHandler) ;
			//
			//			// Basic dimensioning function: Select parts and add dimensions
			//			Console.WriteLine("Starting the dimensioning process...");
			//
			//			// Select parts from the model (for simplicity, selecting all parts)
			//			List<TSM.ModelObject> parts = SelectParts();
			//			
			//			if (parts == null)
			//			{
			//				Console.WriteLine("No parts found in the list.");
			//				return;
			//			}
			//
			//			// Add dimensions to the selected parts
			//			AddDimensions(parts);

			Console.WriteLine("Dimensioning process completed.");
		}

		public class DrawingObjectProcessor
		{
			// Global variables for DrawingHandler and Model
			private readonly Model _model;
			private readonly DrawingHandler _drawingHandler;
			private readonly Drawing _currentDrawing;

			// Constructor to initialize the global variables
			public DrawingObjectProcessor()
			{
				_drawingHandler = new DrawingHandler();
				_model = new Model();
				_currentDrawing = _drawingHandler.GetActiveDrawing();

				if (!_model.GetConnectionStatus())
				{
					throw new InvalidOperationException("Failed to connect to the Tekla Model.");
				}

				if (!_drawingHandler.GetConnectionStatus())
				{
					throw new InvalidOperationException("Failed to connect to the Tekla Drawing Handler.");
				}

				if (_currentDrawing == null)
				{
					throw new InvalidOperationException("No active drawing found.");
				}

			}

			/// <summary>
			/// Processes the selected objects in the drawing and performs operations on parts and bolts.
			/// </summary>
			public void ProcessSelectedObjects()
			{
				try
				{
					Drawing currentDrawing = _drawingHandler.GetActiveDrawing();
					if (currentDrawing == null)
					{
						throw new InvalidOperationException("No active drawing found.");
					}

					// Get selected drawing objects
					DrawingObjectEnumerator selectedObjects = GetSelectedDrawingObjects();

					// Separate the selected objects into parts and bolts
					var (modelParts, modelBolts, drawingParts, drawingBolts) = SeparatePartsAndBolts(selectedObjects);


					// pick a dimensionset
					DrawingObject pickedObject = null; // we need to convert this to DimensionSetBase type later, after we pick the dim
					ViewBase pickedView = null;
					Point pickedPoint = null;
					Picker currentPicker = _drawingHandler.GetPicker();
					currentPicker.PickObject("Pick Dimension", new Type[] { typeof(DrawingObject)}, out pickedObject, out pickedView, out pickedPoint );

					Console.WriteLine(pickedObject.GetType().ToString());
					
					
					// correct type
					DimensionSetBase pickedDimensionSet = pickedObject as DimensionSetBase;

					AddObjectsToDimension(pickedDimensionSet, drawingParts, 0);

					// Perform operations on parts and bolts
					foreach (TSD.Part part in drawingParts)
					{
						//Point[] boundingBox = GetBoundingBoxFromModelPartAABB(part);
						//Point[] boundingBox2 = GetBoundingBoxFromModelPartOBB(part, true, true);
						//List<Point> boltPoints = GetBoltPointsInPart(part, currentDrawing);

						// Example usage of boundingBox and boltPoints
						//Console.WriteLine($"Part ID: {part.Identifier.ID}, BoundingBox: {boundingBox}");
						//Console.WriteLine($"Bolt Points Count: {boltPoints.Count}");
					}

					//					foreach (var bolt in bolts)
					//					{
					//						List<Point> boltPoints = GetBoltPoints(bolt, currentDrawing);
					//						Console.WriteLine($"Bolt ID: {bolt.Identifier.ID}, Points Count: {boltPoints.Count}");
					//					}
					//
					//					// Display counts
					//					DisplayCounts(parts, bolts);

					_currentDrawing.CommitChanges();
					
					Console.ReadLine(); // Keeps the console open until Enter is pressed

				}
				catch (Exception ex)
				{
					Console.WriteLine($"Error processing model objects: {ex.Message}");
				}
			}

			/// <summary>
			/// Retrieves the selected objects in the drawing.
			/// </summary>
			/// <returns>An enumerator of selected drawing objects.</returns>
			private DrawingObjectEnumerator GetSelectedDrawingObjects()
			{
				DrawingObjectEnumerator.AutoFetch = true;
				return _drawingHandler.GetDrawingObjectSelector().GetSelected();
			}


			/// <summary>
			/// Separates the selected drawing objects into parts and bolts, and fetches the corresponding model objects.
			/// </summary>
			/// <param name="selectedObjects">Enumerator of selected drawing objects.</param>
			/// <returns>
			/// A tuple containing four lists:
			/// - modelParts: A list of model objects corresponding to the selected parts in the drawing.
			/// - modelBolts: A list of model objects corresponding to the selected bolts in the drawing.
			/// - drawingParts: A list of drawing objects that are parts, selected in the drawing.
			/// - drawingBolts: A list of drawing objects that are bolts, selected in the drawing.
			/// </returns>
			/// <remarks>
			/// This function iterates through the selected drawing objects, identifying those that are parts or bolts.
			/// It collects the identifiers of these objects to fetch the corresponding model objects from the Tekla model.
			/// The function then returns a tuple containing both the model objects and the original drawing objects, 
			/// allowing for further operations to be performed on either set.
			/// </remarks>
			private (List<TSM.ModelObject> modelParts, List<TSM.ModelObject> modelBolts, 
				List<DrawingObject> drawingParts, List<DrawingObject> drawingBolts) 
				SeparatePartsAndBolts(DrawingObjectEnumerator selectedObjects)
			{
				// Define lists to store the drawing side Parts and Bolts
				List<DrawingObject> drawingParts = new List<DrawingObject>();
				List<DrawingObject> drawingBolts = new List<DrawingObject>();

				// Identifiers to get model objects
				List<Tekla.Structures.Identifier> idListParts = new List<Tekla.Structures.Identifier>();
				List<Tekla.Structures.Identifier> idListBolts = new List<Tekla.Structures.Identifier>();

				while (selectedObjects.MoveNext())
				{
					if (selectedObjects.Current is TSD.Part drawingPart)
					{
						idListParts.Add(drawingPart.ModelIdentifier);
						drawingParts.Add(drawingPart);
					}
					else if (selectedObjects.Current is TSD.Bolt drawingBolt)
					{
						idListBolts.Add(drawingBolt.ModelIdentifier);
						drawingBolts.Add(drawingBolt);
					}
				}

				// fetch model objects in the drawing
				Model model = new Model();
				List<TSM.ModelObject> modelParts = model.FetchModelObjects(idListParts, false);
				List<TSM.ModelObject> modelBolts = model.FetchModelObjects(idListBolts, false);

				return (modelParts, modelBolts, drawingParts, drawingBolts);
			}

			/// <summary>
			/// Displays the count of parts and bolts found.
			/// </summary>
			/// <param name="parts">List of parts.</param>
			/// <param name="bolts">List of bolts.</param>
			private void DisplayCounts(List<TSM.ModelObject> parts, List<TSM.ModelObject> bolts)
			{
				MessageBox.Show(
					$"{parts.Count} Parts found.\n" +
					$"{bolts.Count} Bolts found."
				);
			}

			/// <summary>
			/// Retrieves the bolt points from a bolt object.
			/// </summary>
			/// <param name="bolt">The bolt object.</param>
			/// <param name="view">The drawing view.</param>
			/// <returns>A list of points representing the bolt's points.</returns>
			private List<Point> GetBoltPoints(TSM.BoltArray bolt, ViewBase view)
			{
				// Implement logic to retrieve bolt points
				List<Point> boltPoints = new List<Point>();
				// Add logic here based on the bolt and view
				return boltPoints;
			}

			private Point[] GetBoundingBoxFromModelPartAABB(TSD.Part part, bool drawPolygon = false, bool drawPoints = false)
			{
				// Get View
				var thisView = part.GetView();

				// Store View Plane
				TSM.TransformationPlane viewPlane = new TSM.TransformationPlane((thisView as TSD.View).ViewCoordinateSystem);

				// Store Model work plane
				TSM.WorkPlaneHandler workPlaneHandler = _model.GetWorkPlaneHandler();
				TSM.TransformationPlane currentPlane = workPlaneHandler.GetCurrentTransformationPlane();

				//Clear workplane to new blank global
				workPlaneHandler.SetCurrentTransformationPlane(new TSM.TransformationPlane());
				//Set model everything to the view
				workPlaneHandler.SetCurrentTransformationPlane(viewPlane);


				// Get mode Object 
				TSM.ModelObject equivalentModelObject = _model.SelectModelObject(part.ModelIdentifier);
				// Get solid
				Solid mySolid = (equivalentModelObject as TSM.Part).GetSolid();
				// Define Bounding Box				
				AABB MyAxisAlignedBoundingBox1 = new AABB(mySolid.MinimumPoint, mySolid.MaximumPoint);

				Point[] BBPoints = MyAxisAlignedBoundingBox1.GetCornerPoints();

				// Return the original Model Plane
				workPlaneHandler.SetCurrentTransformationPlane(currentPlane);

				// Draw the Polygon
				if (drawPolygon)
				{
					DrawPolygon(thisView, BBPoints, DrawingColors.Red);

				}

				if (drawPoints)
				{
					// Draw Points
					DrawPoints(thisView, new ArrayList(BBPoints));

				}




				return BBPoints;

			}

			private Point[] GetBoundingBoxFromModelPartOBB(TSD.Part part, bool drawPolygon = false, bool drawPoints = false)
			{
				// Get View
				var thisView = part.GetView();

				// Store View Plane
				TSM.TransformationPlane viewPlane = new TSM.TransformationPlane((thisView as TSD.View).ViewCoordinateSystem);
				// Store Model work plane
				TSM.WorkPlaneHandler workPlaneHandler = _model.GetWorkPlaneHandler();
				TSM.TransformationPlane currentPlane = workPlaneHandler.GetCurrentTransformationPlane();

				//Set model everything to the view
				workPlaneHandler.SetCurrentTransformationPlane(viewPlane);

				// Working in the View Plane
				// Get solid
				TSM.ModelObject equivalentModelObject = _model.SelectModelObject(part.ModelIdentifier);
				Solid mySolid = (equivalentModelObject as TSM.Part).GetSolid();

				// Points
				Point minPointInCurrentPlane = mySolid.MinimumPoint;
				Point maxPointInCurrentPlane = mySolid.MaximumPoint;
				Point centerPoint = CalculateCenterPoint(minPointInCurrentPlane, maxPointInCurrentPlane);


				// Change to part Coordinate System
				// Working in the local Plane
				CoordinateSystem coordSys = equivalentModelObject.GetCoordinateSystem();
				TransformationPlane localTransformationPlane = new TransformationPlane(coordSys);
				workPlaneHandler.SetCurrentTransformationPlane(localTransformationPlane);

				// Get Local Points
				mySolid = (equivalentModelObject as TSM.Part).GetSolid();
				Point minPoint = mySolid.MinimumPoint;
				Point maxPoint = mySolid.MaximumPoint;
				double extent0 = (maxPoint.X - minPoint.X) / 2;
				double extent1 = (maxPoint.Y - minPoint.Y) / 2;
				double extent2 = (maxPoint.Z - minPoint.Z) / 2;


				// Get back to original Work Plane
				workPlaneHandler.SetCurrentTransformationPlane(currentPlane);



				OBB obb = new OBB(centerPoint, coordSys.AxisX, coordSys.AxisY,
							coordSys.AxisX.Cross(coordSys.AxisY), extent0, extent1, extent2);

				Point[] BBPoints = obb.ComputeVertices();


				// Draw the Polygon
				if (drawPolygon)
				{
					DrawPolygon(thisView, BBPoints, DrawingColors.Blue);

				}

				if (drawPoints)
				{
					// Draw Points
					DrawPoints(thisView, new ArrayList(BBPoints));

				}



				return BBPoints;



			}


			/// <summary>
			/// Retrieves the bounding box of a drawing object in the drawing view.
			/// </summary>
			/// <param name="drawingObject">The drawing object.</param>
			/// <returns>The bounding box of the part.</returns>
			private RectangleBoundingBox GetBoundingBox(DrawingObject drawingObject)
			{
				RectangleBoundingBox currentBB = null;


				if (drawingObject is IAxisAlignedBoundingBox)
				{
					drawingObject.Select();
					Console.WriteLine("Attempting to retrieve Axis-Aligned Bounding Box (AABB) for {0}", drawingObject.GetType().Name);

					// Retrieve Axis-Aligned Bounding Box
					currentBB = (drawingObject as IAxisAlignedBoundingBox)!.GetAxisAlignedBoundingBox();
					Console.WriteLine("Successfully retrieved AABB for {0}", drawingObject.GetType().Name);

					// Draw the bounding box on the drawing view
					DrawBB(drawingObject.GetView(), currentBB, DrawingColors.Red);
					Console.WriteLine("AABB drawn on view.");
				}

				if (drawingObject is IObjectAlignedBoundingBox)
				{
					drawingObject.Select();
					Console.WriteLine("Attempting to retrieve Object-Aligned Bounding Box (OABB) for {0}", drawingObject.GetType().Name);

					// Retrieve Object-Aligned Bounding Box
					currentBB = (drawingObject as IObjectAlignedBoundingBox)!.GetObjectAlignedBoundingBox();
					Console.WriteLine("Successfully retrieved OABB for {0}: Width: {1}, Height: {2}, Angle: {3}",
						drawingObject.GetType().Name,
						Distance.PointToPoint(currentBB.LowerRight, currentBB.LowerLeft),
						Distance.PointToPoint(currentBB.UpperLeft, currentBB.LowerLeft),
						currentBB.AngleToAxis);

					// Draw the bounding box on the drawing view
					DrawBB(drawingObject.GetView(), currentBB, DrawingColors.Cyan);
					Console.WriteLine("OABB drawn on view.");
				}

				if (currentBB == null)
				{
					Console.WriteLine("No bounding box could be retrieved for {0}.", drawingObject.GetType().Name);
				}


				Console.WriteLine("Processing completed. Press Enter to exit.");
				Console.ReadLine(); // Keeps the console open until Enter is pressed

				return currentBB; // Return the bounding box if successfully retrieved
			}



			static PointList BBToPointList(RectangleBoundingBox BB)
			{
				PointList polygonPoints = new PointList();
				polygonPoints.Add(BB.LowerLeft);
				polygonPoints.Add(BB.UpperLeft);
				polygonPoints.Add(BB.UpperRight);
				polygonPoints.Add(BB.LowerRight);
				return polygonPoints;
			}

			static void DrawBB(ViewBase DrawView, RectangleBoundingBox BB, DrawingColors Color)
			{
				PointList polygonPoints = BBToPointList(BB);
				TSD.Polygon MyPolygon = new TSD.Polygon(DrawView, polygonPoints);
				MyPolygon.Attributes.Line.Color = Color;
				MyPolygon.Insert();
				MyPolygon.SetUserProperty("__MyBBPolygon__", 1);
			}

			private Point CalculateCenterPoint(Point min, Point max)
			{
				double x = min.X + ((max.X - min.X) / 2);
				double y = min.Y + ((max.Y - min.Y) / 2);
				double z = min.Z + ((max.Z - min.Z) / 2);

				return new Point(x, y, z);
			}

			static void DrawPolygon(ViewBase DrawView, Point[] PointsArray, DrawingColors Color)
			{

				PointList polygonPoints = new PointList();

				foreach (Point p in PointsArray)
				{
					polygonPoints.Add(p);
				}

				TSD.Polygon MyPolygon = new TSD.Polygon(DrawView, polygonPoints);
				MyPolygon.Attributes.Line.Color = Color;
				MyPolygon.Insert();
				MyPolygon.SetUserProperty("__MyBBPolygon__", 1);
			}

			private void DrawPoints(ViewBase currentView, ArrayList points)
			{
				int i = 0;
				foreach (Point point in points)
				{
					Point unfoldedPoint = UnfoldPoint(currentView, point); // Point Converted to local
					Circle MyCircle = new Circle(currentView, unfoldedPoint, 10);
					MyCircle.Insert();

					Text myText = new Text(currentView, unfoldedPoint, i.ToString());
					myText.Insert();
					i++;
				}
			}

			private Point UnfoldPoint(ViewBase currentView, Point point)
			{
				Point unfoldedPoint = TSDT.DrawingCoordinateConverter.Convert(currentView, currentView, point); // Point Converted to local
				return unfoldedPoint;
			}

			/// <summary>
			/// Retrieves the bolt points within a part.
			/// </summary>
			/// <param name="part">The part object.</param>
			/// <param name="view">The drawing view.</param>
			/// <returns>A list of points representing the bolt's points within the part.</returns>
			private List<Point> GetBoltPointsInPart(TSM.Part part, ViewBase view)
			{
				// Implement logic to retrieve the bolts in the part within the drawing view
				List<Point> boltPoints = new List<Point>();
				// Add logic here based on the part and view
				return boltPoints;
			}


			/// <summary>
			/// Adds selected objects to a dimension set.
			/// </summary>
			/// <param name="dimensionSet">The dimension set.</param>
			/// <param name="selectedObjects">List of selected model objects.</param>
			/// <param name="pointSelectionOption">The point selection option (e.g., BottomLeft, TopLeft, etc.).</param>
			public void AddObjectsToDimension(TSD.DimensionSetBase dimensionSet, List<TSD.DrawingObject> selectedObjects, int pointIndex)
			{
				// Create pointlist
				PointList dimensionPoints = new PointList();
				
				foreach (var obj in selectedObjects)
				{				

					if (obj is TSD.Part part)
					{
						
						// Get Bounding Box Points, typically 8 points
						Point[] boxPoints = GetBoundingBoxFromModelPartOBB(part);

						// Point to be dimensioned
						Point partPoint = boxPoints[pointIndex];

						dimensionPoints.Add(partPoint);					
												

					}
					else if (obj is TSD.Bolt bolt)
					{
						// dimensionPoints.AddRange(GetBoltPoints(bolt, dimensionSet.GetView()));

					}

					foreach (var point in dimensionPoints)
					{
//						// dimensionSet.AddPoint(point); // This is not direct
//						PointList pointList = new PointList();
//						Vector v = new Vector(); // We will use the vector from the original dimension set here
//						TSD.StraightDimensionSet newStraightDimensionSet = new StraightDimensionSetHandler().CreateDimensionSet(dimensionSet.GetView(), pointList, v, 0.0);
//						dimensionSet.AddToDimensionSet(newStraightDimensionSet);
//						// Manage Curved Dimension Sets too
					}
				}
				
				// Create a dimensionset with the selected objects
				Vector v = new Vector(); // TODO: We will use the vector from the original dimension set here
				TSD.StraightDimensionSet newDimensionSet = 
					new StraightDimensionSetHandler().CreateDimensionSet(dimensionSet.GetView(), dimensionPoints, v, 0.0);
				
				// Combine the new dimensions with the picked dimension
				dimensionSet.AddToDimensionSet(newDimensionSet);
				dimensionSet.Modify();
			}

		}



		// Function to check if Tekla Structures is connected
		static bool IsTeklaConnected()
		{
			try
			{
				Model model = new Model();
				return model.GetConnectionStatus();
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error connecting to Tekla Structures: {ex.Message}");
				return false;
			}
		}

		// Function to select parts from the model
		static List<TSM.ModelObject> SelectParts()
		{
			try
			{
				List<TSM.ModelObject> modelPartList = null;
				List<TSM.ModelObject> modelBoltList = null;
				List<TSM.ModelObject> drawingParts = GetEquivalentModelObjectsFromSelected(out modelPartList, out modelBoltList);

				// Initialize a list to hold the model object identifiers
				List<Tekla.Structures.Identifier> modelIdentifiers = new List<Tekla.Structures.Identifier>();

				// Define selector to get model Objects by ID
				Model model = new Model();
				ModelObjectSelector selector = model.GetModelObjectSelector();

				// Collect model identifiers from drawing parts
				foreach (TSM.ModelObject drawingPart in drawingParts)
				{
					double a = 0;

				}


				// Use the model identifiers to get the corresponding model objects

				List<TSM.ModelObject> modelPartsList = model.FetchModelObjects(modelIdentifiers, false);

				return modelPartsList;
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error selecting parts: {ex.Message}");
				return null;
			}
		}

		// Function to select parts from the drawing
		static DrawingObjectEnumerator SelectDrawingParts()
		{
			try
			{
				DrawingHandler drawingHandler = new DrawingHandler();
				if (!drawingHandler.GetConnectionStatus())
				{
					Console.WriteLine("Failed to connect to the Tekla Drawing Handler.");
					return null;
				}

				// Assuming we're working with the first open drawing (for MVP simplicity)
				Drawing currentDrawing = drawingHandler.GetActiveDrawing();
				if (currentDrawing == null)
				{
					Console.WriteLine("No active drawing found.");
					return null;
				}


				// Select all parts (like beams, polybeams, plates) from the drawing view
				DrawingObjectEnumerator.AutoFetch = true;
				DrawingObjectEnumerator drawingParts = currentDrawing.GetSheet().GetAllObjects(typeof(TSD.Part));


				return drawingParts;
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error selecting parts from drawing: {ex.Message}");
				return null;
			}
		}

		// Function to select parts from the drawing
		static List<TSM.ModelObject> GetEquivalentModelObjectsFromSelected(out List<TSM.ModelObject> mPartList, out List<TSM.ModelObject> mBoltList)
		{
			// Assign before any return
			mPartList = null;
			mBoltList = null;

			// Define the lists of ID's
			List<Tekla.Structures.Identifier> idListParts = new List<Tekla.Structures.Identifier>();
			List<Tekla.Structures.Identifier> idListBolts = new List<Tekla.Structures.Identifier>();
			// Define this list just in case we have something else
			List<Tekla.Structures.Identifier> idListOther = new List<Tekla.Structures.Identifier>();

			try
			{
				DrawingHandler drawingHandler = new DrawingHandler();
				if (!drawingHandler.GetConnectionStatus())
				{
					Console.WriteLine("Failed to connect to the Tekla Drawing Handler.");
					return null;
				}

				Drawing currentDrawing = drawingHandler.GetActiveDrawing();
				if (currentDrawing == null)
				{
					Console.WriteLine("No active drawing found.");
					return null;
				}

				// Retrieve the model objects from the drawing view
				// Get objects selected from the view
				DrawingObjectEnumerator.AutoFetch = true;
				DrawingObjectEnumerator selected = drawingHandler.GetDrawingObjectSelector().GetSelected();
				while (selected.MoveNext())
				{

					TSD.ModelObject drawingModelObject = selected.Current as TSD.ModelObject; // This filters the ModelObjects only
					if (drawingModelObject != null)
					{
						// Get the type, it can be Bolt or Part
						var t = drawingModelObject.GetType();
						Console.WriteLine("type = " + t);

						if (drawingModelObject is TSD.Part)
						{
							// Add the model identifier to the list
							idListParts.Add(drawingModelObject.ModelIdentifier);

						}

						else if (drawingModelObject is TSD.Bolt)
						{
							// Add the model identifier to the list
							idListBolts.Add(drawingModelObject.ModelIdentifier);
						}
						else
						{
							idListOther.Add(drawingModelObject.ModelIdentifier);
						}

					}

				}

				// fetch model objects in the drawing
				Model model = new Model();
				mPartList = model.FetchModelObjects(idListParts, false);
				mBoltList = model.FetchModelObjects(idListBolts, false);

				// Display the count of parts, bolts, and others found
				MessageBox.Show(
					$"{mPartList.Count} Parts found.\n" +
					$"{mBoltList.Count} Bolts found.\n" +
					$"{idListOther.Count} Other objects found."
				);


				return null;
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error selecting parts from drawing: {ex.Message}");
				return null;
			}
		}


		// Function to add dimensions to selected parts
		static void AddDimensions(List<TSM.ModelObject> parts)
		{
			try
			{
				Model model = new Model();
				DrawingHandler drawingHandler = new DrawingHandler();

				if (!drawingHandler.GetConnectionStatus())
				{
					Console.WriteLine("Failed to connect to the Tekla Drawing Handler.");
					return;
				}

				// Assuming we're working with the first open drawing (for MVP simplicity)
				Drawing currentDrawing = drawingHandler.GetActiveDrawing();
				if (currentDrawing == null)
				{
					Console.WriteLine("No active drawing found.");
					return;
				}

				// Loop through each part and add a simple dimension
				foreach (TSM.ModelObject part in parts)
				{
					TSM.Part P = part as TSM.Part;
					if (P != null)
					{
						Console.WriteLine($"Adding dimension to part: {part.Identifier.ID}");

						// Create a simple straight dimension for the part (example: from its bounding box)
						var boundingBox = P.GetSolid().MinimumPoint; // Simplified example
						var dimensionPoint1 = new Point(boundingBox.X, boundingBox.Y, boundingBox.Z);
						var dimensionPoint2 = new Point(boundingBox.X + 1000, boundingBox.Y + 1000, boundingBox.Z); // Example dimension points

						StraightDimensionSet dimensionSet = new StraightDimensionSet(currentDrawing, dimensionPoint1, dimensionPoint2);
						dimensionSet.Insert();
					}
				}

				Console.WriteLine("Dimensions added successfully.");
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error adding dimensions: {ex.Message}");
			}
		}



	}

	public static class BoundingBoxDrawer
	{
		static readonly DrawingHandler _drawingHandler = new DrawingHandler();



		public static void DeleteDrawingObjectsByUda(ViewBase view, String Name)
		{
			DrawingObjectEnumerator objects = view.GetAllObjects();
			objects.SelectInstances = false;
			foreach (DrawingObject drawingObject in objects)
			{
				if (drawingObject is TSD.Polygon)
				{
					int Value = 0;
					if (drawingObject.GetUserProperty(Name, ref Value))
						drawingObject.Delete();
				}
			}
		}

		public static void DrawBBs(DrawingHandler DrawHandler)
		{
			Drawing ActiveDrawing = DrawHandler.GetActiveDrawing();
			ViewBase Sheet = ActiveDrawing.GetSheet();
			DrawingObjectEnumerator allObjectsOnSheet = Sheet.GetAllObjects();

			allObjectsOnSheet.Reset();
			DrawingObjectSelector dos = DrawHandler.GetDrawingObjectSelector();
			allObjectsOnSheet = dos.GetSelected();

			foreach (DrawingObject drawingObject in allObjectsOnSheet)
			{
				DrawBBForObject(drawingObject);
				DrawBBForObjectFrame(drawingObject);
			}
		}

		private static void DrawBBForObjectFrame(DrawingObject drawingObject)
		{
			try
			{
				// Will throw System.Reflection.AmbiguousMatchException for Mark(Base)
				PropertyInfo AttributesInfo = drawingObject.GetType().GetProperty("Attributes");
				object Attributes = AttributesInfo.GetValue(drawingObject, null);
				object Frame = AttributesInfo.PropertyType.GetProperty("Frame").GetValue(Attributes, null);
				if (Frame is IObjectAlignedBoundingBox)
				{
					Console.WriteLine("Drawing OABB for {0}.Attributes.Frame", drawingObject.GetType().Name);
					DrawBB(drawingObject.GetView(),
						   (Frame as IObjectAlignedBoundingBox).GetObjectAlignedBoundingBox(),
						   DrawingColors.Cyan);
				}

				if (Frame is IAxisAlignedBoundingBox)
				{
					Console.WriteLine("Drawing AABB for {0}.Attributes.Frame", drawingObject.GetType().Name);
					DrawBB(drawingObject.GetView(),
						   (Frame as IAxisAlignedBoundingBox).GetAxisAlignedBoundingBox(),
						   DrawingColors.Red);
				}
			}
			catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine(ex.ToString());
			}
		}

		private static void DrawBBForObject(DrawingObject drawingObject)
		{
			if (drawingObject is IAxisAlignedBoundingBox)
			{
				drawingObject.Select();
				RectangleBoundingBox currentBB = (drawingObject as IAxisAlignedBoundingBox)!.GetAxisAlignedBoundingBox();
				Console.WriteLine("Drawing AABB for {0}", drawingObject.GetType().Name);

				DrawBB(drawingObject.GetView(), currentBB, DrawingColors.Red);
			}

			if (drawingObject is IObjectAlignedBoundingBox)
			{
				drawingObject.Select();
				RectangleBoundingBox currentBB = (drawingObject as IObjectAlignedBoundingBox)!.GetObjectAlignedBoundingBox();
				Console.WriteLine("Drawing OABB for {0}: Width: {1}, Height: {2}, Angle: {3}",
					drawingObject.GetType().Name,
					Distance.PointToPoint(currentBB.LowerRight, currentBB.LowerLeft),
					Distance.PointToPoint(currentBB.UpperLeft, currentBB.LowerLeft),
					currentBB.AngleToAxis);

				DrawBB(drawingObject.GetView(), currentBB, DrawingColors.Cyan);
			}
		}

		static PointList BBToPointList(RectangleBoundingBox BB)
		{
			PointList polygonPoints = new PointList();
			polygonPoints.Add(BB.LowerLeft);
			polygonPoints.Add(BB.UpperLeft);
			polygonPoints.Add(BB.UpperRight);
			polygonPoints.Add(BB.LowerRight);
			return polygonPoints;
		}

		static void DrawBB(ViewBase DrawView, RectangleBoundingBox BB, DrawingColors Color)
		{
			PointList polygonPoints = BBToPointList(BB);
			TSD.Polygon MyPolygon = new TSD.Polygon(DrawView, polygonPoints);
			MyPolygon.Attributes.Line.Color = Color;
			MyPolygon.Insert();
			MyPolygon.SetUserProperty("__MyBBPolygon__", 1);
		}
	}

	// Placeholder class for StraightDimensionSet
	// In a real implementation, you would create the necessary methods to work with dimensions
	public class StraightDimensionSet
	{
		public StraightDimensionSet(Drawing drawing, Point startPoint, Point endPoint)
		{
			// Placeholder constructor
		}

		public void Insert()
		{
			// Placeholder method for inserting a dimension
		}
	}
}
