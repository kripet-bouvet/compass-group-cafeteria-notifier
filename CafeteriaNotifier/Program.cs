using System.Text.RegularExpressions;

namespace CafeteriaNotifier;

internal static class Program
{
    /// <summary>
    ///  The main entry point for the application.
    /// </summary>
    [STAThread]
    static int Main(string[] args) => AsyncMain(args).GetAwaiter().GetResult();

    static async Task<int> AsyncMain(string[] args)
    {
        string phone; 
        string token; 
        int balanceLimit;
        int balance;
        try
        {
            (phone, token, balanceLimit) = ParseArgs(args);
        }
        catch (Exception ex) {
            CreateMessageBox($"Could not parse arguments: {ex.Message}");
            return -1;
        }
        
        try
        {
            balance = await GetCafeteriaBalance(phone, token);
        }
        catch (HttpRequestException e)
        {
            CreateMessageBox($"Could not connect to server. Got status code {e.StatusCode}");
            return -1;
        }
        catch (InvalidResponseException e)
        {
            CreateMessageBox($"Could not parse response: {e.Data["content"]}");
            return -1;
        }
        catch (Exception)
        {
            CreateMessageBox("An unexpected error occurred");
            return -1;
        }

        if (balance < balanceLimit)
        {
            ShowLowBalanceNotification(balance);
        }
        return 0;
    }

    private static void ShowLowBalanceNotification(int balance)
    {
        var result = MessageBox.Show(
                        $"Cafeteria balance is low: {balance} kr\n\nDo you want to top up your balance in your browser\n(Firefox not supported)?",
                        "CafeteriaNotifier",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Warning,
                        MessageBoxDefaultButton.Button1,
                        MessageBoxOptions.ServiceNotification);

        if (result == DialogResult.Yes)
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "https://www.alreadyordered.no/compass9002/content/uncode-lite_child/TemplateProductTable_pure.php",
                UseShellExecute = true
            });
        }
    }

    static (string phone, string token, int balanceLimit) ParseArgs(string[] args)
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
        return (args[0], args[1], balanceInt);
    }

    /// <summary>
    /// Get the current cafeteria balance, in NOK
    /// </summary>
    /// <returns> The current cafeteria balance</returns>
    /// <exception cref="HttpRequestException"></exception>
    /// <exception cref="InvalidResponseException"></exception>"
    static async Task<int> GetCafeteriaBalance(string phone, string token)
    {
        const string url = "https://www.alreadyordered.no/compass9002/content/uncode-lite_child/TemplateProductTable_find_current_top_up_value.php";
        var client = new HttpClient();
        var response = await client.PostAsync(url, new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["phone"] = phone,
            ["token"] = token
        }));

        // Throw exception if not successful
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();

        // Throw exception if content is not of the format \d+;(\d+)!, capture the second number
        var match = Regex.Match(content, @"\d+;(\d+)!");
        if (!match.Success)
        {
            throw new InvalidResponseException($"Could not parse response", content);
        }
        // Parse number
        return int.Parse(match.Groups[1].Value);
    }

    static void CreateMessageBox(string message)
    {
        MessageBox.Show(message, "CafeteriaNotifier", MessageBoxButtons.OK, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1, MessageBoxOptions.ServiceNotification);
    }
}

/// Custom exception for when the response is not of the expected format
class InvalidResponseException: Exception
{
    public InvalidResponseException(string message, string data) : base(message)
    {
        Data.Add("content", data);
    }
}