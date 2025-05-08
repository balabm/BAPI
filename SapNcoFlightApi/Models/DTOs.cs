public class CreateUserRequest
{
    public string Username { get; set; }
    public string Password { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Email { get; set; }
    public string Department { get; set; }
    public string ValidFrom { get; set; }  // Format: YYYYMMDD
    public string ValidTo { get; set; }    // Format: YYYYMMDD
}

public class UpdateUserRequest
{
    public string Email { get; set; }
    public string Department { get; set; }
    public string ValidFrom { get; set; }
    public string ValidTo { get; set; }
}
