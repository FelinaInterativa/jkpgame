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

    [SerializeField] private int _startinEnemies;
    [SerializeField] private int _enemiesAddedPerWave;
    [SerializeField] private float _timeBetweenWaves;
    private float _counter;

    [SerializeField] private ParticleSystem _dieFX;
    [SerializeField] private ParticleSystem _dmgFX;

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

    private void Update()
    {
        _counter += Time.deltaTime;
        if(_counter > _timeBetweenWaves)
        {

        }
    }


    //Drop enemies after player chooses his/her position
    IEnumerator DropEnemies()
    {
        yield return new WaitForSeconds( _timeBetweenEnemiesActions * 3 );

        for(int i = 0; i < _startinEnemies; i++)
        {
            var enemy = Instantiate( _enemyPrefab ).GetComponent<EnemyController>();
            yield return new WaitForSeconds( _timeBetweenEnemiesActions );
            _enemies.Add( enemy );
        }

        StartCoroutine( MoveEnemiesTorwardPlayer() );
    }

    IEnumerator MoveEnemiesTorwardPlayer()
    {
        yield return new WaitForSeconds( _timeBetweenEnemiesActions * 3 );

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
                    case CharacterAction.Die:
                        ResolveDyingCharacter( action.Character );
                        break;
                }

                break;
            case CharacterType.Enemy:

                switch(action.Action)
                {
                    case CharacterAction.Spawn:
                        break;
                    case CharacterAction.Move:
                        break;
                    case CharacterAction.Attack:
                        ResolveCombat( action.Character );
                        break;
                    case CharacterAction.Die:
                        ResolveDyingCharacter( action.Character );
                        break;
                }

                break;
        }
    }

    private void ResolveDyingCharacter( CharacterInfo character )
    {
        _dieFX.transform.position = character.transform.position;
        _dieFX.Play();

        if(character.Type == CharacterType.Enemy)
        {
            _enemies.Remove( character );
            Destroy( character.gameObject );
        }
    }

    private void ResolveCombat( CharacterInfo enemy )
    {
        if(_player.Weapon == enemy.Weapon)
        {
            //Draw
            return;
        }

        else if(_player.Weapon == Weapon.ROCK && enemy.Weapon == Weapon.SCISSORS ||
                _player.Weapon == Weapon.PAPER && enemy.Weapon == Weapon.ROCK ||
                _player.Weapon == Weapon.SCISSORS && enemy.Weapon == Weapon.PAPER)
        {
            _dmgFX.transform.position = enemy.transform.position;
            enemy.TakeDamage( _player.Damage );
        }

        else if(_player.Weapon == Weapon.ROCK && enemy.Weapon == Weapon.PAPER ||
                _player.Weapon == Weapon.PAPER && enemy.Weapon == Weapon.SCISSORS ||
                _player.Weapon == Weapon.SCISSORS && enemy.Weapon == Weapon.ROCK)
        {
            _dmgFX.transform.position = _player.transform.position;
            _player.TakeDamage( enemy.Damage );
        }

        _dmgFX.Play();
    }
}
