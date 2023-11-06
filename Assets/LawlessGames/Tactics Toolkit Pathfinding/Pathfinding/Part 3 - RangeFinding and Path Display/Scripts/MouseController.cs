using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MouseController : CharacterInfo
{
    
    
    private GameObject _weaponSign;

    public GameObject cursor;

    
    private ArrowTranslator arrowTranslator;
    
    private bool _isMoving;
    private Vector2Int _lastMove;

    private void Start()
    {
        _weapon = (Weapon)Random.Range( 0, 3 );
        _weaponSignPos = transform.GetChild( 0 ).localPosition;
        MapManager.Instance.SetHelperPattern( _weapon );

        arrowTranslator = new ArrowTranslator();

        _isMoving = false;        
    }

    void LateUpdate()
    {
        RaycastHit2D? hit = GetFocusedOnTile();

        if(hit.HasValue)
        {
            Status = Status.Moving;

            OverlayTile tile = hit.Value.collider.gameObject.GetComponent<OverlayTile>();
            cursor.transform.position = tile.transform.position;
            cursor.gameObject.GetComponent<SpriteRenderer>().sortingOrder = tile.transform.GetComponent<SpriteRenderer>().sortingOrder +1;

            if(_rangeFinderTiles.Contains( tile ) && !_isMoving)
            {
                _path = _pathFinder.FindPath( StandingOnTile, tile, _rangeFinderTiles );

                foreach(var item in _rangeFinderTiles)
                {
                    MapManager.Instance.Map[ item.grid2DLocation ].SetSprite( ArrowDirection.None );
                }

                for(int i = 0; i < _path.Count; i++)
                {
                    var previousTile = i > 0 ? _path[ i - 1 ] : StandingOnTile;
                    var futureTile = i < _path.Count - 1 ? _path[ i + 1 ] : null;

                    var arrow = arrowTranslator.TranslateDirection( previousTile, _path[ i ], futureTile );
                    _path[ i ].SetSprite( arrow );
                }
            }

            if(Input.GetMouseButtonDown( 0 ))
            {

                if(tile.Previous != null)
                {
                    _lastMove = tile.grid2DLocation - tile.Previous.grid2DLocation;
                }


                tile.ShowTile();

                if(!_isSpawned)
                {
                    PositionCharacterOnTile( tile );
                    _spriteRenderer.sortingOrder = 3;
                    GetInRangeTiles();
                    SetSign();
                }
                else
                {
                    _isMoving = true;
                    tile.gameObject.GetComponent<OverlayTile>().HideTile();
                }
            }
        }

        if(_path.Count > 0 && _isMoving)
        {
            MoveAlongPath();
        }
    }

    private void MoveAlongPath()
    {
        var step = _speed * Time.deltaTime;

        float zIndex = _path[ 0 ].transform.position.z;
        transform.position = Vector2.MoveTowards( transform.position, _path[ 0 ].transform.position, step );
        transform.position = new Vector3( transform.position.x, transform.position.y, zIndex );

        if(Vector2.Distance( transform.position, _path[ 0 ].transform.position ) < 0.00001f)
        {
            PositionCharacterOnTile( _path[ 0 ] );
            _path[ 0 ].CharacterOnIt = this;
            _path[ 0 ].Previous.CharacterOnIt = null;
            _path.RemoveAt( 0 );
        }

        if(_path.Count == 0)
        {
            GetInRangeTiles();
            _isMoving = false;

            _weapon = MapManager.Instance.ProcessMovement( _weapon, _lastMove, out Sprite sprite );
            SetSign();

            OnCharacterActed( new CharacterMove() {
                Action = CharacterAction.Move,
                Character = this,
                Type = _type
            } );
        }
    }

    private void SetSign()
    {
        if(_weaponSign)
            Destroy( _weaponSign );

        switch(_weapon)
        {
            case Weapon.ROCK:
                _weaponSign = Instantiate( _rockPrefab, transform );
                break;
            case Weapon.PAPER:
                _weaponSign = Instantiate( _paperPrefab, transform );
                break;
            case Weapon.SCISSORS:
                _weaponSign = Instantiate( _scissorsPrefab, transform );
                break;
        }
        _weaponSign.transform.localPosition = _weaponSignPos;
    }

    private static RaycastHit2D? GetFocusedOnTile()
    {
        Vector3 mousePos = Camera.main.ScreenToWorldPoint( Input.mousePosition );
        Vector2 mousePos2D = new Vector2( mousePos.x, mousePos.y );

        RaycastHit2D[] hits = Physics2D.RaycastAll( mousePos2D, Vector2.zero );

        if(hits.Length > 0)
        {
            return hits.OrderByDescending( i => i.collider.transform.position.z ).First();
        }

        return null;
    }
}