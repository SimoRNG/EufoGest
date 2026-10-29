Progetto personale di studio per un gestionale per una scuola di lingue. Il focus di studio era il back end, front end delegato in modo sostanziale all'AI

# Obiettivi
- Servizi REST e MySQL: Scrittura di servizi REST e query MySQL
- Sicurezza: Autenticazione JWT ed altri concetti correlati

# Schema Db
<img width="1916" height="871" alt="EufoGest" src="https://github.com/user-attachments/assets/fef3223b-6e62-4f34-8f85-38351053d564" />
Nello schema non sono presenti le tabelle che riguardano la parte di sicurezza

# Stato attuale
- Gestione anagrafiche clienti e professori
- Inserimento e gestione delle lezioni

# Configurazione
- I progetti di avvio sono EufoGestREST e EufoGestWebApp
- Necessari .env preciso (parametri di connessione e chiavi JWT) e database MariaDB MySQL
- Nel progetto EufoGestREST bisogna avere un file secrets.json del tipo:
```json
  {
  "ConnectionStrings": {
    "ServerDati": "localhost",
    "PortDati": "3306",
    "DbNameDati": "nomeDb",
    "DbUserDati": "utenteDb",
    "PasswordDati": "passDb"

  },

  "Jwt": {
    "Key": "chiaveLungaAlmeno32caratteriMoltoSicura",
    "Issuer": "EufoGestREST",
    "Audience": "EufoGestClients",
    "ExpiresInMinutes": 10
  }

  }
```

appsettings.Development.json:
```json
{
  "AllowedOrigins": [ "https://localhost:7266" ]
}
```

La cartella properties deve contenere il file launchSettings.json del tipo:

  ```json
  {
    "$schema": "https://json.schemastore.org/launchsettings.json",
    "profiles": {
      
      "https": {
        "commandName": "Project",
        "dotnetRunMessages": true,
        "launchBrowser": false,
        "applicationUrl": "https://localhost:7004",
        "environmentVariables": {
          "ASPNETCORE_ENVIRONMENT": "Development"
        }
      }
    }
  }
  ```

