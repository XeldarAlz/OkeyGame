using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Runtime.Domain.Enums;
using Runtime.Domain.Models;
using Runtime.Domain.ValueObjects;
using Runtime.Infrastructure.Persistence;
using Runtime.Services.GameLogic;
using Runtime.Services.GameLogic.Score;
using Runtime.Services.GameLogic.State;
using Runtime.Services.GameLogic.Tiles;
using Runtime.Services.GameLogic.Turn;
using Runtime.Services.Validation;
using UnityEngine;
using Zenject;

namespace Runtime.Testing
{
    public sealed class Phase2Tester : MonoBehaviour
    {
        [Inject] private ITileService _tileService;
        [Inject] private IGameRulesService _gameRulesService;
        [Inject] private IGameStateService _gameStateService;
        [Inject] private IValidationService _validationService;
        [Inject] private ITurnManager _turnManager;
        [Inject] private IScoreService _scoreService;
        [Inject] private IPersistenceService _persistenceService;

        private bool _testsPassed = true;
        private int _totalTests = 0;
        private int _passedTests = 0;

        [ContextMenu("Run Phase 2 Tests")]
        public async void RunAllTests()
        {
            Debug.Log("=== PHASE 2 COMPREHENSIVE TESTING STARTED ===");
            
            _testsPassed = true;
            _totalTests = 0;
            _passedTests = 0;

            await TestTileService();
            await TestGameRulesService();
            await TestGameStateService();
            await TestValidationService();
            await TestTurnManager();
            await TestScoreService();
            await TestPersistenceService();
            await TestServiceIntegration();

            Debug.Log($"=== PHASE 2 TESTING COMPLETED ===");
            Debug.Log($"Results: {_passedTests}/{_totalTests} tests passed");
            
            if (_testsPassed)
            {
                Debug.Log(" ALL PHASE 2 TESTS PASSED! ");
            }
            else
            {
                Debug.LogError(" SOME PHASE 2 TESTS FAILED ");
            }
        }

        private async UniTask TestTileService()
        {
            Debug.Log("--- Testing TileService ---");

            // Test tile set creation
            await TestMethod("TileService.CreateTileSetAsync", async () =>
            {
                List<OkeyPiece> tiles = await _tileService.CreateTileSetAsync();
                return tiles != null && tiles.Count == 106; // 104 numbered + 2 false jokers
            });

            // Test tile shuffling
            await TestMethod("TileService.ShuffleTilesAsync", async () =>
            {
                List<OkeyPiece> originalTiles = await _tileService.CreateTileSetAsync();
                List<OkeyPiece> shuffledTiles = await _tileService.ShuffleTilesAsync(originalTiles);
                return shuffledTiles != null && shuffledTiles.Count == originalTiles.Count;
            });

            // Test indicator tile determination
            await TestMethod("TileService.DetermineIndicatorTileAsync", async () =>
            {
                List<OkeyPiece> tiles = await _tileService.CreateTileSetAsync();
                TileData indicatorTile = await _tileService.DetermineIndicatorTileAsync(tiles);
                return indicatorTile != null;
            });

            // Test joker calculation
            await TestMethod("TileService.CalculateJokerTile", async () =>
            {
                TileData indicatorTile = new TileData(5, OkeyColor.Red, OkeyPieceType.Normal);
                TileData jokerTile = _tileService.CalculateJokerTile(indicatorTile);
                return jokerTile != null && jokerTile.Number == 6 && jokerTile.Color == OkeyColor.Red;
            });

            // Test tile set validation
            await TestMethod("TileService.ValidateTileSet", async () =>
            {
                List<TileData> validSet = new List<TileData>
                {
                    new TileData(5, OkeyColor.Red, OkeyPieceType.Normal),
                    new TileData(5, OkeyColor.Yellow, OkeyPieceType.Normal),
                    new TileData(5, OkeyColor.Black, OkeyPieceType.Normal)
                };
                return _tileService.ValidateTileSet(validSet);
            });

            // Test tile sequence validation
            await TestMethod("TileService.ValidateTileSequence", async () =>
            {
                List<TileData> validSequence = new List<TileData>
                {
                    new TileData(3, OkeyColor.Red, OkeyPieceType.Normal),
                    new TileData(4, OkeyColor.Red, OkeyPieceType.Normal),
                    new TileData(5, OkeyColor.Red, OkeyPieceType.Normal)
                };
                return _tileService.ValidateTileSequence(validSequence);
            });
        }

        private async UniTask TestGameRulesService()
        {
            Debug.Log("--- Testing GameRulesService ---");

            // Create test game state and players
            GameState gameState = new GameState();
            Player player1 = new Player(1, "TestPlayer1", PlayerType.Human);
            Player player2 = new Player(2, "TestPlayer2", PlayerType.AI);
            
            List<Player> players = new List<Player> { player1, player2 };
            GameConfiguration config = GameConfiguration.CreateDefault();
            gameState.Initialize(config);
            
            // Create and add tiles to the draw pile
            List<OkeyPiece> tiles = await _tileService.CreateTileSetAsync();
            gameState.SetDrawPile(tiles);

            // Test win score calculation
            await TestMethod("GameRulesService.CalculateWinScore", async () =>
            {
                int normalWinScore = _gameRulesService.CalculateWinScore(WinType.Normal);
                int pairsWinScore = _gameRulesService.CalculateWinScore(WinType.Pairs);
                int okeyWinScore = _gameRulesService.CalculateWinScore(WinType.Okey);
                
                return normalWinScore > 0 && pairsWinScore > normalWinScore && okeyWinScore > pairsWinScore;
            });

            // Test draw pile validation
            await TestMethod("GameRulesService.CanDrawFromPile", async () =>
            {
                return _gameRulesService.CanDrawFromPile(gameState);
            });

            // Test set validation with joker
            await TestMethod("GameRulesService.ValidateSet", async () =>
            {
                List<TileData> validSet = new List<TileData>
                {
                    new TileData(7, OkeyColor.Red, OkeyPieceType.Normal),
                    new TileData(7, OkeyColor.Yellow, OkeyPieceType.Normal),
                    new TileData(7, OkeyColor.Black, OkeyPieceType.Normal)
                };
                TileData jokerTile = new TileData(1, OkeyColor.Red, OkeyPieceType.Normal);
                
                return _gameRulesService.ValidateSet(validSet, jokerTile);
            });

            // Test sequence validation with joker
            await TestMethod("GameRulesService.ValidateSequence", async () =>
            {
                List<TileData> validSequence = new List<TileData>
                {
                    new TileData(8, OkeyColor.Red, OkeyPieceType.Normal),
                    new TileData(9, OkeyColor.Red, OkeyPieceType.Normal),
                    new TileData(10, OkeyColor.Red, OkeyPieceType.Normal)
                };
                TileData jokerTile = new TileData(1, OkeyColor.Red, OkeyPieceType.Normal);
                
                return _gameRulesService.ValidateSequence(validSequence, jokerTile);
            });
        }

        private async UniTask TestGameStateService()
        {
            Debug.Log("--- Testing GameStateService ---");

            // Reset the game state service before testing
            await _gameStateService.InitializeAsync();

            // Test game state initialization
            await TestMethod("GameStateService.StartNewGameAsync", async () =>
            {
                GameConfiguration config = new GameConfiguration(2, 20);
                
                // Clear existing player configurations and add test players
                config.AddPlayerConfiguration(new PlayerConfiguration("TestPlayer1", PlayerType.Human));
                config.AddPlayerConfiguration(new PlayerConfiguration("TestPlayer2", PlayerType.AI));
                
                bool result = await _gameStateService.StartNewGameAsync(config);
                return result && _gameStateService.IsGameActive;
            });

            // Test state transitions
            await TestMethod("GameStateService.TransitionToStateAsync", async () =>
            {
                bool result = await _gameStateService.TransitionToStateAsync(Runtime.Domain.Enums.GameStateType.Paused);
                return result && _gameStateService.CurrentStateType == Runtime.Domain.Enums.GameStateType.Paused;
            });

            // Test turn advancement
            await TestMethod("GameStateService.NextPlayerTurnAsync", async () =>
            {
                await _gameStateService.TransitionToStateAsync(Runtime.Domain.Enums.GameStateType.PlayerTurn);
                bool result = await _gameStateService.NextPlayerTurnAsync();
                return result;
            });

            // Test game ending
            await TestMethod("GameStateService.EndGameAsync", async () =>
            {
                bool result = await _gameStateService.EndGameAsync();
                return result && _gameStateService.CurrentStateType == Runtime.Domain.Enums.GameStateType.GameEnded;
            });
        }

        private async UniTask TestValidationService()
        {
            Debug.Log("--- Testing ValidationService ---");

            // Create test data
            Player testPlayer = new Player(1, "TestPlayer", PlayerType.Human);
            GameState gameState = new GameState();
            GameConfiguration config = GameConfiguration.CreateDefault();
            gameState.Initialize(config);

            // Test grid position validation
            await TestMethod("ValidationService.ValidateGridPosition", async () =>
            {
                GridPosition validPosition = new GridPosition(0, 5);
                ValidationResult result = _validationService.ValidateGridPosition(validPosition);
                return result == ValidationResult.Valid;
            });

            // Test invalid grid position
            await TestMethod("ValidationService.ValidateGridPosition_Invalid", async () =>
            {
                GridPosition invalidPosition = new GridPosition(2, 20); // Invalid row and column
                ValidationResult result = _validationService.ValidateGridPosition(invalidPosition);
                return result != ValidationResult.Valid;
            });

            // Test tile set validation
            await TestMethod("ValidationService.ValidateTileSet", async () =>
            {
                List<TileData> validSet = new List<TileData>
                {
                    new TileData(9, OkeyColor.Red, OkeyPieceType.Normal),
                    new TileData(9, OkeyColor.Yellow, OkeyPieceType.Normal),
                    new TileData(9, OkeyColor.Black, OkeyPieceType.Normal)
                };
                TileData jokerTile = new TileData(1, OkeyColor.Red, OkeyPieceType.Normal);
                ValidationResult result = _validationService.ValidateTileSet(validSet, jokerTile);
                return result == ValidationResult.Valid;
            });

            // Test pairs hand validation
            await TestMethod("ValidationService.IsValidPairsHand", async () =>
            {
                List<TileData> pairsHand = new List<TileData>();
                
                // Create 7 pairs (14 tiles total)
                for (int i = 1; i <= 7; i++)
                {
                    OkeyColor color = (OkeyColor)((i - 1) % 4);
                    pairsHand.Add(new TileData(i, color, OkeyPieceType.Normal));
                    pairsHand.Add(new TileData(i, color, OkeyPieceType.Normal));
                }
                
                return _validationService.IsValidPairsHand(pairsHand);
            });
        }

        private async UniTask TestTurnManager()
        {
            Debug.Log("--- Testing TurnManager ---");

            // Create test players
            Player[] players = new Player[]
            {
                new Player(1, "Player1", PlayerType.Human),
                new Player(2, "Player2", PlayerType.AI),
                new Player(3, "Player3", PlayerType.Human),
                new Player(4, "Player4", PlayerType.AI)
            };

            // Test turn order setup
            await TestMethod("TurnManager.SetTurnOrder", async () =>
            {
                _turnManager.SetTurnOrder(players);
                return _turnManager.CurrentPlayer != null && _turnManager.CurrentPlayerIndex == 0;
            });

            // Test turn start
            await TestMethod("TurnManager.StartTurnAsync", async () =>
            {
                bool result = await _turnManager.StartTurnAsync(players[0]);
                return result && _turnManager.IsPlayerTurn(players[0].Id);
            });

            // Test turn advancement
            await TestMethod("TurnManager.NextTurnAsync", async () =>
            {
                Player previousPlayer = _turnManager.CurrentPlayer;
                bool result = await _turnManager.NextTurnAsync();
                return result && _turnManager.CurrentPlayer != previousPlayer;
            });

            // Test turn ending
            await TestMethod("TurnManager.EndTurnAsync", async () =>
            {
                bool result = await _turnManager.EndTurnAsync();
                return result;
            });

            // Test player index retrieval
            await TestMethod("TurnManager.GetPlayerIndex", async () =>
            {
                int index = _turnManager.GetPlayerIndex(players[1]);
                return index == 1;
            });
        }

        private async UniTask TestScoreService()
        {
            Debug.Log("--- Testing ScoreService ---");

            // Create test players
            Player player1 = new Player(1, "ScorePlayer1", PlayerType.Human);
            Player player2 = new Player(2, "ScorePlayer2", PlayerType.AI);
            List<Player> players = new List<Player> { player1, player2 };

            // Test score setting and getting
            await TestMethod("ScoreService.SetPlayerScore", async () =>
            {
                _scoreService.SetPlayerScore(player1.Id, 50);
                int score = _scoreService.GetPlayerScore(player1.Id);
                return score == 50;
            });

            // Test score addition
            await TestMethod("ScoreService.AddPlayerScore", async () =>
            {
                _scoreService.AddPlayerScore(player1.Id, 25);
                int score = _scoreService.GetPlayerScore(player1.Id);
                return score == 75;
            });

            // Test score subtraction
            await TestMethod("ScoreService.SubtractPlayerScore", async () =>
            {
                _scoreService.SubtractPlayerScore(player1.Id, 30);
                int score = _scoreService.GetPlayerScore(player1.Id);
                return score == 45;
            });

            // Test win score calculation
            await TestMethod("ScoreService.CalculateWinScoreAsync", async () =>
            {
                int normalWinScore = await _scoreService.CalculateWinScoreAsync(WinType.Normal);
                int pairsWinScore = await _scoreService.CalculateWinScoreAsync(WinType.Pairs);
                return normalWinScore > 0 && pairsWinScore > normalWinScore;
            });

            // Test winner determination
            await TestMethod("ScoreService.GetWinner", async () =>
            {
                _scoreService.SetPlayerScore(player2.Id, 30);
                Player winner = _scoreService.GetWinner(players);
                return winner != null && winner.Id == player1.Id; // player1 has score 45
            });

            // Test elimination check
            await TestMethod("ScoreService.IsPlayerEliminated", async () =>
            {
                _scoreService.SetPlayerScore(player2.Id, -150); // Below elimination threshold
                bool isEliminated = _scoreService.IsPlayerEliminated(player2);
                return isEliminated;
            });
        }

        private async UniTask TestPersistenceService()
        {
            Debug.Log("--- Testing PersistenceService ---");

            // Test storage availability
            await TestMethod("PersistenceService.IsStorageAvailable", async () =>
            {
                return _persistenceService.IsStorageAvailable();
            });

            // Test game configuration save/load
            await TestMethod("PersistenceService.SaveGameConfigurationAsync", async () =>
            {
                GameConfiguration config = GameConfiguration.CreateDefault();
                bool saveResult = await _persistenceService.SaveGameConfigurationAsync(config);
                return saveResult;
            });

            await TestMethod("PersistenceService.LoadGameConfigurationAsync", async () =>
            {
                GameConfiguration loadedConfig = await _persistenceService.LoadGameConfigurationAsync();
                return loadedConfig != null;
            });

            // Test player scores save/load
            await TestMethod("PersistenceService.SavePlayerScoresAsync", async () =>
            {
                Dictionary<int, int> scores = new Dictionary<int, int>
                {
                    { 1, 100 },
                    { 2, 75 },
                    { 3, 50 }
                };
                bool saveResult = await _persistenceService.SavePlayerScoresAsync(scores);
                return saveResult;
            });

            await TestMethod("PersistenceService.LoadPlayerScoresAsync", async () =>
            {
                Dictionary<int, int> loadedScores = await _persistenceService.LoadPlayerScoresAsync();
                return loadedScores != null && loadedScores.Count == 3;
            });

            // Test settings save/load
            await TestMethod("PersistenceService.SaveSettingsAsync", async () =>
            {
                Dictionary<string, object> settings = new Dictionary<string, object>
                {
                    { "volume", 0.8f },
                    { "difficulty", "medium" },
                    { "language", "en" }
                };
                bool saveResult = await _persistenceService.SaveSettingsAsync(settings);
                return saveResult;
            });

            await TestMethod("PersistenceService.LoadSettingsAsync", async () =>
            {
                Dictionary<string, object> loadedSettings = await _persistenceService.LoadSettingsAsync();
                return loadedSettings != null && loadedSettings.Count == 3;
            });

            // Test storage size calculation
            await TestMethod("PersistenceService.GetStorageSizeAsync", async () =>
            {
                long storageSize = await _persistenceService.GetStorageSizeAsync();
                return storageSize >= 0;
            });
        }

        private async UniTask TestServiceIntegration()
        {
            Debug.Log("--- Testing Service Integration ---");

            // Test complete game flow integration
            await TestMethod("Service Integration - Complete Game Flow", async () =>
            {
                try
                {
                    // 1. Initialize game state
                    GameConfiguration config = GameConfiguration.CreateDefault();
                    
                    // 2. Create and shuffle tiles
                    List<OkeyPiece> tiles = await _tileService.CreateTileSetAsync();
                    List<OkeyPiece> shuffledTiles = await _tileService.ShuffleTilesAsync(tiles);
                    
                    // 3. Determine indicator and joker
                    TileData indicatorTile = await _tileService.DetermineIndicatorTileAsync(shuffledTiles);
                    TileData jokerTile = _tileService.CalculateJokerTile(indicatorTile);
                    
                    // 4. Setup players and turn order
                    Player[] players = new Player[]
                    {
                        new Player(1, "IntegrationPlayer1", PlayerType.Human),
                        new Player(2, "IntegrationPlayer2", PlayerType.AI)
                    };
                    _turnManager.SetTurnOrder(players);
                    
                    // 5. Start first turn
                    bool turnStarted = await _turnManager.StartTurnAsync(players[0]);
                    
                    // 6. Test score operations
                    await _scoreService.CalculateWinScoreAsync(WinType.Normal);
                    _scoreService.SetPlayerScore(players[0].Id, 25);
                    
                    // 7. Save game state
                    await _persistenceService.SaveGameConfigurationAsync(config);
                    
                    return turnStarted && shuffledTiles.Count == 106;
                }
                catch (Exception exception)
                {
                    Debug.LogError($"Integration test failed: {exception.Message}");
                    return false;
                }
            });
        }

        private async UniTask TestMethod(string testName, Func<UniTask<bool>> testFunction)
        {
            _totalTests++;
            
            try
            {
                bool result = await testFunction();
                
                if (result)
                {
                    _passedTests++;
                    Debug.Log($" {testName} - PASSED");
                }
                else
                {
                    _testsPassed = false;
                    Debug.LogError($" {testName} - FAILED");
                }
            }
            catch (Exception exception)
            {
                _testsPassed = false;
                Debug.LogError($" {testName} - EXCEPTION: {exception.Message}");
            }
        }
    }
}
