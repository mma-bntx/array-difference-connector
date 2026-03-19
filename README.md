# Array Operations Connector - Implementation Guide

## Understanding the Files

You have two files to work with:

### 1. **swagger.json** - OpenAPI Definition
Defines the REST API contract for the custom connector with three operations:

#### **Operation 1: ArrayExceptStrings**
- **Path**: `/array/except` (POST)
- **Inputs**:
  - `array1`: array of strings (the source array to filter from)
  - `array2`: array of strings (elements to exclude)
- **Output**: `result` (array of strings containing elements from array1 not in array2)

#### **Operation 2: ArrayExceptNumbers**
- **Path**: `/array/except-numbers` (POST)
- **Inputs**:
  - `array1`: array of numbers (the source array to filter from)
  - `array2`: array of numbers (elements to exclude)
- **Output**: `result` (array of numbers containing elements from array1 not in array2)

#### **Operation 3: ArrayExceptObjects**
- **Path**: `/array/except-objects` (POST)
- **Inputs**:
  - `array1`: array of objects (the source array to filter from)
  - `fieldNameA`: string (the field name in array1 to compare on, e.g., 'accountid')
  - `array2`: array of objects (elements to exclude)
  - `fieldNameB`: string (the field name in array2 to compare on, e.g., 'parentaccountid')
- **Output**: `result` (array of objects from array1 where the fieldNameA value is NOT found in any array2 fieldNameB values)

**swagger.json** defines three operations (**ArrayExceptStrings**, **ArrayExceptNumbers**, **ArrayExceptObjects**) with inputs, outputs, and error handling. You'll upload this to Power Automate as-is—no changes needed.

### 2. **script.cs** - C# Backend Implementation
Contains the C# code that powers all three operations. The script handles:
- **Strings**: Extracts and compares string arrays using a HashSet for fast lookups
- **Numbers**: Same logic with numeric double values
- **Objects**: Compares objects across two arrays using specified field names, returning full objects from Array A that don't match Array B

**Key feature**: Uses `HashSet<T>` for efficient O(n+m) performance—no code changes needed.

---

## Summary of Behavior

### String Action Example
```
Input:
  array1 = ["apple", "banana", "cherry", "date"]
  array2 = ["banana", "date"]

Output:
  result = ["apple", "cherry"]
```

### Numbers Action Example
```
Input:
  array1 = [1, 2, 3, 4, 5]
  array2 = [2, 4]

Output:
  result = [1, 3, 5]
```

  - *Note: Inputs are defined as string type in the swagger definition to handle Power Platform formatting quirks. The code parses strings to numbers internally — this is transparent to the user.*

### Objects Action Example
Find all **active accounts** that have **no associated opportunities**:

```json
Array A - Accounts:
[
  { "accountid": "001", "name": "TechCorp Inc", "status": "Active" },
  { "accountid": "002", "name": "RetailMax Ltd", "status": "Active" },
  { "accountid": "003", "name": "FinanceHub Bank", "status": "Active" }
]

Array B - Opportunities:
[
  { "opportunityid": "O100", "title": "Laptop Deal", "parentaccountid": "001" },
  { "opportunityid": "O101", "title": "Software License", "parentaccountid": "003" }
]

Parameters:
  fieldNameA = "accountid"
  fieldNameB = "parentaccountid"

Output - Accounts with NO opportunities:
[
  { "accountid": "002", "name": "RetailMax Ltd", "status": "Active" }
]
```

**Explanation**:
- **TechCorp (ID: 001)** → Excluded (has opportunity O100)
- **RetailMax (ID: 002)** → **Included** (no opportunities)
- **FinanceHub (ID: 003)** → Excluded (has opportunity O101)

---

## Installation Steps

### Step 1: Start a New Connector in Power Automate
1. Go to **Power Automate** → **Custom Connectors**
2. Click **Create new connector** → **From OpenAPI**
3. Upload the `swagger.json` file or paste its contents
4. Power Automate will auto-detect all three operations and show them in the preview

### Step 2: Add the C# Code
Navigate to the **Code** section (tab 4):  

1. **Enable code**: Click the toggle to enable the code editor
2. **Select functions**: From the dropdown menu, select all three functions:
   - `ArrayExceptStrings`
   - `ArrayExceptNumbers`
   - `ArrayExceptObjects`
3. **Replace the code**: 
   - Delete the default placeholder code
   - Copy the entire contents of `script.cs`
   - Paste it into the code editor
   - Power Automate will validate the syntax automatically

### Step 3: Create the Connector
1. Click the **"Create connector"** button
2. Wait for Power Automate to finalize and deploy the connector (this may take a few seconds)

### Step 4: Configure Security (Optional)
- For initial testing, leave security as **"No authentication"**
- You can upgrade to API Key, Basic Auth, or OAuth later

### Step 5: Test the Connector
1. Click **Test** to validate the connector
2. Test **Array Except - Strings**:
   - Sample input: `array1 = ["apple", "banana"]`, `array2 = ["banana"]`
   - Expected output: `result = ["apple"]`
3. Test **Array Except - Numbers**:
   - Sample input: `array1 = [1, 2, 3]`, `array2 = [2]`
   - Expected output: `result = [1, 3]`
4. Test **Array Except - Objects**:
   - Sample input: `array1 = [{"id": "001", "name": "Acme"}, {"id": "002", "name": "Globex"}]`, `fieldNameA = "id"`, `array2 = [{"ref_id": "001"}]`, `fieldNameB = "ref_id"`
   - Expected output: `result = [{"id": "002", "name": "Globex"}]`

### Step 6: Deploy & Use in Flows
1. Click **Save** to deploy the connector
2. Create a test flow:
   - Add the **"Array Except - Strings"**, **"Array Except - Numbers"**, or **"Array Except - Objects"** action
   - Map input arrays from dynamic content or static values
   - For the Objects action, specify the field names to compare on
   - Use the output `result` in subsequent actions

---

## Configuration Notes

### Data Type Support
- **Strings Action**: Compares values as case-sensitive strings
  - "Apple" ≠ "apple"
  - Works with any string values

- **Numbers Action**: Compares values as numeric doubles
  - Supports integers and decimal values (1.5, 2.5, etc.)
  - Non-numeric values in arrays are logged as warnings and skipped

- **Objects Action**: Compares specified fields in objects across two arrays
  - Uses automatic string conversion for all field values (numbers, strings, booleans all become strings for comparison)
  - **Cross-datatype matching**: Account ID `123` (number) will match ParentAccountID `"123"` (string)
  - Returns entire objects from array1, not just the field values
  - Both field names must exist in the respective arrays; missing fields are logged as warnings

### Behavior Details
- **Preserves Duplicates**: If `array1 = [1, 1, 2]` and `array2 = [3]`, result = [1, 1, 2]
- **Empty Array Handling**:
  - If `array1 = []`, result = []
  - If `array2 = []`, result = array1 (nothing is excluded)
- **Full Overlap**: If all elements of array1 are in array2, result = []
- **Objects with Missing Fields**: Objects where fieldNameA is not found are skipped (logged as warnings) and not included in the result

### Performance
- Uses `HashSet<T>` for O(1) lookups → O(n + m) overall complexity
- Efficient for large arrays

---

## Performance Comparison

The connector's HashSet-based approach significantly outperforms alternative Power Automate methods:

| Approach | 25 Objects Each | 100 Objects Each |
|----------|-----------------|------------------|
| **This Connector** | <1 second | <1 second |
| For Each + Filter Loop | 10 seconds | 47 seconds |
| Double Loop (nested) | ~3 minutes 15 seconds | 1 hour 1 minute |

*At 13k vs 8k objects, the connector still completed in **less than 1 second**.

**Why the difference?**
- **Connector (efficient)**: Uses a HashSet to store excluded values, enabling instant O(1) lookups for each comparison
- **Filter Loop (moderate)**: Evaluates a filter condition for each item — O(n+m) with some (slow) API overhead per iteration.
- **Double Loop (naive)**: Compares every item in A against a reduced set of items in a temporary array containing the common items between A and B (using 'intersection(A,B)'). Worst case is O(n²) operations

**Bottom line**: For arrays over 100-200 objects, the connector is not just faster—it's orders of magnitude faster and reduces flow execution time significantly.

> *Note: These results are from informal testing in a specific environment. Actual performance may vary depending on your Power Automate region, plan, and current platform load.*

---

## Troubleshooting

### If Code Doesn't Compile in Power Automate
- Ensure all `using` statements are present at the top
- Check that `ScriptBase` class is recognized (provided by Power Automate runtime)
- Verify no typos in method names: `ExecuteAsync`, `HandleArrayExceptStrings`, `HandleArrayExceptNumbers`, `HandleArrayExceptObjects`

### If Actions Don't Appear in Power Automate
- Verify `operationId` in swagger.json matches exactly: `ArrayExceptStrings`, `ArrayExceptNumbers`, and `ArrayExceptObjects`
- Ensure the OpenAPI definition has no syntax errors
- Refresh the custom connector or reload Power Automate

### If Tests Return Errors
- Check the error message in the response (includes exception details)
- Verify both `array1` and `array2` are provided in the request
- Ensure array values are correctly typed (strings or numbers, not mixed)

### If Objects Action Returns Empty or Unexpected Results
- Verify `fieldNameA` and `fieldNameB` are spelled exactly as they appear in your data (case-sensitive)
- Ensure the arrays contain actual JSON objects, not strings or primitive values
- Check that the field exists on the objects — missing fields are silently skipped and won't appear in the result
- If comparing across different data sources, remember that values are matched as strings (e.g., `123` matches `"123"`)

---

## Files Location
```
Repository
├── swagger.json         (OpenAPI definition)
├── script.cs            (C# implementation)
└── README.md            (This file - implementation guide)
```

---

**Ready to deploy!** Follow the "Installation Steps" section above to create the connector in Power Automate.

---

## License
This project is licensed under the [MIT License](LICENSE).