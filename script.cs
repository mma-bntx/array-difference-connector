using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

public class Script : ScriptBase
{
    public override async Task<HttpResponseMessage> ExecuteAsync()
    {
        // Get the operationId to determine which action was called
        string operationId = this.Context.OperationId;
        
        // Handle potential base64 encoding issue in certain regions
        try
        {
            byte[] data = Convert.FromBase64String(operationId);
            operationId = System.Text.Encoding.UTF8.GetString(data);
        }
        catch (FormatException) { }
        
        // Route to appropriate handler based on operationId
        switch (operationId)
        {
            case "ArrayExceptStrings":
                return await HandleArrayExceptStrings().ConfigureAwait(false);
            
            case "ArrayExceptNumbers":
                return await HandleArrayExceptNumbers().ConfigureAwait(false);
            
            case "ArrayExceptObjects":
                return await HandleArrayExceptObjects().ConfigureAwait(false);
            
            default:
                // Handle unknown operation
                HttpResponseMessage errorResponse = new HttpResponseMessage(HttpStatusCode.BadRequest);
                errorResponse.Content = CreateJsonContent($"Unknown operation ID: '{operationId}'");
                return errorResponse;
        }
    }

    /// <summary>
    /// Handles the "Array Except - Strings" operation.
    /// Returns all elements from array1 that are not present in array2.
    /// </summary>
    private async Task<HttpResponseMessage> HandleArrayExceptStrings()
    {
        try
        {
            // Read and parse the incoming request body
            var contentAsString = await this.Context.Request.Content.ReadAsStringAsync()
                .ConfigureAwait(false);
            var contentAsJson = JObject.Parse(contentAsString);

            // Extract arrays from request and handle both direct arrays and string-encoded arrays
            var array1 = ParseArrayInput(contentAsJson["array1"]);
            var array2 = ParseArrayInput(contentAsJson["array2"]);

            // Validate arrays are not null
            if (array1 == null || array2 == null)
            {
                HttpResponseMessage badRequest = new HttpResponseMessage(HttpStatusCode.BadRequest);
                badRequest.Content = CreateJsonContent("Both 'array1' and 'array2' are required and must be valid arrays.");
                return badRequest;
            }

            // Convert array2 to HashSet for efficient lookup (O(1) per item)
            var excludeSet = new HashSet<string>();
            foreach (var item in array2)
            {
                excludeSet.Add(item.ToString());
            }

            // Filter array1, excluding items that exist in array2
            var resultArray = new JArray();
            foreach (var item in array1)
            {
                string itemStr = item.ToString();
                if (!excludeSet.Contains(itemStr))
                {
                    resultArray.Add(item);
                }
            }

            // Build response
            var result = new JObject
            {
                ["result"] = resultArray
            };

            HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.OK);
            response.Content = CreateJsonContent(result.ToString());
            return response;
        }
        catch (Exception ex)
        {
            HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.InternalServerError);
            response.Content = CreateJsonContent(new JObject
            {
                ["error"] = $"Error processing array except for strings: {ex.Message}"
            }.ToString());
            return response;
        }
    }

    /// <summary>
    /// Handles the "Array Except - Numbers" operation.
    /// Returns all elements from array1 that are not present in array2 (numeric comparison).
    /// </summary>
    private async Task<HttpResponseMessage> HandleArrayExceptNumbers()
    {
        try
        {
            // Read and parse the incoming request body
            var contentAsString = await this.Context.Request.Content.ReadAsStringAsync()
                .ConfigureAwait(false);
            var contentAsJson = JObject.Parse(contentAsString);

            // Extract arrays from request and handle both direct arrays and string-encoded arrays
            var array1 = ParseArrayInput(contentAsJson["array1"]);
            var array2 = ParseArrayInput(contentAsJson["array2"]);

            // Validate arrays are not null
            if (array1 == null || array2 == null)
            {
                HttpResponseMessage badRequest = new HttpResponseMessage(HttpStatusCode.BadRequest);
                badRequest.Content = CreateJsonContent("Both 'array1' and 'array2' are required and must be valid arrays.");
                return badRequest;
            }

            this.Context.Logger.LogInformation($"Number handler - array1 count: {array1.Count}, array2 count: {array2.Count}");
            this.Context.Logger.LogInformation($"Number handler - array1 items: {string.Join(",", array1.Select(x => x.ToString()))}");
            this.Context.Logger.LogInformation($"Number handler - array2 items: {string.Join(",", array2.Select(x => x.ToString()))}");

            // Convert array2 to HashSet<double> for efficient lookup
            var excludeSet = new HashSet<double>();
            foreach (var item in array2)
            {
                string itemStr = item.ToString();
                this.Context.Logger.LogInformation($"Trying to parse array2 item: '{itemStr}'");
                if (double.TryParse(itemStr, out var numValue))
                {
                    this.Context.Logger.LogInformation($"Successfully parsed '{itemStr}' as {numValue}");
                    excludeSet.Add(numValue);
                }
                else
                {
                    this.Context.Logger.LogInformation($"Warning: Could not parse '{itemStr}' as a number in array2");
                }
            }

            this.Context.Logger.LogInformation($"Exclude set contains {excludeSet.Count} items: {string.Join(",", excludeSet)}");

            // Filter array1, excluding items that exist in array2
            var resultArray = new JArray();
            foreach (var item in array1)
            {
                string itemStr = item.ToString();
                this.Context.Logger.LogInformation($"Checking array1 item: '{itemStr}'");
                if (double.TryParse(itemStr, out var numValue))
                {
                    this.Context.Logger.LogInformation($"Parsed as {numValue}, checking if in exclude set: {excludeSet.Contains(numValue)}");
                    if (!excludeSet.Contains(numValue))
                    {
                        resultArray.Add(item);
                    }
                }
                else
                {
                    this.Context.Logger.LogInformation($"Warning: Could not parse '{itemStr}' as a number in array1");
                }
            }

            // Build response
            var result = new JObject
            {
                ["result"] = resultArray
            };

            HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.OK);
            response.Content = CreateJsonContent(result.ToString());
            return response;
        }
        catch (Exception ex)
        {
            this.Context.Logger.LogInformation($"Exception in HandleArrayExceptNumbers: {ex.ToString()}");
            HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.InternalServerError);
            response.Content = CreateJsonContent(new JObject
            {
                ["error"] = $"Error processing array except for numbers: {ex.Message}"
            }.ToString());
            return response;
        }
    }

    /// <summary>
    /// Handles the "Array Except - Objects" operation.
    /// Returns all objects from array1 where the value of fieldNameA is not found in array2's fieldNameB values.
    /// </summary>
    private async Task<HttpResponseMessage> HandleArrayExceptObjects()
    {
        try
        {
            // Read and parse the incoming request body
            var contentAsString = await this.Context.Request.Content.ReadAsStringAsync()
                .ConfigureAwait(false);
            var contentAsJson = JObject.Parse(contentAsString);

            // Extract arrays and field names from request
            var array1 = ParseArrayInput(contentAsJson["array1"]);
            var array2 = ParseArrayInput(contentAsJson["array2"]);
            var fieldNameA = contentAsJson["fieldNameA"]?.Value<string>();
            var fieldNameB = contentAsJson["fieldNameB"]?.Value<string>();

            // Validate all inputs are present
            if (array1 == null || array2 == null || string.IsNullOrWhiteSpace(fieldNameA) || string.IsNullOrWhiteSpace(fieldNameB))
            {
                HttpResponseMessage badRequest = new HttpResponseMessage(HttpStatusCode.BadRequest);
                badRequest.Content = CreateJsonContent("All fields required: 'array1', 'array2', 'fieldNameA', and 'fieldNameB'");
                return badRequest;
            }

            this.Context.Logger.LogInformation($"Objects handler - array1 count: {array1.Count}, array2 count: {array2.Count}");
            this.Context.Logger.LogInformation($"Objects handler - comparing fieldNameA '{fieldNameA}' with fieldNameB '{fieldNameB}'");

            // Build HashSet from array2's fieldNameB values for efficient lookup
            var excludeSet = new HashSet<string>();
            foreach (var item in array2)
            {
                if (item is JObject jobj)
                {
                    var fieldValue = jobj[fieldNameB];
                    if (fieldValue != null)
                    {
                        excludeSet.Add(fieldValue.ToString());
                        this.Context.Logger.LogInformation($"Added to exclude set: '{fieldValue}'");
                    }
                    else
                    {
                        this.Context.Logger.LogInformation($"Warning: Field '{fieldNameB}' not found in array2 object");
                    }
                }
            }

            this.Context.Logger.LogInformation($"Exclude set contains {excludeSet.Count} items");

            // Filter array1: keep only objects where fieldNameA value is NOT in excludeSet
            var resultArray = new JArray();
            foreach (var item in array1)
            {
                if (item is JObject jobj)
                {
                    var fieldValue = jobj[fieldNameA];
                    if (fieldValue != null)
                    {
                        string fieldValueStr = fieldValue.ToString();
                        this.Context.Logger.LogInformation($"Checking object with fieldNameA value: '{fieldValueStr}'");
                        if (!excludeSet.Contains(fieldValueStr))
                        {
                            resultArray.Add(jobj);
                            this.Context.Logger.LogInformation($"  -> Included in result");
                        }
                        else
                        {
                            this.Context.Logger.LogInformation($"  -> Excluded from result (found in exclude set)");
                        }
                    }
                    else
                    {
                        this.Context.Logger.LogInformation($"Warning: Field '{fieldNameA}' not found in array1 object");
                    }
                }
            }

            // Build response
            var result = new JObject
            {
                ["result"] = resultArray
            };

            HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.OK);
            response.Content = CreateJsonContent(result.ToString());
            return response;
        }
        catch (Exception ex)
        {
            this.Context.Logger.LogInformation($"Exception in HandleArrayExceptObjects: {ex.ToString()}");
            HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.InternalServerError);
            response.Content = CreateJsonContent(new JObject
            {
                ["error"] = $"Error processing array except for objects: {ex.Message}"
            }.ToString());
            return response;
        }
    }

    /// <summary>
    /// Helper method to create JSON content for HTTP responses.
    /// </summary>
    private StringContent CreateJsonContent(string jsonString)
    {
        return new StringContent(jsonString, System.Text.Encoding.UTF8, "application/json");
    }

    /// <summary>
    /// Parses array input that may come as either a direct JSON array or as a string representation of an array.
    /// Handles cases where arrays are double-wrapped: [["a", "b"]] becomes ["a", "b"]
    /// </summary>
    private JArray ParseArrayInput(JToken token)
    {
        if (token == null)
            return null;

        // If it's a JArray, check if it contains a single string element that looks like an array
        if (token is JArray jarray)
        {
            // If the array has exactly one element and it's a string starting with [
            if (jarray.Count == 1 && jarray[0] is JValue jval && jval.Type == JTokenType.String)
            {
                string potentialArray = jval.Value<string>().Trim();
                if (potentialArray.StartsWith("[") && potentialArray.EndsWith("]"))
                {
                    try
                    {
                        // Parse the string as JSON array
                        var parsed = JToken.Parse(potentialArray);
                        if (parsed is JArray parsedArray)
                            return parsedArray;
                    }
                    catch
                    {
                        // If parsing fails, fall through to return the original array
                    }
                }
            }
            
            // Otherwise return the array as-is
            return jarray;
        }

        // If it's a string, try to parse it as JSON
        if (token is JValue jvalue && jvalue.Type == JTokenType.String)
        {
            string arrayString = jvalue.Value<string>().Trim();
            
            if (arrayString.StartsWith("[") && arrayString.EndsWith("]"))
            {
                try
                {
                    var parsed = JToken.Parse(arrayString);
                    if (parsed is JArray parsedArray)
                        return parsedArray;
                }
                catch
                {
                    // If parsing fails, treat as single element
                    return new JArray(arrayString);
                }
            }
            else
            {
                return new JArray(arrayString);
            }
        }

        return null;
    }
}
