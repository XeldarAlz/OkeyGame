using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Runtime.Core.Architecture;
using Runtime.Domain.Models;
using Runtime.Domain.ValueObjects;

namespace Runtime.Services
{
    public interface ITileService : IInitializableService
    {
        UniTask<List<OkeyPiece>> CreateTileSetAsync();
        UniTask<List<OkeyPiece>> ShuffleTilesAsync(List<OkeyPiece> tiles);
        UniTask<TileData> DetermineIndicatorTileAsync(List<OkeyPiece> tiles);
        TileData CalculateJokerTile(TileData indicatorTile);
        bool IsTileJoker(TileData tileData, TileData jokerTile);
        bool ValidateTileSet(List<TileData> tiles);
        bool ValidateTileSequence(List<TileData> tiles);
        List<List<TileData>> FindAllValidSets(List<TileData> tiles);
        List<List<TileData>> FindAllValidSequences(List<TileData> tiles);
    }
}