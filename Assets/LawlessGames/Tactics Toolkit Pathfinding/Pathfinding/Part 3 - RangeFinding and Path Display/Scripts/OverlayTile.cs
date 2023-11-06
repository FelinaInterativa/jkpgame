using System.Collections.Generic;
using UnityEngine;

public class OverlayTile : MonoBehaviour
{
    public int G;
    public int H;
    public int F { get { return G + H; } }

    public bool isBlocked = false;
    //public bool isOccupied = false;
    private CharacterInfo _characterOnIt;
    public CharacterInfo CharacterOnIt {
        get => _characterOnIt;
        set => _characterOnIt = value;
    }

    public OverlayTile Previous;
    public Vector3Int gridLocation;
    public Vector2Int grid2DLocation { get { return new Vector2Int( gridLocation.x, gridLocation.y ); } }

    public List<Sprite> arrows;


    private void Update()
    {
        if(Input.GetMouseButtonDown( 0 ))
        {
            HideTile();
        }
    }

    public void HideTile()
    {
        gameObject.GetComponent<SpriteRenderer>().color = new Color( 1, 1, 1, 0 );
    }

    public void ShowTile()
    {
        gameObject.GetComponent<SpriteRenderer>().color = new Color( 1, 1, 1, 1 );
    }

    public void SetSprite( ArrowDirection d )
    {
        if(d == ArrowDirection.None)
            GetComponentsInChildren<SpriteRenderer>()[ 1 ].color = new Color( 1, 1, 1, 0 );
        else
        {
            GetComponentsInChildren<SpriteRenderer>()[ 1 ].color = new Color( 1, 1, 1, 1 );
            GetComponentsInChildren<SpriteRenderer>()[ 1 ].sprite = arrows[ (int)d ];
            GetComponentsInChildren<SpriteRenderer>()[ 1 ].sortingOrder = gameObject.GetComponent<SpriteRenderer>().sortingOrder;
        }
    }

}