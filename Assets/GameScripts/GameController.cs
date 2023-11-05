using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameController : MonoBehaviour
{
    [SerializeField] private List<CharacterInfo> _enemies = new List<CharacterInfo>();
    
    private CharacterInfo _player;
    
    [SerializeField] private GameObject _playerController;

    [SerializeField] private GameObject _enemyPrefab;

    [SerializeField] private float _timeBetweenEnemiesActions = 0.5f;

    [SerializeField] private int _numEnemies;

    private void Awake()
    {
        _player = _playerController.GetComponent<CharacterInfo>();
        _playerController.SetActive( false );
        CharacterInfo.CharacterActed += OnCharacterAction;
        MapManager.Instance.OnMapBuilded += Init;
    }



    private void Start()
    {
    }

    private void Init()
    {
        Debug.Log( "Initializing..." );
        _playerController.SetActive( true );
    }

    //Drop enemies after player chooses his/her position
    IEnumerator DropEnemies()
    {
        yield return new WaitForSeconds( _timeBetweenEnemiesActions * 3 );

        for(int i = 0; i < _numEnemies; i++)
        {
            var enemy = Instantiate( _enemyPrefab ).GetComponent<EnemyController>();
            yield return new WaitForSeconds( _timeBetweenEnemiesActions );
            _enemies.Add( enemy );
        }

        StartCoroutine( MoveEnemiesTorwardPlayer() );
    }

    IEnumerator MoveEnemiesTorwardPlayer()
    {
        yield return new WaitForSeconds( _timeBetweenEnemiesActions  * 3);

        for(int i = 0; i < _enemies.Count; i++)
        {
            ((EnemyController)_enemies[ i ]).MoveTo( _player.StandingOnTile );
            yield return new WaitForSeconds( _timeBetweenEnemiesActions );
        }

        _player.GetComponent<MouseController>().enabled = true;
    }

    private void OnCharacterAction( CharacterMove action )
    {
        switch(action.Type)
        {
            case CharacterType.Player:

                switch(action.Action)
                {
                    case CharacterAction.Spawn:
                        _player = action.Character;
                        _player.GetComponent<MouseController>().enabled = false;
                        StartCoroutine( DropEnemies() );
                        break;
                    case CharacterAction.Move:
                        _player.GetComponent<MouseController>().enabled = false;
                        StartCoroutine( MoveEnemiesTorwardPlayer() );
                        break;
                    case CharacterAction.Attack:
                        break;
                }

                break;
            case CharacterType.Enemy:
                break;
            default:
                break;
        }
    }
}
