using Microsoft.AspNetCore.Mvc;
using SAP.Middleware.Connector;

[ApiController]
[Route("api/[controller]")]
public class FlightController : ControllerBase
{
    [HttpGet("flights")]
    public IActionResult GetFlights()
    {
        try
        {
            var dest = RfcDestinationManager.GetDestination("SAP_DEST");
            var repo = dest.Repository;
            var bapi = repo.CreateFunction("BAPI_FLIGHT_GETLIST");

            bapi.SetValue("AIRLINE", "LH"); // Lufthansa as example
            bapi.Invoke(dest);

            var flights = bapi.GetTable("FLIGHT_LIST");
            var result = new List<object>();

            foreach (var row in flights)
            {
                result.Add(new
                {
                    Airline = row.GetString("AIRLINE"),
                    Destination = row.GetString("CITYTO")
                });
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }
}
