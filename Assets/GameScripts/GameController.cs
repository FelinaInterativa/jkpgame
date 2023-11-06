using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameController : MonoBehaviour
{
    [SerializeField] private List<CharacterInfo> _enemies = new List<CharacterInfo>();

    private CharacterInfo _player;

    [SerializeField] private GameObject _playerController;

    [SerializeField] private GameObject _enemyPrefab;

    [SerializeField] private float _timeBetweenEnemiesActions = 0.5f;

    [SerializeField] private int _enemiesQuantity;
    [SerializeField] private int _enemiesAddedPerWave;
    [SerializeField] private float _timeBetweenWaves;

    [SerializeField]
    private float _counter;

    [SerializeField] private AudioClip _combatMusic;
    private AudioSource _audioSource;

    [SerializeField] private GameObject _dieFXObj;
    [SerializeField] private GameObject _dmgFXObj;

    private Animator _dieFX;
    private Animator _dmgFX;
    private string _dieFXAnimationTriggerName = "dieFXTrigger";
    
    private string _dmgFXAnimationTriggerName = "dmgFXTrigger";

    public delegate void OnWaveStarted();
    public event Action WaveStarted;

    [SerializeField] private Image _lifeBar;
    [SerializeField] private TMP_Text _lifeText;

    [SerializeField] private Button _skipTurnButton;

    private void Awake()
    {
        _player = _playerController.GetComponent<CharacterInfo>();
        _playerController.SetActive( false );
        CharacterInfo.CharacterActed += OnCharacterAction;
        MapManager.Instance.OnMapBuilded += Init;
        _dieFX = _dieFXObj.GetComponent<Animator>();
        _dmgFX = _dmgFXObj.GetComponent<Animator>();
        _audioSource = Camera.main.GetComponent<AudioSource>();
    }

    private void Start()
    {
        _skipTurnButton.interactable = false;
        _lifeBar.fillAmount = 1;
    }

    private void Init()
    {
        Debug.Log( "Initializing..." );
        _playerController.SetActive( true );
    }

    private void Update()
    {
        //_counter += Time.deltaTime;
        //if(_counter > _timeBetweenWaves)
        //{
        //    _enemiesQuantity += _enemiesAddedPerWave;
        //    StartCoroutine( DropEnemies() );
        //    _counter = 0;
        //}
    }


    //Drop enemies after player chooses his/her position
    IEnumerator DropEnemies()
    {
        WaveStarted?.Invoke();

        yield return new WaitForSeconds( _timeBetweenEnemiesActions );

        for(int i = 0; i < _enemiesQuantity; i++)
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
        _skipTurnButton.interactable = true;
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
                        _skipTurnButton.interactable = false;
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
                        StartCoroutine(ResolveCombat( action.Character ));
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
        StartCoroutine( PlayAnimation( _dieFX ) );
        _dieFX.ResetTrigger( _dieFXAnimationTriggerName );

        if(character.Type == CharacterType.Enemy)
        {
            _enemies.Remove( character );
            Destroy( character.gameObject );
        }
    }

    IEnumerator ResolveCombat( CharacterInfo enemy )
    {
        _audioSource.clip = _combatMusic;
        _audioSource.Play();

        Debug.Log($"player; {_player.Weapon} enemy: {enemy.Weapon}");

        if(_player.Weapon == enemy.Weapon)
        {
            //Draw
            Debug.Log($"DRAW");
            yield break;
        }
        else if(_player.Weapon == Weapon.ROCK && enemy.Weapon == Weapon.SCISSORS ||
                _player.Weapon == Weapon.PAPER && enemy.Weapon == Weapon.ROCK ||
                _player.Weapon == Weapon.SCISSORS && enemy.Weapon == Weapon.PAPER)
        {
            yield return new WaitForSeconds( _timeBetweenEnemiesActions * 2 );
            _dmgFX.transform.position = enemy.transform.position;
            enemy.TakeDamage( _player.Damage );
        }

        else if(_player.Weapon == Weapon.ROCK && enemy.Weapon == Weapon.PAPER ||
                _player.Weapon == Weapon.PAPER && enemy.Weapon == Weapon.SCISSORS ||
                _player.Weapon == Weapon.SCISSORS && enemy.Weapon == Weapon.ROCK)
        {
            yield return new WaitForSeconds( _timeBetweenEnemiesActions * 2);
            _dmgFX.transform.position = _player.transform.position;
            _player.TakeDamage( enemy.Damage );
            _lifeBar.fillAmount = (float)_player.LifeLeft / _player.InitialLifeAmount;
            Debug.Log( $"player took {enemy.Damage} of damage and has {_player.LifeLeft} left" );
        }


        StartCoroutine( PlayAnimation( _dmgFX ) );
        _player.GetComponent<MouseController>().enabled = true;
        _skipTurnButton.interactable = true;
    }


    public void SkipTurn()
    {
        _player.OnCharacterActed( new CharacterMove()
        {
            Action = CharacterAction.Move,
            Character = _player,
            Type = _player.Type
        } );
    }

    private IEnumerator PlayAnimation( Animator animator )
    {
        animator.SetTrigger( "playAnimation" );

        yield return new WaitForSeconds(animator.playbackTime);

        animator.transform.position = Vector3.up * 1000;
    }
}
