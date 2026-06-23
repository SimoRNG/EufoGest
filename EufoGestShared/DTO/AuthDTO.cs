namespace EufoGestShared.DTO;

public class LoginRequestDTO
{
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
}

public class LoginResponseDTO
{
    public string Token { get; set; } = "";
    public string Username { get; set; } = "";
    public string Ruolo { get; set; } = "";
    public DateTime Scadenza { get; set; }
}
