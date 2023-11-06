using System;
using System.Collections.Generic;
using UnityEngine;

public enum CharacterType
{
    Player, Enemy
}
public enum Status
{
    Outside,
    Moving,
    Awaiting
}

public enum CharacterAction
{
    Spawn,
    Move,
    Attack,
    Die
}

public class CharacterMove
{
    public CharacterInfo Character;
    public CharacterType Type;
    public CharacterAction Action;
}

public class CharacterInfo : MonoBehaviour
{
    private OverlayTile _standingOnTile;
    public OverlayTile StandingOnTile
    {
        get => _standingOnTile;
        set => _standingOnTile = value;
    }
    
    [SerializeField]
    protected CharacterType _type;
    public CharacterType Type => _type;
    
    [SerializeField]
    protected Status Status;

    protected Vector3 _weaponSignPos;

    protected bool _isSpawned;

    [SerializeField]
    protected Weapon _weapon;
    public Weapon Weapon => _weapon;

    [SerializeField]
    private int _lifeLeft = 100;

    [SerializeField]
    private int _damage = 100;
    public int Damage => _damage;

    [SerializeField]
    protected float _speed;

    [SerializeField]
    protected int _movementRange;

    [SerializeField]
    protected List<OverlayTile> _path;

    protected PathFinder _pathFinder;

    protected RangeFinder _rangeFinder;

    [SerializeField]
    protected List<OverlayTile> _rangeFinderTiles;

    protected SpriteRenderer _spriteRenderer;

    [SerializeField] protected GameObject _rockPrefab;
    [SerializeField] protected GameObject _paperPrefab;
    [SerializeField] protected GameObject _scissorsPrefab;

    public delegate void CharacterActionEvent( CharacterMove action );

    public static event CharacterActionEvent CharacterActed;

    protected void OnCharacterActed( CharacterMove action )
    {
        CharacterActed?.Invoke( action );
    }

    protected virtual void Awake()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
        switch(_type)
        {
            case CharacterType.Player:
                _spriteRenderer.sortingOrder = 0;
                break;
            case CharacterType.Enemy:
                _spriteRenderer.sortingOrder = 3;
                break;
        }
        Status = Status.Outside;
        _pathFinder = new PathFinder();
        _rangeFinder = new RangeFinder();
        _rangeFinderTiles = new List<OverlayTile>();
        _path = new List<OverlayTile>();
    }

    protected void PositionCharacterOnTile( OverlayTile tile )
    {
        tile.isOccupied = true;

        if(tile.Previous != null)
            tile.Previous.isOccupied = false;

        StandingOnTile = tile;
        transform.position = new Vector3( tile.transform.position.x, tile.transform.position.y + 0.0001f, tile.transform.position.z );
        //GetComponent<SpriteRenderer>().sortingOrder = tile.GetComponent<SpriteRenderer>().sortingOrder;
        GetComponent<SpriteRenderer>().sortingOrder = 3;

        if(!_isSpawned)
        {
            _isSpawned = true;
            OnCharacterActed( new CharacterMove()
            {
                Action = CharacterAction.Spawn,
                Character = this,
                Type = _type
            }
            );
            Status = Status.Awaiting;
            GetInRangeTiles();
        }
    }

    protected void GetInRangeTiles()
    {
        switch(_type)
        {
            case CharacterType.Player:
            _rangeFinderTiles = _rangeFinder.GetTilesInRange( new Vector2Int( StandingOnTile.gridLocation.x, StandingOnTile.gridLocation.y ), _movementRange );
                break;
            case CharacterType.Enemy:
            _rangeFinderTiles = _rangeFinder.GetTilesInRange( new Vector2Int( StandingOnTile.gridLocation.x, StandingOnTile.gridLocation.y ), 10 );
                break;
        }

        foreach(var item in _rangeFinderTiles)
        {
            item.ShowTile();
        }
    }

    public void TakeDamage(int dmg )
    {
        _lifeLeft -= dmg;

        if( _lifeLeft <= 0)
        {
            OnCharacterActed( new CharacterMove() {
                Action = CharacterAction.Die,
                Character = this,
                Type = _type
            } );
        }
    }

    protected virtual void OnDestroy()
    {
        _spriteRenderer.sortingOrder = 0;
    }
}

