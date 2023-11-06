using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;

public class EnemyController : CharacterInfo
{

    private GameObject _weaponSign;

    private ArrowTranslator _arrowTranslator;

    [SerializeField] private OverlayTile _nextMove;

    private Vector2Int _lastMove;

    [SerializeField] private int _stepsCounter;

    [SerializeField]
    protected int _visibilityRange;

    protected override void Awake()
    {
        base.Awake();
    }

    private void Start()
    {
        _weapon = (Weapon)Random.Range( 0, 3 );
        _weaponSignPos = transform.GetChild( 0 ).localPosition;

        _arrowTranslator = new ArrowTranslator();

        _path = new List<OverlayTile>();
        //isMoving = false;
        _rangeFinderTiles = new List<OverlayTile>();

        var randomTile = MapManager.Instance.GetRandomEdgeTile();

        while(randomTile.isOccupied)
        {
            randomTile = MapManager.Instance.GetRandomEdgeTile();
        }

        PositionCharacterOnTile( randomTile );





        GetInRangeTiles();
    }

    void LateUpdate()
    {
        if(_nextMove != null)
        {
            _path = _pathFinder.FindPath( StandingOnTile, _nextMove, _rangeFinderTiles );

            for(int i = 0; i < _path.Count; i++)
            {
                var previousTile = i > 0 ? _path[ i - 1 ] : StandingOnTile;
                var futureTile = i < _path.Count - 1 ? _path[ i + 1 ] : null;

                var arrow = _arrowTranslator.TranslateDirection( previousTile, _path[ i ], futureTile );
                _path[ i ].SetSprite( arrow );
            }

            Status = Status.Moving;
        }

        if(_path.Count > 0 && Status == Status.Moving)
        {
            MoveAlongPath();
        }
    }

    public void MoveTo( OverlayTile tile )
    {
        var path = _pathFinder.FindPath( StandingOnTile, tile, _rangeFinderTiles );

        Debug.Log( "asdçlfkj: " + path.Count );
        if(path.Count <= 1)
        {
            OnCharacterActed( new CharacterMove()
            {
                Action = CharacterAction.Attack,
                Character = this,
                Type = _type
            } ) ;
        }
        else
        {
            _nextMove = tile;
            _stepsCounter = _movementRange;
        }

    }

    //private bool CanAttack()
    //{
    //    _path = _pathFinder.FindPath( StandingOnTile, _nextMove, _rangeFinderTiles );
    //}

    private void MoveAlongPath()
    {
        if(_path.Count == 1)
        {
            GetInRangeTiles();
            Status = Status.Awaiting;
            _nextMove = null;

            _weapon = MapManager.Instance.ProcessMovement( _weapon, _lastMove, out Sprite sprite );
            SetSign();
            return;
        }


        var step = _speed * Time.deltaTime;

        float zIndex = _path[ 0 ].transform.position.z;
        transform.position = Vector2.MoveTowards( transform.position, _path[ 0 ].transform.position, step );
        transform.position = new Vector3( transform.position.x, transform.position.y, zIndex );

        if(Vector2.Distance( transform.position, _path[ 0 ].transform.position ) < 0.00001f)
        {
            PositionCharacterOnTile( _path[ 0 ] );

            _path[ 0 ].isOccupied = true;
            _path[ 0 ].Previous.isOccupied = false;

            _path.RemoveAt( 0 );


            _stepsCounter--;

            if(_stepsCounter == 0)
            {
                GetInRangeTiles();
                Status = Status.Awaiting;
                _nextMove = null;

                _weapon = MapManager.Instance.ProcessMovement( _weapon, _lastMove, out Sprite sprite );
                SetSign();
            }
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

}
