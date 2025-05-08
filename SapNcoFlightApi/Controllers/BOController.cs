using Microsoft.AspNetCore.Mvc;
using SAP.Middleware.Connector;
using System;
using System.Collections.Generic;
using System.Linq; // Required for .ToList() if using LINQ

// --- Model Definition ---
// This class defines the structure of the data you send in the request body.
// It includes fields present in the BAPI_EPM_BP_HEADER structure based on your screenshots.
// Note: FIRST_NAME and LAST_NAME are intentionally excluded as they are not in your system's version.
public class BusinessPartnerRequest
{
    // Fields available in BAPI_EPM_BP_HEADER
    public string? BpRole { get; set; } // Maps to BP_ROLE
    public string? Email { get; set; } // Maps to EMAIL_ADDRESS
    public string? Phone { get; set; } // Maps to PHONE_NUMBER
    public string? Fax { get; set; } // Maps to FAX_NUMBER
    public string? WebAddress { get; set; } // Maps to WEB_ADDRESS
    public string? Company { get; set; } // Maps to COMPANY_NAME
    public string? LegalForm { get; set; } // Maps to LEGAL_FORM
    public string? Currency { get; set; } // Maps to CURRENCY_CODE
    public string? City { get; set; } // Maps to CITY
    public string? PostalCode { get; set; } // Maps to POSTAL_CODE
    public string? Street { get; set; } // Maps to STREET
    public string? Building { get; set; } // Maps to BUILDING
    public string? Country { get; set; } // Maps to COUNTRY
    public string? AddressType { get; set; } // Maps to ADDRESS_TYPE

    // Add other fields from BAPI_EPM_BP_HEADER as needed
}

// --- Controller Definition ---
[ApiController]
[Route("api/[controller]")]
public class BOController : ControllerBase
{
    // Method to get the SAP RFC Destination
    // Ensure "SAP_DEST" is correctly configured in your NCo connection file (sapnco.config)
    private RfcDestination GetDestination()
    {
        try
        {
            // RfcDestinationManager is thread-safe
            return RfcDestinationManager.GetDestination("SAP_DEST");
        }
        catch (Exception ex)
        {
            // In a real application, use a proper logging framework
            Console.WriteLine($"Error getting SAP destination 'SAP_DEST': {ex.Message}");
            throw new Exception("Failed to connect to SAP. Check SAP destination configuration.", ex);
        }
    }

    // Helper to process BAPI RETURN table messages
    private List<string> ProcessReturnMessages(IRfcTable returnTable, out bool hasError)
    {
        hasError = false;
        var messages = new List<string>();

        if (returnTable == null) return messages;

        foreach (IRfcStructure returnMsg in returnTable)
        {
            string messageType = returnMsg.GetString("TYPE");
            string messageText = returnMsg.GetString("MESSAGE");
            string messageId = returnMsg.GetString("ID");
            string messageNumber = returnMsg.GetString("NUMBER");
            // You can also get MESSAGE_V1, V2, V3, V4 for variables if they contain useful info
            // string messageV1 = returnMsg.GetString("MESSAGE_V1");

            string fullMessage = $"Type: {messageType}, ID: {messageId}, Number: {messageNumber}, Message: {messageText}";
            messages.Add(fullMessage);

            if (messageType == "E" || messageType == "A") // Error or Abort
            {
                hasError = true;
            }
        }
        return messages;
    }

    [HttpPost]
    public IActionResult CreateBusinessPartner([FromBody] BusinessPartnerRequest request)
    {
        try
        {
            var dest = GetDestination();
            var repo = dest.Repository;

            // Create the BAPI function object for creation
            var bapi = repo.CreateFunction("BAPI_EPM_BP_CREATE");

            // Get the HEADERDATA structure (confirmed from SE37)
            IRfcStructure bpData = bapi.GetStructure("HEADERDATA");

            // Set the data fields within the HEADERDATA structure from the request
            // Use the exact field names from the BAPI_EPM_BP_HEADER structure (from SE11 screenshots)
            // Only include fields present in your system's BAPI_EPM_BP_HEADER
            if (!string.IsNullOrEmpty(request.BpRole))
                bpData.SetValue("BP_ROLE", request.BpRole);
            if (!string.IsNullOrEmpty(request.Email))
                bpData.SetValue("EMAIL_ADDRESS", request.Email);
            if (!string.IsNullOrEmpty(request.Phone))
                bpData.SetValue("PHONE_NUMBER", request.Phone);
            if (!string.IsNullOrEmpty(request.Fax))
                bpData.SetValue("FAX_NUMBER", request.Fax);
            if (!string.IsNullOrEmpty(request.WebAddress))
                bpData.SetValue("WEB_ADDRESS", request.WebAddress);
            if (!string.IsNullOrEmpty(request.Company))
                bpData.SetValue("COMPANY_NAME", request.Company);
            if (!string.IsNullOrEmpty(request.LegalForm))
                bpData.SetValue("LEGAL_FORM", request.LegalForm);
            if (!string.IsNullOrEmpty(request.Currency))
                bpData.SetValue("CURRENCY_CODE", request.Currency);
            if (!string.IsNullOrEmpty(request.City))
                bpData.SetValue("CITY", request.City);
            if (!string.IsNullOrEmpty(request.PostalCode))
                bpData.SetValue("POSTAL_CODE", request.PostalCode);
            if (!string.IsNullOrEmpty(request.Street))
                bpData.SetValue("STREET", request.Street);
            if (!string.IsNullOrEmpty(request.Building))
                bpData.SetValue("BUILDING", request.Building);
            if (!string.IsNullOrEmpty(request.Country))
                bpData.SetValue("COUNTRY", request.Country);
            if (!string.IsNullOrEmpty(request.AddressType))
                bpData.SetValue("ADDRESS_TYPE", request.AddressType);
            // Note: FIRST_NAME and LAST_NAME are intentionally NOT included

            // Additional parameters if required by BAPI_EPM_BP_CREATE (e.g., PERSIST_TO_DB)
            // bapi.SetValue("PERSIST_TO_DB", "X"); // Or "ABAP_TRUE"

            // Invoke the BAPI in SAP
            bapi.Invoke(dest);

            // --- Code to extract the created BP ID ---
            string createdBpId = "";
            try
            {
                // Get the value from the Export parameter named BUSINESSPARTNERID (confirmed from SE37 Export tab)
                createdBpId = bapi.GetString("BUSINESSPARTNERID");
            }
            catch (RfcInvalidParameterException paramEx)
            {
                 // This might happen if "BUSINESSPARTNERID" is not actually an export parameter in this system
                 Console.WriteLine($"RfcInvalidParameterException getting BUSINESSPARTNERID: {paramEx.Message}.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error extracting created BP ID: {ex.Message}");
            }
            // --- End of code to extract BP ID ---


            // Process RETURN table messages
            IRfcTable returnTable = bapi.GetTable("RETURN");
            bool hasError;
            var messages = ProcessReturnMessages(returnTable, out hasError);

            if (hasError)
            {
                // If BAPI returned errors, rollback the transaction
                var rollback = repo.CreateFunction("BAPI_TRANSACTION_ROLLBACK");
                rollback.Invoke(dest);
                // Log or return all messages for debugging
                Console.WriteLine($"SAP Error during Create. Messages: {string.Join("; ", messages)}");
                return BadRequest($"SAP Error during Create. Messages: {string.Join("; ", messages)}");
            }
            else
            {
                // If no errors, commit the transaction
                // BAPIs usually require an explicit COMMIT WORK to make changes permanent
                var commit = repo.CreateFunction("BAPI_TRANSACTION_COMMIT");
                commit.Invoke(dest);

                // Return success message including the extracted ID
                Console.WriteLine($"Business Partner created successfully. Messages: {string.Join("; ", messages)}. Created BP ID: {createdBpId}");
                return Ok($"Business Partner created successfully. Messages: {string.Join("; ", messages)}. Created BP ID: {createdBpId}");
            }
        }
        catch (RfcAbapException abapEx)
        {
            // Catches exceptions from ABAP runtime (e.g., communication failure before BAPI execution)
            Console.WriteLine($"RfcAbapException during Create: {abapEx.Message}");
            return BadRequest($"SAP ABAP Runtime Error during Create: {abapEx.Message}");
        }
        catch (Exception ex)
        {
            // Catch other general exceptions (e.g., .NET Connector configuration, network issues)
            Console.WriteLine($"General Exception during Create: {ex.Message}");
            return StatusCode(500, $"An unexpected error occurred during Create: {ex.Message}");
        }
    }

    [HttpGet("{bpId}")]
    public IActionResult GetBusinessPartner(string bpId)
    {
        try
        {
            var dest = GetDestination();
            var repo = dest.Repository;

            // Create the BAPI function object for getting details
            var bapi = repo.CreateFunction("BAPI_EPM_BP_GET_DETAIL");

            // Set the Business Partner ID parameter (confirmed from SE37 screenshot)
            bapi.SetValue("BP_ID", bpId);

            // Invoke the BAPI
            bapi.Invoke(dest);

            // Process RETURN table messages
             IRfcTable returnTable = bapi.GetTable("RETURN");
             bool hasError;
             var messages = ProcessReturnMessages(returnTable, out hasError);

             if (hasError)
             {
                 Console.WriteLine($"SAP Error during Get. Messages: {string.Join("; ", messages)}");
                 return BadRequest($"SAP Error during Get. Messages: {string.Join("; ", messages)}");
             }

            // Get the HEADERDATA structure from the export parameters (likely correct, verify in SE37)
            // Check the Export tab of BAPI_EPM_BP_GET_DETAIL in SE37 for the exact parameter name
            IRfcStructure bpData = bapi.GetStructure("HEADERDATA");

            // Extract data from the HEADERDATA structure
            // Only include fields present in your system's BAPI_EPM_BP_HEADER
            var response = new
            {
                Id = bpId, // The requested ID
                BpRole = bpData.GetString("BP_ROLE"),
                Email = bpData.GetString("EMAIL_ADDRESS"),
                Phone = bpData.GetString("PHONE_NUMBER"),
                Fax = bpData.GetString("FAX_NUMBER"),
                WebAddress = bpData.GetString("WEB_ADDRESS"),
                Company = bpData.GetString("COMPANY_NAME"),
                LegalForm = bpData.GetString("LEGAL_FORM"),
                Currency = bpData.GetString("CURRENCY_CODE"),
                City = bpData.GetString("CITY"),
                PostalCode = bpData.GetString("POSTAL_CODE"),
                Street = bpData.GetString("STREET"),
                Building = bpData.GetString("BUILDING"),
                Country = bpData.GetString("COUNTRY"),
                AddressType = bpData.GetString("ADDRESS_TYPE")
                 // Note: FIRST_NAME and LAST_NAME are intentionally NOT included
                // Add other available fields as needed
            };

            // Note: GET BAPIs typically do not require COMMIT WORK

            Console.WriteLine($"Business Partner {bpId} retrieved successfully. Messages: {string.Join("; ", messages)}");
            return Ok(response);
        }
        catch (RfcAbapException abapEx)
        {
             Console.WriteLine($"RfcAbapException during Get: {abapEx.Message}");
            return BadRequest($"SAP ABAP Runtime Error during Get: {abapEx.Message}");
        }
        catch (Exception ex)
        {
             Console.WriteLine($"General Exception during Get: {ex.Message}");
            return StatusCode(500, $"An unexpected error occurred during Get: {ex.Message}");
        }
    }

    [HttpPut("{bpId}")]
    public IActionResult UpdateBusinessPartner(string bpId, [FromBody] BusinessPartnerRequest request)
    {
        try
        {
            var dest = GetDestination();
            var repo = dest.Repository;

            // Create the BAPI function object for changing
            var bapi = repo.CreateFunction("BAPI_EPM_BP_CHANGE");

            // Set the Business Partner ID parameter (confirmed from SE37 screenshot)
            bapi.SetValue("BP_ID", bpId);

            // Get the HEADERDATA structure for the data and HEADERDATAX for update flags
            IRfcStructure bpData = bapi.GetStructure("HEADERDATA");
            IRfcStructure bpDataX = bapi.GetStructure("HEADERDATAX"); // Confirmed from SE37 screenshot of BAPI_EPM_BP_CHANGE

            // Set data fields only if provided in the request and set the corresponding flag in HEADERDATAX
            // Use the exact field names from the BAPI_EPM_BP_HEADER structure
            // Only include fields present in your system's BAPI_EPM_BP_HEADER
            // Set value in bpData and 'X' in bpDataX for each field you want to update
            if (!string.IsNullOrEmpty(request.BpRole))
            {
                bpData.SetValue("BP_ROLE", request.BpRole);
                bpDataX.SetValue("BP_ROLE", "X");
            }
            if (!string.IsNullOrEmpty(request.Email))
            {
                bpData.SetValue("EMAIL_ADDRESS", request.Email);
                bpDataX.SetValue("EMAIL_ADDRESS", "X");
            }
            if (!string.IsNullOrEmpty(request.Phone))
            {
                bpData.SetValue("PHONE_NUMBER", request.Phone);
                 bpDataX.SetValue("PHONE_NUMBER", "X");
            }
             if (!string.IsNullOrEmpty(request.Fax))
            {
                bpData.SetValue("FAX_NUMBER", request.Fax);
                 bpDataX.SetValue("FAX_NUMBER", "X");
            }
             if (!string.IsNullOrEmpty(request.WebAddress))
            {
                bpData.SetValue("WEB_ADDRESS", request.WebAddress);
                 bpDataX.SetValue("WEB_ADDRESS", "X");
            }
            if (!string.IsNullOrEmpty(request.Company))
            {
                bpData.SetValue("COMPANY_NAME", request.Company);
                bpDataX.SetValue("COMPANY_NAME", "X");
            }
             if (!string.IsNullOrEmpty(request.LegalForm))
            {
                bpData.SetValue("LEGAL_FORM", request.LegalForm);
                 bpDataX.SetValue("LEGAL_FORM", "X");
            }
             if (!string.IsNullOrEmpty(request.Currency))
            {
                bpData.SetValue("CURRENCY_CODE", request.Currency);
                 bpDataX.SetValue("CURRENCY_CODE", "X");
            }
             if (!string.IsNullOrEmpty(request.City))
            {
                bpData.SetValue("CITY", request.City);
                 bpDataX.SetValue("CITY", "X");
            }
             if (!string.IsNullOrEmpty(request.PostalCode))
            {
                bpData.SetValue("POSTAL_CODE", request.PostalCode);
                 bpDataX.SetValue("POSTAL_CODE", "X");
            }
             if (!string.IsNullOrEmpty(request.Street))
            {
                bpData.SetValue("STREET", request.Street);
                 bpDataX.SetValue("STREET", "X");
            }
             if (!string.IsNullOrEmpty(request.Building))
            {
                bpData.SetValue("BUILDING", request.Building);
                 bpDataX.SetValue("BUILDING", "X");
            }
             if (!string.IsNullOrEmpty(request.Country))
            {
                bpData.SetValue("COUNTRY", request.Country);
                 bpDataX.SetValue("COUNTRY", "X");
            }
             if (!string.IsNullOrEmpty(request.AddressType))
            {
                bpData.SetValue("ADDRESS_TYPE", request.AddressType);
                 bpDataX.SetValue("ADDRESS_TYPE", "X");
            }
            // Note: FIRST_NAME and LAST_NAME are intentionally NOT included here

            // Invoke the BAPI
            bapi.Invoke(dest);

            // Process RETURN table messages
            IRfcTable returnTable = bapi.GetTable("RETURN");
            bool hasError;
            var messages = ProcessReturnMessages(returnTable, out hasError);

            if (hasError)
            {
                // If BAPI returned errors, rollback the transaction
                var rollback = repo.CreateFunction("BAPI_TRANSACTION_ROLLBACK");
                rollback.Invoke(dest);
                 Console.WriteLine($"SAP Error during Update. Messages: {string.Join("; ", messages)}");
                return BadRequest($"SAP Error during Update. Messages: {string.Join("; ", messages)}");
            }
            else
            {
                // If no errors, commit the transaction
                var commit = repo.CreateFunction("BAPI_TRANSACTION_COMMIT");
                commit.Invoke(dest);
                 Console.WriteLine($"Business Partner {bpId} updated successfully. Messages: {string.Join("; ", messages)}");
                return Ok($"Business Partner {bpId} updated successfully. Messages: {string.Join("; ", messages)}");
            }
        }
        catch (RfcAbapException abapEx)
        {
             Console.WriteLine($"RfcAbapException during Update: {abapEx.Message}");
            return BadRequest($"SAP ABAP Runtime Error during Update: {abapEx.Message}");
        }
        catch (Exception ex)
        {
             Console.WriteLine($"General Exception during Update: {ex.Message}");
            return StatusCode(500, $"An unexpected error occurred during Update: {ex.Message}");
        }
    }

     [HttpDelete("{bpId}")]
    public IActionResult DeleteBusinessPartner(string bpId)
    {
        try
        {
            var dest = GetDestination();
            var repo = dest.Repository;

            // Create the BAPI function object for deletion
            var bapi = repo.CreateFunction("BAPI_EPM_BP_DELETE");

            // Set the Business Partner ID parameter (likely correct, verify in SE37)
            bapi.SetValue("BP_ID", bpId);

            // Invoke the BAPI
            bapi.Invoke(dest);

            // Process RETURN table messages
            IRfcTable returnTable = bapi.GetTable("RETURN");
            bool hasError;
            var messages = ProcessReturnMessages(returnTable, out hasError);

            if (hasError)
            {
                // If BAPI returned errors, rollback the transaction (DELETE BAPIs might behave differently, verify)
                 var rollback = repo.CreateFunction("BAPI_TRANSACTION_ROLLBACK");
                 rollback.Invoke(dest);
                 Console.WriteLine($"SAP Error during Delete. Messages: {string.Join("; ", messages)}");
                return BadRequest($"SAP Error during Delete. Messages: {string.Join("; ", messages)}");
            }
            else
            {
                 // If no errors, commit the transaction (DELETE BAPIs usually require COMMIT)
                 var commit = repo.CreateFunction("BAPI_TRANSACTION_COMMIT");
                 commit.Invoke(dest);
                 Console.WriteLine($"Business Partner {bpId} deleted successfully. Messages: {string.Join("; ", messages)}");
                return Ok($"Business Partner {bpId} deleted successfully. Messages: {string.Join("; ", messages)}");
            }
        }
         catch (RfcAbapException abapEx)
        {
             Console.WriteLine($"RfcAbapException during Delete: {abapEx.Message}");
            return BadRequest($"SAP ABAP Runtime Error during Delete: {abapEx.Message}");
        }
        catch (System.Exception ex)
        {
             Console.WriteLine($"General Exception during Delete: {ex.Message}");
            return StatusCode(500, $"An unexpected error occurred during Delete: {ex.Message}");
        }
    }
}