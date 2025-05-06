using SAP.Middleware.Connector;

public class CustomDestinationConfig : IDestinationConfiguration
{
    public RfcConfigParameters GetParameters(string destinationName)
    {
        if ("SAP_DEST".Equals(destinationName))
        {
               RfcConfigParameters parameters = new RfcConfigParameters
            {
                { RfcConfigParameters.Name, "SAP_DEST" },
                { RfcConfigParameters.AppServerHost, "192.168.3.33" },
                { RfcConfigParameters.SystemNumber, "00" },
                { RfcConfigParameters.User, "Developer" },
                { RfcConfigParameters.Password, "Down1oad" },
                { RfcConfigParameters.Client, "001" },
                { RfcConfigParameters.Language, "EN" },
                { RfcConfigParameters.PoolSize, "5" },
                //{ RfcConfigParameters.MaxPoolSize, "10" }
            };
            return parameters;
        }
        return null;
    }

    public bool ChangeEventsSupported() => false;
    public event RfcDestinationManager.ConfigurationChangeHandler ConfigurationChanged;
}
