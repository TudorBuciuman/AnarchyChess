using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
using System.Threading.Tasks;

public class ChessBot : MonoBehaviour
{
    public GameObject controller;
    public GameObject target;
    public LegalMovesManager LegalMovesManager;
    public Game game;

    private int searchDepth = 4; //I count the full moves, which means I count my move and of my enemy in the same move

    private List<int> availableMoves;
    private bool moveFound = false;

    // Positive = white, Negative = black, 0 = empty
    // value: King=6, Queen=5, Rook=4, Bishop=3, Knight=2, Pawn=1
    private const int W_PAWN = 1; private const int B_PAWN = -1;
    private const int W_KNIGHT = 2; private const int B_KNIGHT = -2;
    private const int W_BISHOP = 3; private const int B_BISHOP = -3;
    private const int W_ROOK = 4; private const int B_ROOK = -4;
    private const int W_QUEEN = 5; private const int B_QUEEN = -5;
    private const int W_KING = 6; private const int B_KING = -6;

    private const int CastlingBonus = 60;

    private struct BoardState
    {
        public int[,] Board;     // [x,y] encoded piece or 0
        public bool BotIsWhite;
        public bool EnPassantActive;
        public int EnPassantX;   // column of the pawn that could be captured
        public int EnPassantY;   // row of the pawn that could be captured

        public bool WhiteKingMoved;
        public bool BlackKingMoved;
        public bool WhiteRookAMoved;    
        public bool WhiteRookHMoved;   
        public bool BlackRookAMoved;
        public bool BlackRookHMoved;

        public bool WhiteHasCastled;
        public bool BlackHasCastled;

        public BoardState DeepCopy()
        {
            var s = this;
            s.Board = (int[,])Board.Clone();
            return s;
        }
    }

    private struct PureMove
    {
        public int FromX, FromY;
        public int ToX, ToY;
        public int Piece;              
        public int Captured;           
        public bool IsEnPassant;
        public bool IsCastleKingside;
        public bool IsCastleQueenside;
    }

    private struct MoveCandidate
    {
        public GameObject piece;
        public int fromX, fromY;
        public int toX, toY;
        public bool isAttack;
        public bool isCastleKingside;
        public bool isCastleQueenside;
    }

    public void BotTurn()
    {
        if (Game.currentMinigame == Game.Minigame.FogOfWar)
            searchDepth = 2;
        if (Game.currentMinigame == Game.Minigame.Duck)
            searchDepth = 5;
        StartCoroutine(BotTurnAndPauseOneFrame());
    }

    private IEnumerator BotTurnAndPauseOneFrame()
    {
        yield return null;
        game = GetComponent<Game>();
        if (!game.IsGameOver())
            MakeBotMoveAsync();
    }

    //MakeBotMoveAsync
    //1. Snapshots the board and builds root moves on the main thread
    //2. Searches entirely on thread-pool threads (Task.Run + Parallel.For)
    //3. Resumes on the main thread to execute the chosen move

    private async void MakeBotMoveAsync()
    {
        List<GameObject> botPieces = game.GetPlayerPieces();
        if (botPieces == null || botPieces.Count == 0) return;
        botPieces = botPieces
            .Where(p => p != null && p.GetComponent<Chessman>() != null)
            .ToList();
        if (botPieces.Count == 0) return;

        bool botIsWhite = botPieces[0].name.StartsWith("white");

        List<MoveCandidate> rootCandidates = GenerateAllMoves(botPieces);
        if (rootCandidates.Count == 0)
        {
            Debug.LogWarning("Bot has no legal moves!");
            return;
        }
        rootCandidates = OrderMoves(rootCandidates);

        BoardState snapshot = CaptureBoard(botIsWhite);

        List<PureMove> rootPureMoves = rootCandidates
            .Select(mc => new PureMove
            {
                FromX = mc.fromX,
                FromY = mc.fromY,
                ToX = mc.toX,
                ToY = mc.toY,
                Piece = snapshot.Board[mc.fromX, mc.fromY],
                Captured = snapshot.Board[mc.toX, mc.toY],
                IsCastleKingside = mc.isCastleKingside,
                IsCastleQueenside = mc.isCastleQueenside
            }).ToList();

        int bestIndex = await Task.Run(() =>
            FindBestMoveIndex(snapshot, rootPureMoves, botIsWhite)
        );

        ExecuteBestMove(rootCandidates[bestIndex]);
    }

    //Must be called on the main thread!!!!!

    private BoardState CaptureBoard(bool botIsWhite)
    {
        var state = new BoardState
        {
            Board = new int[8, 8],
            BotIsWhite = botIsWhite
        };

        for (int x = 0; x < 8; x++)
            for (int y = 0; y < 8; y++)
            {
                GameObject p = game.GetPosition(x, y);
                if (p != null)
                    state.Board[x, y] = EncodeGameObject(p);
            }

        // Google En passant
        Game gm = controller.GetComponent<Game>();
        Game.Move last = gm.GetTheLastMove();
        if (last?.piece != null && last.piece.name.Contains("pawn"))
        {
            if (Math.Abs(last.toY - last.fromY) == 2)
            {
                state.EnPassantActive = true;
                state.EnPassantX = last.toX;
                state.EnPassantY = last.toY;
            }
        }
        else if (gm.EnPassant && gm.EPassant != null)
        {
            Chessman ep = gm.EPassant.GetComponent<Chessman>();
            if (ep != null)
            {
                state.EnPassantActive = true;
                state.EnPassantX = ep.GetXBoard();
                state.EnPassantY = ep.GetYBoard();
            }
        }
        state.WhiteKingMoved = state.Board[4, 0] != W_KING;
        state.BlackKingMoved = state.Board[4, 7] != B_KING;
        state.WhiteRookAMoved = state.Board[0, 0] != W_ROOK;
        state.WhiteRookHMoved = state.Board[7, 0] != W_ROOK;
        state.BlackRookAMoved = state.Board[0, 7] != B_ROOK;
        state.BlackRookHMoved = state.Board[7, 7] != B_ROOK;

        state.WhiteHasCastled =
            state.Board[6, 0] == W_KING || state.Board[2, 0] == W_KING;
        state.BlackHasCastled =
            state.Board[6, 7] == B_KING || state.Board[2, 7] == B_KING;

        return state;
    }

    private int EncodeGameObject(GameObject p)
    {
        if (p == null) return 0;
        switch (p.name)
        {
            case "white_pawn": return W_PAWN;
            case "black_pawn": return B_PAWN;
            case "white_knight": return W_KNIGHT;
            case "black_knight": return B_KNIGHT;
            case "white_bishop": return W_BISHOP;
            case "black_bishop": return B_BISHOP;
            case "white_rook": return W_ROOK;
            case "black_rook": return B_ROOK;
            case "white_queen": return W_QUEEN;
            case "black_queen": return B_QUEEN;
            case "white_king": return W_KING;
            case "black_king": return B_KING;
        }
        return 0;
    }

    private static bool IsWhite(int piece) => piece > 0;

    private static bool IsBotPiece(int piece, bool botIsWhite)
        => botIsWhite ? piece > 0 : piece < 0;

    private static int PieceValue(int piece)
    {
        switch (Math.Abs(piece))
        {
            case 1: return 1;   // pawn
            case 2: return 3;   // knight
            case 3: return 3;   // bishop
            case 4: return 5;   // rook
            case 5: return 9;   // queen
            case 6: return 0;   // king (handled separately in eval, because you obviously cant take the king)
        }
        return 0;
    }

    private const int MaxNonPawnMaterial = 5000;

    private static float GamePhase(BoardState state)
    {
        int material = 0;
        for (int x = 0; x < 8; x++)
            for (int y = 0; y < 8; y++)
            {
                int p = state.Board[x, y];
                int abs = Math.Abs(p);
                if (abs >= 2 && abs <= 5)          
                    material += PieceValue(p) * 100;
            }
        return 1f - Math.Min(1f, (float)material / MaxNonPawnMaterial);
    }

    //parallel root search, thread-pool only!!

    private int FindBestMoveIndex(BoardState state, List<PureMove> moves, bool botIsWhite)
    {
        var scores = new int[moves.Count];

        Parallel.For(0, moves.Count, i =>
        {
            BoardState branch = ApplyPureMove(state, moves[i]);
            scores[i] = MinimaxPure(branch, searchDepth - 1,
                                    int.MinValue, int.MaxValue,
                                    false, botIsWhite);
        });

        int best = 0;
        for (int i = 1; i < scores.Length; i++)
            if (scores[i] > scores[best]) best = i;

        return best;
    }

    private int MinimaxPure(BoardState state, int depth, int alpha, int beta,
                            bool maximising, bool botIsWhite)
    {
        if (depth == 0)
            return EvaluatePure(state, botIsWhite);

        List<PureMove> moves = GenerateAllMovesFromState(state, botSide: maximising);
        moves = OrderPureMoves(moves, state);

        if (moves.Count == 0)
            return EvaluatePure(state, botIsWhite);

        if (maximising)
        {
            int maxEval = int.MinValue;
            foreach (var move in moves)
            {
                BoardState next = ApplyPureMove(state, move);
                int eval = MinimaxPure(next, depth - 1, alpha, beta, false, botIsWhite);
                if (eval > maxEval) maxEval = eval;
                if (eval > alpha) alpha = eval;
                if (beta <= alpha) break;
            }
            return maxEval;
        }
        else
        {
            int minEval = int.MaxValue;
            foreach (var move in moves)
            {
                BoardState next = ApplyPureMove(state, move);
                int eval = MinimaxPure(next, depth - 1, alpha, beta, true, botIsWhite);
                if (eval < minEval) minEval = eval;
                if (eval < beta) beta = eval;
                if (beta <= alpha) break;
            }
            return minEval;
        }
    }

    private List<PureMove> GenerateAllMovesFromState(BoardState state, bool botSide)
    {
        var moves = new List<PureMove>();
        bool playWhite = botSide ? state.BotIsWhite : !state.BotIsWhite;

        for (int x = 0; x < 8; x++)
            for (int y = 0; y < 8; y++)
            {
                int piece = state.Board[x, y];
                if (piece == 0 || IsWhite(piece) != playWhite) continue;

                switch (Math.Abs(piece))
                {
                    case 1: GeneratePawnMoves(state, x, y, piece, moves); break;
                    case 2: GenerateKnightMoves(state, x, y, piece, moves); break;
                    case 3:
                        GenerateSlidingMoves(state, x, y, piece, moves,
                            new[] { (1, 1), (-1, 1), (1, -1), (-1, -1) });
                        break;
                    case 4:
                        GenerateSlidingMoves(state, x, y, piece, moves,
                            new[] { (1, 0), (-1, 0), (0, 1), (0, -1) });
                        break;
                    case 5:
                        GenerateSlidingMoves(state, x, y, piece, moves,
                            new[] { (1, 0), (-1, 0), (0, 1), (0, -1), (1, 1), (-1, 1), (1, -1), (-1, -1) });
                        break;
                    case 6:
                        GenerateKingMoves(state, x, y, piece, moves);
                        GenerateCastlingMoves(state, x, y, piece, moves);
                        break;
                }
            }
        return moves;
    }
    private void GenerateSlidingMoves(BoardState state, int x, int y, int piece,
                                      List<PureMove> moves, (int dx, int dy)[] dirs)
    {
        foreach (var (dx, dy) in dirs)
        {
            int nx = x + dx, ny = y + dy;
            while (nx >= 0 && nx < 8 && ny >= 0 && ny < 8)
            {
                int target = state.Board[nx, ny];
                if (target == 0)
                    moves.Add(new PureMove { FromX = x, FromY = y, ToX = nx, ToY = ny, Piece = piece });
                else
                {
                    if (IsWhite(target) != IsWhite(piece))
                        moves.Add(new PureMove
                        { FromX = x, FromY = y, ToX = nx, ToY = ny, Piece = piece, Captured = target });
                    break;
                }
                nx += dx; ny += dy;
            }
        }
    }

    private void GenerateKnightMoves(BoardState state, int x, int y, int piece, List<PureMove> moves)
    {
        int[][] offs = { new[]{1,2},new[]{-1,2},new[]{2,1},new[]{-2,1},
                         new[]{-2,-1},new[]{-1,-2},new[]{1,-2},new[]{2,-1} };
        foreach (var o in offs)
        {
            int nx = x + o[0], ny = y + o[1];
            if (nx < 0 || nx > 7 || ny < 0 || ny > 7) continue;
            int target = state.Board[nx, ny];
            if (target == 0 || IsWhite(target) != IsWhite(piece))
                moves.Add(new PureMove
                { FromX = x, FromY = y, ToX = nx, ToY = ny, Piece = piece, Captured = target });
        }
    }
    private void GenerateKingMoves(BoardState state, int x, int y, int piece,
                                   List<PureMove> moves)
    {
        for (int dx = -1; dx <= 1; dx++)
            for (int dy = -1; dy <= 1; dy++)
            {
                if (dx == 0 && dy == 0) continue;
                int nx = x + dx, ny = y + dy;
                if (nx < 0 || nx > 7 || ny < 0 || ny > 7) continue;
                int target = state.Board[nx, ny];
                if (target == 0 || IsWhite(target) != IsWhite(piece))
                    moves.Add(new PureMove
                    { FromX = x, FromY = y, ToX = nx, ToY = ny, Piece = piece, Captured = target });
            }
    }

    private void GenerateCastlingMoves(BoardState state, int x, int y, int piece,
                                       List<PureMove> moves)
    {
        bool white = IsWhite(piece);
        int row = white ? 0 : 7;

        if (x != 4 || y != row) return;

        bool kingMoved = white ? state.WhiteKingMoved : state.BlackKingMoved;
        bool rookAMoved = white ? state.WhiteRookAMoved : state.BlackRookAMoved;
        bool rookHMoved = white ? state.WhiteRookHMoved : state.BlackRookHMoved;

        if (kingMoved) return;

        //kingside: squares f and g must be empty
        if (!rookHMoved &&
            state.Board[5, row] == 0 &&
            state.Board[6, row] == 0)
        {
            moves.Add(new PureMove
            {
                FromX = 4,
                FromY = row,
                ToX = 6,
                ToY = row,
                Piece = piece,
                IsCastleKingside = true
            });
        }

        //Queenside: squares b, c, d must be empty
        if (!rookAMoved &&
            state.Board[3, row] == 0 &&
            state.Board[2, row] == 0 &&
            state.Board[1, row] == 0)
        {
            moves.Add(new PureMove
            {
                FromX = 4,
                FromY = row,
                ToX = 2,
                ToY = row,
                Piece = piece,
                IsCastleQueenside = true
            });
        }
    }

    private void GeneratePawnMoves(BoardState state, int x, int y, int piece,
                                   List<PureMove> moves)
    {
        bool white = IsWhite(piece);
        int dir = white ? 1 : -1;
        int startY = white ? 1 : 6;

        int ny = y + dir;
        if (ny >= 0 && ny < 8 && state.Board[x, ny] == 0)
        {
            moves.Add(new PureMove { FromX = x, FromY = y, ToX = x, ToY = ny, Piece = piece });
            int ny2 = y + dir * 2;
            if (y == startY && ny2 >= 0 && ny2 < 8 && state.Board[x, ny2] == 0)
                moves.Add(new PureMove { FromX = x, FromY = y, ToX = x, ToY = ny2, Piece = piece });
        }

        foreach (int dx in new[] { -1, 1 })
        {
            int nx = x + dx;
            ny = y + dir;
            if (nx < 0 || nx > 7 || ny < 0 || ny > 7) continue;

            int target = state.Board[nx, ny];
            if (target != 0 && IsWhite(target) != white)
                moves.Add(new PureMove
                { FromX = x, FromY = y, ToX = nx, ToY = ny, Piece = piece, Captured = target });

            if (state.EnPassantActive &&
                nx == state.EnPassantX &&
                y == state.EnPassantY)
            {
                int epCaptured = state.Board[nx, state.EnPassantY];
                moves.Add(new PureMove
                {
                    FromX = x,
                    FromY = y,
                    ToX = nx,
                    ToY = ny,
                    Piece = piece,
                    Captured = epCaptured,
                    IsEnPassant = true
                });
            }
        }
    }
    private BoardState ApplyPureMove(BoardState state, PureMove move)
    {
        BoardState next = state.DeepCopy();
        bool white = IsWhite(move.Piece);
        int row = white ? 0 : 7;

        next.Board[move.FromX, move.FromY] = 0;
        next.Board[move.ToX, move.ToY] = move.Piece;

        if (move.IsEnPassant)
            next.Board[move.ToX, move.FromY] = 0;

        if (move.IsCastleKingside)
        {
            next.Board[7, row] = 0;
            next.Board[5, row] = white ? W_ROOK : B_ROOK;
            if (white) next.WhiteHasCastled = true;
            else next.BlackHasCastled = true;
        }
        else if (move.IsCastleQueenside)
        {
            next.Board[0, row] = 0;
            next.Board[3, row] = white ? W_ROOK : B_ROOK;
            if (white) next.WhiteHasCastled = true;
            else next.BlackHasCastled = true;
        }

        if (Math.Abs(move.Piece) == 1)
        {
            if ((white && move.ToY == 7) || (!white && move.ToY == 0))
                next.Board[move.ToX, move.ToY] = white ? W_QUEEN : B_QUEEN;
        }

        if (Math.Abs(move.Piece) == 6)
        {
            if (white) next.WhiteKingMoved = true;
            else next.BlackKingMoved = true;
        }
        if (move.FromX == 0 && move.FromY == 0) next.WhiteRookAMoved = true;
        if (move.FromX == 7 && move.FromY == 0) next.WhiteRookHMoved = true;
        if (move.FromX == 0 && move.FromY == 7) next.BlackRookAMoved = true;
        if (move.FromX == 7 && move.FromY == 7) next.BlackRookHMoved = true;

        next.EnPassantActive = false;
        if (Math.Abs(move.Piece) == 1 && Math.Abs(move.ToY - move.FromY) == 2)
        {
            next.EnPassantActive = true;
            next.EnPassantX = move.ToX;
            next.EnPassantY = move.ToY;
        }

        return next;
    }

    //EvaluatePure = material + positional + castling + game-phase king

    private int EvaluatePure(BoardState state, bool botIsWhite)
    {
        float phase = GamePhase(state);  // 0 = opening/mid, 1 = endgame
        int score = 0;

        for (int x = 0; x < 8; x++)
            for (int y = 0; y < 8; y++)
            {
                int piece = state.Board[x, y];
                if (piece == 0) continue;

                int value = PieceValue(piece) * 100
                          + GetPositionalBonusPure(piece, x, y, phase);

                if (IsBotPiece(piece, botIsWhite))
                    score += value;
                else
                    score -= value;
            }

        bool botCastled = botIsWhite ? state.WhiteHasCastled : state.BlackHasCastled;
        bool oppCastled = botIsWhite ? state.BlackHasCastled : state.WhiteHasCastled;
        if (botCastled) score += CastlingBonus;
        if (oppCastled) score -= CastlingBonus;

        return score;
    }
    private int GetPositionalBonusPure(int piece, int x, int y, float phase)
    {
        int dist = Math.Max(Math.Abs(x - 3), Math.Abs(y - 3));

        switch (piece)
        {
            case W_PAWN: return (7 - dist) * 2 + y * 3;
            case B_PAWN: return (7 - dist) * 2 + (7 - y) * 3;
            case W_KNIGHT:
            case B_KNIGHT: return (4 - dist) * 5;
            case W_BISHOP:
            case B_BISHOP: return (4 - dist) * 3;
            case W_QUEEN:
            case B_QUEEN: return (4 - dist) * 2;

            case W_KING:
                {
                    int mgBonus = KingMidgameBonus(x, y, white: true);
                    int egBonus = (4 - dist) * 8;
                    return (int)Mathf.Lerp(mgBonus, egBonus, phase);
                }
            case B_KING:
                {
                    int mgBonus = KingMidgameBonus(x, y, white: false);
                    int egBonus = (4 - dist) * 8;
                    return (int)Mathf.Lerp(mgBonus, egBonus, phase);
                }
        }
        return 0;
    }

    //Piece position matrix for the king in the middlegame
    //penalises central exposure and uncastled edge wandering
    //Rewards castled positions (g1/c1 for white, g8/c8 for black)
    //I'm actually really proud of this design
    private static int KingMidgameBonus(int x, int y, bool white)
    {
        int row = white ? y : (7 - y);
        int[,] pst = {
            { 20,  30,  10,   0,   0,  10,  30,  20 },  
            {  0,   0,   0, -10, -10,   0,   0,   0 },
            {-10, -20, -20, -20, -20, -20, -20, -10 },
            {-20, -30, -30, -40, -40, -30, -30, -20 },
            {-30, -40, -40, -50, -50, -40, -40, -30 },
            {-30, -40, -40, -50, -50, -40, -40, -30 },
            {-30, -40, -40, -50, -50, -40, -40, -30 },
            {-30, -40, -40, -50, -50, -40, -40, -30 },
        };

        if (row < 0 || row > 7 || x < 0 || x > 7) return 0;
        return pst[row, x];
    }

    private List<PureMove> OrderPureMoves(List<PureMove> moves, BoardState state)
    {
        return moves
            .OrderByDescending(m =>
            {
                if (m.IsCastleKingside || m.IsCastleQueenside) return 5; 
                if (m.Captured == 0) return 0;
                return PieceValue(m.Captured) * 10 - PieceValue(m.Piece);
            })
            .ToList();
    }

    private void ExecuteBestMove(MoveCandidate move)
    {
        moveFound = false;

        if (move.isCastleKingside || move.isCastleQueenside)
        {
            ExecuteCastle(move);
            return;
        }

        if (move.isAttack)
            MovePlateAttackSpawn(move.piece, move.toX, move.toY, move.fromX, move.fromY);
        else
            MovePlateSpawn(move.piece, move.toX, move.toY, move.fromX, move.fromY);

        if (!moveFound)
            Debug.LogWarning("Bot: best move could not be executed.");
    }

    private void ExecuteCastle(MoveCandidate move)
    {
        bool white = move.piece.name.StartsWith("white");
        int row = white ? 0 : 7;
        bool kingside = move.isCastleKingside;

        int rookFromX = kingside ? 7 : 0;
        int rookToX = kingside ? 5 : 3;

        MovePlateSpawn(move.piece, move.toX, move.toY, move.fromX, move.fromY);

        GameObject rook = game.GetPosition(rookFromX, row);
        if (rook != null)
        {
            Chessman rookCm = rook.GetComponent<Chessman>();
            if (rookCm != null)
            {
                game.SetPosition(null, rookFromX, row);
                game.SetPosition(rook, rookToX, row);
                rookCm.SetXBoard(rookToX);
                rookCm.SetYBoard(row);
                rookCm.SetCoords();
            }
        }

        moveFound = true;
    }

    //GenerateAllMoves —  root move generation (main thread only)
    //Touch it and you die! 
    private List<MoveCandidate> GenerateAllMoves(List<GameObject> pieces)
    {
        var moves = new List<MoveCandidate>();

        foreach (GameObject piece in pieces)
        {
            if (piece == null) continue;
            Chessman cm = piece.GetComponent<Chessman>();
            if (cm == null) continue;

            int fromX = cm.GetXBoard();
            int fromY = cm.GetYBoard();

            availableMoves = new List<int>();
            PossibleMoves(fromX, fromY, piece);

            foreach (int moveIndex in availableMoves)
            {
                int toX = moveIndex % 8;
                int toY = moveIndex / 8;

                if (LegalMovesManager == null)
                    LegalMovesManager = controller.GetComponent<LegalMovesManager>();
                if (!LegalMovesManager.IsLegal(piece, fromX, fromY, toX, toY)) continue;

                moves.Add(new MoveCandidate
                {
                    piece = piece,
                    fromX = fromX,
                    fromY = fromY,
                    toX = toX,
                    toY = toY,
                    isAttack = game.GetPosition(toX, toY) != null
                });
            }

            if (Math.Abs(EncodeGameObject(piece)) == 6)
                AddCastlingCandidates(piece, fromX, fromY, moves);
        }
        return moves;
    }

    private void AddCastlingCandidates(GameObject king, int fromX, int fromY,
                                       List<MoveCandidate> moves)
    {
        bool white = king.name.StartsWith("white");
        int row = white ? 0 : 7;

        if (fromX != 4 || fromY != row) return;  

        // Kingside
        if (game.GetPosition(5, row) == null &&
            game.GetPosition(6, row) == null)
        {
            GameObject hRook = game.GetPosition(7, row);
            if (hRook != null && hRook.name == (white ? "white_rook" : "black_rook"))
            {
                moves.Add(new MoveCandidate
                {
                    piece = king,
                    fromX = fromX,
                    fromY = fromY,
                    toX = 6,
                    toY = row,
                    isAttack = false,
                    isCastleKingside = true
                });
            }
        }

        // Queenside
        if (game.GetPosition(3, row) == null &&
            game.GetPosition(2, row) == null &&
            game.GetPosition(1, row) == null)
        {
            GameObject aRook = game.GetPosition(0, row);
            if (aRook != null && aRook.name == (white ? "white_rook" : "black_rook"))
            {
                moves.Add(new MoveCandidate
                {
                    piece = king,
                    fromX = fromX,
                    fromY = fromY,
                    toX = 2,
                    toY = row,
                    isAttack = false,
                    isCastleQueenside = true
                });
            }
        }
    }

    private List<MoveCandidate> OrderMoves(List<MoveCandidate> moves)
    {
        return moves.OrderByDescending(m =>
        {
            if (m.isCastleKingside || m.isCastleQueenside) return 5;
            if (!m.isAttack) return 0;
            GameObject victim = game.GetPosition(m.toX, m.toY);
            if (victim == null) return 0;
            return GetValue(victim) * 10 - GetValue(m.piece);
        }).ToList();
    }

    public void MovePlateSpawn(GameObject piece, int matrixX, int matrixY, int i, int j)
    {
        if (moveFound) return;
        LegalMovesManager = controller.GetComponent<LegalMovesManager>();
        if (LegalMovesManager.IsLegal(piece, i, j, matrixX, matrixY))
        {
            MoveThePlate mp = controller.GetComponent<MoveThePlate>();
            mp.attack = false;
            mp.piece = piece;
            mp.enPassant = false;
            if (piece.name == "white_pawn" || piece.name == "black_pawn")
                mp.PwnTQn = (matrixY == 7 || matrixY == 0);
            mp.IX = i; mp.IY = j;
            mp.SetReference(game.GetPosition(i, j));
            mp.SetCoords(matrixX, matrixY);
            StartCoroutine(mp.MakeMoveAnimated(isPlayer: false));
            moveFound = true;
        }
    }

    public void MovePlateAttackSpawn(GameObject piece, int matrixX, int matrixY, int i, int j)
    {
        if (moveFound) return;
        LegalMovesManager = controller.GetComponent<LegalMovesManager>();
        if (LegalMovesManager.IsLegal(piece, i, j, matrixX, matrixY))
        {
            MoveThePlate mp = controller.GetComponent<MoveThePlate>();
            mp.attack = true;
            mp.piece = piece;
            mp.enPassant = false;
            if (piece.name == "white_pawn" || piece.name == "black_pawn")
                mp.PwnTQn = (matrixY == 7 || matrixY == 0);
            mp.IX = i; mp.IY = j;
            mp.SetReference(game.GetPosition(i, j));
            mp.SetCoords(matrixX, matrixY);
            StartCoroutine(mp.MakeMoveAnimated(false));
            moveFound = true;
        }
    }

    public int GetValue(GameObject piece)
    {
        switch (piece.name)
        {
            case "black_king": case "white_king": return 0;
            case "black_queen": case "white_queen": return 9;
            case "black_rook": case "white_rook": return 5;
            case "black_knight": case "white_knight": return 3;
            case "black_bishop": case "white_bishop": return 3;
            case "black_pawn": case "white_pawn": return 1;
        }
        return 0;
    }

    private void GenerateMoveBitmap(GameObject piece)
    {
        availableMoves = new List<int>();
        if (piece == null || piece.GetComponent<Chessman>() == null) return;
        int x = piece.GetComponent<Chessman>().GetXBoard();
        int y = piece.GetComponent<Chessman>().GetYBoard();
        PossibleMoves(x, y, piece);
    }

    public void PossibleMoves(int a, int b, GameObject piece)
    {
        if (piece == null) return;
        switch (piece.name)
        {
            case "black_queen":
            case "white_queen":
                LineMovePlate(piece, a, b, 1, 0); LineMovePlate(piece, a, b, 0, 1);
                LineMovePlate(piece, a, b, 1, 1); LineMovePlate(piece, a, b, -1, 0);
                LineMovePlate(piece, a, b, 0, -1); LineMovePlate(piece, a, b, -1, -1);
                LineMovePlate(piece, a, b, -1, 1); LineMovePlate(piece, a, b, 1, -1);
                break;
            case "black_knight": case "white_knight": LMovePlate(a, b, piece); break;
            case "black_bishop":
            case "white_bishop":
                LineMovePlate(piece, a, b, 1, 1); LineMovePlate(piece, a, b, -1, -1);
                LineMovePlate(piece, a, b, -1, 1); LineMovePlate(piece, a, b, 1, -1);
                break;
            case "black_king": case "white_king": SurroundMovePlate(a, b, piece); break;
            case "black_rook":
            case "white_rook":
                LineMovePlate(piece, a, b, 1, 0); LineMovePlate(piece, a, b, 0, 1);
                LineMovePlate(piece, a, b, -1, 0); LineMovePlate(piece, a, b, 0, -1);
                break;
            case "black_pawn": BPawnMovePlate(a, b, piece); break;
            case "white_pawn": WPawnMovePlate(a, b, piece); break;
        }
    }

    private void LineMovePlate(GameObject piece, int a, int b, int xDir, int yDir)
    {
        int x = a + xDir, y = b + yDir;
        while (PositionOnBoard(x, y) && GetPosition(x, y) == null)
        {
            AddMoveToBitmap(x, y);
            x += xDir; y += yDir;
        }
        if (PositionOnBoard(x, y))
        {
            GameObject t = GetPosition(x, y);
            if (t != null && t.GetComponent<Chessman>() != null &&
                PlayerColour(t) != PlayerColour(piece))
                AddMoveToBitmap(x, y);
        }
    }

    private void LMovePlate(int a, int b, GameObject piece)
    {
        int[][] km = { new[]{1,2},new[]{-1,2},new[]{2,1},new[]{-2,1},
                       new[]{-2,-1},new[]{-1,-2},new[]{1,-2},new[]{2,-1} };
        foreach (var m in km) PointMovePlate(a + m[0], b + m[1], a, b, piece);
    }

    private void SurroundMovePlate(int a, int b, GameObject piece)
    {
        for (int dx = -1; dx <= 1; dx++)
            for (int dy = -1; dy <= 1; dy++)
            {
                if (dx == 0 && dy == 0) continue;
                PointMovePlate(a + dx, b + dy, a, b, piece);
            }
    }

    private void WPawnMovePlate(int x, int y, GameObject piece)
    {
        Game gm = controller.GetComponent<Game>();
        if (gm.PositionOnBoard(x, y + 1) && gm.GetPosition(x, y + 1) == null)
        {
            AddMoveToBitmap(x, y + 1);
            if (y == 1 && gm.GetPosition(x, y + 2) == null) AddMoveToBitmap(x, y + 2);
        }
        CheckPawnCapture(x + 1, y + 1, piece, gm);
        CheckPawnCapture(x - 1, y + 1, piece, gm);
        if (y == 4)
        {
            CheckEnPassant(x + 1, y, x + 1, y + 1, piece, gm);
            CheckEnPassant(x - 1, y, x - 1, y + 1, piece, gm);
        }
    }

    private void BPawnMovePlate(int x, int y, GameObject piece)
    {
        Game gm = controller.GetComponent<Game>();
        if (gm.PositionOnBoard(x, y - 1) && gm.GetPosition(x, y - 1) == null)
        {
            AddMoveToBitmap(x, y - 1);
            if (y == 6 && gm.GetPosition(x, y - 2) == null) AddMoveToBitmap(x, y - 2);
        }
        CheckPawnCapture(x + 1, y - 1, piece, gm);
        CheckPawnCapture(x - 1, y - 1, piece, gm);
        if (y == 3)
        {
            CheckEnPassant(x + 1, y, x + 1, y - 1, piece, gm);
            CheckEnPassant(x - 1, y, x - 1, y - 1, piece, gm);
        }
    }

    private void CheckPawnCapture(int x, int y, GameObject piece, Game gm)
    {
        if (!gm.PositionOnBoard(x, y)) return;
        GameObject t = gm.GetPosition(x, y);
        if (t != null && t.GetComponent<Chessman>() != null &&
            PlayerColour(t) != PlayerColour(piece))
            AddMoveToBitmap(x, y);
    }

    private void CheckEnPassant(int checkX, int checkY, int moveX, int moveY,
                                 GameObject piece, Game gm)
    {
        if (gm.PositionOnBoard(moveX, moveY) && GetEnPassant(checkX, checkY, piece))
            AddMoveToBitmap(moveX, moveY);
    }

    private void PointMovePlate(int x, int y, int a, int b, GameObject piece)
    {
        Game gm = controller.GetComponent<Game>();
        if (!gm.PositionOnBoard(x, y)) return;
        GameObject t = gm.GetPosition(x, y);
        if (t == null)
            AddMoveToBitmap(x, y);
        else if (t.GetComponent<Chessman>() != null && PlayerColour(t) != PlayerColour(piece))
            AddMoveToBitmap(x, y);
    }

    private void AddMoveToBitmap(int x, int y)
    {
        int bit = y * 8 + x;
        if (!availableMoves.Contains(bit)) availableMoves.Add(bit);
    }

    private bool PositionOnBoard(int x, int y) => x >= 0 && x <= 7 && y >= 0 && y <= 7;

    private GameObject GetPosition(int x, int y)
    {
        if (game == null) game = GetComponent<Game>();
        return game.positions[x, y];
    }

    private int PlayerColour(GameObject piece)
    {
        if (piece == null) return -1;
        return piece.name.ToLower().StartsWith("white") ? 1 : 0;
    }

    private bool GetEnPassant(int tox, int toy, GameObject obj)
    {
        Game gm = controller.GetComponent<Game>();
        Game.Move lastMove = gm.GetTheLastMove();
        if (lastMove?.piece != null)
        {
            string n = lastMove.piece.name;
            if (!n.Contains("pawn")) return false;
            if (toy == 5 && !n.Contains("black")) return false;
            if (toy == 2 && !n.Contains("white")) return false;
            return (lastMove.toX == tox && lastMove.toY == toy);
        }
        if (gm.EnPassant)
        {
            GameObject t = gm.positions[tox, toy];
            if (t != null && gm.EPassant == t) return true;
        }
        return false;
    }
}