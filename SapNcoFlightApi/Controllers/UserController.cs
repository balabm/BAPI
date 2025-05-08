using Microsoft.AspNetCore.Mvc;
using SAP.Middleware.Connector;
using System.Collections.Generic;

[ApiController]
[Route("api/[controller]")]
public class UserController : ControllerBase
{
    private RfcDestination GetDestination() => RfcDestinationManager.GetDestination("SAP_DEST");

    [HttpPost]
    public IActionResult CreateUser([FromBody] CreateUserRequest request)
    {
        try
        {
            var dest = GetDestination();
            var repo = dest.Repository;

            var bapi = repo.CreateFunction("BAPI_USER_CREATE1");

            bapi.SetValue("USERNAME", request.Username.ToUpper());

            IRfcStructure logonData = bapi.GetStructure("LOGONDATA");
            logonData.SetValue("USTYP", "A"); // Dialog user
            logonData.SetValue("GLTGV", request.ValidFrom);
            logonData.SetValue("GLTGB", request.ValidTo);
            logonData.SetValue("CLASS", "DEFAULT");

            IRfcStructure password = bapi.GetStructure("PASSWORD");
            password.SetValue("BAPIPWD", request.Password);

            IRfcStructure address = bapi.GetStructure("ADDRESS");
            address.SetValue("FIRSTNAME", request.FirstName);
            address.SetValue("LASTNAME", request.LastName);
            address.SetValue("E_MAIL", request.Email);
            address.SetValue("DEPARTMENT", request.Department);

            bapi.Invoke(dest);

            // Commit transaction
            var commit = repo.CreateFunction("BAPI_TRANSACTION_COMMIT");
            commit.Invoke(dest);

            return Ok($"User {request.Username} created successfully.");
        }
        catch (RfcAbapException abapEx)
        {
            return BadRequest($"SAP Error: {abapEx.Message}");
        }
        catch (System.Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }

    [HttpGet("{username}")]
    public IActionResult GetUser(string username)
    {
        try
        {
            var dest = GetDestination();
            var repo = dest.Repository;

            var bapi = repo.CreateFunction("BAPI_USER_GET_DETAIL");
            bapi.SetValue("USERNAME", username.ToUpper());
            bapi.Invoke(dest);

            var address = bapi.GetStructure("ADDRESS");
            var logon = bapi.GetStructure("LOGONDATA");
            var roles = bapi.GetTable("ACTIVITYGROUPS");

            var response = new
            {
                Username = username,
                FirstName = address.GetString("FIRSTNAME"),
                LastName = address.GetString("LASTNAME"),
                Email = address.GetString("E_MAIL"),
                Department = address.GetString("DEPARTMENT"),
                UserType = logon.GetString("USTYP"),
                Roles = roles.Count > 0 ? string.Join(", ", roles) : "None"
            };

            return Ok(response);
        }
        catch (RfcAbapException abapEx)
        {
            return BadRequest($"SAP Error: {abapEx.Message}");
        }
        catch (System.Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }

    [HttpPut("{username}")]
    public IActionResult UpdateUser(string username, [FromBody] UpdateUserRequest request)
    {
        try
        {
            var dest = GetDestination();
            var repo = dest.Repository;

            var bapi = repo.CreateFunction("BAPI_USER_CHANGE");
            bapi.SetValue("USERNAME", username.ToUpper());

            IRfcStructure address = bapi.GetStructure("ADDRESS");
            address.SetValue("E_MAIL", request.Email);
            address.SetValue("DEPARTMENT", request.Department);

            IRfcStructure logon = bapi.GetStructure("LOGONDATA");
            logon.SetValue("GLTGV", request.ValidFrom);
            logon.SetValue("GLTGB", request.ValidTo);

            bapi.Invoke(dest);

            var commit = repo.CreateFunction("BAPI_TRANSACTION_COMMIT");
            commit.Invoke(dest);

            return Ok($"User {username} updated successfully.");
        }
        catch (RfcAbapException abapEx)
        {
            return BadRequest($"SAP Error: {abapEx.Message}");
        }
        catch (System.Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }

    [HttpDelete("{username}")]
    public IActionResult DeleteUser(string username)
    {
        try
        {
            var dest = GetDestination();
            var repo = dest.Repository;

            // Technically no standard BAPI for delete — simulate using system command
            var bapi = repo.CreateFunction("SUSR_USER_DELETE");
            bapi.SetValue("USER_NAME", username.ToUpper());
            bapi.Invoke(dest);

            var commit = repo.CreateFunction("BAPI_TRANSACTION_COMMIT");
            commit.Invoke(dest);

            return Ok($"User {username} deleted.");
        }
        catch (RfcAbapException abapEx)
        {
            return BadRequest($"SAP Error: {abapEx.Message}");
        }
        catch (System.Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }
}
