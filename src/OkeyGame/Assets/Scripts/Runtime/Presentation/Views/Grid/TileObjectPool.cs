using System;
using System.Collections.Generic;
using UnityEngine;
using Runtime.Domain.Models;
using Runtime.Domain.ValueObjects;

namespace Runtime.Presentation.Views.Grid
{
    public sealed class TileObjectPool : MonoBehaviour, IDisposable
    {
        [Header("Pool Configuration")]
        [SerializeField] private GameObject _tilePrefab;
        [SerializeField] private int _initialPoolSize = 30;
        [SerializeField] private int _maxPoolSize = 50;
        [SerializeField] private bool _allowPoolExpansion = true;

        [Header("Pool Organization")]
        [SerializeField] private Transform _poolContainer;
        [SerializeField] private Transform _activeContainer;

        private readonly Stack<GameObject> _availableTiles = new Stack<GameObject>();
        private readonly HashSet<GameObject> _activeTiles = new HashSet<GameObject>();
        private readonly Dictionary<GameObject, TilePoolData> _tileData = new Dictionary<GameObject, TilePoolData>();

        private bool _isInitialized;
        private int _totalCreatedTiles;

        public event Action<GameObject> OnTileCreated;
        public event Action<GameObject> OnTileReturned;
        public event Action<GameObject> OnTileDestroyed;

        public int AvailableCount => _availableTiles.Count;
        public int ActiveCount => _activeTiles.Count;
        public int TotalCount => _totalCreatedTiles;
        public bool IsInitialized => _isInitialized;

        private sealed class TilePoolData
        {
            public OkeyPiece associatedPiece;
            public GridPosition gridPosition;
            public bool isInUse;
            public float lastUsedTime;
            public int usageCount;

            public TilePoolData()
            {
                Reset();
            }

            public void Reset()
            {
                associatedPiece = null;
                gridPosition = new GridPosition(-1, -1);
                isInUse = false;
                lastUsedTime = Time.time;
            }
        }

        private void Awake()
        {
            SetupPoolContainers();
        }

        private void Start()
        {
            InitializePool();
        }

        private void SetupPoolContainers()
        {
            if (_poolContainer == null)
            {
                GameObject poolContainerObj = new GameObject("TilePool_Available");
                poolContainerObj.transform.SetParent(transform);
                _poolContainer = poolContainerObj.transform;
            }

            if (_activeContainer == null)
            {
                GameObject activeContainerObj = new GameObject("TilePool_Active");
                activeContainerObj.transform.SetParent(transform);
                _activeContainer = activeContainerObj.transform;
            }
        }

        public void InitializePool()
        {
            if (_isInitialized)
            {
                return;
            }

            if (_tilePrefab == null)
            {
                Debug.LogError("TileObjectPool: Tile prefab is not assigned!");
                return;
            }

            CreateInitialTiles();
            _isInitialized = true;
        }

        private void CreateInitialTiles()
        {
            for (int index = 0; index < _initialPoolSize; index++)
            {
                GameObject tileObject = CreateNewTileObject();
                if (tileObject != null)
                {
                    ReturnTileToPool(tileObject);
                }
            }
        }

        private GameObject CreateNewTileObject()
        {
            if (_totalCreatedTiles >= _maxPoolSize && !_allowPoolExpansion)
            {
                return null;
            }

            GameObject tileObject = Instantiate(_tilePrefab, _poolContainer);
            tileObject.name = $"PooledTile_{_totalCreatedTiles}";
            
            // Initialize tile components
            InitializeTileComponents(tileObject);
            
            // Create pool data
            TilePoolData poolData = new TilePoolData();
            _tileData[tileObject] = poolData;
            
            _totalCreatedTiles++;
            OnTileCreated?.Invoke(tileObject);
            
            return tileObject;
        }

        private void InitializeTileComponents(GameObject tileObject)
        {
            // Ensure the tile has required components
            TileView tileView = tileObject.GetComponent<TileView>();
            if (tileView == null)
            {
                tileView = tileObject.AddComponent<TileView>();
            }

            DraggableTile draggableTile = tileObject.GetComponent<DraggableTile>();
            if (draggableTile == null)
            {
                draggableTile = tileObject.AddComponent<DraggableTile>();
            }

            // Set initial state
            tileObject.SetActive(false);
        }

        public GameObject GetTile(OkeyPiece piece, GridPosition gridPosition)
        {
            if (piece == null)
            {
                return null;
            }

            GameObject tileObject = GetAvailableTile();
            if (tileObject == null)
            {
                return null;
            }

            // Configure the tile
            ConfigureTile(tileObject, piece, gridPosition);
            
            // Move to active container
            tileObject.transform.SetParent(_activeContainer);
            tileObject.SetActive(true);
            
            // Update tracking
            _activeTiles.Add(tileObject);
            
            if (_tileData.TryGetValue(tileObject, out TilePoolData poolData))
            {
                poolData.isInUse = true;
                poolData.associatedPiece = piece;
                poolData.gridPosition = gridPosition;
                poolData.lastUsedTime = Time.time;
                poolData.usageCount++;
            }

            return tileObject;
        }

        private GameObject GetAvailableTile()
        {
            if (_availableTiles.Count > 0)
            {
                return _availableTiles.Pop();
            }

            // Try to create a new tile if pool expansion is allowed
            if (_allowPoolExpansion && _totalCreatedTiles < _maxPoolSize)
            {
                return CreateNewTileObject();
            }

            return null;
        }

        private void ConfigureTile(GameObject tileObject, OkeyPiece piece, GridPosition gridPosition)
        {
            // Initialize TileView
            TileView tileView = tileObject.GetComponent<TileView>();
            if (tileView != null)
            {
                tileView.Initialize(piece);
            }

            // Initialize DraggableTile (will be set up by the grid manager)
            DraggableTile draggableTile = tileObject.GetComponent<DraggableTile>();
            if (draggableTile != null)
            {
                // Grid manager will call Initialize on this component
            }
        }

        public void ReturnTile(GameObject tileObject)
        {
            if (tileObject == null || !_activeTiles.Contains(tileObject))
            {
                return;
            }

            // Clean up the tile
            CleanupTile(tileObject);
            
            // Return to pool
            ReturnTileToPool(tileObject);
            
            // Update tracking
            _activeTiles.Remove(tileObject);
            
            if (_tileData.TryGetValue(tileObject, out TilePoolData poolData))
            {
                poolData.Reset();
            }

            OnTileReturned?.Invoke(tileObject);
        }

        private void CleanupTile(GameObject tileObject)
        {
            // Reset visual state
            TileView tileView = tileObject.GetComponent<TileView>();
            if (tileView != null)
            {
                tileView.ResetVisualState();
                tileView.SetInteractable(false);
            }

            // Clean up draggable component
            DraggableTile draggableTile = tileObject.GetComponent<DraggableTile>();
            if (draggableTile != null)
            {
                // The draggable tile will clean itself up
            }

            // Reset transform
            tileObject.transform.localPosition = Vector3.zero;
            tileObject.transform.localRotation = Quaternion.identity;
            tileObject.transform.localScale = Vector3.one;
        }

        private void ReturnTileToPool(GameObject tileObject)
        {
            tileObject.SetActive(false);
            tileObject.transform.SetParent(_poolContainer);
            _availableTiles.Push(tileObject);
        }

        public void ReturnAllTiles()
        {
            List<GameObject> activeTilesList = new List<GameObject>(_activeTiles);
            
            foreach (GameObject tileObject in activeTilesList)
            {
                ReturnTile(tileObject);
            }
        }

        public void PrewarmPool(int additionalTiles)
        {
            int tilesToCreate = Mathf.Min(additionalTiles, _maxPoolSize - _totalCreatedTiles);
            
            for (int index = 0; index < tilesToCreate; index++)
            {
                GameObject tileObject = CreateNewTileObject();
                if (tileObject != null)
                {
                    ReturnTileToPool(tileObject);
                }
            }
        }

        public void TrimPool(int targetSize)
        {
            targetSize = Mathf.Max(0, targetSize);
            
            while (_availableTiles.Count > targetSize)
            {
                GameObject tileObject = _availableTiles.Pop();
                if (tileObject != null)
                {
                    _tileData.Remove(tileObject);
                    OnTileDestroyed?.Invoke(tileObject);
                    DestroyImmediate(tileObject);
                    _totalCreatedTiles--;
                }
            }
        }

        public void ClearPool()
        {
            // Return all active tiles
            ReturnAllTiles();
            
            // Destroy all tiles
            while (_availableTiles.Count > 0)
            {
                GameObject tileObject = _availableTiles.Pop();
                if (tileObject != null)
                {
                    _tileData.Remove(tileObject);
                    OnTileDestroyed?.Invoke(tileObject);
                    DestroyImmediate(tileObject);
                }
            }

            _totalCreatedTiles = 0;
        }

        public PoolStatistics GetStatistics()
        {
            return new PoolStatistics
            {
                totalTiles = _totalCreatedTiles,
                availableTiles = _availableTiles.Count,
                activeTiles = _activeTiles.Count,
                maxPoolSize = _maxPoolSize,
                poolUtilization = _totalCreatedTiles > 0 ? (float)_activeTiles.Count / _totalCreatedTiles : 0f
            };
        }

        public bool IsValidTile(GameObject tileObject)
        {
            return tileObject != null && _tileData.ContainsKey(tileObject);
        }

        public OkeyPiece GetAssociatedPiece(GameObject tileObject)
        {
            if (_tileData.TryGetValue(tileObject, out TilePoolData poolData))
            {
                return poolData.associatedPiece;
            }
            return null;
        }

        public GridPosition GetGridPosition(GameObject tileObject)
        {
            if (_tileData.TryGetValue(tileObject, out TilePoolData poolData))
            {
                return poolData.gridPosition;
            }
            return new GridPosition(-1, -1);
        }

        public void SetMaxPoolSize(int newMaxSize)
        {
            _maxPoolSize = Mathf.Max(1, newMaxSize);
            
            // Trim pool if necessary
            if (_totalCreatedTiles > _maxPoolSize)
            {
                TrimPool(_maxPoolSize);
            }
        }

        public void SetAllowPoolExpansion(bool allowExpansion)
        {
            _allowPoolExpansion = allowExpansion;
        }

        public void Dispose()
        {
            ClearPool();
            _tileData.Clear();
            _isInitialized = false;
        }

        private void OnDestroy()
        {
            Dispose();
        }

        private void OnValidate()
        {
            if (_initialPoolSize < 0)
            {
                _initialPoolSize = 0;
            }

            if (_maxPoolSize < _initialPoolSize)
            {
                _maxPoolSize = _initialPoolSize;
            }
        }

        [Serializable]
        public struct PoolStatistics
        {
            public int totalTiles;
            public int availableTiles;
            public int activeTiles;
            public int maxPoolSize;
            public float poolUtilization;

            public override string ToString()
            {
                return $"Pool Stats - Total: {totalTiles}, Available: {availableTiles}, Active: {activeTiles}, Utilization: {poolUtilization:P1}";
            }
        }
    }
}
