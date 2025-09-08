using System;
using System.Collections.Generic;
using Runtime.Domain.Models;
using Runtime.Domain.ValueObjects;
using Runtime.Domain.Enums;
using Runtime.Presentation.Views;
using Runtime.Services.GameLogic;
using Runtime.Services.GameLogic.State;
using Runtime.Services.GameLogic.Turn;
using Zenject;
using Cysharp.Threading.Tasks;
using Runtime.Services.GameLogic.Tiles;

namespace Runtime.Presentation.Presenters
{
    public sealed class PlayerRackPresenter : BasePresenter<PlayerRackView>
    {
        private readonly IGameStateService _gameStateService;
        private readonly ITileService _tileService;
        private readonly IGameRulesService _gameRulesService;
        private readonly ITurnManager _turnManager;

        private Player _currentPlayer;
        private bool _isPlayerTurn;
        private OkeyPiece _selectedTile;

        public event Action<OkeyPiece, GridPosition> OnTilePlacedInRack;
        public event Action<OkeyPiece, GridPosition, GridPosition> OnTileMovedInRack;
        public event Action<OkeyPiece> OnTileSelectedFromRack;
        public event Action<List<OkeyPiece>> OnRackUpdated;

        public Player CurrentPlayer => _currentPlayer;
        public bool IsPlayerTurn => _isPlayerTurn;
        public OkeyPiece SelectedTile => _selectedTile;
        public int TileCount => _view?.TileCount ?? 0;

        public List<OkeyPiece> GetRackTiles()
        {
            return _currentPlayer?.GetTiles() ?? new List<OkeyPiece>();
        }

        [Inject]
        public PlayerRackPresenter(IGameStateService gameStateService, ITileService tileService, 
            IGameRulesService gameRulesService, ITurnManager turnManager)
        {
            _gameStateService = gameStateService;
            _tileService = tileService;
            _gameRulesService = gameRulesService;
            _turnManager = turnManager;
        }

        protected override void InitializeView()
        {
            base.InitializeView();
            InitializePlayerRackAsync().Forget();
        }

        protected override void SubscribeToEvents()
        {
            base.SubscribeToEvents();

            if (_view != null)
            {
                _view.OnTilePlaced += HandleTilePlaced;
                _view.OnTileMoved += HandleTileMoved;
                _view.OnTileSelected += HandleTileSelected;
                _view.OnTileDeselected += HandleTileDeselected;
                _view.OnRackCleared += HandleRackCleared;
            }

            if (_gameStateService != null)
            {
                _gameStateService.OnStateChanged += HandleGameStateChanged;
            }

            if (_turnManager != null)
            {
                _turnManager.OnTurnChanged += HandleTurnChanged;
            }
        }

        protected override void UnsubscribeFromEvents()
        {
            if (_view != null)
            {
                _view.OnTilePlaced -= HandleTilePlaced;
                _view.OnTileMoved -= HandleTileMoved;
                _view.OnTileSelected -= HandleTileSelected;
                _view.OnTileDeselected -= HandleTileDeselected;
                _view.OnRackCleared -= HandleRackCleared;
            }

            if (_gameStateService != null)
            {
                _gameStateService.OnStateChanged -= HandleGameStateChanged;
            }

            if (_turnManager != null)
            {
                _turnManager.OnTurnChanged -= HandleTurnChanged;
            }

            base.UnsubscribeFromEvents();
        }

        private async UniTask InitializePlayerRackAsync()
        {
            if (_gameStateService?.CurrentPlayer != null)
            {
                await SetPlayerAsync(_gameStateService.CurrentPlayer);
            }
        }

        public async UniTask SetPlayerAsync(Player player)
        {
            if (player == null)
            {
                return;
            }

            _currentPlayer = player;
            await UpdateRackDisplayAsync();
        }

        public async UniTask AddTileToRackAsync(OkeyPiece tile, GridPosition? preferredPosition = null)
        {
            if (tile == null || _view == null)
            {
                return;
            }

            await _view.AddTileAsync(tile, preferredPosition);
            
            if (_currentPlayer != null)
            {
                _currentPlayer.AddTile(tile);
                OnRackUpdated?.Invoke(GetRackTiles());
            }
        }

        public async UniTask RemoveTileFromRackAsync(OkeyPiece tile)
        {
            if (tile == null || _view == null)
            {
                return;
            }

            await _view.RemoveTileAsync(tile);
            
            if (_currentPlayer != null)
            {
                _currentPlayer.RemoveTile(tile);
                OnRackUpdated?.Invoke(GetRackTiles());
            }
        }

        public async UniTask MoveTileInRackAsync(OkeyPiece tile, GridPosition newPosition)
        {
            if (tile == null || _view == null)
            {
                return;
            }

            await _view.MoveTileAsync(tile, newPosition);
        }

        public void SelectTile(OkeyPiece tile)
        {
            if (tile == null || !_isPlayerTurn)
            {
                return;
            }

            _selectedTile = tile;
            OnTileSelectedFromRack?.Invoke(tile);
        }

        public void DeselectTile()
        {
            _selectedTile = null;
        }

        public bool CanPlaceTile(OkeyPiece tile, GridPosition position)
        {
            if (tile == null || _view == null)
            {
                return false;
            }

            return _view.IsPositionAvailable(position);
        }

        public bool ValidateRackArrangement()
        {
            if (_currentPlayer == null || _gameRulesService == null)
            {
                return false;
            }

            // TODO: Implement rack arrangement validation logic
            // This would check if the tiles in the rack form valid sets/sequences
            return true;
        }

        public List<List<OkeyPiece>> GetValidSets()
        {
            if (_currentPlayer == null || _gameRulesService == null)
            {
                return new List<List<OkeyPiece>>();
            }

            // TODO: Implement valid sets finding logic
            // This would analyze the rack tiles and find valid sets/sequences
            return new List<List<OkeyPiece>>();
        }

        public async UniTask ArrangeRackOptimallyAsync()
        {
            if (_currentPlayer == null || _view == null)
            {
                return;
            }

            List<OkeyPiece> rackTiles = GetRackTiles();
            List<OkeyPiece> arrangedTiles = ArrangeForOptimalDisplay(rackTiles);
            
            _view.SetTiles(arrangedTiles);
            await UniTask.Yield(); // Allow UI to update
        }

        public async UniTask SortRackByColorAsync()
        {
            if (_currentPlayer == null || _view == null)
            {
                return;
            }

            List<OkeyPiece> rackTiles = GetRackTiles();
            rackTiles.Sort((a, b) => 
            {
                int colorCompare = a.Color.CompareTo(b.Color);
                return colorCompare != 0 ? colorCompare : a.Number.CompareTo(b.Number);
            });
            
            _view.SetTiles(rackTiles);
            await UniTask.Yield();
        }

        public async UniTask SortRackByNumberAsync()
        {
            if (_currentPlayer == null || _view == null)
            {
                return;
            }

            List<OkeyPiece> rackTiles = GetRackTiles();
            rackTiles.Sort((a, b) => 
            {
                int numberCompare = a.Number.CompareTo(b.Number);
                return numberCompare != 0 ? numberCompare : a.Color.CompareTo(b.Color);
            });
            
            _view.SetTiles(rackTiles);
            await UniTask.Yield();
        }

        public void HighlightValidMoves(OkeyPiece selectedTile)
        {
            if (selectedTile == null || _view == null)
            {
                return;
            }

            _view.HighlightValidDropZones(true);
        }

        public void ClearHighlights()
        {
            _view?.ClearAllHighlights();
        }

        public bool CanDiscardTile(OkeyPiece tile)
        {
            if (tile == null || _currentPlayer == null || _gameRulesService == null)
            {
                return false;
            }

            return _gameRulesService.CanDiscard(_currentPlayer, tile.TileData);
        }

        public async UniTask<bool> TryDiscardTileAsync(OkeyPiece tile)
        {
            if (!CanDiscardTile(tile))
            {
                return false;
            }

            await RemoveTileFromRackAsync(tile);
            return true;
        }

        private async UniTask UpdateRackDisplayAsync()
        {
            if (_currentPlayer == null || _view == null)
            {
                return;
            }

            List<OkeyPiece> rackTiles = GetRackTiles();
            _view.SetTiles(rackTiles);
            
            OnRackUpdated?.Invoke(rackTiles);
            await UniTask.Yield();
        }

        private List<OkeyPiece> ArrangeForOptimalDisplay(List<OkeyPiece> tiles)
        {
            if (tiles == null || tiles.Count == 0)
            {
                return new List<OkeyPiece>();
            }

            // Create a copy to avoid modifying the original list
            List<OkeyPiece> arrangedTiles = new List<OkeyPiece>(tiles);

            // Group by potential sets first, then by color and number
            arrangedTiles.Sort((a, b) =>
            {
                // Jokers and special tiles at the end
                if (a.IsJoker && !b.IsJoker) return 1;
                if (!a.IsJoker && b.IsJoker) return -1;
                
                if (a.PieceType == OkeyPieceType.FalseJoker && b.PieceType != OkeyPieceType.FalseJoker) return 1;
                if (a.PieceType != OkeyPieceType.FalseJoker && b.PieceType == OkeyPieceType.FalseJoker) return -1;

                // Sort by color first, then by number
                int colorCompare = a.Color.CompareTo(b.Color);
                return colorCompare != 0 ? colorCompare : a.Number.CompareTo(b.Number);
            });

            return arrangedTiles;
        }

        private void HandleTilePlaced(OkeyPiece tile, GridPosition position)
        {
            OnTilePlacedInRack?.Invoke(tile, position);
        }

        private void HandleTileMoved(OkeyPiece tile, GridPosition oldPosition, GridPosition newPosition)
        {
            OnTileMovedInRack?.Invoke(tile, oldPosition, newPosition);
        }

        private void HandleTileSelected(OkeyPiece tile)
        {
            SelectTile(tile);
        }

        private void HandleTileDeselected(OkeyPiece tile)
        {
            if (_selectedTile == tile)
            {
                DeselectTile();
            }
        }

        private void HandleRackCleared()
        {
            _selectedTile = null;
            if (_currentPlayer != null)
            {
                OnRackUpdated?.Invoke(new List<OkeyPiece>());
            }
        }

        private void HandleGameStateChanged(GameStateType newState)
        {
            _isPlayerTurn = newState == GameStateType.PlayerTurn;
            
            if (!_isPlayerTurn)
            {
                DeselectTile();
                ClearHighlights();
            }
        }

        private void HandleTurnChanged(Player newCurrentPlayer)
        {
            _isPlayerTurn = newCurrentPlayer == _currentPlayer && !newCurrentPlayer.IsAI;
            
            if (!_isPlayerTurn)
            {
                DeselectTile();
                ClearHighlights();
            }
        }

        public override void Dispose()
        {
            DeselectTile();
            ClearHighlights();
            base.Dispose();
        }
    }
}
