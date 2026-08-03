using System.Text.Json;
using Othello.WinUI.Models;

namespace Othello.WinUI.Services;

/// <summary>
/// JSONL から棋譜を読み込むサービス実装です。
/// </summary>
public sealed class ReplayRecordService : IReplayRecordService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// JSON Lines ファイルから棋譜一覧を読み込みます。
    /// </summary>
    /// <param name="jsonlFilePath">読み込み対象ファイルパスです。</param>
    /// <returns>読み込んだゲーム一覧です。</returns>
    public IReadOnlyList<ReplayGameRecordModel> LoadGames(string jsonlFilePath)
    {
        if (string.IsNullOrWhiteSpace(jsonlFilePath))
        {
            throw new ArgumentException("JSONL file path is required.", nameof(jsonlFilePath));
        }

        if (!File.Exists(jsonlFilePath))
        {
            throw new FileNotFoundException("JSONL file not found.", jsonlFilePath);
        }

        var games = new List<ReplayGameRecordModel>();

        foreach (string line in File.ReadLines(jsonlFilePath))
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            ReplayGameRecordModel? game = JsonSerializer.Deserialize<ReplayGameRecordModel>(line, JsonOptions);
            if (game is not null)
            {
                games.Add(game);
            }
        }

        return games;
    }
}
