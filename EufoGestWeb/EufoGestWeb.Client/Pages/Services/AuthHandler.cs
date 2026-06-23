//using EufoGestShared.DTO;
//using EufoGestWeb.Client.Services;
//using Microsoft.AspNetCore.Components;
//using System.Net;
//using System.Net.Http.Headers;
//using System.Net.Http.Json;

//namespace EufoGestWeb.Client.Pages.Services;

///// <summary>
///// Intercetta ogni risposta HTTP. Se riceve 401:
///// 1. Tenta il refresh chiamando /api/Auth/refresh (usa il cookie HttpOnly automaticamente)
///// 2. Se il refresh riesce, salva il nuovo access token e ripete la richiesta originale
///// 3. Se il refresh fallisce, esegue il logout e reindirizza al login
///// </summary>
//public class AuthHandler : DelegatingHandler
//{
//    private readonly AuthService _auth;
//    private readonly NavigationManager _nav;

    
//    public AuthHandler(AuthService auth, NavigationManager nav)
//    {
//        _auth = auth;
//        _nav = nav;
//    }

//    //protected override async Task<HttpResponseMessage> SendAsync(
//    //    HttpRequestMessage request, CancellationToken ct)
//    //{
//    //    var risposta = await base.SendAsync(request, ct);

//    //    if (risposta.StatusCode == HttpStatusCode.Unauthorized)
//    //    {
//    //        var refreshRisposta = await base.SendAsync(
//    //            new HttpRequestMessage(HttpMethod.Post, "api/Auth/refresh"), ct);

//    //        if (refreshRisposta.IsSuccessStatusCode)
//    //        {
//    //            var body = await refreshRisposta.Content
//    //                .ReadFromJsonAsync<LoginResponseDTO>(cancellationToken: ct);

//    //            if (body != null)
//    //            {
//    //                await _auth.SalvaTokenAsync(body.Token, body.Username, body.Ruolo);
//    //                request.Headers.Authorization =
//    //                    new AuthenticationHeaderValue("Bearer", body.Token);
//    //                risposta = await base.SendAsync(request, ct);
//    //            }
//    //        }
//    //        else
//    //        {
//    //            // Refresh fallito — sessione scaduta, logout forzato
//    //            await _auth.LogoutAsync();
//    //            _nav.NavigateTo("/login", replace: true);
//    //        }
//    //    }

//    //    return risposta;
//    //}

//    protected override async Task<HttpResponseMessage> SendAsync(
//        HttpRequestMessage request, CancellationToken ct)
//    {
//        // Esegue la richiesta originale
//        var risposta = await base.SendAsync(request, ct);

//        // Se non è 401 restituisce direttamente senza fare nulla
//        if (risposta.StatusCode != HttpStatusCode.Unauthorized)
//            return risposta;

//        // Evita loop infinito: non tenta il refresh se la richiesta fallita
//        // era già una chiamata a /refresh o /login
//        if (request.RequestUri?.PathAndQuery.Contains("/api/Auth/") == true)
//            return risposta;

//        // Tenta il refresh — il cookie eg_refresh viene inviato automaticamente
//        // dal browser perché SameSite=Strict e la richiesta va allo stesso dominio
//        var refreshRequest = new HttpRequestMessage(HttpMethod.Post, "api/Auth/refresh");
//        var refreshRisposta = await base.SendAsync(refreshRequest, ct);

//        if (!refreshRisposta.IsSuccessStatusCode)
//        {
//            // Refresh fallito - sessione definitivamente scaduta
//            await _auth.LogoutAsync();
//            _nav.NavigateTo("/login", replace: true);
//            return risposta; // restituisce il 401 originale
//        }

//        // Refresh riuscito - aggiorna il token in memoria e nell'header
//        var body = await refreshRisposta.Content
//            .ReadFromJsonAsync<LoginResponseDTO>(cancellationToken: ct);

//        if (body is null)
//        {
//            await _auth.LogoutAsync();
//            _nav.NavigateTo("/login", replace: true);
//            return risposta;
//        }

//        await _auth.SalvaTokenAsync(body.Token, body.Username, body.Ruolo);

//        // Ripete la richiesta originale con il nuovo token
//        // clona la richiesta perché HttpRequestMessage non è riutilizzabile
//        var nuovaRichiesta = await ClonaRichiestaAsync(request, body.Token);
//        return await base.SendAsync(nuovaRichiesta, ct);
//    }


//    private static async Task<HttpRequestMessage> ClonaRichiestaAsync(
//        HttpRequestMessage originale, string nuovoToken)
//    {
//        var copia = new HttpRequestMessage(originale.Method, originale.RequestUri);

//        // Copia gli header (escluso Authorization che verrà rimpiazzato)
//        foreach (var header in originale.Headers)
//        {
//            if (!header.Key.Equals("Authorization", StringComparison.OrdinalIgnoreCase))
//                copia.Headers.TryAddWithoutValidation(header.Key, header.Value);
//        }

//        copia.Headers.Authorization =
//            new AuthenticationHeaderValue("Bearer", nuovoToken);

//        // Copia il body se presente
//        if (originale.Content is not null)
//        {
//            var bodyBytes = await originale.Content.ReadAsByteArrayAsync();
//            copia.Content = new ByteArrayContent(bodyBytes);
//            foreach (var header in originale.Content.Headers)
//                copia.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
//        }

//        return copia;
//    }
//}
