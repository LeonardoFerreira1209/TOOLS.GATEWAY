namespace APPLICATION.DOMAIN.DTOS.CONFIGURATION;

/// <summary>
/// Classe responsavel por receber os dados do Appsettings.
/// </summary>
public class AppSettings
{
    public Auth Auth { get; set; }
}

/// <summary>
/// Classe de config de autenticação.
/// </summary>
public class Auth
{
    public string ValidIssuer { get; set; }
    public string ValidAudience { get; set; }
    public string SecurityKey { get; set; }
    public int ExpiresIn { get; set; }
    public Password Password { get; set; }
}

/// <summary>
/// Classe de config de senha.
/// </summary>
public class Password
{
    public int RequiredLength { get; set; }
}