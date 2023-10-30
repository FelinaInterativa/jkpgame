using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

namespace finished3
{
    public class MapManager : MonoBehaviour
    {
        private static MapManager _instance;
        public static MapManager Instance { get { return _instance; } }

        public GameObject overlayPrefab;
        public GameObject overlayContainer;

        public Dictionary<Vector2Int, OverlayTile> map;


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
            var tileMaps = gameObject.transform.GetComponentsInChildren<Tilemap>().OrderByDescending( x => x.GetComponent<TilemapRenderer>().sortingOrder );
            map = new Dictionary<Vector2Int, OverlayTile>();

            foreach(var tm in tileMaps)
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
                                if(!map.ContainsKey( new Vector2Int( x, y ) ))
                                {
                                    var overlayTile = Instantiate( overlayPrefab, overlayContainer.transform );
                                    var cellWorldPosition = tm.GetCellCenterWorld( new Vector3Int( x, y, z ) );
                                    overlayTile.transform.position = new Vector3( cellWorldPosition.x, cellWorldPosition.y, cellWorldPosition.z + 1 );
                                    overlayTile.GetComponent<SpriteRenderer>().sortingOrder = tm.GetComponent<TilemapRenderer>().sortingOrder;
                                    overlayTile.gameObject.GetComponent<OverlayTile>().gridLocation = new Vector3Int( x, y, z );

                                    map.Add( new Vector2Int( x, y ), overlayTile.gameObject.GetComponent<OverlayTile>() );
                                }
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
            if(map.ContainsKey( TileToCheck ))
            {
                if(Mathf.Abs( map[ TileToCheck ].transform.position.z - map[ originTile ].transform.position.z ) <= 1)
                    surroundingTiles.Add( map[ TileToCheck ] );
            }

            TileToCheck = new Vector2Int( originTile.x - 1, originTile.y );
            if(map.ContainsKey( TileToCheck ))
            {
                if(Mathf.Abs( map[ TileToCheck ].transform.position.z - map[ originTile ].transform.position.z ) <= 1)
                    surroundingTiles.Add( map[ TileToCheck ] );
            }

            TileToCheck = new Vector2Int( originTile.x, originTile.y + 1 );
            if(map.ContainsKey( TileToCheck ))
            {
                if(Mathf.Abs( map[ TileToCheck ].transform.position.z - map[ originTile ].transform.position.z ) <= 1)
                    surroundingTiles.Add( map[ TileToCheck ] );
            }

            TileToCheck = new Vector2Int( originTile.x, originTile.y - 1 );
            if(map.ContainsKey( TileToCheck ))
            {
                if(Mathf.Abs( map[ TileToCheck ].transform.position.z - map[ originTile ].transform.position.z ) <= 1)
                    surroundingTiles.Add( map[ TileToCheck ] );
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
                var index = Random.Range( 0, horizontalIndexes.Count - 1 );
                var weapon = (Weapon)horizontalIndexes[ index ];
                horizontalIndexes.RemoveAt( index );
                _horizontalWeaponsSequence.Add( weapon );
            }

            var verticalIndexes = new List<int>() { 0, 1, 2 };

            while(verticalIndexes.Count > 0)
            {
                var index = Random.Range( 0, verticalIndexes.Count - 1 );
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

    }
}
