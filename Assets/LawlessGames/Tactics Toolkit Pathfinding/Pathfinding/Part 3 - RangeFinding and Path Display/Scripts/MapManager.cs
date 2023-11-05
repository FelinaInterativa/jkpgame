using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;

public class MapManager : MonoBehaviour
{
    private static MapManager _instance;
    public static MapManager Instance { get { return _instance; } }

    public GameObject overlayPrefab;
    public GameObject overlayContainer;

    private Dictionary<Vector2Int, OverlayTile> _map;
    public Dictionary<Vector2Int, OverlayTile> Map => _map;

    private Dictionary<Vector2Int, OverlayTile> _edgeMap = new Dictionary<Vector2Int, OverlayTile>();
    public Dictionary<Vector2Int, OverlayTile> EdgeMap => _edgeMap;


    private IOrderedEnumerable<Tilemap> _tilemaps;
    public IOrderedEnumerable<Tilemap> Tilemaps => _tilemaps;

    [SerializeField]
    private Sprite[] _weaponsSprites;

    [SerializeField]
    private Color[] _weaponColor;

    private Dictionary<string, Sprite> _sprites = new Dictionary<string, Sprite>();

    [SerializeField]
    private List<Weapon> _horizontalWeaponsSequence = new List<Weapon>();
    [SerializeField]
    private List<Weapon> _verticalWeaponsSequence = new List<Weapon>();

    [SerializeField]
    private SpriteRenderer _up;
    [SerializeField]
    private SpriteRenderer _left;
    [SerializeField]
    private SpriteRenderer _right;
    [SerializeField]
    private SpriteRenderer _down;

    private event Action _onMapBuilded;
    public event Action OnMapBuilded
    {
        add { _onMapBuilded += value; }
        remove { _onMapBuilded -= value; }
    }

    private void Awake()
    {
        if(_instance != null && _instance != this)
        {
            Destroy( this.gameObject );
        }
        else
        {
            _instance = this;
        }
    }

    void Start()
    {
        RandomizePattern();
        SetTiles();
    }

    private void SetTiles()
    {
        _tilemaps = gameObject.transform.GetComponentsInChildren<Tilemap>().OrderByDescending( x => x.GetComponent<TilemapRenderer>().sortingOrder );
        _map = new Dictionary<Vector2Int, OverlayTile>();

        foreach(var tm in _tilemaps)
        {
            BoundsInt bounds = tm.cellBounds;

            for(int z = bounds.max.z; z > bounds.min.z; z--)
            {
                for(int y = bounds.min.y; y < bounds.max.y; y++)
                {
                    for(int x = bounds.min.x; x < bounds.max.x; x++)
                    {
                        if(tm.HasTile( new Vector3Int( x, y, z ) ))
                        {
                            if(!_map.ContainsKey( new Vector2Int( x, y ) ))
                            {
                                var overlayTile = Instantiate( overlayPrefab, overlayContainer.transform );
                                var cellWorldPosition = tm.GetCellCenterWorld( new Vector3Int( x, y, z ) );
                                overlayTile.transform.position = new Vector3( cellWorldPosition.x, cellWorldPosition.y, cellWorldPosition.z + 1 );
                                overlayTile.GetComponent<SpriteRenderer>().sortingOrder = tm.GetComponent<TilemapRenderer>().sortingOrder;
                                overlayTile.gameObject.GetComponent<OverlayTile>().gridLocation = new Vector3Int( x, y, z );

                                _map.Add( new Vector2Int( x, y ), overlayTile.gameObject.GetComponent<OverlayTile>() );
                                var tilebase = tm.GetTile( new Vector3Int( x, y, z ) );
                                if(IsEdgeTile( new Vector2Int( x, y ) ))
                                {
                                    _edgeMap.Add( new Vector2Int( x, y ), overlayTile.gameObject.GetComponent<OverlayTile>() );
                                }
                            }
                        }
                    }
                }
            }
        }

        _onMapBuilded?.Invoke();
    }

    private bool IsEdgeTile( Vector2Int originTile )
    {

        var surroundingTiles = new List<OverlayTile>();


        Vector2Int TileToCheck = new Vector2Int( originTile.x + 1, originTile.y );
        if(_map.ContainsKey( TileToCheck ))
        {
            if(Mathf.Abs( _map[ TileToCheck ].transform.position.z - _map[ originTile ].transform.position.z ) <= 1)
                surroundingTiles.Add( _map[ TileToCheck ] );
        }

        TileToCheck = new Vector2Int( originTile.x - 1, originTile.y );
        if(_map.ContainsKey( TileToCheck ))
        {
            if(Mathf.Abs( _map[ TileToCheck ].transform.position.z - _map[ originTile ].transform.position.z ) <= 1)
                surroundingTiles.Add( _map[ TileToCheck ] );
        }

        TileToCheck = new Vector2Int( originTile.x, originTile.y + 1 );
        if(_map.ContainsKey( TileToCheck ))
        {
            if(Mathf.Abs( _map[ TileToCheck ].transform.position.z - _map[ originTile ].transform.position.z ) <= 1)
                surroundingTiles.Add( _map[ TileToCheck ] );
        }

        TileToCheck = new Vector2Int( originTile.x, originTile.y - 1 );
        if(_map.ContainsKey( TileToCheck ))
        {
            if(Mathf.Abs( _map[ TileToCheck ].transform.position.z - _map[ originTile ].transform.position.z ) <= 1)
                surroundingTiles.Add( _map[ TileToCheck ] );
        }

        return surroundingTiles.Count < 4;
    }



    public List<OverlayTile> GetTilesInRange( Vector2Int location )
    {
        var startingTile = Map[ location ];
        var inRangeTiles = new List<OverlayTile>();
        int stepCount = 0;

        inRangeTiles.Add( startingTile );

        //Should contain the surroundingTiles of the previous step. 
        var tilesForPreviousStep = new List<OverlayTile>();
        tilesForPreviousStep.Add( startingTile );
        while(stepCount < 1)
        {
            var surroundingTiles = new List<OverlayTile>();

            foreach(var item in tilesForPreviousStep)
            {
                surroundingTiles.AddRange( MapManager.Instance.GetSurroundingTiles( new Vector2Int( item.gridLocation.x, item.gridLocation.y ) ) );
            }

            inRangeTiles.AddRange( surroundingTiles );
            tilesForPreviousStep = surroundingTiles.Distinct().ToList();
            stepCount++;
        }

        return inRangeTiles.Distinct().ToList();
    }





    private void FindMapEdges()
    {
        foreach(var tilemap in _tilemaps)
        {
            var bounds = tilemap.cellBounds;
            for(int x = bounds.xMin; x < bounds.xMax; x++)
            {
                for(int y = bounds.yMin; y < bounds.yMax; y++)
                {
                    Vector3Int tilePosition = new Vector3Int( x, y, 0 );
                    TileBase tileBase = tilemap.GetTile( tilePosition );

                    if(tileBase != null)
                    {
                        Vector3Int above = new Vector3Int( x, y + 1, 0 );
                        Vector3Int below = new Vector3Int( x, y - 1, 0 );
                        Vector3Int left = new Vector3Int( x - 1, y, 0 );
                        Vector3Int right = new Vector3Int( x + 1, y, 0 );

                        if(tilemap.GetTile( above ) == null || tilemap.GetTile( below ) == null || tilemap.GetTile( left ) == null || tilemap.GetTile( right ) == null)
                        {
                            // This tile is an edge tile.
                        }
                    }
                }
            }

        }
    }



    public List<OverlayTile> GetSurroundingTiles( Vector2Int originTile )
    {
        var surroundingTiles = new List<OverlayTile>();


        Vector2Int TileToCheck = new Vector2Int( originTile.x + 1, originTile.y );
        if(_map.ContainsKey( TileToCheck ))
        {
            if(Mathf.Abs( _map[ TileToCheck ].transform.position.z - _map[ originTile ].transform.position.z ) <= 1)
                surroundingTiles.Add( _map[ TileToCheck ] );
        }

        TileToCheck = new Vector2Int( originTile.x - 1, originTile.y );
        if(_map.ContainsKey( TileToCheck ))
        {
            if(Mathf.Abs( _map[ TileToCheck ].transform.position.z - _map[ originTile ].transform.position.z ) <= 1)
                surroundingTiles.Add( _map[ TileToCheck ] );
        }

        TileToCheck = new Vector2Int( originTile.x, originTile.y + 1 );
        if(_map.ContainsKey( TileToCheck ))
        {
            if(Mathf.Abs( _map[ TileToCheck ].transform.position.z - _map[ originTile ].transform.position.z ) <= 1)
                surroundingTiles.Add( _map[ TileToCheck ] );
        }

        TileToCheck = new Vector2Int( originTile.x, originTile.y - 1 );
        if(_map.ContainsKey( TileToCheck ))
        {
            if(Mathf.Abs( _map[ TileToCheck ].transform.position.z - _map[ originTile ].transform.position.z ) <= 1)
                surroundingTiles.Add( _map[ TileToCheck ] );
        }

        return surroundingTiles;
    }

    private void RandomizePattern()
    {
        foreach(var item in _weaponsSprites)
        {
            _sprites.Add( item.name.ToString().ToLower(), item );
        }

        var horizontalIndexes = new List<int>() { 0, 1, 2 };

        while(horizontalIndexes.Count > 0)
        {
            var index = UnityEngine.Random.Range( 0, horizontalIndexes.Count - 1 );
            var weapon = (Weapon)horizontalIndexes[ index ];
            horizontalIndexes.RemoveAt( index );
            _horizontalWeaponsSequence.Add( weapon );
        }

        var verticalIndexes = new List<int>() { 0, 1, 2 };

        while(verticalIndexes.Count > 0)
        {
            var index = UnityEngine.Random.Range( 0, verticalIndexes.Count - 1 );
            var weapon = (Weapon)verticalIndexes[ index ];
            verticalIndexes.RemoveAt( index );
            _verticalWeaponsSequence.Add( weapon );
        }
    }

    public void SetHelperPattern( Weapon weapon )
    {
        //player weapon
        var currentIndex = _verticalWeaponsSequence.IndexOf( weapon );

        var upIndex = (int)Mathf.Repeat( currentIndex + 1, _verticalWeaponsSequence.Count );
        var downIndex = (int)Mathf.Repeat( currentIndex - 1, _verticalWeaponsSequence.Count );

        var upEnum = _verticalWeaponsSequence[ upIndex ];
        _sprites.TryGetValue( upEnum.ToString().ToLower(), out Sprite upSprite );
        _up.sprite = upSprite;

        var downEnum = _verticalWeaponsSequence[ downIndex ];
        _sprites.TryGetValue( downEnum.ToString().ToLower(), out Sprite downSprite );
        _down.sprite = downSprite;

        var leftEnum = _horizontalWeaponsSequence[ (int)Mathf.Repeat( currentIndex - 1, _horizontalWeaponsSequence.Count ) ];
        _sprites.TryGetValue( leftEnum.ToString().ToLower(), out Sprite leftSprite );
        _left.sprite = leftSprite;

        var rightEnum = _horizontalWeaponsSequence[ (int)Mathf.Repeat( currentIndex + 1, _horizontalWeaponsSequence.Count ) ];
        _sprites.TryGetValue( rightEnum.ToString().ToLower(), out Sprite rightSprite );
        _right.sprite = rightSprite;
    }

    public Weapon ProcessMovement( Weapon weapon, Vector2Int deltaMovement, out Sprite sprite )
    {
        Weapon weaponEnum = Weapon.ROCK;
        if(deltaMovement.y != 0)
        {
            var currentVerticalIndex = _verticalWeaponsSequence.IndexOf( weapon );
            var index = (int)Mathf.Repeat( currentVerticalIndex + deltaMovement.y, _verticalWeaponsSequence.Count );
            weaponEnum = _verticalWeaponsSequence[ index ];
        }
        if(deltaMovement.x != 0)
        {
            var currentHorizontalIndex = _horizontalWeaponsSequence.IndexOf( weapon );
            var index = (int)Mathf.Repeat( currentHorizontalIndex + deltaMovement.x, _horizontalWeaponsSequence.Count );
            weaponEnum = _horizontalWeaponsSequence[ index ];
        }

        _sprites.TryGetValue( weaponEnum.ToString().ToLower(), out sprite );
        SetHelperPattern( weaponEnum );

        return weaponEnum;
    }

    public OverlayTile GetRandomTile()
    {
        return Map.ElementAt( UnityEngine.Random.Range( 0, Map.Count - 1 ) ).Value;
    }

    public OverlayTile GetRandomEdgeTile()
    {
        return EdgeMap.ElementAt( UnityEngine.Random.Range( 0, EdgeMap.Count - 1 ) ).Value;
    }
}