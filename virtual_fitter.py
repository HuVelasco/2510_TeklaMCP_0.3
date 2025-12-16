#!/usr/bin/env python3
"""
PHASE 2: Virtual Fitter Tool
A powerful tool that finds the required dimensions to fit an assembly.

Uses Operations Research techniques to:
1. Build constraint system from dimensions
2. Solve for part positions that satisfy all dimensional constraints
3. Compare with ground truth assembly
4. Identify missing dimensions
"""
import json
import numpy as np
from typing import Dict, List, Tuple, Optional
from dataclasses import dataclass
import pulp  # Linear programming solver


@dataclass
class Part3D:
    """3D part with position and geometry."""
    part_id: str
    profile: str
    material: str
    # Position (to be solved)
    position: np.ndarray = None  # [x, y, z]
    rotation: np.ndarray = None  # [rx, ry, rz] Euler angles
    # Geometry
    start_point: np.ndarray = None  # Local coordinates
    end_point: np.ndarray = None
    bounding_box: Dict = None


@dataclass
class DimensionConstraint:
    """Dimensional constraint between parts."""
    dimension_id: str
    dimension_type: str  # 'length', 'distance', 'angle'
    value: float  # Target value
    # What it measures
    from_part_id: str
    to_part_id: str
    from_feature: str  # 'start', 'end', 'centerline', 'face'
    to_feature: str
    # 3D interpretation
    direction: np.ndarray = None  # Unit vector of measurement direction
    tolerance: float = 1.0  # mm


class VirtualFitter:
    """
    Virtual Fitter: Solves for part positions given dimensional constraints.

    Algorithm:
    1. Parse assembly data (parts + dimensions)
    2. Build constraint system (equality and inequality constraints)
    3. Formulate as optimization problem
    4. Solve using OR tools (PuLP)
    5. Validate against ground truth
    6. Report missing dimensions
    """

    def __init__(self, assembly_data: Dict):
        """Initialize with assembly data from extraction."""
        self.assembly_data = assembly_data
        self.parts: Dict[str, Part3D] = {}
        self.dimensions: List[DimensionConstraint] = []
        self.ground_truth: Dict[str, np.ndarray] = {}
        self.solved_positions: Dict[str, np.ndarray] = {}

    def parse_assembly(self):
        """Parse assembly data into parts and dimension constraints."""
        print("\n" + "=" * 80)
        print("VIRTUAL FITTER: Parsing Assembly")
        print("=" * 80)

        # Extract parts from view-centric structure
        views = self.assembly_data.get('views', [])
        part_count = 0

        for view in views:
            objects = view.get('objects', {})
            parts = objects.get('parts', [])

            for part_obj in parts:
                part_id = part_obj.get('object_id', f'part_{part_count}')
                model_id = part_obj.get('model_object_id')

                # Extract geometry from geometric_data.part_geometry
                geom = part_obj.get('geometric_data', {})
                part_geom = geom.get('part_geometry', {})

                part = Part3D(
                    part_id=part_id,
                    profile=part_geom.get('profile', 'unknown'),
                    material=part_geom.get('material', 'unknown')
                )

                # Ground truth position from model
                sp = part_geom.get('start_point')
                if sp and isinstance(sp, dict):
                    part.start_point = np.array([sp['x'], sp['y'], sp['z']])
                    self.ground_truth[part_id] = part.start_point

                ep = part_geom.get('end_point')
                if ep and isinstance(ep, dict):
                    part.end_point = np.array([ep['x'], ep['y'], ep['z']])

                self.parts[part_id] = part
                part_count += 1

        print(f"  Parsed {len(self.parts)} parts")

        # Extract dimension constraints
        dim_count = 0
        for view in views:
            objects = view.get('objects', {})
            dimensions = objects.get('dimensions', [])

            for dim_obj in dimensions:
                # Get 3D interpretation
                meas_3d = dim_obj.get('measurement_3d', {})
                if not meas_3d:
                    continue

                dim_id = dim_obj.get('object_id', f'dim_{dim_count}')

                # Parse related objects to find measured parts
                related = dim_obj.get('related_objects', [])
                part_refs = [r for r in related if 'Part' in r]

                if len(part_refs) >= 2:
                    # Create constraint between two parts
                    start_3d = meas_3d.get('start_point_3d', {})
                    end_3d = meas_3d.get('end_point_3d', {})

                    if start_3d and end_3d:
                        start_vec = np.array([start_3d['x'], start_3d['y'], start_3d['z']])
                        end_vec = np.array([end_3d['x'], end_3d['y'], end_3d['z']])

                        direction = end_vec - start_vec
                        distance = np.linalg.norm(direction)
                        if distance > 0:
                            direction = direction / distance

                        constraint = DimensionConstraint(
                            dimension_id=dim_id,
                            dimension_type='distance',
                            value=meas_3d.get('measured_value', distance),
                            from_part_id=part_refs[0],
                            to_part_id=part_refs[1],
                            from_feature='centerline',
                            to_feature='centerline',
                            direction=direction
                        )

                        self.dimensions.append(constraint)
                        dim_count += 1

        print(f"  Parsed {len(self.dimensions)} dimension constraints")
        print(f"  Ground truth available for {len(self.ground_truth)} parts")

    def build_constraint_system(self):
        """Build constraint system for optimization."""
        print("\n" + "=" * 80)
        print("VIRTUAL FITTER: Building Constraint System")
        print("=" * 80)

        # Create optimization problem
        prob = pulp.LpProblem("Assembly_Fitting", pulp.LpMinimize)

        # Decision variables: position of each part (x, y, z)
        # Use large bounds to accommodate typical building coordinates
        part_vars = {}
        for part_id in self.parts.keys():
            part_vars[part_id] = {
                'x': pulp.LpVariable(f"{part_id}_x", lowBound=-100000, upBound=100000),
                'y': pulp.LpVariable(f"{part_id}_y", lowBound=-100000, upBound=100000),
                'z': pulp.LpVariable(f"{part_id}_z", lowBound=-100000, upBound=100000)
            }

        # Objective: Set variables close to ground truth using constraints
        # Note: PuLP is a linear programming solver and doesn't support quadratic objectives
        # Instead, we'll add soft constraints that pull variables toward ground truth
        # For a simple linear objective, we minimize the sum of position variables
        # (constraints will handle the actual fitting)

        # Simple objective: minimize sum of absolute positions
        # The dimensional constraints will force the correct solution
        objective_terms = []
        for part_id in part_vars.keys():
            vars = part_vars[part_id]
            # Just use a dummy objective - constraints will do the work
            objective_terms.extend([vars['x'], vars['y'], vars['z']])

        if objective_terms:
            prob += pulp.lpSum(objective_terms), "Minimize_total_positions"

        # Add soft constraints to anchor to ground truth if available
        for part_id, gt_pos in self.ground_truth.items():
            if part_id in part_vars:
                vars = part_vars[part_id]
                # Set initial values close to ground truth (helps solver)
                vars['x'].setInitialValue(gt_pos[0])
                vars['y'].setInitialValue(gt_pos[1])
                vars['z'].setInitialValue(gt_pos[2])

        # Dimensional constraints
        constraint_count = 0
        for constraint in self.dimensions:
            from_part = constraint.from_part_id
            to_part = constraint.to_part_id

            # Skip if parts not in system
            if from_part not in part_vars or to_part not in part_vars:
                continue

            # Distance constraint along dimension direction
            # Note: PuLP doesn't support non-linear constraints directly
            # For linear approximation, we use Manhattan distance
            # For better results, would use non-linear solver (scipy.optimize)

            from_vars = part_vars[from_part]
            to_vars = part_vars[to_part]

            # Simple constraint: absolute distance in primary direction
            # This is a simplified linear approximation
            if constraint.direction is not None:
                # Find dominant direction
                abs_dir = np.abs(constraint.direction)
                if abs_dir[0] > abs_dir[1] and abs_dir[0] > abs_dir[2]:
                    # X-direction dominant
                    prob += (to_vars['x'] - from_vars['x'] == constraint.value), f"Constraint_{constraint_count}_x"
                elif abs_dir[1] > abs_dir[2]:
                    # Y-direction dominant
                    prob += (to_vars['y'] - from_vars['y'] == constraint.value), f"Constraint_{constraint_count}_y"
                else:
                    # Z-direction dominant
                    prob += (to_vars['z'] - from_vars['z'] == constraint.value), f"Constraint_{constraint_count}_z"

                constraint_count += 1

        print(f"  Created {len(part_vars)} part position variables (3 DOF each)")
        print(f"  Added {constraint_count} dimensional constraints")

        return prob, part_vars

    def solve(self) -> Dict[str, Dict]:
        """Solve the constraint system to find part positions."""
        print("\n" + "=" * 80)
        print("VIRTUAL FITTER: Solving Constraint System")
        print("=" * 80)

        prob, part_vars = self.build_constraint_system()

        # Solve using default solver (CBC)
        print("\n  Solving with PuLP (CBC solver)...")
        prob.solve()

        # Extract solution
        status = pulp.LpStatus[prob.status]
        print(f"  Status: {status}")

        if status == 'Optimal':
            for part_id, vars in part_vars.items():
                position = np.array([
                    pulp.value(vars['x']),
                    pulp.value(vars['y']),
                    pulp.value(vars['z'])
                ])
                self.solved_positions[part_id] = position
                self.parts[part_id].position = position

            print(f"  Solution found for {len(self.solved_positions)} parts")
        else:
            print(f"  WARNING: No optimal solution found")

        return {
            'status': status,
            'solved_parts': len(self.solved_positions),
            'total_parts': len(self.parts)
        }

    def validate_against_ground_truth(self) -> Dict:
        """Compare solved positions with ground truth."""
        print("\n" + "=" * 80)
        print("VIRTUAL FITTER: Validation Against Ground Truth")
        print("=" * 80)

        errors = []
        max_error = 0.0
        total_error = 0.0

        for part_id, gt_pos in self.ground_truth.items():
            if part_id in self.solved_positions:
                solved_pos = self.solved_positions[part_id]
                error = np.linalg.norm(solved_pos - gt_pos)
                errors.append({
                    'part_id': part_id,
                    'error_mm': round(error, 3),
                    'ground_truth': gt_pos.tolist(),
                    'solved': solved_pos.tolist()
                })
                max_error = max(max_error, error)
                total_error += error

        avg_error = total_error / len(errors) if errors else 0.0

        print(f"  Parts validated: {len(errors)}")
        print(f"  Average error: {avg_error:.3f} mm")
        print(f"  Max error: {max_error:.3f} mm")

        # Print worst cases
        if errors:
            print("\n  Worst 3 fits:")
            sorted_errors = sorted(errors, key=lambda x: x['error_mm'], reverse=True)
            for i, err in enumerate(sorted_errors[:3], 1):
                print(f"    {i}. {err['part_id']}: {err['error_mm']:.3f} mm error")

        return {
            'errors': errors,
            'avg_error_mm': avg_error,
            'max_error_mm': max_error,
            'validated_parts': len(errors)
        }

    def identify_missing_dimensions(self) -> List[str]:
        """Identify dimensions needed but not provided."""
        print("\n" + "=" * 80)
        print("VIRTUAL FITTER: Identifying Missing Dimensions")
        print("=" * 80)

        # Simple heuristic: check DOF coverage
        # For N parts in 3D, we need 3N constraints to fully determine positions
        # (assuming one part is fixed as reference)

        required_constraints = (len(self.parts) - 1) * 3
        provided_constraints = len(self.dimensions)

        missing_count = max(0, required_constraints - provided_constraints)

        print(f"  Required constraints (3 DOF per part): {required_constraints}")
        print(f"  Provided dimensional constraints: {provided_constraints}")
        print(f"  Missing constraints: {missing_count}")

        if missing_count > 0:
            print(f"\n  RECOMMENDATION: Add {missing_count} more dimensions to fully constrain assembly")
            print(f"  Consider adding:")
            print(f"    - Vertical dimensions (Z-direction)")
            print(f"    - Cross-view dimensions")
            print(f"    - Centerline-to-centerline distances")
        else:
            print(f"\n  Assembly is fully constrained!")

        return {
            'required': required_constraints,
            'provided': provided_constraints,
            'missing': missing_count
        }

    def run(self) -> Dict:
        """Run complete Virtual Fitter workflow."""
        print("\n" + "=" * 80)
        print("VIRTUAL FITTER TOOL - PHASE 2")
        print("=" * 80)
        print("Using Advanced Operations Research for Assembly Fitting")
        print("=" * 80)

        # Step 1: Parse
        self.parse_assembly()

        # Step 2: Solve
        solve_result = self.solve()

        # Step 3: Validate
        validation_result = self.validate_against_ground_truth()

        # Step 4: Identify missing
        missing_result = self.identify_missing_dimensions()

        # Summary
        print("\n" + "=" * 80)
        print("VIRTUAL FITTER: Summary")
        print("=" * 80)
        print(f"  Parts: {len(self.parts)}")
        print(f"  Dimensions: {len(self.dimensions)}")
        print(f"  Solver status: {solve_result['status']}")
        print(f"  Average error: {validation_result['avg_error_mm']:.3f} mm")
        print(f"  Missing constraints: {missing_result['missing']}")
        print("=" * 80)

        return {
            'parsing': {
                'parts_count': len(self.parts),
                'dimensions_count': len(self.dimensions)
            },
            'solving': solve_result,
            'validation': validation_result,
            'missing_dimensions': missing_result
        }


def main():
    """Test Virtual Fitter with ML-processed data."""
    import sys

    # Use command line argument or default to B18
    if len(sys.argv) > 1:
        input_file = sys.argv[1]
    else:
        input_file = 'ml_training_data_B18_view_centric.json'

    print(f"Loading assembly data from: {input_file}")
    try:
        with open(input_file, 'r', encoding='utf-8') as f:
            assembly_data = json.load(f)
    except FileNotFoundError:
        print(f"ERROR: {input_file} not found. Run phase1_postprocessor.py first.")
        return

    # Create and run Virtual Fitter
    fitter = VirtualFitter(assembly_data)
    results = fitter.run()

    # Save results
    output_file = 'virtual_fitter_results_B18.json'
    with open(output_file, 'w', encoding='utf-8') as f:
        json.dump(results, f, indent=2)

    print(f"\nResults saved to: {output_file}")


if __name__ == '__main__':
    main()
