using System.Net.Http.Json;

namespace NukeShare.CLI.Infrastructure;

public class StatusRestApi(HttpClient http)
{
    public async Task<StatusDTO?> GetStatusAsync(CancellationToken ct = default)
    {
        try
        {
            return await http.GetFromJsonAsync<StatusDTO>("v1/status", ct);
        }
        catch (HttpRequestException)
        {
            return null;
        }
    }

    public async Task<HealthDTO?> GetHealthAsync(CancellationToken ct = default)
    {
        try
        {
            return await http.GetFromJsonAsync<HealthDTO>("v1/status/health", ct);
        }
        catch (HttpRequestException)
        {
            return null;
        }
    }

    public async Task<PeersDTO?> GetPeersAsync(CancellationToken ct = default)
    {
        try
        {
            return await http.GetFromJsonAsync<PeersDTO>("v1/status/peers", ct);
        }
        catch (HttpRequestException)
        {
            return null;
        }
    }

    public async Task<TransfersDTO?> GetTransfersAsync(CancellationToken ct = default)
    {
        try
        {
            return await http.GetFromJsonAsync<TransfersDTO>("v1/status/transfers", ct);
        }
        catch (HttpRequestException)
        {
            return null;
        }
    }

    public async Task<ShutdownDTO?> ShutdownAsync(CancellationToken ct = default)
    {
        try
        {
            var response = await http.PostAsync("v1/status/shutdown", null, ct);
            return await response.Content.ReadFromJsonAsync<ShutdownDTO>(ct);
        }
        catch (HttpRequestException)
        {
            return null;
        }
    }

    public async Task<ConfigDTO?> GetConfigAsync(CancellationToken ct = default)
    {
        try
        {
            return await http.GetFromJsonAsync<ConfigDTO>("v1/status/config", ct);
        }
        catch (HttpRequestException)
        {
            return null;
        }
    }

    public async Task<TrustResponseDTO?> SetPeerTrustAsync(string nodeId, string trust, CancellationToken ct = default)
    {
        try
        {
            var request = new TrustRequestDTO(trust);
            var response = await http.PatchAsJsonAsync($"v1/status/peers/{nodeId}/trust", request, ct);
            return await response.Content.ReadFromJsonAsync<TrustResponseDTO>(ct);
        }
        catch (HttpRequestException)
        {
            return null;
        }
    }

    public async Task<RemovePeerResponseDTO?> RemovePeerAsync(string nodeId, CancellationToken ct = default)
    {
        try
        {
            var response = await http.DeleteAsync($"v1/status/peers/{nodeId}", ct);
            return await response.Content.ReadFromJsonAsync<RemovePeerResponseDTO>(ct);
        }
        catch (HttpRequestException)
        {
            return null;
        }
    }
}
