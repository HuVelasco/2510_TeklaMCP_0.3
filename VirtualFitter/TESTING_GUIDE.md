# Virtual Fitter - Testing Guide

## 🔨 Build Instructions

### Quick Build
```batch
cd VirtualFitter
build.bat
```

Or manually:
```batch
cd VirtualFitter
dotnet restore
dotnet build VirtualFitter.csproj -c Release
```

Output: `bin\Release\net48\VirtualFitter.exe`

---

## 🧪 Testing Workflow

### Prerequisites
1. Tekla Structures 2025 running
2. Drawing with assembly open (e.g., B18)
3. Assembly must have:
   - Main part (longest beam)
   - Secondary parts attached to main part

### Test Steps

#### 1. Launch Virtual Fitter
```
Double-click VirtualFitter\bin\Release\net48\VirtualFitter.exe
```

GUI window opens with 3 buttons.

---

#### 2. Click "1. Analyze Assembly"

**What It Does:**
- Identifies main part (longest beam)
- Calculates main part's local X axis (along beam)
- For each secondary part, computes offsets in 3 directions:
  1. **Along Main Axis (X)**: Distance along main part's length
  2. **Normal to Axis (Y)**: Perpendicular offset horizontally
  3. **Vertical (Z)**: Vertical offset

**Expected Output:**
```
=== ASSEMBLY ANALYSIS ===
Drawing: B18
Parts found: 15
Existing dimensions: 23
Available views: 3

=== REQUIRED DIMENSIONS ===
Missing dimensions: 12

Dimensions to create:
  - Along Main Axis (X): 1234.5mm [✓ Can show in FRONT]
  - Normal to Axis (Y): 567.8mm [✓ Can show in TOP]
  - Vertical (Z): 890.2mm [✓ Can show in FRONT]
  - Along Main Axis (X): 2345.6mm [✗ NO VIEW in None]
  ... and 8 more

⚠ 3 dimensions CANNOT be shown in available views!

=== VIEW ANALYSIS ===
Sufficient views: NO

MISSING VIEWS:
  - Top View (for horizontal positioning)
  - Front View (for vertical positioning and lengths)
```

**Verify:**
- ✓ Main part identified correctly
- ✓ Secondary parts found
- ✓ 3 dimensions per secondary part (X, Y, Z)
- ✓ Each dimension checked against available views
- ✓ Missing views suggested

---

#### 3. Add Missing Views (if suggested)

In Tekla:
1. Right-click drawing → "Create View"
2. Add suggested views (Top, Front, Side)
3. Return to Virtual Fitter

---

#### 4. Re-analyze (Click "1. Analyze Assembly" again)

**Expected:**
```
=== VIEW ANALYSIS ===
Sufficient views: YES

All dimensions can be shown!
```

---

#### 5. Click "2. Create Dimensions"

**Confirmation Dialog:**
```
This will create 12 dimensions in the drawing.

Created dimensions will be marked with [VF] prefix for easy identification.

Continue?
```

Click **Yes**.

**Expected Output:**
```
=== DIMENSION CREATION ===
Successfully created: 12
Failed: 0

Created 12 dimensions!
All created dimensions are prefixed with [VF] for easy identification.
Click 'Select Created Dims' to select them all in the drawing.
```

---

#### 6. Check in Tekla Drawing

You should see:
- **Red dimensions** (easy to spot)
- **Bold text**
- **[VF] marking** (via UserProperty "VirtualFitter=Created")
- Dimensions positioned correctly relative to parts

**Verify Each Type:**

1. **Along Main Axis (X) dimensions:**
   - Measure distance along main part's length
   - Shown in view where main axis is visible

2. **Normal to Axis (Y) dimensions:**
   - Measure perpendicular offset from main part
   - Shown in top/plan view

3. **Vertical (Z) dimensions:**
   - Measure vertical offset
   - Shown in elevation/front view

---

#### 7. Click "Select Created Dims" (Green Button)

**What It Does:**
- Selects ALL [VF] dimensions in Tekla
- You can now:
  - Move them
  - Adjust positions
  - Delete them all at once

**Expected:**
```
Selected 12 created dimensions in the drawing.

You can now modify, move, or delete them as needed.
```

**Verify in Tekla:**
- All red dimensions are selected
- Can modify as a group
- Can delete all at once

---

## 🎯 Expected Results Summary

### For Each Secondary Part:

| Dimension | Direction | Reference | View Requirement |
|-----------|-----------|-----------|------------------|
| Along Main Axis (X) | Main part's local X | Main part start | View perpendicular to main axis |
| Normal to Axis (