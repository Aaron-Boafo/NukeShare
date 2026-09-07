using System.Net.Http.Json;

namespace NukeShare.CLI.Infrastructure;

public class TransferRestApi(HttpClient http)
{
    public async Task<TransferSendResultDTO> SendAsync(string nodeId, string filePath, CancellationToken ct = default)
    {
        try
        {
            var response = await http.PostAsJsonAsync("v1/transfer", new TransferSendRequestDTO(nodeId, filePath), ct);
            using (response)
            {
                if (response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadFromJsonAsync<TransferSendResponseDTO>(ct);
                    return new TransferSendResultDTO(body?.Transfer, null);
                }

                return new TransferSendResultDTO(null, await ReadErrorAsync(response, ct));
            }
        }
        catch (HttpRequestException)
        {
            return new TransferSendResultDTO(null, null);
        }
    }

    public async Task<TransferListViewDTO?> GetActiveAsync(CancellationToken ct = default)
    {
        try
        {
            return await http.GetFromJsonAsync<TransferListViewDTO>("v1/transfer", ct);
        }
        catch (HttpRequestException)
        {
            return null;
        }
    }

    public async Task<TransferHistoryDTO?> GetRecentAsync(CancellationToken ct = default)
    {
        try
        {
            return await http.GetFromJsonAsync<TransferHistoryDTO>("v1/transfer/recent", ct);
        }
        catch (HttpRequestException)
        {
            return null;
        }
    }

    public async Task<TransferDetailDTO?> GetAsync(Guid transferId, CancellationToken ct = default)
    {
        try
        {
            return await http.GetFromJsonAsync<TransferDetailDTO>($"v1/transfer/{transferId}", ct);
        }
        catch (HttpRequestException)
        {
            return null;
        }
    }

    public async Task<CancelTransferResultDTO?> CancelAsync(Guid transferId, CancellationToken ct = default)
    {
        try
        {
            var response = await http.PostAsync($"v1/transfer/{transferId}/cancel", null, ct);
            using (response)
            {
                if (response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadFromJsonAsync<CancelTransferDTO>(ct);
                    return new CancelTransferResultDTO(body?.Message, null);
                }

                return new CancelTransferResultDTO(null, await ReadErrorAsync(response, ct));
            }
        }
        catch (HttpRequestException)
        {
            return null;
        }
    }

    private static async Task<string?> ReadErrorAsync(HttpResponseMessage response, CancellationToken ct)
    {
        try
        {
            var error = await response.Content.ReadFromJsonAsync<ErrorResponseDTO>(ct);
            return error?.Error;
        }
        catch
        {
            return null;
        }
    }
}