using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Chess/Piece Set")]
public class PieceSetDefinition : ScriptableObject
{
    public Sprite white_pawn;
    public Sprite white_knight;
    public Sprite white_bishop;
    public Sprite white_rook;
    public Sprite white_queen;
    public Sprite white_king;

    public Sprite black_pawn;
    public Sprite black_knight;
    public Sprite black_bishop;
    public Sprite black_rook;
    public Sprite black_queen;
    public Sprite black_king;

}
