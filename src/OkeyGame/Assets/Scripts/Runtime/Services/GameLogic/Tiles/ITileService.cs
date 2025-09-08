using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Runtime.Core.Architecture;
using Runtime.Domain.Models;
using Runtime.Domain.ValueObjects;

namespace Runtime.Services.GameLogic.Tiles
{
    public interface ITileService : IInitializableService
    {
        UniTask<List<OkeyPiece>> CreateTileSetAsync();
        UniTask<List<OkeyPiece>> CreateFullTileSetAsync();
        UniTask<List<OkeyPiece>> ShuffleTilesAsync(List<OkeyPiece> tiles);
        List<OkeyPiece> ShuffleTiles(List<OkeyPiece> tiles);
        UniTask<TileData> DetermineIndicatorTileAsync(List<OkeyPiece> tiles);
        UniTask<bool> SetIndicatorTileAsync(OkeyPiece indicatorTile);
        UniTask<OkeyPiece> GetIndicatorTileAsync();
        UniTask<OkeyPiece> DrawTileFromPileAsync();
        UniTask<OkeyPiece> DrawTileFromDiscardAsync();
        UniTask<bool> AddTileToDiscardAsync(OkeyPiece tile);
        TileData CalculateJokerTile(TileData indicatorTile);
        bool IsTileJoker(TileData tileData, TileData jokerTile);
        bool ValidateTileSet(List<TileData> tiles);
        bool ValidateTileSequence(List<TileData> tiles);
        List<List<TileData>> FindAllValidSets(List<TileData> tiles);
        List<List<TileData>> FindAllValidSequences(List<TileData> tiles);
    }
}