using Cecs475.BoardGames.Chess.Model;
using Cecs475.BoardGames.ComputerOpponent;
using Cecs475.BoardGames.Model;
using Cecs475.BoardGames.WpfView;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CECS475.BoardGames.Chess.WpfView
{
    public class ChessSquare : INotifyPropertyChanged {
		private int mPlayer;
		/// <summary>
		/// The player that has a piece in the given square, or 0 if empty.
		/// </summary>
		public int Player
		{
			get { return mPlayer; }
			set
			{
				if (value != mPlayer)
				{
					mPlayer = value;
					OnPropertyChanged(nameof(Player));
				}
			}
		}
		/// <summary>
		/// The position of the square.
		/// </summary>
		public BoardPosition Position
		{
			get; set;
		}

		private ChessPiece mPiece;
		public ChessPiece Piece
		{
			get { return mPiece; }
			set
			{
				if (value.Player != mPiece.Player)
				{
					mPiece = value;
					OnPropertyChanged(nameof(Piece));
				}
			}
		}
		private bool mIsCheck;
		public bool IsCheck
		{
			get { return mIsCheck; }
			set
			{
				if (value != mIsCheck)
				{
					mIsCheck = value;
					OnPropertyChanged(nameof(IsCheck));
				}
			}
		}
		private bool mIsCheckmate;
		public bool IsCheckmate
		{
			get { return mIsCheckmate; }
			set
			{
				if (value != mIsCheckmate)
				{
					mIsCheckmate = value;
					OnPropertyChanged(nameof(IsCheckmate));
				}
			}
		}
		private bool mIsSelected;
		public bool IsSelected
		{
			get { return mIsSelected; }
			set
			{
				if (value != mIsSelected)
				{
					mIsSelected = value;
					OnPropertyChanged(nameof(IsSelected));
				}
			}
		}

		
		private bool mIsHightlighted;
		/// <summary>
		/// Whether the square should be highlighted because of a user action.
		/// </summary>
		public bool IsHighlighted
		{
			get { return mIsHightlighted; }
			set
			{
				if (value != mIsHightlighted)
				{
					mIsHightlighted = value;
					OnPropertyChanged(nameof(IsHighlighted));
				}
			}
		}

		public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string name) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }

	public class ChessViewModel : INotifyPropertyChanged, IGameViewModel
	{
		private ChessBoard mBoard;
		private ObservableCollection<ChessSquare> mSquares;
		private ChessSquare mSelectedSquare;
		public event EventHandler GameFinished;
		private const int MAX_AI_DEPTH = 4;
		private IGameAi mGameAi = new MinimaxAi(MAX_AI_DEPTH);

		public ChessViewModel()
		{
			mBoard = new ChessBoard();

			mSquares = new ObservableCollection<ChessSquare>(
				BoardPosition.GetRectangularPositions(8, 8)
				.Select(pos => new ChessSquare()
				{
					Position = pos,
					Player = mBoard.GetPlayerAtPosition(pos),
					Piece = mBoard.GetPieceAtPosition(pos)
				})
			);
			PossMoves = new HashSet<ChessMove>(
				from ChessMove m in mBoard.GetPossibleMoves()
				select m
			);
			StartMoves = new HashSet<BoardPosition>(
				from ChessMove m in mBoard.GetPossibleMoves()
				select m.StartPosition
			) ;
			mSelectedSquare = null;
		}
		public async Task ApplyMove(BoardPosition start, BoardPosition end, ChessPieceType promotionPiece)
		{
			var possMoves = mBoard.GetPossibleMoves() as IEnumerable<ChessMove>;
			// Validate the move as possible.
			foreach (var move in possMoves)
			{
				if (move.StartPosition.Equals(start) && move.EndPosition.Equals(end))
				{
					if (move.PromotionPiece == promotionPiece)
					{
						mBoard.ApplyMove(move);
						break;
					}
				}
			}
			RebindState();
			if (Players == NumberOfPlayers.One && !mBoard.IsFinished)
			{
				var bestMove = await Task.Run(() => mGameAi.FindBestMove(mBoard));
				if (bestMove != null)
				{
					mBoard.ApplyMove(bestMove as ChessMove);
				}
				RebindState();
			}

			if (mBoard.IsFinished)
			{
				GameFinished?.Invoke(this, new EventArgs());
			}
		}

		private void RebindState()
		{
			PossMoves = new HashSet<ChessMove>(
				from ChessMove m in mBoard.GetPossibleMoves()
				select m
			);
			// Rebind the possible moves, now that the board has changed.
			StartMoves = new HashSet<BoardPosition>(
				from ChessMove m in mBoard.GetPossibleMoves()
				select m.StartPosition
			);
			// Update the collection of squares by examining the new board state.
			var newSquares = BoardPosition.GetRectangularPositions(8, 8);
			int i = 0;
			foreach (var pos in newSquares)
			{
				ChessSquare square = mSquares[i];
				mSquares[i].Player = mBoard.GetPlayerAtPosition(pos);
				mSquares[i].Piece = mBoard.GetPieceAtPosition(pos);

				if (Check && square.Piece.PieceType == ChessPieceType.King && square.Player == CurrentPlayer)
				{
					mSquares[i].IsCheck = true;
				}
				else
				{
					mSquares[i].IsCheck = false;
				}
				if (mBoard.IsFinished && square.Piece.PieceType == ChessPieceType.King && square.Player == CurrentPlayer)
				{
					mSquares[i].IsCheckmate = true;
				}
				else
				{
					mSquares[i].IsCheckmate = false;
				}
				i++;
			}
			OnPropertyChanged(nameof(BoardAdvantage));
			OnPropertyChanged(nameof(CurrentPlayer));
			OnPropertyChanged(nameof(CanUndo));
			OnPropertyChanged(nameof(Check));
			OnPropertyChanged(nameof(Checkmate));
		}

		
		public HashSet<BoardPosition> StartMoves
		{
			get; private set;
		}
		public HashSet<ChessMove> PossMoves
		{
			get; private set;
		}
		public ChessSquare SelectedSquare
		{
			get { return mSelectedSquare; }
			set
			{
				if (value != mSelectedSquare)
				{
					mSelectedSquare = value;
					OnPropertyChanged(nameof(SelectedSquare));
				}
			}
		}

		private bool enabler;
		public bool UndoEnabler
		{
			get { return enabler; }
			set
			{
				enabler = value;
				OnPropertyChanged(nameof(CanUndo));
			}
		}
		public ObservableCollection<ChessSquare> Squares => mSquares;
		public GameAdvantage BoardAdvantage => mBoard.CurrentAdvantage;
		public bool Check => mBoard.IsCheck;
		public bool Checkmate => mBoard.IsCheckmate;
		public int CurrentPlayer => mBoard.CurrentPlayer;
		public bool CanUndo => mBoard.MoveHistory.Any() && UndoEnabler;
		public NumberOfPlayers Players { get; set; }

		public event PropertyChangedEventHandler PropertyChanged;
		private void OnPropertyChanged(string name)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
		}
		public void UndoMove()
		{
			if (CanUndo)
			{
				mBoard.UndoLastMove();
				// In one-player mode, Undo has to remove an additional move to return to the
				// human player's turn.
				if (Players == NumberOfPlayers.One && CanUndo)
				{
					mBoard.UndoLastMove();
				}
				RebindState();
			}
		}
	}
}
