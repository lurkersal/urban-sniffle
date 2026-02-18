# Measurements Validation - Bust+Cup Only Format Support

## Issue Fixed
When only a model's bust and cup size are known (e.g., "36B"), the measurements field was showing an error. This should be valid since waist and hip measurements are not always available.

## Previous Behavior
The `MeasurementsValidator.TryParseMeasurements` method required all three measurements:
- **Required format:** `bust-waist-hip` (e.g., "36B-28-38")
- **Rejected:** "36B" with error "Expected three parts separated by '-'"

This was problematic when only partial information was available from sources.

## New Behavior
The validator now accepts two valid formats:
1. **Bust+cup only:** e.g., "36B", "34C", "36DD", or even "36" (bust without cup)
2. **Full measurements:** e.g., "36B-28-38" (unchanged)

**Invalid:** Two-part measurements like "36B-28" are still rejected (must provide all three or just bust)

## Implementation

**File:** `/home/justin/repos/urban-sniffle/src/common/Shared/MeasurementsValidator.cs`

### Changes Made

#### 1. Updated Format Hint
```csharp
// Before:
public static string FormatHint => "Format: bust(+cup)-waist-hip (integers only) — e.g. '36B-28-38' (separator must be '-')";

// After:
public static string FormatHint => "Format: bust(+cup) OR bust(+cup)-waist-hip (integers only) — e.g. '36B' or '36B-28-38' (separator must be '-')";
```

#### 2. Modified Validation Logic

**Parts validation:**
```csharp
// Allow either:
// - 1 part: bust+cup only (e.g., "36B")
// - 3 parts: bust-waist-hip (e.g., "36B-28-38")
if (parts.Length < 1)
{
    error = $"Expected measurements. {FormatHint}";
    return false;
}

bool bustOnly = parts.Length == 1;
if (parts.Length != 1 && parts.Length < 3)
{
    error = $"Expected either bust+cup only OR all three measurements (bust-waist-hip). Example: '36B' or '36B-28-38'. {FormatHint}";
    return false;
}
```

**Bust-only mode:**
```csharp
// If bust-only mode, we're done - waist and hip remain 0
if (bustOnly)
{
    // Only validate bust is plausible
    if (!IsPlausible(bustValue))
    {
        error = "Bust measurement is outside plausible range";
        return false;
    }
    return true;
}

// Otherwise, parse waist and hip as before...
```

### Output Parameters

When parsing bust-only format (e.g., "36B"):
- `bustValue` = 36
- `bustCup` = "B"
- `waistValue` = 0
- `hipValue` = 0
- `error` = null
- Returns `true`

When parsing full format (e.g., "36B-28-38"):
- `bustValue` = 36
- `bustCup` = "B"
- `waistValue` = 28
- `hipValue` = 38
- `error` = null
- Returns `true`

## Valid Examples

✅ **Bust-only formats:**
- `"36B"` - bust with cup
- `"34C"` - bust with cup
- `"36DD"` - bust with two-letter cup
- `"36"` - bust without cup (cup size unknown)

✅ **Full formats (unchanged):**
- `"36B-28-38"` - full measurements with cup
- `"36-28-38"` - full measurements without cup
- `"34C - 22 - 34"` - spaces allowed
- `"34C–22–34"` - en-dash normalized

❌ **Invalid formats:**
- `""` - empty
- `"36B-28"` - two parts (must be 1 or 3)
- `"36B/28/38"` - wrong separator
- `"5-4-3"` - implausible values

## Use Cases

This change supports real-world scenarios where:
1. **Historical data** - Old magazine records may only list bust/cup
2. **Partial information** - Source material incomplete
3. **Privacy** - Model chooses to only disclose bust size
4. **Data entry** - User doesn't have full measurements yet

## Testing

**File:** `/home/justin/repos/urban-sniffle/src/common/Tests/MeasurementsValidatorTests.cs`

Added test cases for bust-only format:
```csharp
[InlineData("36B")] // bust+cup only should be valid
[InlineData("34C")] // bust+cup only should be valid
[InlineData("36DD")] // bust+cup only with two-letter cup should be valid
[InlineData("36")] // bust only (no cup) should be valid
```

## UI Impact

In the index-editor:
- **Before:** Entering "36B" in Measurements field → Red border, error message
- **After:** Entering "36B" in Measurements field → Accepted, no error

The error message format hint now shows users they can enter either format.

## Backward Compatibility

✅ **Fully backward compatible**
- All previously valid formats still work
- Full measurements (bust-waist-hip) still validated with same rules
- Only adds support for new bust-only format
- No breaking changes to existing code

## Build Status
```
Build succeeded.
    0 Warning(s)
    0 Error(s)
```

## Date Implemented
February 17, 2026

