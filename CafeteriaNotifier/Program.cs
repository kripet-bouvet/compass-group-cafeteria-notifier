using System.Text.RegularExpressions;

namespace CafeteriaNotifier;

internal static partial class Program
{
    const string BalanceUrl = "https://www.alreadyordered.no/compass9002/content/uncode-lite_child/TemplateProductTable_find_current_top_up_value.php";
    const string TopUpUrl = "https://www.alreadyordered.no/compass9002/content/uncode-lite_child/TemplateProductTable_pure.php";
    const string MessageBoxTitle = "CafeteriaNotifier";

    /// <summary>
    ///  The main entry point for the application.
    /// </summary>
    [STAThread]
    static int Main(string[] args) => AsyncMain(args).GetAwaiter().GetResult();

    static async Task<int> AsyncMain(string[] args)
    {
        CafeteriaConfig config;
        int balance;
        try
        {
            config = ParseArgs(args);
        }
        catch (Exception ex) {
            CreateMessageBox($"Could not parse arguments: {ex.Message}");
            return -1;
        }
        
        try
        {
            balance = await GetCafeteriaBalance(config.Phone, config.Token);
        }
        catch (Exception ex)
        {
            var message = ex switch
            {
                HttpRequestException httpEx => $"Could not connect to server. Got status code {httpEx.StatusCode}",
                ApiInvalidResponseException invEx => $"Could not parse response: {invEx.Data["content"]}",
                _ => "An unexpected error occurred"
            };
            CreateMessageBox(message);
            return -1;
        }

        if (balance < config.BalanceLimit)
        {
            ShowLowBalanceNotification(balance);
        }
        return 0;
    }

    private static void ShowLowBalanceNotification(int balance)
    {
        var result = MessageBox.Show(
                        $"Cafeteria balance is low: {balance} kr\n\nDo you want to top up your balance in your browser\n(Firefox not supported)?",
                        MessageBoxTitle,
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Warning,
                        MessageBoxDefaultButton.Button1,
                        MessageBoxOptions.ServiceNotification);

        if (result == DialogResult.Yes)
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = TopUpUrl,
                UseShellExecute = true
            });
        }
    }

    static CafeteriaConfig ParseArgs(string[] args)
    {
        if (args.Length != 3)
        {
            throw new ArgumentException("Expected 3 arguments: phone, token, balanceLimit");
        }
        // Throw exception if balanceLimit is not a number
        if (!int.TryParse(args[2], out int balanceInt))
        {
            throw new ArgumentException("Expected balanceLimit to be a number");
        }
        return new(args[0], args[1], balanceInt);
    }

    /// <summary>
    /// Get the current cafeteria balance, in NOK
    /// </summary>
    /// <returns> The current cafeteria balance</returns>
    /// <exception cref="HttpRequestException"></exception>
    /// <exception cref="ApiInvalidResponseException"></exception>"
    static async Task<int> GetCafeteriaBalance(string phone, string token)
    {
        using var client = new HttpClient();
        var response = await client.PostAsync(BalanceUrl, new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["phone"] = phone,
            ["token"] = token
        }));

        // Throw exception if not successful
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();

        // Throw exception if content is not of the format \d+;(\d+)!, capture the second number
        var match = BalanceResponseRegex().Match(content);
        if (!match.Success)
        {
            throw new ApiInvalidResponseException($"Could not parse response", content);
        }
        // Parse number
        return int.Parse(match.Groups[1].Value);
    }

    static void CreateMessageBox(string message)
    {
        MessageBox.Show(message, MessageBoxTitle, MessageBoxButtons.OK, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1, MessageBoxOptions.ServiceNotification);
    }

    [GeneratedRegex(@"\d+;(\d+)!")]
    private static partial Regex BalanceResponseRegex();
}

/// Custom exception for when the response is not of the expected format
internal class ApiInvalidResponseException: Exception
{
    public ApiInvalidResponseException(string message, string data) : base(message)
    {
        Data.Add("content", data);
    }
}

internal record CafeteriaConfig(string Phone, string Token, int BalanceLimit);