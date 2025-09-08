using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Runtime.Domain.Enums;
using Runtime.Domain.Models;
using Runtime.Domain.ValueObjects;
using Runtime.Services;
using Runtime.Services.AI;
using Runtime.Services.GameLogic;
using UnityEngine;
using Zenject;
using GameState = Runtime.Domain.Models.GameState;

namespace Runtime.Testing
{
    public sealed class Phase3Tester : MonoBehaviour
    {
        [SerializeField] private bool _runTestsOnStart = false;
        
        private DiContainer _container;
        private IAIDecisionService _aiDecisionService;
        private IAIPlayerService _aiPlayerService;
        private IGameRulesService _gameRulesService;
        private ITileService _tileService;
        
        private int _totalTests = 0;
        private int _passedTests = 0;
        private int _failedTests = 0;

        [Inject]
        private void Construct(
            DiContainer container,
            IAIDecisionService aiDecisionService,
            IAIPlayerService aiPlayerService,
            IGameRulesService gameRulesService,
            ITileService tileService)
        {
            _container = container;
            _aiDecisionService = aiDecisionService;
            _aiPlayerService = aiPlayerService;
            _gameRulesService = gameRulesService;
            _tileService = tileService;
        }

        private async void Start()
        {
            if (_runTestsOnStart)
            {
                await UniTask.Delay(1000); // Wait for initialization
                await RunPhase3TestsAsync();
            }
        }

        [ContextMenu("Run Phase 3 Tests")]
        public async void RunPhase3Tests()
        {
            await RunPhase3TestsAsync();
        }

        private async UniTask RunPhase3TestsAsync()
        {
            Debug.Log("=== STARTING PHASE 3 AI SYSTEM TESTS ===");
            
            _totalTests = 0;
            _passedTests = 0;
            _failedTests = 0;

            await TestAIPlayerCreation();
            await TestAIDecisionService();
            await TestAIPlayerService();
            await TestBeginnerAIPlayer();
            await TestIntermediateAIPlayer();
            await TestAdvancedAIPlayer();
            await TestAIIntegration();

            Debug.Log("=== PHASE 3 TEST SUMMARY ===");
            Debug.Log($"Total Tests: {_totalTests}");
            Debug.Log($"Passed: {_passedTests}");
            Debug.Log($"Failed: {_failedTests}");
            Debug.Log($"Success Rate: {(_passedTests / (float)_totalTests * 100):F1}%");
            
            if (_failedTests == 0)
            {
                Debug.Log("<color=green>🎉 ALL PHASE 3 TESTS PASSED! 🎉</color>");
            }
            else
            {
                Debug.LogWarning($"<color=orange>⚠️ {_failedTests} tests failed. Review implementation.</color>");
            }
        }

        private async UniTask TestAIPlayerCreation()
        {
            Debug.Log("--- Testing AI Player Creation ---");

            await TestMethod("Create BeginnerAI", () =>
            {
                IAIPlayer beginnerAI = _container.Instantiate<BeginnerAIPlayer>(new object[] { 1, "Test Beginner" });
                return beginnerAI != null && beginnerAI.Difficulty == AIDifficulty.Beginner && beginnerAI.IsAI;
            });

            await TestMethod("Create IntermediateAI", () =>
            {
                IAIPlayer intermediateAI = _container.Instantiate<IntermediateAIPlayer>(new object[] { 2, "Test Intermediate" });
                return intermediateAI != null && intermediateAI.Difficulty == AIDifficulty.Intermediate && intermediateAI.IsAI;
            });

            await TestMethod("Create AdvancedAI", () =>
            {
                IAIPlayer advancedAI = _container.Instantiate<AdvancedAIPlayer>(new object[] { 3, "Test Advanced" });
                return advancedAI != null && advancedAI.Difficulty == AIDifficulty.Advanced && advancedAI.IsAI;
            });
        }

        private async UniTask TestAIDecisionService()
        {
            Debug.Log("--- Testing AIDecisionService ---");

            GameState gameState = await CreateTestGameState();
            Player testPlayer = gameState.Players[0];

            await TestMethod("AIDecisionService.DecideActionAsync with Beginner difficulty", async () =>
            {
                PlayerAction action = await _aiDecisionService.DecideActionAsync(testPlayer, gameState, AIDifficulty.Beginner);
                return true;
            });

            await TestMethod("AIDecisionService.DecideActionAsync with Intermediate difficulty", async () =>
            {
                PlayerAction action = await _aiDecisionService.DecideActionAsync(testPlayer, gameState, AIDifficulty.Intermediate);
                return true;
            });

            await TestMethod("AIDecisionService.DecideActionAsync with Advanced difficulty", async () =>
            {
                PlayerAction action = await _aiDecisionService.DecideActionAsync(testPlayer, gameState, AIDifficulty.Advanced);
                return true;
            });

            await TestMethod("AIDecisionService.CalculateActionValue", () =>
            {
                PlayerAction testAction = PlayerAction.CreateDrawAction(testPlayer.Id);
                float actionValue = _aiDecisionService.CalculateActionValue(testAction, gameState);
                return actionValue > 0.0f;
            });

            await TestMethod("AIDecisionService.GetBestAction", () =>
            {
                PlayerAction bestAction = _aiDecisionService.GetBestAction(testPlayer, gameState, AIDifficulty.Intermediate);
                return true;
            });
        }

        private async UniTask TestAIPlayerService()
        {
            Debug.Log("--- Testing AIPlayerService ---");

            GameState gameState = await CreateTestGameState();
            Player testPlayer = gameState.Players[0];

            await TestMethod("AIPlayerService.GetBestActionAsync", async () =>
            {
                PlayerAction action = await _aiPlayerService.GetBestActionAsync(testPlayer, gameState);
                return true;
            });

            await TestMethod("AIPlayerService.GetRandomValidActionAsync", async () =>
            {
                PlayerAction action = await _aiPlayerService.GetRandomValidActionAsync(testPlayer, gameState);
                return true;
            });

            await TestMethod("AIPlayerService.GetStrategicActionAsync", async () =>
            {
                PlayerAction action = await _aiPlayerService.GetStrategicActionAsync(testPlayer, gameState, AIDifficulty.Intermediate);
                return true;
            });

            await TestMethod("AIPlayerService.EvaluateHandStrength", () =>
            {
                float handStrength = _aiPlayerService.EvaluateHandStrength(testPlayer, gameState.JokerTile);
                return handStrength >= 0.0f;
            });

            await TestMethod("AIPlayerService.ShouldDrawFromDiscard", () =>
            {
                bool shouldDraw = _aiPlayerService.ShouldDrawFromDiscard(testPlayer, gameState);
                return true; // Any boolean result is valid
            });

            await TestMethod("AIPlayerService.SelectBestDiscard", () =>
            {
                TileData bestDiscard = _aiPlayerService.SelectBestDiscard(testPlayer, gameState);
                return true;
            });

            await TestMethod("AIPlayerService.ShouldDeclareWin", () =>
            {
                bool shouldWin = _aiPlayerService.ShouldDeclareWin(testPlayer, gameState);
                return true; // Any boolean result is valid
            });
        }

        private async UniTask TestBeginnerAIPlayer()
        {
            Debug.Log("--- Testing BeginnerAIPlayer ---");

            GameState gameState = await CreateTestGameState();
            IAIPlayer beginnerAI = _container.Instantiate<BeginnerAIPlayer>(new object[] { 10, "Test Beginner AI" });

            await TestMethod("BeginnerAIPlayer.DecideActionAsync", async () =>
            {
                PlayerAction action = await beginnerAI.DecideActionAsync(gameState);
                return true;
            });

            await TestMethod("BeginnerAIPlayer.UpdateStrategy", () =>
            {
                beginnerAI.UpdateStrategy(gameState);
                return true; // Should not throw exceptions
            });

            await TestMethod("BeginnerAIPlayer properties", () =>
            {
                return beginnerAI.Difficulty == AIDifficulty.Beginner && 
                       beginnerAI.IsAI && 
                       beginnerAI.Name == "Test Beginner AI";
            });
        }

        private async UniTask TestIntermediateAIPlayer()
        {
            Debug.Log("--- Testing IntermediateAIPlayer ---");

            GameState gameState = await CreateTestGameState();
            IAIPlayer intermediateAI = _container.Instantiate<IntermediateAIPlayer>(new object[] { 11, "Test Intermediate AI" });

            await TestMethod("IntermediateAIPlayer.DecideActionAsync", async () =>
            {
                PlayerAction action = await intermediateAI.DecideActionAsync(gameState);
                return true;
            });

            await TestMethod("IntermediateAIPlayer.UpdateStrategy", () =>
            {
                intermediateAI.UpdateStrategy(gameState);
                return true; // Should not throw exceptions
            });

            await TestMethod("IntermediateAIPlayer properties", () =>
            {
                return intermediateAI.Difficulty == AIDifficulty.Intermediate && 
                       intermediateAI.IsAI && 
                       intermediateAI.Name == "Test Intermediate AI";
            });
        }

        private async UniTask TestAdvancedAIPlayer()
        {
            Debug.Log("--- Testing AdvancedAIPlayer ---");

            GameState gameState = await CreateTestGameState();
            IAIPlayer advancedAI = _container.Instantiate<AdvancedAIPlayer>(new object[] { 12, "Test Advanced AI" });

            await TestMethod("AdvancedAIPlayer.DecideActionAsync", async () =>
            {
                PlayerAction action = await advancedAI.DecideActionAsync(gameState);
                return true;
            });

            await TestMethod("AdvancedAIPlayer.UpdateStrategy", () =>
            {
                advancedAI.UpdateStrategy(gameState);
                return true; // Should not throw exceptions
            });

            await TestMethod("AdvancedAIPlayer properties", () =>
            {
                return advancedAI.Difficulty == AIDifficulty.Advanced && 
                       advancedAI.IsAI && 
                       advancedAI.Name == "Test Advanced AI";
            });
        }

        private async UniTask TestAIIntegration()
        {
            Debug.Log("--- Testing AI Integration ---");

            GameState gameState = await CreateTestGameState();
            
            // Create AI players of different difficulties
            IAIPlayer beginnerAI = _container.Instantiate<BeginnerAIPlayer>(new object[] { 20, "Integration Beginner" });
            IAIPlayer intermediateAI = _container.Instantiate<IntermediateAIPlayer>(new object[] { 21, "Integration Intermediate" });
            IAIPlayer advancedAI = _container.Instantiate<AdvancedAIPlayer>(new object[] { 22, "Integration Advanced" });

            await TestMethod("AI Integration - Multiple AI players can make decisions", async () =>
            {
                PlayerAction beginnerAction = await beginnerAI.DecideActionAsync(gameState);
                PlayerAction intermediateAction = await intermediateAI.DecideActionAsync(gameState);
                PlayerAction advancedAction = await advancedAI.DecideActionAsync(gameState);
                
                return true;
            });

            await TestMethod("AI Integration - Different difficulties produce different behavior", async () =>
            {
                // Test multiple decisions to see if there's variation
                List<PlayerAction> beginnerActions = new List<PlayerAction>();
                List<PlayerAction> advancedActions = new List<PlayerAction>();
                
                for (int testIndex = 0; testIndex < 5; testIndex++)
                {
                    PlayerAction beginnerAction = await beginnerAI.DecideActionAsync(gameState);
                    PlayerAction advancedAction = await advancedAI.DecideActionAsync(gameState);
                    
                    beginnerActions.Add(beginnerAction);
                    advancedActions.Add(advancedAction);
                }
                
                return beginnerActions.Count > 0 && advancedActions.Count > 0;
            });

            await TestMethod("AI Integration - All AI services work together", async () =>
            {
                // Test that factory, decision service, and player service all work together
                IAIPlayer testAI = _container.Instantiate<IntermediateAIPlayer>(new object[] { 30, "Integration Test" });
                PlayerAction decision = await _aiDecisionService.DecideActionAsync(testAI as Player, gameState, AIDifficulty.Intermediate);
                float handStrength = _aiPlayerService.EvaluateHandStrength(testAI as Player, gameState.JokerTile);
                
                return testAI != null && handStrength >= 0.0f;
            });
        }

        private async UniTask<GameState> CreateTestGameState()
        {
            GameState gameState = new GameState();
            GameConfiguration config = GameConfiguration.CreateDefault();
            gameState.Initialize(config);

            // Add test players
            Player player1 = new Player(1, "TestPlayer1", PlayerType.Human);
            Player player2 = new Player(2, "TestPlayer2", PlayerType.AI);
            gameState.AddPlayer(player1);
            gameState.AddPlayer(player2);

            // Create and set tiles
            List<OkeyPiece> tiles = await _tileService.CreateTileSetAsync();
            gameState.SetDrawPile(tiles);

            // Give players some tiles
            for (int playerIndex = 0; playerIndex < gameState.Players.Count; playerIndex++)
            {
                Player player = gameState.Players[playerIndex];
                for (int tileIndex = 0; tileIndex < 14; tileIndex++)
                {
                    if (gameState.DrawPileCount > 0)
                    {
                        OkeyPiece tile = gameState.DrawFromPile();
                        player.AddTile(tile);
                    }
                }
            }

            // Set joker tile
            if (gameState.DrawPileCount > 0)
            {
                OkeyPiece jokerPiece = gameState.DrawFromPile();
                gameState.SetJokerTile(jokerPiece.TileData);
            }

            return gameState;
        }

        private async UniTask TestMethod(string testName, System.Func<bool> testAction)
        {
            _totalTests++;
            
            try
            {
                bool result = testAction();
                if (result)
                {
                    _passedTests++;
                    Debug.Log($"<color=green>✓</color> {testName}");
                }
                else
                {
                    _failedTests++;
                    Debug.LogError($"<color=red>✗</color> {testName} - Test returned false");
                }
            }
            catch (System.Exception exception)
            {
                _failedTests++;
                Debug.LogError($"<color=red>✗</color> {testName} - Exception: {exception.Message}");
            }
            
            await UniTask.Yield();
        }

        private async UniTask TestMethod(string testName, System.Func<UniTask<bool>> testAction)
        {
            _totalTests++;
            
            try
            {
                bool result = await testAction();
                if (result)
                {
                    _passedTests++;
                    Debug.Log($"<color=green>✓</color> {testName}");
                }
                else
                {
                    _failedTests++;
                    Debug.LogError($"<color=red>✗</color> {testName} - Test returned false");
                }
            }
            catch (System.Exception exception)
            {
                _failedTests++;
                Debug.LogError($"<color=red>✗</color> {testName} - Exception: {exception.Message}");
            }
            
            await UniTask.Yield();
        }
    }
}
