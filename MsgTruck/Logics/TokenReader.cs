using System.Text.RegularExpressions;

namespace MsgTruck
{
    internal static class TokenReader
    {
        private const string _tokenFile = "token.txt";
        private static string _fullPath => Path.Combine(Directory.GetCurrentDirectory(), _tokenFile);
        internal static async Task<string> ReadTokenAsync(ILogger logger)
        {
            if (!File.Exists(_tokenFile))
            {
                await logger.LogAsync("-- !! TOKEN FILE DOES NOT EXIST !! --", Discord.LogSeverity.Critical);
                try
                {
                    (File.Create(_fullPath)).Dispose();
                    if (File.Exists(_tokenFile))
                    {
                        await logger.LogAsync($"Token flie is created in {_fullPath}.",
                            Discord.LogSeverity.Critical);
                    }
                }
                catch
                {
                    await logger.LogAsync($"Failed to create token file in {_fullPath}. " +
                        $"Please create one manaully.", Discord.LogSeverity.Error);
                }

                await LogTokenGuide(logger);
                return "";
            }
            try
            {
                string token = (await File.ReadAllTextAsync(_tokenFile)).Trim();
                if (IsTokenValid(token))
                {
                    return token;
                }
                else
                {
                    await logger.LogAsync("The token is invalid. Make sure the token file MUST contain the Token ONLY.",
                         Discord.LogSeverity.Critical);
                    return "";
                }
            }
            catch
            {
                await logger.LogAsync($"Can't read the file {_fullPath}. " +
                    $"Check the permission, and check if the file is valid.", Discord.LogSeverity.Error);
                await LogTokenGuide(logger);
                return "";
            }
            //var token = File.ReadAllText(_tokenFile).Trim();
        }
        private static Task LogTokenGuide(ILogger logger)
            => logger.LogAsync($"Get the token on the Discord Developer Portal, Copy and paste it to the token file."
                + Environment.NewLine
                + "Do not share ever your token!"
                , Discord.LogSeverity.Critical);

        internal static bool IsTokenValid(string token)
            => Regex.IsMatch(token, @"^[A-Za-z0-9-_\.]+$");
    }
}
