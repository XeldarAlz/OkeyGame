using System;
using System.Collections.Generic;
using System.IO;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using Runtime.Domain.Models;
using UnityEngine;

namespace Runtime.Infrastructure.Persistence
{
    public sealed class PersistenceService : IPersistenceService
    {
        private const string GAME_STATE_PREFIX = "gamestate_";
        private const string PLAYER_DATA_PREFIX = "player_";
        private const string FILE_EXTENSION = ".json";

        private readonly string _persistentDataPath;
        private readonly string _gameStateFolder;
        private readonly string _playerDataFolder;
        private readonly string _configurationFile;
        private readonly string _scoresFile;
        private readonly string _settingsFile;

        public PersistenceService()
        {
            _persistentDataPath = Application.persistentDataPath;
            _gameStateFolder = Path.Combine(_persistentDataPath, "GameStates");
            _playerDataFolder = Path.Combine(_persistentDataPath, "PlayerData");
            _configurationFile = Path.Combine(_persistentDataPath, "game_configuration.json");
            _scoresFile = Path.Combine(_persistentDataPath, "player_scores.json");
            _settingsFile = Path.Combine(_persistentDataPath, "settings.json");
        }

        public async UniTask InitializeAsync()
        {
            try
            {
                // Create necessary directories
                EnsureDirectoryExists(_gameStateFolder);
                EnsureDirectoryExists(_playerDataFolder);
                Debug.Log($"[PersistenceService] Initialized with path: {_persistentDataPath}");
                await UniTask.Yield();
            }
            catch (Exception exception)
            {
                Debug.LogError($"[PersistenceService] Initialization failed: {exception.Message}");
            }
        }

        public async UniTask<bool> SaveGameStateAsync(GameState gameState, string saveSlot)
        {
            if (gameState == null || string.IsNullOrEmpty(saveSlot))
            {
                Debug.LogWarning("[PersistenceService] Invalid parameters for SaveGameStateAsync");
                return false;
            }

            try
            {
                string fileName = GAME_STATE_PREFIX + saveSlot + FILE_EXTENSION;
                string filePath = Path.Combine(_gameStateFolder, fileName);
                GameStateData gameStateData = ConvertToGameStateData(gameState);
                string jsonData = JsonConvert.SerializeObject(gameStateData, Formatting.Indented);
                await File.WriteAllTextAsync(filePath, jsonData);
                Debug.Log($"[PersistenceService] Game state saved to slot: {saveSlot}");
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogError($"[PersistenceService] Failed to save game state: {exception.Message}");
                return false;
            }
        }

        public async UniTask<GameState> LoadGameStateAsync(string saveSlot)
        {
            if (string.IsNullOrEmpty(saveSlot))
            {
                Debug.LogWarning("[PersistenceService] Invalid save slot for LoadGameStateAsync");
                return null;
            }

            try
            {
                string fileName = GAME_STATE_PREFIX + saveSlot + FILE_EXTENSION;
                string filePath = Path.Combine(_gameStateFolder, fileName);

                if (!File.Exists(filePath))
                {
                    Debug.LogWarning($"[PersistenceService] Save file not found: {saveSlot}");
                    return null;
                }

                string jsonData = await File.ReadAllTextAsync(filePath);
                GameStateData gameStateData = JsonConvert.DeserializeObject<GameStateData>(jsonData);
                GameState gameState = ConvertFromGameStateData(gameStateData);
                Debug.Log($"[PersistenceService] Game state loaded from slot: {saveSlot}");
                return gameState;
            }
            catch (Exception exception)
            {
                Debug.LogError($"[PersistenceService] Failed to load game state: {exception.Message}");
                return null;
            }
        }

        public async UniTask<bool> DeleteSaveAsync(string saveSlot)
        {
            if (string.IsNullOrEmpty(saveSlot))
            {
                return false;
            }
            
            try
            {
                string fileName = GAME_STATE_PREFIX + saveSlot + FILE_EXTENSION;
                string filePath = Path.Combine(_gameStateFolder, fileName);

                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    Debug.Log($"[PersistenceService] Save deleted: {saveSlot}");
                }

                await UniTask.Yield();
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogError($"[PersistenceService] Failed to delete save: {exception.Message}");
                return false;
            }
        }

        public async UniTask<List<string>> GetAvailableSavesAsync()
        {
            List<string> availableSaves = new List<string>();

            try
            {
                if (!Directory.Exists(_gameStateFolder))
                {
                    return availableSaves;
                }

                string[] files = Directory.GetFiles(_gameStateFolder, GAME_STATE_PREFIX + "*" + FILE_EXTENSION);
                for (int index = 0; index < files.Length; index++)
                {
                    string file = files[index];
                    string fileName = Path.GetFileNameWithoutExtension(file);
                    string saveSlot = fileName.Substring(GAME_STATE_PREFIX.Length);
                    availableSaves.Add(saveSlot);
                }

                await UniTask.Yield();
            }
            catch (Exception exception)
            {
                Debug.LogError($"[PersistenceService] Failed to get available saves: {exception.Message}");
            }

            return availableSaves;
        }

        public bool HasSave(string saveSlot)
        {
            if (string.IsNullOrEmpty(saveSlot))
            {
                return false;
            }

            string fileName = GAME_STATE_PREFIX + saveSlot + FILE_EXTENSION;
            string filePath = Path.Combine(_gameStateFolder, fileName);
            return File.Exists(filePath);
        }

        public async UniTask<bool> SavePlayerDataAsync(Player player)
        {
            if (player == null)
            {
                return false;
            }

            try
            {
                string fileName = PLAYER_DATA_PREFIX + player.Id + FILE_EXTENSION;
                string filePath = Path.Combine(_playerDataFolder, fileName);
                PlayerData playerData = ConvertToPlayerData(player);
                string jsonData = JsonConvert.SerializeObject(playerData, Formatting.Indented);
                await File.WriteAllTextAsync(filePath, jsonData);
                Debug.Log($"[PersistenceService] Player data saved for: {player.Name}");
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogError($"[PersistenceService] Failed to save player data: {exception.Message}");
                return false;
            }
        }

        public async UniTask<Player> LoadPlayerDataAsync(int playerId)
        {
            try
            {
                string fileName = PLAYER_DATA_PREFIX + playerId + FILE_EXTENSION;
                string filePath = Path.Combine(_playerDataFolder, fileName);
                if (!File.Exists(filePath))
                {
                    return null;
                }

                string jsonData = await File.ReadAllTextAsync(filePath);
                PlayerData playerData = JsonConvert.DeserializeObject<PlayerData>(jsonData);
                Player player = ConvertFromPlayerData(playerData);
                Debug.Log($"[PersistenceService] Player data loaded for ID: {playerId}");
                return player;
            }
            catch (Exception exception)
            {
                Debug.LogError($"[PersistenceService] Failed to load player data: {exception.Message}");
                return null;
            }
        }

        public async UniTask<bool> SaveGameConfigurationAsync(GameConfiguration configuration)
        {
            if (configuration == null)
            {
                return false;
            }

            try
            {
                string jsonData = JsonConvert.SerializeObject(configuration, Formatting.Indented);
                await File.WriteAllTextAsync(_configurationFile, jsonData);
                Debug.Log("[PersistenceService] Game configuration saved");
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogError($"[PersistenceService] Failed to save game configuration: {exception.Message}");
                return false;
            }
        }

        public async UniTask<GameConfiguration> LoadGameConfigurationAsync()
        {
            try
            {
                if (!File.Exists(_configurationFile))
                {
                    return GameConfiguration.CreateDefault();
                }

                string jsonData = await File.ReadAllTextAsync(_configurationFile);
                GameConfiguration configuration = JsonConvert.DeserializeObject<GameConfiguration>(jsonData);
                Debug.Log("[PersistenceService] Game configuration loaded");
                return configuration ?? GameConfiguration.CreateDefault();
            }
            catch (Exception exception)
            {
                Debug.LogError($"[PersistenceService] Failed to load game configuration: {exception.Message}");
                return GameConfiguration.CreateDefault();
            }
        }

        public async UniTask<bool> SavePlayerScoresAsync(Dictionary<int, int> scores)
        {
            if (scores == null)
            {
                return false;
            }

            try
            {
                string jsonData = JsonConvert.SerializeObject(scores, Formatting.Indented);
                await File.WriteAllTextAsync(_scoresFile, jsonData);
                Debug.Log("[PersistenceService] Player scores saved");
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogError($"[PersistenceService] Failed to save player scores: {exception.Message}");
                return false;
            }
        }

        public async UniTask<Dictionary<int, int>> LoadPlayerScoresAsync()
        {
            try
            {
                if (!File.Exists(_scoresFile))
                {
                    return new Dictionary<int, int>();
                }

                string jsonData = await File.ReadAllTextAsync(_scoresFile);
                Dictionary<int, int> scores = JsonConvert.DeserializeObject<Dictionary<int, int>>(jsonData);
                Debug.Log("[PersistenceService] Player scores loaded");
                return scores ?? new Dictionary<int, int>();
            }
            catch (Exception exception)
            {
                Debug.LogError($"[PersistenceService] Failed to load player scores: {exception.Message}");
                return new Dictionary<int, int>();
            }
        }

        public async UniTask<bool> SaveSettingsAsync(Dictionary<string, object> settings)
        {
            if (settings == null)
            {
                return false;
            }

            try
            {
                string jsonData = JsonConvert.SerializeObject(settings, Formatting.Indented);
                await File.WriteAllTextAsync(_settingsFile, jsonData);
                Debug.Log("[PersistenceService] Settings saved");
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogError($"[PersistenceService] Failed to save settings: {exception.Message}");
                return false;
            }
        }

        public async UniTask<Dictionary<string, object>> LoadSettingsAsync()
        {
            try
            {
                if (!File.Exists(_settingsFile))
                {
                    return new Dictionary<string, object>();
                }

                string jsonData = await File.ReadAllTextAsync(_settingsFile);
                Dictionary<string, object> settings =
                    JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonData);
                Debug.Log("[PersistenceService] Settings loaded");
                return settings ?? new Dictionary<string, object>();
            }
            catch (Exception exception)
            {
                Debug.LogError($"[PersistenceService] Failed to load settings: {exception.Message}");
                return new Dictionary<string, object>();
            }
        }

        public async UniTask<bool> ClearAllDataAsync()
        {
            try
            {
                // Clear game states
                if (Directory.Exists(_gameStateFolder))
                {
                    Directory.Delete(_gameStateFolder, true);
                    EnsureDirectoryExists(_gameStateFolder);
                }

                // Clear player data
                if (Directory.Exists(_playerDataFolder))
                {
                    Directory.Delete(_playerDataFolder, true);
                    EnsureDirectoryExists(_playerDataFolder);
                }

                // Clear individual files
                if (File.Exists(_configurationFile)) File.Delete(_configurationFile);
                if (File.Exists(_scoresFile)) File.Delete(_scoresFile);
                if (File.Exists(_settingsFile)) File.Delete(_settingsFile);
                Debug.Log("[PersistenceService] All data cleared");
                await UniTask.Yield();
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogError($"[PersistenceService] Failed to clear all data: {exception.Message}");
                return false;
            }
        }

        public async UniTask<long> GetStorageSizeAsync()
        {
            long totalSize = 0;
            try
            {
                // Calculate size of all persistence files
                if (Directory.Exists(_gameStateFolder))
                {
                    totalSize += GetDirectorySize(_gameStateFolder);
                }

                if (Directory.Exists(_playerDataFolder))
                {
                    totalSize += GetDirectorySize(_playerDataFolder);
                }

                if (File.Exists(_configurationFile))
                {
                    totalSize += new FileInfo(_configurationFile).Length;
                }

                if (File.Exists(_scoresFile))
                {
                    totalSize += new FileInfo(_scoresFile).Length;
                }

                if (File.Exists(_settingsFile))
                {
                    totalSize += new FileInfo(_settingsFile).Length;
                }
                
                await UniTask.Yield();
            }
            catch (Exception exception)
            {
                Debug.LogError($"[PersistenceService] Failed to calculate storage size: {exception.Message}");
            }

            return totalSize;
        }

        public bool IsStorageAvailable()
        {
            try
            {
                return Directory.Exists(_persistentDataPath);
            }
            catch
            {
                return false;
            }
        }

        private void EnsureDirectoryExists(string directoryPath)
        {
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }
        }

        private long GetDirectorySize(string directoryPath)
        {
            long size = 0;
            
            try
            {
                string[] files = Directory.GetFiles(directoryPath, "*", SearchOption.AllDirectories);
                for (int index = 0; index < files.Length; index++)
                {
                    FileInfo fileInfo = new FileInfo(files[index]);
                    size += fileInfo.Length;
                }
            }
            catch (Exception exception)
            {
                Debug.LogError($"[PersistenceService] Failed to calculate directory size: {exception.Message}");
            }

            return size;
        }

        private GameStateData ConvertToGameStateData(GameState gameState)
        {
            // Convert GameState to serializable data structure
            return new GameStateData
            {
                CurrentStateType = gameState.CurrentStateType,
                CurrentPlayerIndex = gameState.CurrentPlayerIndex,
                RoundNumber = gameState.RoundNumber,
                // Add other necessary fields for serialization
            };
        }

        private GameState ConvertFromGameStateData(GameStateData gameStateData)
        {
            // Convert serializable data back to GameState
            GameState gameState = new GameState();
            // Initialize with loaded data
            return gameState;
        }

        private PlayerData ConvertToPlayerData(Player player)
        {
            return new PlayerData
            {
                Id = player.Id, Name = player.Name, PlayerType = player.PlayerType, Score = player.Score
            };
        }

        private Player ConvertFromPlayerData(PlayerData playerData)
        {
            Player player = new Player(playerData.Id, playerData.Name, playerData.PlayerType);
            player.SetScore(playerData.Score);
            return player;
        }
    }

    [Serializable]
    public sealed class GameStateData
    {
        public Runtime.Domain.Enums.GameStateType CurrentStateType;
        public int CurrentPlayerIndex;

        public int RoundNumber;
        // Add other serializable fields as needed
    }

    [Serializable]
    public sealed class PlayerData
    {
        public int Id;
        public string Name;
        public Runtime.Domain.Enums.PlayerType PlayerType;
        public int Score;
    }
}