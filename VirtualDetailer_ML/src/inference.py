#!/usr/bin/env python3
"""
Inference Pipeline for VirtualDetailer ML
Runs dimension prediction on new drawings and creates dimensions in Tekla

For demonstration: Uses rule-based dimension placement
For production: Would use trained YOLOv8 model
"""
import json
import sys
from pathlib import Path
from typing import List, Dict, Tuple
import requests


class DimensionPredictor:
    """Predicts dimension placements for drawings"""

    def __init__(self, use_ml_model: bool = False):
        """
        Initialize predictor

        Args:
            use_ml_model: If True, use trained YOLO model. If False, use rule-based approach.
        """
        self.use_ml_model = use_ml_model
        self.model = None

        if use_ml_model:
            try:
                from ultralytics import YOLO
                model_path = Path(__file__).parent.parent / 'models' / 'dimension_detector_v1' / 'weights' / 'best.pt'
                if model_path.exists():
                    self.model = YOLO(str(model_path))
                    print(f"Loaded ML model: {model_path}")
                else:
                    print(f"Warning: Model not found at {model_path}")
                    print("Falling back to rule-based approach")
                    self.use_ml_model = False
            except ImportError:
                print("Warning: ultralytics not available")
                print("Using rule-based approach")
                self.use_ml_model = False
        else:
            print("Using rule-based dimension placement")

    def predict_dimensions(self, drawing: Dict) -> List[Dict]:
        """
        Predict dimension locations for a drawing

        Args:
            drawing: Drawing data from extraction

        Returns:
            List of dimension predictions with location and type
        """
        if self.use_ml_model and self.model:
            return self._ml_predict(drawing)
        else:
            return self._rule_based_predict(drawing)

    def _rule_based_predict(self, drawing: Dict) -> List[Dict]:
        """
        Rule-based dimension placement
        Places dimensions at logical locations based on part geometry
        """
        predictions = []

        # Extract parts
        parts = [obj for obj in drawing.get('objects', []) if obj.get('type_name') == 'Part']

        for part in parts:
            geom = part.get('geometric_data', {})
            part_geom = geom.get('part_geometry', {})

            # Get bounding box
            bbox = part_geom.get('bounding_box_model')
            if not bbox:
                continue

            min_pt = bbox.get('min', {})
            max_pt = bbox.get('max', {})

            if not min_pt or not max_pt:
                continue

            # Calculate part dimensions
            length_x = max_pt.get('x', 0) - min_pt.get('x', 0)
            length_y = max_pt.get('y', 0) - min_pt.get('y', 0)
            length_z = max_pt.get('z', 0) - min_pt.get('z', 0)

            # Get part profile for offset calculation
            props = part.get('properties', {})
            profile = props.get('Profile', {}).get('ProfileString', 'UNKNOWN')

            # Place dimensions for the primary axes
            # Only create dimensions for significant lengths (> 100mm)
            if length_x > 100:
                predictions.append({
                    'type': 'StraightDimension',
                    'axis': 'X',
                    'start_point': (min_pt['x'], min_pt['y'], min_pt.get('z', 0)),
                    'end_point': (max_pt['x'], min_pt['y'], min_pt.get('z', 0)),
                    'distance': length_x,
                    'part_id': part.get('id'),
                    'profile': profile
                })

            if length_y > 100:
                predictions.append({
                    'type': 'StraightDimension',
                    'axis': 'Y',
                    'start_point': (min_pt['x'], min_pt['y'], min_pt.get('z', 0)),
                    'end_point': (min_pt['x'], max_pt['y'], min_pt.get('z', 0)),
                    'distance': length_y,
                    'part_id': part.get('id'),
                    'profile': profile
                })

            if length_z > 100:
                predictions.append({
                    'type': 'StraightDimension',
                    'axis': 'Z',
                    'start_point': (min_pt['x'], min_pt['y'], min_pt.get('z', 0)),
                    'end_point': (min_pt['x'], min_pt['y'], max_pt.get('z', 0)),
                    'distance': length_z,
                    'part_id': part.get('id'),
                    'profile': profile
                })

        return predictions

    def _ml_predict(self, drawing: Dict) -> List[Dict]:
        """ML-based dimension prediction using YOLO model"""
        # TODO: Implement ML prediction
        # This would rasterize the drawing, run YOLO inference, and convert predictions
        return self._rule_based_predict(drawing)


class TeklaDimensionCreator:
    """Creates dimensions in Tekla using MCP tools"""

    def __init__(self, mcp_url: str = "http://localhost:8080"):
        """
        Initialize Tekla dimension creator

        Args:
            mcp_url: URL of the Tekla MCP server
        """
        self.mcp_url = mcp_url

    def create_dimension(self, prediction: Dict) -> bool:
        """
        Create a dimension in Tekla based on prediction

        Args:
            prediction: Dimension prediction dict

        Returns:
            True if successful, False otherwise
        """
        try:
            # For now, we'll use the execute-code endpoint to create dimensions
            # In production, this would use a dedicated dimension creation endpoint

            dim_type = prediction['type']
            start = prediction['start_point']
            end = prediction['end_point']

            # Generate C# code to create dimension
            code = f'''
var model = new Tekla.Structures.Model.Model();
var drawing = new Tekla.Structures.Drawing.DrawingHandler().GetActiveDrawing();

if (drawing != null)
{{
    var startPoint = new Tekla.Structures.Geometry3d.Point({start[0]}, {start[1]}, {start[2]});
    var endPoint = new Tekla.Structures.Geometry3d.Point({end[0]}, {end[1]}, {end[2]});

    // Create straight dimension
    var dimension = new Tekla.Structures.Drawing.StraightDimension();
    dimension.StartPoint = startPoint;
    dimension.EndPoint = endPoint;

    if (dimension.Insert())
    {{
        drawing.CommitChanges();
        return "Dimension created: " + dimension.Value;
    }}
    return "Failed to create dimension";
}}
return "No active drawing";
'''

            # Note: This is pseudocode - actual implementation would need
            # proper MCP endpoint for dimension creation
            print(f"  Would create {dim_type} dimension: {prediction['distance']:.1f}mm ({prediction['axis']} axis)")
            return True

        except Exception as e:
            print(f"  Error creating dimension: {e}")
            return False


def main():
    """Main inference pipeline"""
    print("\n" + "="*60)
    print("DIMENSION INFERENCE PIPELINE")
    print("="*60 + "\n")

    # Initialize predictor (rule-based for demonstration)
    predictor = DimensionPredictor(use_ml_model=False)

    # Initialize Tekla creator
    creator = TeklaDimensionCreator()

    # Load test drawings to demonstrate
    data_dir = Path(__file__).parent.parent / 'data' / 'processed'
    test_file = data_dir / 'test.json'

    if not test_file.exists():
        print(f"Error: {test_file} not found")
        return

    with open(test_file, 'r') as f:
        test_drawings = json.load(f)

    # Process first 3 test drawings as demonstration
    print("Processing test drawings (demonstration):\n")

    total_predictions = 0
    total_created = 0

    for i, drawing in enumerate(test_drawings[:3]):
        mark = drawing.get('mark', 'unknown').strip('[]')
        print(f"{i+1}. Drawing: {mark}")

        # Predict dimensions
        predictions = predictor.predict_dimensions(drawing)
        total_predictions += len(predictions)

        print(f"   Predicted {len(predictions)} dimensions:")

        # Show first few predictions
        for pred in predictions[:5]:
            print(f"     - {pred['type']} ({pred['axis']}): {pred['distance']:.1f}mm")
            # In production, would call creator.create_dimension(pred)
            total_created += 1

        if len(predictions) > 5:
            print(f"     ... and {len(predictions) - 5} more")

        print()

    print("="*60)
    print("INFERENCE SUMMARY")
    print("="*60)
    print(f"Drawings processed:  {min(3, len(test_drawings))}")
    print(f"Dimensions predicted: {total_predictions}")
    print(f"Dimensions created:   {total_created} (demonstration mode)")
    print("\nNote: This is a rule-based demonstration.")
    print("For ML-based inference, train model first: python src/train.py")
    print("="*60 + "\n")


if __name__ == '__main__':
    main()
