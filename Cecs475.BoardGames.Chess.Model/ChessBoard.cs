using System;
using System.Collections.Generic;
using System.Text;
using Cecs475.BoardGames.Model;
using System.Linq;
using System.Transactions;
using System.Collections.Specialized;

namespace Cecs475.BoardGames.Chess.Model {
	/// <summary>
	/// Represents the board state of a game of chess. Tracks which squares of the 8x8 board are occupied
	/// by which player's pieces.
	/// </summary>
	public class ChessBoard : IGameBoard {
		#region Member fields.
		// The history of moves applied to the board.
		private List<ChessMove> mMoveHistory = new List<ChessMove>();
		private List<ChessMove> mMoves;

		public const int BoardSize = 8;
		private const int byteArraySize = 32;
		//byte array to represent the state of the chess board
		private byte[] mChessBoard = new byte[byteArraySize];

		//constant byte variables representing all the pieces on the chess board
		//to help initialize the array of the starting positions for all pieces on a chess board
		private const byte mEmptySpace = 0b_0000_0000;

		private const byte mWhitePawn = 0b_0000_0001;
		private const byte mWhiteRook = 0b_0000_0010;
		private const byte mWhiteKnight = 0b_0000_0011;
		private const byte mWhiteBishop = 0b_0000_0100;
		private const byte mWhiteQueen = 0b_0000_0101;
		private const byte mWhiteKing = 0b_0000_0110;

		private const byte mBlackPawn = 0b_0000_1001;
		private const byte mBlackRook = 0b_0000_1010;
		private const byte mBlackKnight = 0b_0000_1011;
		private const byte mBlackBishop = 0b_0000_1100;
		private const byte mBlackQueen = 0b_0000_1101;
		private const byte mBlackKing = 0b_0000_1110;

		// TODO: Add a means of tracking miscellaneous board state, like captured pieces and the 50-move rule.
		private bool mLeftWhiteRookHasMoved = false;
		private bool mRightWhiteRookHasMoved = false;
		private bool mLeftBlackRookHasMoved = false;
		private bool mRightBlackRookHasMoved = false;
		private bool mWhiteKingHasMoved = false;
		private bool mBlackKingHasMoved = false;

		private int leftWhiteRook = 0;
		private int leftBlackRook = 0;
		private int rightWhiteRook = 0;
		private int rightBlackRook = 0;
		private int whiteKing = 0;
		private int blackKing = 0;

		private BoardPosition whiteLeftRookPos;
		private BoardPosition blackLeftRookPos;
		private BoardPosition whiteRightRookPos;
		private BoardPosition blackRightRookPos;
		private BoardPosition whiteKingPos;
		private BoardPosition blackKingPos;

		private const int pawnValue = 1;
		private const int knightBishopValue = 3;
		private const int rookValue = 5;
		private const int queenValue = 9;

		private bool check;
		private bool checkmate;
		private bool stalemate;

		private IEnumerable<BoardDirection> BishopDirections { get; }
			= new BoardDirection[] {
				new BoardDirection(-1, -1),
				new BoardDirection(-1, 1),
				new BoardDirection(1, -1),
				new BoardDirection(1, 1),
			};
		private IEnumerable<BoardDirection> RookDirections { get; }
			= new BoardDirection[] {
				new BoardDirection(-1, 0),
				new BoardDirection(0, -1),
				new BoardDirection(0, 1),
				new BoardDirection(1, 0),
			};
		private IEnumerable<BoardDirection> KnightDirections { get; }
				= new BoardDirection[] {
					new BoardDirection(-2, -1),
					new BoardDirection(-2, 1),
					new BoardDirection(-1, -2),
					new BoardDirection(-1, 2),
					new BoardDirection(1, -2),
					new BoardDirection(1, 2),
					new BoardDirection(2, -1),
					new BoardDirection(2, 1),

			 };
		private struct CaptureSet {

			public BoardPosition CapturePosition { get; set; }
			public ChessPiece CapturePiece { get; set; }

			public CaptureSet(BoardPosition position, ChessPiece piece) {

				CapturePosition = position;
				CapturePiece = piece;
			}
		}
		private List<CaptureSet> mCaptures = new List<CaptureSet>();
		private List<int> drawCounterValues = new List<int>();


		#endregion

		#region Properties.
		// TODO: implement these properties.
		// You can choose to use auto properties, computed properties, or normal properties 
		// using a private field to back the property.

		// You can add set bodies if you think that is appropriate, as long as you justify
		// the access level (public, private).

		public bool IsFinished { 
			get {
				// return IsCheckmate || IsStalemate || IsDraw;

				var possMoves = GetPossibleMoves();
				return !possMoves.Any() || IsDraw; 
			} 
		}

		public int CurrentPlayer { get; private set; }

		public GameAdvantage CurrentAdvantage { get { return Advantage(); } }

		public IReadOnlyList<ChessMove> MoveHistory => mMoveHistory;

		public bool IsPawnPromotion { get; private set; }

		public bool IsCheck  {

			get {

				if (!checkmate) {

					int oppositePlayer = CurrentPlayer == 1 ? 2 : 1;
					var king = GetPositionsOfPiece(ChessPieceType.King, CurrentPlayer);
					bool checkTest = PositionIsAttacked(king.FirstOrDefault(), oppositePlayer);

					if (checkTest) {
						check = true;
						checkmate = false;
						stalemate = false;
					}
					else {
						check = false;
					}
				}
				else {
					check = false;
				}
				return check;
			}
			set { check = value;  }
		}
		public bool IsCheckmate {
			
			get {

				int oppositePlayer = CurrentPlayer == 1 ? 2 : 1;
				var king = GetPositionsOfPiece(ChessPieceType.King, CurrentPlayer);
				bool checkTest = PositionIsAttacked(king.FirstOrDefault(), oppositePlayer);
				bool checkmateTest = CheckForCheckmate();

				if (checkTest && CheckForCheckmate()) {

					check = false; ;
					checkmate = true;
					stalemate = false;
				}
				else {

					check = false;
				}
				return checkmate;
			}
			set { checkmate = value; }
		}
		public bool IsStalemate {
			
			get {
				int oppositePlayer = CurrentPlayer == 1 ? 2 : 1;
				var king = GetPositionsOfPiece(ChessPieceType.King, CurrentPlayer);
				bool checkTest = PositionIsAttacked(king.FirstOrDefault(), oppositePlayer);
				var possibleMoves = GetPossibleMoves();

				if (!checkTest && possibleMoves.Count() == 0) {

					check = false;
					checkmate = false;
					stalemate = true;
				}
			    else {
					stalemate = false;
				}
				return stalemate;
			} 
			set { stalemate = value; }
	    }


		public bool IsDraw { get { return DrawCounter == 100; } }
	

        /// <summary>
        /// Tracks the current draw counter, which goes up by 1 for each non-capturing, non-pawn move, and resets to 0
        /// for other moves. If the counter reaches 100 (50 full turns), the game is a draw.
        /// </summary>
        public int DrawCounter { get; private set; } = 0;
		#endregion


		#region Public methods.
		public IEnumerable<ChessMove> GetPossibleMoves() {

			if (mMoves != null)
			{
				return mMoves;
			}
			int oppositePlayer = CurrentPlayer == 1 ? 2 : 1, currentPlayer = CurrentPlayer == 1 ? 1 : 2;
			List<ChessMove> possibleMoves = new List<ChessMove>();
			var kingPosition = GetPositionsOfPiece(ChessPieceType.King, currentPlayer).FirstOrDefault();

			foreach (var pos in BoardPosition.GetRectangularPositions(BoardSize, BoardSize)) {

				var currentPiece = GetPieceAtPosition(pos);

				if (currentPiece.Player == currentPlayer) {

					if (currentPiece.PieceType == ChessPieceType.Pawn) {

						var pawnMoves = GetPawnPossibleMoves(currentPlayer, pos);
						foreach (var move in pawnMoves)
						{

							ApplyMove(move);
							if (!PositionIsAttacked(kingPosition, oppositePlayer))
							{

								possibleMoves.Add(move);
							}
							UndoLastMove();
						}
						var pawnAttacks = GetPawnAttackMoves(currentPlayer, pos);
						foreach (var move in pawnAttacks)
						{

							ApplyMove(move);
							if (!PositionIsAttacked(kingPosition, oppositePlayer))
							{

								possibleMoves.Add(move);
							}
							UndoLastMove();
						}
						if (AttemptPawnPromotion(currentPlayer, pos))
						{

							var pawnPromotions = PawnPromotionMoves(currentPlayer, pos);
							foreach (var move in pawnPromotions)
							{

								ApplyMove(move);
								if (!PositionIsAttacked(kingPosition, oppositePlayer))
								{

									possibleMoves.Add(move);
								}
								UndoLastMove();
							}
						}
						if (AttemptEnPassant(currentPlayer, pos))
						{

							var enPassant = EnPassantMoves(CurrentPlayer, pos);
							foreach (var move in enPassant)
							{

								ApplyMove(move);
								if (!PositionIsAttacked(kingPosition, oppositePlayer))
								{

									possibleMoves.Add(move);
								}
								UndoLastMove();
							}
						}
					}
					else if (currentPiece.PieceType == ChessPieceType.Rook)
                    {
						var rookAttacks = GetRookAttackMoves(currentPlayer, pos);
						foreach (var move in rookAttacks)
						{

							ApplyMove(move);
							if (!PositionIsAttacked(kingPosition, oppositePlayer))
							{

								possibleMoves.Add(move);
							}
							UndoLastMove();
						}
					}
                    else if (currentPiece.PieceType == ChessPieceType.Bishop)
                    {
						var bishopAttacks = GetBishopAttackMoves(currentPlayer, pos);
						foreach (var move in bishopAttacks)
						{

							ApplyMove(move);
							if (!PositionIsAttacked(kingPosition, oppositePlayer))
							{

								possibleMoves.Add(move);
							}
							UndoLastMove();
						}
					}
					else if (currentPiece.PieceType == ChessPieceType.Knight)
                    {
						var knightAttacks = GetKnightAttackMoves(currentPlayer, pos);
						foreach (var move in knightAttacks)
						{

							ApplyMove(move);
							if (!PositionIsAttacked(kingPosition, oppositePlayer))
							{

								possibleMoves.Add(move);
							}
							UndoLastMove();
						}
					}
					else if (currentPiece.PieceType == ChessPieceType.Queen)
                    {
						var queenAttacks = GetQueenAttackMoves(currentPlayer, pos);
						foreach (var move in queenAttacks)
						{

							ApplyMove(move);
							if (!PositionIsAttacked(kingPosition, oppositePlayer))
							{

								possibleMoves.Add(move);
							}
							UndoLastMove();
						}
					}
					else if (currentPiece.PieceType == ChessPieceType.King)
                    {
						var kingAttacks = GetKingAttackMoves(currentPlayer, pos);
						foreach (var move in kingAttacks)
						{

							ApplyMove(move);
							if (!PositionIsAttacked(move.EndPosition, oppositePlayer))
							{

								possibleMoves.Add(move);
							}
							UndoLastMove();
						}
						if (AttemptCastleQueenSide(currentPlayer))
						{

							var castleMoves = CastleQueenSideMoves(currentPlayer);
							foreach (var move in castleMoves)
							{

								ApplyMove(move);
								if (!PositionIsAttacked(kingPosition, oppositePlayer))
								{

									possibleMoves.Add(move);
								}
								UndoLastMove();
							}
						}
						if (AttemptCastleKingSide(currentPlayer))
						{

							var castleMoves = CastleKingSideMoves(currentPlayer);
							foreach (var move in castleMoves)
							{

								ApplyMove(move);
								if (!PositionIsAttacked(kingPosition, oppositePlayer))
								{

									possibleMoves.Add(move);
								}
								UndoLastMove();
							}
						}
					}
                }
            }
			if (possibleMoves.Count() == 0 && check == true)
			{
				checkmate = true;
			}
			else
			{
				stalemate = true;
			}
			return mMoves = possibleMoves;
		}

		public void ApplyMove(ChessMove m) {

			if (m == null) {

				throw new ArgumentNullException(nameof(m));
			}

			BoardPosition startPos = m.StartPosition, endPos = m.EndPosition;
			ChessMoveType moveType = m.MoveType;
			ChessPiece setPiece = GetPieceAtPosition(startPos), capturedPiece = GetPieceAtPosition(endPos);
			m.Player = CurrentPlayer;
			int oppositePlayer = CurrentPlayer == 1 ? 2 : 1;
			drawCounterValues.Add(DrawCounter);

			if (setPiece.PieceType != ChessPieceType.Pawn && capturedPiece.PieceType == ChessPieceType.Empty) {

				DrawCounter++;
			}
			else {
				DrawCounter = 0;
			}

			if (moveType.Equals(ChessMoveType.Normal)) {

				if (setPiece.PieceType.Equals(ChessPieceType.King)) {

					if (setPiece.Player == 1) {

						if (startPos.Equals(whiteKingPos)) {
							
							mWhiteKingHasMoved = true;
							whiteKing++;
							whiteKingPos = endPos;
						}
						
					}
					else {

						if (startPos.Equals(blackKingPos)) {
							mBlackKingHasMoved = true;
							blackKing++;
							blackKingPos = endPos;
						}
						
					}

				} 
				if (setPiece.PieceType.Equals(ChessPieceType.Rook)) {

					if (setPiece.Player == 1) {

						if (startPos.Equals(whiteLeftRookPos)) {
							
							mLeftWhiteRookHasMoved = true;
							leftWhiteRook++;
							whiteLeftRookPos = endPos;
						}
						else {

							mRightWhiteRookHasMoved = true;
							rightWhiteRook++;
							whiteRightRookPos = endPos;
						}
					}  
					else {

						if (startPos.Equals(blackLeftRookPos)) {

							mLeftBlackRookHasMoved = true;
							leftBlackRook++;
							blackLeftRookPos = endPos;
						}
						else {

							mRightBlackRookHasMoved = true;
							rightBlackRook++;
							blackRightRookPos = endPos;
						}
					}
				}
				mCaptures.Add(new CaptureSet(endPos, capturedPiece));
				SetPieceAtPosition(endPos, setPiece);
				SetPieceAtPosition(startPos, ChessPiece.Empty);
				ChangeAdvantage(capturedPiece, 1);
			}
			else if (moveType == ChessMoveType.CastleQueenSide || moveType == ChessMoveType.CastleKingSide) {

				BoardPosition newRookPos, oldRookPos;

				if (moveType == ChessMoveType.CastleQueenSide) {

					oldRookPos = m.Player == 1 ? new BoardPosition(7, 0) : new BoardPosition(0, 0);
					newRookPos = new BoardPosition(startPos.Row, startPos.Col - 1);
				}
				else {

					oldRookPos = m.Player == 1 ? new BoardPosition(7, 7) : new BoardPosition(0, 7);
					newRookPos = new BoardPosition(startPos.Row, startPos.Col + 1);
				}
				mCaptures.Add(new CaptureSet(endPos, capturedPiece));
				SetPieceAtPosition(oldRookPos, ChessPiece.Empty);
				SetPieceAtPosition(newRookPos, new ChessPiece(ChessPieceType.Rook, CurrentPlayer));
				SetPieceAtPosition(endPos, setPiece);
				SetPieceAtPosition(startPos, ChessPiece.Empty);
			}
			else if (moveType == ChessMoveType.EnPassant) {

				ChessPiece pawnToCapture;

				if (CurrentPlayer == 2) {

					pawnToCapture = GetPieceAtPosition(endPos.Translate(-1, 0));
					SetPieceAtPosition(endPos.Translate(-1, 0), ChessPiece.Empty);
					SetPieceAtPosition(endPos, new ChessPiece(ChessPieceType.Pawn, CurrentPlayer));
					SetPieceAtPosition(startPos, ChessPiece.Empty);
					mCaptures.Add(new CaptureSet(endPos.Translate(-1, 0), pawnToCapture));
				}
				else {

					pawnToCapture = GetPieceAtPosition(endPos.Translate(1, 0));
					SetPieceAtPosition(endPos.Translate(1, 0), ChessPiece.Empty);
					SetPieceAtPosition(endPos, new ChessPiece(ChessPieceType.Pawn, CurrentPlayer));
					SetPieceAtPosition(startPos, ChessPiece.Empty);
					mCaptures.Add(new CaptureSet(endPos.Translate(1, 0), pawnToCapture));
				}
				ChangeAdvantage(new ChessPiece(ChessPieceType.Pawn, oppositePlayer), 1);
			}
			else if (moveType == ChessMoveType.PawnPromote) {

				ChessPieceType promotionPiece = m.PromotionPiece;

				mCaptures.Add(new CaptureSet(endPos, capturedPiece));
				SetPieceAtPosition(endPos, new ChessPiece(promotionPiece, CurrentPlayer));
				SetPieceAtPosition(startPos, ChessPiece.Empty);
				ChangeAdvantage(new ChessPiece(ChessPieceType.Pawn, CurrentPlayer) , 1);
				ChangeAdvantage(new ChessPiece(promotionPiece, CurrentPlayer), -1);
				ChangeAdvantage(capturedPiece, 1);
				IsPawnPromotion = true;
			}
			mMoveHistory.Add(m);
			CurrentPlayer = CurrentPlayer == 1 ? 2 : 1;
			mMoves = null;
		}
		public void UndoLastMove() {

			ChessMove lastMove = mMoveHistory.Last();
			if (lastMove == null) {

				throw new ArgumentNullException(nameof(lastMove));
			}

			BoardPosition startPos = lastMove.StartPosition, endPos = lastMove.EndPosition;
			ChessMoveType moveType = lastMove.MoveType;
			ChessPiece moveBackPiece = GetPieceAtPosition(endPos);
			CaptureSet capturedMove = mCaptures.LastOrDefault();
			int player, oppositePlayer = CurrentPlayer == 1 ? 2 : 1;
			DrawCounter = drawCounterValues.LastOrDefault();

			if (moveType == ChessMoveType.Normal) {

				if (moveBackPiece.PieceType == ChessPieceType.King)
				{

					if (moveBackPiece.Player == 1) {

						whiteKing--;

						if (whiteKing == 0)
						{
							mWhiteKingHasMoved = false;
						}
						whiteKingPos = startPos;
					}
					else
					{
						blackKing--;

						if (blackKing == 0)
						{
							mBlackKingHasMoved = false;
						}
						blackKingPos = startPos;
					}
				}
				if (moveBackPiece.PieceType == ChessPieceType.Rook)
				{

					if (moveBackPiece.Player == 1)
					{

						if (endPos.Equals(whiteLeftRookPos))
						{
							leftWhiteRook--;

							if (leftWhiteRook == 0)
							{
								mLeftWhiteRookHasMoved = false;
							}
							whiteLeftRookPos = startPos;
						}
						else
						{
							rightWhiteRook--;

							if (rightWhiteRook == 0)
							{
								mRightWhiteRookHasMoved = false;
							}
							whiteRightRookPos = startPos;
						}
					}
					else
					{

						if (endPos.Equals(blackLeftRookPos)) {

								leftBlackRook--;

								if (leftBlackRook == 0)
								{
									mLeftBlackRookHasMoved = false;
								}
								blackLeftRookPos = startPos;
							}
						else
							{
								rightBlackRook--;

								if (rightBlackRook == 0)
								{
									mRightBlackRookHasMoved = false;
								}
								blackRightRookPos = startPos;
						}
					}
				}

				SetPieceAtPosition(endPos, new ChessPiece(capturedMove.CapturePiece.PieceType, CurrentPlayer));
				SetPieceAtPosition(startPos, moveBackPiece);
				ChangeAdvantage(new ChessPiece(capturedMove.CapturePiece.PieceType, player = lastMove.Player == 1 ? 2 : 1), -1);
			}
			else if (moveType == ChessMoveType.CastleQueenSide || moveType == ChessMoveType.CastleKingSide) {

				BoardPosition newRookPos, oldRookPos;

				if (moveType == ChessMoveType.CastleQueenSide) {

					oldRookPos = lastMove.Player == 1 ? new BoardPosition(7, 0) : new BoardPosition(0, 0);
					newRookPos = new BoardPosition(startPos.Row, startPos.Col - 1);
				}
				else {

					oldRookPos = lastMove.Player == 1 ? new BoardPosition(7, 7) : new BoardPosition(0, 7);
					newRookPos = new BoardPosition(startPos.Row, startPos.Col + 1);
				}
				SetPieceAtPosition(oldRookPos, new ChessPiece(ChessPieceType.Rook, lastMove.Player));
				SetPieceAtPosition(newRookPos, ChessPiece.Empty);
				SetPieceAtPosition(endPos, ChessPiece.Empty);
				SetPieceAtPosition(startPos, new ChessPiece(ChessPieceType.King, lastMove.Player));
			}
			else if (moveType == ChessMoveType.EnPassant) {

				if (lastMove.Player == 2) {

					SetPieceAtPosition(endPos.Translate(-1, 0), new ChessPiece(ChessPieceType.Pawn, CurrentPlayer));
					SetPieceAtPosition(endPos, ChessPiece.Empty);
					SetPieceAtPosition(startPos, new ChessPiece(ChessPieceType.Pawn, lastMove.Player));
				}
				else { 

					SetPieceAtPosition(endPos.Translate(1, 0), new ChessPiece(ChessPieceType.Pawn, CurrentPlayer));
					SetPieceAtPosition(endPos, ChessPiece.Empty);
					SetPieceAtPosition(startPos, new ChessPiece(ChessPieceType.Pawn, lastMove.Player));
				}
				ChangeAdvantage(new ChessPiece(ChessPieceType.Pawn, player = lastMove.Player == 1 ? 2 : 1), -1);
			}
			else if (moveType == ChessMoveType.PawnPromote) {

				SetPieceAtPosition(endPos, new ChessPiece(capturedMove.CapturePiece.PieceType, CurrentPlayer));
				SetPieceAtPosition(startPos, new ChessPiece(ChessPieceType.Pawn, lastMove.Player));
				ChangeAdvantage(new ChessPiece(ChessPieceType.Pawn, player = lastMove.Player == 1 ? 2 : 1), 1);
				ChangeAdvantage(new ChessPiece(lastMove.PromotionPiece, player = lastMove.Player == 1 ? 2 : 1), -1);
				ChangeAdvantage(new ChessPiece(capturedMove.CapturePiece.PieceType, player = lastMove.Player == 1 ? 2 : 1), -1);
				IsPawnPromotion = false;
			}
			mMoveHistory.RemoveAt(mMoveHistory.Count - 1);
			mCaptures.RemoveAt(mCaptures.Count - 1);
			drawCounterValues.RemoveAt(drawCounterValues.Count - 1);
			CurrentPlayer = CurrentPlayer == 1 ? 2 : 1;
			mMoves = null;

			if (checkmate == true)
			{
				checkmate = false;
			}
			if (stalemate == true)
			{
				stalemate = false;
			}

		}

		/// <summary>
		/// Returns whatever chess piece is occupying the given position.
		/// </summary>
		public ChessPiece GetPieceAtPosition(BoardPosition position) {

			if (!PositionInBounds(position))
			{
				return new ChessPiece(ChessPieceType.Empty, 0);
			}
			int row = position.Row, col = position.Col, player;
			byte piece = GetByteAtPos(row, col);
			//see what piece the position contains regardless
			if (piece.Equals(mWhitePawn) || piece.Equals(mBlackPawn))
			{

				player = piece.Equals(mWhitePawn) ? 1 : 2;
				return new ChessPiece(ChessPieceType.Pawn, player);
			}
			else if (piece.Equals(mWhiteRook) || piece.Equals(mBlackRook))
			{

				player = piece.Equals(mWhiteRook) ? 1 : 2;
				return new ChessPiece(ChessPieceType.Rook, player);
			}
			else if (piece.Equals(mWhiteKnight) || piece.Equals(mBlackKnight))
			{

				player = piece.Equals(mWhiteKnight) ? 1 : 2;
				return new ChessPiece(ChessPieceType.Knight, player);
			}
			else if (piece.Equals(mWhiteBishop) || piece.Equals(mBlackBishop))
			{

				player = piece.Equals(mWhiteBishop) ? 1 : 2;
				return new ChessPiece(ChessPieceType.Bishop, player);
			}
			else if (piece.Equals(mWhiteQueen) || piece.Equals(mBlackQueen))
			{

				player = piece.Equals(mWhiteQueen) ? 1 : 2;
				return new ChessPiece(ChessPieceType.Queen, player);
			}
			else if (piece.Equals(mWhiteKing) || piece.Equals(mBlackKing))
			{

				player = piece.Equals(mWhiteKing) ? 1 : 2;
				return new ChessPiece(ChessPieceType.King, player);
			}
			else
			{

				return new ChessPiece(ChessPieceType.Empty, 0);
			}
		}

		/// <summary>
		/// Returns whatever player is occupying the given position.
		/// </summary>
		public int GetPlayerAtPosition(BoardPosition pos) {
			// As a hint, you should call GetPieceAtPosition.

			ChessPiece piece = GetPieceAtPosition(pos);

			if (piece.Player == 1) {

				return 1;
			}
			else if (piece.Player == 2) {

				return 2;
			}
			else {

				return 0;
			}
		}

		/// <summary>
		/// Returns true if the given position on the board is empty.
		/// </summary>
		/// <remarks>returns false if the position is not in bounds</remarks>
		public bool PositionIsEmpty(BoardPosition pos) {

			if (PositionInBounds(pos)) {

				return GetPieceAtPosition(pos).PieceType == ChessPieceType.Empty ? true : false;
			}
			return false;
		}

		/// <summary>
		/// Returns true if the given position contains a piece that is the enemy of the given player.
		/// </summary>
		/// <remarks>returns false if the position is not in bounds</remarks>
		public bool PositionIsEnemy(BoardPosition pos, int player) {
			
			if (PositionInBounds(pos)) {

				if (PositionIsEmpty(pos)) {

					return false;
				}
				else {

					return GetPlayerAtPosition(pos) == player ? false : true;
				}
				
			}
			return false;
		}

		/// <summary>
		/// Returns true if the given position is in the bounds of the board.
		/// </summary>
		public static bool PositionInBounds(BoardPosition pos) {

			//position is out of bounds
			if ((pos.Row < 0 || pos.Row > 7) || (pos.Col < 0 || pos.Col > 7)) {

				return false;
			}
			return true;
		}

		/// <summary>
		/// Returns all board positions where the given piece can be found.
		/// </summary>
		public IEnumerable<BoardPosition> GetPositionsOfPiece(ChessPieceType piece, int player) {

			var posList = new List<BoardPosition>();
			byte bytePiece = GetPieceAsByte(piece, player), bytePos;

			foreach (var pos in BoardPosition.GetRectangularPositions(BoardSize, BoardSize)) {

				bytePos = GetByteAtPos(pos.Row, pos.Col);

				if (bytePiece.Equals(bytePos)) {

					posList.Add(pos);
				}
			}
			return posList;
		}

		/// <summary>
		/// Returns true if the given player's pieces are attacking the given position.
		/// </summary>
		public bool PositionIsAttacked(BoardPosition position, int byPlayer) {

			ISet<BoardPosition> boardPos = GetAttackedPositions(byPlayer);
			return boardPos.Contains(position) ? true : false;
		}

		/// <summary>
		/// Returns a set of all BoardPositions that are attacked by the given player.
		/// </summary>
		public ISet<BoardPosition> GetAttackedPositions(int byPlayer) {

			HashSet<BoardPosition> attackedPos = new HashSet<BoardPosition>();

			var pawnAttackPos = GetPawnAttackPositions(byPlayer);
			foreach (var pos in pawnAttackPos) {
				
				attackedPos.Add(pos);
				
			}
			var rookAttackPos = GetRookAttackPositions(byPlayer);
			foreach (var pos in rookAttackPos) {

				

					attackedPos.Add(pos);
				
			}
			var knightAttackPos = GetKnightAttackPositions(byPlayer);
			foreach (var pos in knightAttackPos) {

				

					attackedPos.Add(pos);
				
			}
			var bishopAttackPos = GetBishopAttackPositions(byPlayer);
			foreach (var pos in bishopAttackPos) {

				

					attackedPos.Add(pos);
				
			}
			var queenAttackPos = GetQueenAttackPositions(byPlayer);
			foreach (var pos in queenAttackPos) {


					attackedPos.Add(pos);
				
			}
			var kingAttackPos = GetKingAttackPositions(byPlayer);
			foreach (var pos in kingAttackPos) {

				

					attackedPos.Add(pos);
				
			}
			return attackedPos;
		}
		#endregion

		#region Private methods.
		/// <summary>
		/// Mutates the board state so that the given piece is at the given position.
		/// </summary>
		private void SetPieceAtPosition(BoardPosition position, ChessPiece piece) {

			byte setPiece = GetPieceAsByte(piece.PieceType, piece.Player);
			int row = position.Row, col = position.Col, pos = ((row * 8) + col) / 2;
			byte piecesAtPos = mChessBoard[pos], keepPiece, newPos;

			//shift 4 bits to the right and back to the left again
			if ((col + 1) % 2 == 0) {

				byte low = (byte)(piecesAtPos >> 4);
				keepPiece = (byte)(low << 4);
				newPos = (byte) (keepPiece + setPiece);
			}
			//shift 4 bits to the left and back to the right again
			else {

				byte high = (byte)(piecesAtPos << 4);
				keepPiece = (byte)(high >> 4);
				setPiece = (byte)(setPiece << 4);
				newPos = (byte)(keepPiece + setPiece);
			}
			mChessBoard[pos] = newPos;
		}

		private byte GetPieceAsByte(ChessPieceType piece, int player) {

			byte bytePiece = 0;
			switch (piece){

				case ChessPieceType.Pawn:

					bytePiece = (byte) player == 1 ? mWhitePawn : mBlackPawn;
					break;

				case ChessPieceType.Rook:

					bytePiece = (byte) player == 1 ? mWhiteRook : mBlackRook;
					break;

				case ChessPieceType.Knight:

					bytePiece = (byte) player == 1 ? mWhiteKnight : mBlackKnight;
					break;

				case ChessPieceType.Bishop:

					bytePiece = (byte) player == 1 ? mWhiteBishop : mBlackBishop;
					break;

				case ChessPieceType.Queen:

					bytePiece = (byte) player == 1 ? mWhiteQueen : mBlackQueen;
					break;

				case ChessPieceType.King:

					bytePiece = (byte) player == 1 ? mWhiteKing : mBlackKing;
					break;

				case ChessPieceType.Empty:

					bytePiece = mEmptySpace;
					break;
			}
			return bytePiece;
		}

		private byte GetByteAtPos(int row, int col) {

			int pos = ((row * 8) + col) / 2;
			byte piece, piecesAtPos = mChessBoard[pos];

			//shift 4 bits to the left and back to the right again
			if ((col + 1) % 2 == 0) {

				byte low = (byte)(piecesAtPos << 4);
				piece = (byte)(low >> 4);
			}
			//shift 4 bits to the right
			else {

				var high = piecesAtPos >> 4;
				piece = (byte)high;
			}
			return piece;
		}

		private bool CheckForCheckmate() {

			var possibleMoves = GetPossibleMoves();
			return (possibleMoves.Count() == 0);
		}

		private List<BoardPosition> GetPawnAttackPositions(int byPlayer) {

			List<BoardPosition> pawnAttackPositions = new List<BoardPosition>();
			BoardPosition newPosition;
			var pawnPositions = GetPositionsOfPiece(ChessPieceType.Pawn, byPlayer);
			var pawnAttackDirections = PawnAttackDirections(byPlayer);

			foreach (var pos in pawnPositions) {

				foreach (var dir in pawnAttackDirections) {

					newPosition = pos.Translate(dir);

					if (PositionInBounds(newPosition)) {

						pawnAttackPositions.Add(newPosition);
					}
				}
			}
			return pawnAttackPositions;
		} 
		private List<BoardPosition> GetRookAttackPositions(int byPlayer) {

			List<BoardPosition> rookAttackPositions = new List<BoardPosition>();
			BoardPosition newPosition;

			var rookPositions = GetPositionsOfPiece(ChessPieceType.Rook, byPlayer);

			foreach (var pos in rookPositions) {

				foreach (var dir in RookDirections) {

					newPosition = pos.Translate(dir);

					while (PositionInBounds(newPosition)) {

						rookAttackPositions.Add(newPosition);

						if (PositionIsEnemy(newPosition, byPlayer) || GetPlayerAtPosition(newPosition) == byPlayer) {

							break;
						}
						newPosition = newPosition.Translate(dir);
					}
				}
			}
			return rookAttackPositions;
		}
		private List<BoardPosition> GetKnightAttackPositions(int byPlayer) {

			List<BoardPosition> knightAttackPositions = new List<BoardPosition>();
			BoardPosition newPosition;

			var knightPositions = GetPositionsOfPiece(ChessPieceType.Knight, byPlayer);

			foreach (var pos in knightPositions) {

				foreach (var dir in KnightDirections) {

					newPosition = pos.Translate(dir) ;

					if (PositionInBounds(newPosition)){

						knightAttackPositions.Add(newPosition);
					}
				}
			}
			return knightAttackPositions;
		}

		private List<BoardPosition> GetBishopAttackPositions(int byPlayer)
		{

			List<BoardPosition> bishopAttackPositions = new List<BoardPosition>();
			BoardPosition newPosition;

			var bishopPositions = GetPositionsOfPiece(ChessPieceType.Bishop, byPlayer);

			foreach (var pos in bishopPositions)
			{

				foreach (var dir in BishopDirections)
				{

					newPosition = pos.Translate(dir);

					while (PositionInBounds(newPosition))
					{

						bishopAttackPositions.Add(newPosition);

						if (PositionIsEnemy(newPosition, byPlayer) || GetPlayerAtPosition(newPosition) == byPlayer)
						{

							break;
						}
						newPosition = newPosition.Translate(dir);
					}
				}
			}
			return bishopAttackPositions;
		}
		private List<BoardPosition> GetQueenAttackPositions(int byPlayer) {

			List<BoardPosition> queenAttackPositions = new List<BoardPosition>();
			BoardPosition newPosition;

			var queenPositions = GetPositionsOfPiece(ChessPieceType.Queen, byPlayer);

			foreach (var pos in queenPositions) {

				foreach (var dir in BoardDirection.CardinalDirections) {

					newPosition = pos.Translate(dir);

					while (PositionInBounds(newPosition)) {

						queenAttackPositions.Add(newPosition);

						if (PositionIsEnemy(newPosition, byPlayer) || GetPlayerAtPosition(newPosition) == byPlayer) {

							break;
						}
						newPosition = newPosition.Translate(dir);
					}
				}
			}
			return queenAttackPositions;
		}
		private List<BoardPosition> GetKingAttackPositions(int byPlayer) {

			List<BoardPosition> kingAttackPositions = new List<BoardPosition>();
			BoardPosition newPosition;

			var kingPositions = GetPositionsOfPiece(ChessPieceType.King, byPlayer);

			foreach (var pos in kingPositions) {

				foreach (var dir in BoardDirection.CardinalDirections) {

					newPosition = pos.Translate(dir);

					if (PositionInBounds(newPosition)) {

						kingAttackPositions.Add(newPosition);
					}
				}
			}
			return kingAttackPositions;
		}
		private List<ChessMove> GetPawnPossibleMoves(int byPlayer, BoardPosition pos) {

			List<ChessMove> pawnPossibleMoves = new List<ChessMove>();
			BoardPosition newPosition;
			BoardDirection pawnMoveDirection;

			if (byPlayer == 2) {

				pawnMoveDirection = new BoardDirection(1, 0);

					if (pos.Row == 1) {

						newPosition = pos.Translate(pawnMoveDirection);

						if (PositionIsEmpty(newPosition))
						{
							pawnPossibleMoves.Add(new ChessMove(pos, newPosition));
							newPosition = newPosition.Translate(pawnMoveDirection);
						}

						if (PositionIsEmpty(newPosition)) {
							pawnPossibleMoves.Add(new ChessMove(pos, newPosition));
						}
					
					else {

						newPosition = pos.Translate(pawnMoveDirection);

						if (PositionIsEmpty(newPosition) && pos.Row != 6)
						{
							pawnPossibleMoves.Add(new ChessMove(pos, newPosition));
						}
					}
				}
			}
			else {

				pawnMoveDirection = new BoardDirection(-1, 0);

					if (pos.Row == 6) {

						newPosition = pos.Translate(pawnMoveDirection);

						if (PositionIsEmpty(newPosition))
						{
							pawnPossibleMoves.Add(new ChessMove(pos, newPosition));
							newPosition = newPosition.Translate(pawnMoveDirection);
						}

						if (PositionIsEmpty(newPosition)) {
							pawnPossibleMoves.Add(new ChessMove(pos, newPosition));
						}
					}
					else {

						newPosition = pos.Translate(pawnMoveDirection);

						if (PositionIsEmpty(newPosition) && pos.Row != 1)
						{
							pawnPossibleMoves.Add(new ChessMove(pos, newPosition));
						}
					}
				
			}
			return pawnPossibleMoves;
		}
		private List<ChessMove> GetPawnAttackMoves(int byPlayer, BoardPosition pos) {

			List<ChessMove> pawnAttackMoves = new List<ChessMove>();
			BoardPosition newPosition;
			var pawnAttackDirections = PawnAttackDirections(byPlayer);

				foreach (var dir in pawnAttackDirections) {

					newPosition = pos.Translate(dir);

					if (PositionInBounds(newPosition)) {

						int row = byPlayer == 1 ? 1 : 6;
						if (PositionIsEnemy(newPosition, byPlayer) && pos.Row != row) {

							pawnAttackMoves.Add(new ChessMove(pos, newPosition));
						}
					}
				}
			
			return pawnAttackMoves;
		}
		private List<ChessMove> GetRookAttackMoves(int byPlayer, BoardPosition pos) {

			List<ChessMove> rookAttackMoves = new List<ChessMove>();
			BoardPosition newPosition;

				foreach (var dir in RookDirections) {

					newPosition = pos.Translate(dir);

					while (PositionInBounds(newPosition)) {

						if (PositionIsEmpty(newPosition)) {

							rookAttackMoves.Add(new ChessMove(pos, newPosition));
						}
						else if (PositionIsEnemy(newPosition, byPlayer)){

							rookAttackMoves.Add(new ChessMove(pos, newPosition));
							break;
						}
						else {

							break;
						}
						newPosition = newPosition.Translate(dir) ;
					}
				}
			
			return rookAttackMoves;
		}
		private List<ChessMove> GetKnightAttackMoves(int byPlayer, BoardPosition pos) {

			List<ChessMove> knightAttackMoves = new List<ChessMove>();
			BoardPosition newPosition;

				foreach (var knightDirs in KnightDirections) {

					newPosition = pos.Translate(knightDirs);
					if (PositionInBounds(newPosition)) {

						if (PositionIsEmpty(newPosition)) {

							knightAttackMoves.Add(new ChessMove(pos, newPosition));
						}
						else if (PositionIsEnemy(newPosition, byPlayer)) {

							knightAttackMoves.Add(new ChessMove(pos, newPosition));
							
						}
					}
				}
			
			return knightAttackMoves;
		}
		private List<ChessMove> GetBishopAttackMoves(int byPlayer, BoardPosition pos) {

			List<ChessMove> bishopAttackMoves = new List<ChessMove>();
			BoardPosition newPosition;



				foreach (var dir in BishopDirections) {

					newPosition = pos.Translate(dir);

					while (PositionInBounds(newPosition)) {

						if (PositionIsEmpty(newPosition)) {

							bishopAttackMoves.Add(new ChessMove(pos, newPosition));
						}
						else if (PositionIsEnemy(newPosition, byPlayer)){

							bishopAttackMoves.Add(new ChessMove(pos, newPosition));
							break;
						}
						else {
							break;
						}
						newPosition = newPosition.Translate(dir); ;
					}
				}
			
			return bishopAttackMoves;
		}
		private List<ChessMove> GetQueenAttackMoves(int byPlayer, BoardPosition pos) {

			List<ChessMove> queenAttackMoves = new List<ChessMove>();
			BoardPosition newPosition;


				foreach (var dir in BoardDirection.CardinalDirections) {

					newPosition = pos.Translate(dir);

					while (PositionInBounds(newPosition)) {

						if (PositionIsEmpty(newPosition)) {

							queenAttackMoves.Add(new ChessMove(pos, newPosition));
						}
						else if (PositionIsEnemy(newPosition, byPlayer)) {

							queenAttackMoves.Add(new ChessMove(pos, newPosition));
							break;
						}
						else {
							break;
						}
						newPosition = newPosition.Translate(dir);
					}
				}
			
			return queenAttackMoves;
		}
		private List<ChessMove> GetKingAttackMoves(int byPlayer, BoardPosition pos) {

			List<ChessMove> kingAttackMoves = new List<ChessMove>();
			BoardPosition newPosition;

			var kingPositions = GetPositionsOfPiece(ChessPieceType.King, byPlayer);


				foreach (var dir in BoardDirection.CardinalDirections) {

					newPosition = pos.Translate(dir);

					if (PositionInBounds(newPosition)) {

						if (PositionIsEmpty(newPosition)) {

							kingAttackMoves.Add(new ChessMove(pos, newPosition));
						}
						else if (PositionIsEnemy(newPosition, byPlayer)){

							kingAttackMoves.Add(new ChessMove(pos, newPosition));
						}
					}
				}
			
			return kingAttackMoves;
		}
		private bool AttemptPawnPromotion(int byPlayer, BoardPosition pos) {
			
			if (byPlayer == 2) {


					if (pos.Row == 6) {

						return true;
					}
				
			}
			else {

					if (pos.Row == 1) {

						return true;
					}
				
			}
			return false;
		}
		private bool AttemptEnPassant(int byPlayer, BoardPosition pos) {

			int row = byPlayer == 1 ? 3 : 4;
			if (mMoveHistory.Count == 0) {

				return false;
			}
			else {

				ChessMove lastMove = mMoveHistory.Last();

				if (pos.Row == row){

					BoardPosition leftPos = new BoardPosition(row, pos.Col - 1), rightPos = new BoardPosition(row, pos.Col + 1);

					if (lastMove.EndPosition == leftPos || lastMove.EndPosition == rightPos) {

						if (GetPieceAtPosition(leftPos).PieceType == ChessPieceType.Pawn || GetPieceAtPosition(rightPos).PieceType == ChessPieceType.Pawn) {

							return true;
						}
					}
				}
			}
			return false;
		}
		private bool AttemptCastleQueenSide(int byPlayer) {

			int oppositePlayer = CurrentPlayer == 1 ? 2 : 1;
			if (!IsCheck) {

				if (byPlayer == 1) {

					if (!mWhiteKingHasMoved && !mLeftWhiteRookHasMoved) {

						BoardPosition bp1 = new BoardPosition(7, 1);
						BoardPosition bp2 = new BoardPosition(7, 2);
						BoardPosition bp3 = new BoardPosition(7, 3);
						BoardPosition bp4 = new BoardPosition(7, 4);
						BoardPosition bp5 = new BoardPosition(7, 0);
						if (PositionIsEmpty(bp1) && PositionIsEmpty(bp2) && PositionIsEmpty(bp3) && GetPlayerAtPosition(bp4) == 1 && GetPlayerAtPosition(bp5) == 1) {

							if (!PositionIsAttacked(bp2, oppositePlayer) && !PositionIsAttacked(bp3, oppositePlayer)) {
								return true;
							}
						}
					}
				}
				else {

					if (!mBlackKingHasMoved && !mLeftBlackRookHasMoved) {

						BoardPosition bp1 = new BoardPosition(0, 1);
						BoardPosition bp2 = new BoardPosition(0, 2);
						BoardPosition bp3 = new BoardPosition(0, 3);
						BoardPosition bp4 = new BoardPosition(0, 4);
						BoardPosition bp5 = new BoardPosition(0, 0);
						if (PositionIsEmpty(bp1) && PositionIsEmpty(bp2) && PositionIsEmpty(bp3) && GetPlayerAtPosition(bp4) == 2 && GetPlayerAtPosition(bp5) == 2) {

							if (!PositionIsAttacked(bp2, oppositePlayer) && !PositionIsAttacked(bp3, oppositePlayer)) {
								return true;
							}
						}
					}
				}
			}
			return false;
		}
		private bool AttemptCastleKingSide(int byPlayer) {

			int oppositePlayer = CurrentPlayer == 1 ? 2 : 1;
			if (!IsCheck) {

				if (byPlayer == 1) {

					if (!mWhiteKingHasMoved && !mRightWhiteRookHasMoved) {

						BoardPosition bp1 = new BoardPosition(7, 5);
						BoardPosition bp2 = new BoardPosition(7, 6);
						BoardPosition bp3 = new BoardPosition(7, 4);
						BoardPosition bp4 = new BoardPosition(7, 7);
						if (PositionIsEmpty(bp1) && PositionIsEmpty(bp2) && GetPlayerAtPosition(bp3) == 1 && GetPlayerAtPosition(bp4) == 1) {

							if (!PositionIsAttacked(bp1, oppositePlayer) && !PositionIsAttacked(bp2, oppositePlayer))
							{
								return true;
							}
						}
					}
				}
				else {

					if (!mBlackKingHasMoved && !mRightBlackRookHasMoved) {

						BoardPosition bp1 = new BoardPosition(0, 5);
						BoardPosition bp2 = new BoardPosition(0, 6);
						BoardPosition bp3 = new BoardPosition(0, 4);
						BoardPosition bp4 = new BoardPosition(0, 7);
						if (PositionIsEmpty(bp1) && PositionIsEmpty(bp2) && GetPlayerAtPosition(bp3) == 2 && GetPlayerAtPosition(bp4) == 2)
						{

							if (!PositionIsAttacked(bp1, oppositePlayer) && !PositionIsAttacked(bp2, oppositePlayer))
							{
								return true;
							}
						}
					}
				}
			}
			return false;
		}
		private List<ChessMove> EnPassantMoves(int byPlayer, BoardPosition pos) {

			List<ChessMove> enPassantMoves = new List<ChessMove>();
			int row = byPlayer == 1 ? 3 : 4;
			ChessMove lastMove = mMoveHistory.Last();
			BoardPosition endPos;

			if(pos.Row == row) {

				BoardPosition leftPos = new BoardPosition(row, pos.Col - 1), rightPos = new BoardPosition(row, pos.Col + 1);

				if (lastMove.EndPosition == leftPos || lastMove.EndPosition == rightPos) {

					if (GetPieceAtPosition(leftPos).PieceType == ChessPieceType.Pawn && lastMove.EndPosition == leftPos) {

						endPos = byPlayer == 2 ? new BoardPosition(row + 1, pos.Col - 1) : new BoardPosition(row - 1, pos.Col - 1);
						enPassantMoves.Add(new ChessMove(pos, endPos, ChessMoveType.EnPassant));
					}
					else if (GetPieceAtPosition(rightPos).PieceType == ChessPieceType.Pawn && lastMove.EndPosition == rightPos) {

						endPos = byPlayer == 2 ? new BoardPosition(row + 1, pos.Col + 1) : new BoardPosition(row - 1, pos.Col + 1);
						enPassantMoves.Add(new ChessMove(pos, endPos, ChessMoveType.EnPassant));
					}
				}
			}
			return enPassantMoves;
		}
		private List<ChessMove> PawnPromotionMoves(int byPlayer, BoardPosition pos) {

			List<ChessMove> pawnPromotion = new List<ChessMove>();
			BoardPosition endPos;
			if (byPlayer == 1) {

				

					if (pos.Row == 1 && PositionIsEmpty(new BoardPosition(pos.Row - 1, pos.Col))){

						endPos = new BoardPosition(pos.Row - 1, pos.Col);
						pawnPromotion.Add(new ChessMove(pos, endPos, ChessMoveType.PawnPromote, ChessPieceType.Queen));
						pawnPromotion.Add(new ChessMove(pos, endPos, ChessMoveType.PawnPromote, ChessPieceType.Rook));
						pawnPromotion.Add(new ChessMove(pos, endPos, ChessMoveType.PawnPromote, ChessPieceType.Knight));
						pawnPromotion.Add(new ChessMove(pos, endPos, ChessMoveType.PawnPromote, ChessPieceType.Bishop));
					}
					if (pos.Row == 1 && PositionIsEnemy(new BoardPosition(pos.Row - 1, pos.Col - 1), byPlayer))
					{
						endPos = new BoardPosition(pos.Row - 1, pos.Col - 1);
						pawnPromotion.Add(new ChessMove(pos, endPos, ChessMoveType.PawnPromote, ChessPieceType.Queen));
						pawnPromotion.Add(new ChessMove(pos, endPos, ChessMoveType.PawnPromote, ChessPieceType.Rook));
						pawnPromotion.Add(new ChessMove(pos, endPos, ChessMoveType.PawnPromote, ChessPieceType.Knight));
						pawnPromotion.Add(new ChessMove(pos, endPos, ChessMoveType.PawnPromote, ChessPieceType.Bishop));
					}
					if (pos.Row == 1 && PositionIsEnemy(new BoardPosition(pos.Row - 1, pos.Col + 1), byPlayer))
					{
						endPos = new BoardPosition(pos.Row - 1, pos.Col + 1);
						pawnPromotion.Add(new ChessMove(pos, endPos, ChessMoveType.PawnPromote, ChessPieceType.Queen));
						pawnPromotion.Add(new ChessMove(pos, endPos, ChessMoveType.PawnPromote, ChessPieceType.Rook));
						pawnPromotion.Add(new ChessMove(pos, endPos, ChessMoveType.PawnPromote, ChessPieceType.Knight));
						pawnPromotion.Add(new ChessMove(pos, endPos, ChessMoveType.PawnPromote, ChessPieceType.Bishop));
					}

				
			}
			else {

				

					if (pos.Row == 6 && PositionIsEmpty(new BoardPosition(pos.Row + 1, pos.Col))){

						endPos = new BoardPosition(pos.Row + 1, pos.Col);
						pawnPromotion.Add(new ChessMove(pos, endPos, ChessMoveType.PawnPromote, ChessPieceType.Queen));
						pawnPromotion.Add(new ChessMove(pos, endPos, ChessMoveType.PawnPromote, ChessPieceType.Rook));
						pawnPromotion.Add(new ChessMove(pos, endPos, ChessMoveType.PawnPromote, ChessPieceType.Knight));
						pawnPromotion.Add(new ChessMove(pos, endPos, ChessMoveType.PawnPromote, ChessPieceType.Bishop));
					}
					if (pos.Row == 6 && PositionIsEnemy(new BoardPosition(pos.Row + 1, pos.Col - 1), byPlayer))
					{
						endPos = new BoardPosition(pos.Row + 1, pos.Col - 1);
						pawnPromotion.Add(new ChessMove(pos, endPos, ChessMoveType.PawnPromote, ChessPieceType.Queen));
						pawnPromotion.Add(new ChessMove(pos, endPos, ChessMoveType.PawnPromote, ChessPieceType.Rook));
						pawnPromotion.Add(new ChessMove(pos, endPos, ChessMoveType.PawnPromote, ChessPieceType.Knight));
						pawnPromotion.Add(new ChessMove(pos, endPos, ChessMoveType.PawnPromote, ChessPieceType.Bishop));
					}
					if (pos.Row == 6 && PositionIsEnemy(new BoardPosition(pos.Row + 1, pos.Col + 1), byPlayer))
					{
						endPos = new BoardPosition(pos.Row + 1, pos.Col + 1);
						pawnPromotion.Add(new ChessMove(pos, endPos, ChessMoveType.PawnPromote, ChessPieceType.Queen));
						pawnPromotion.Add(new ChessMove(pos, endPos, ChessMoveType.PawnPromote, ChessPieceType.Rook));
						pawnPromotion.Add(new ChessMove(pos, endPos, ChessMoveType.PawnPromote, ChessPieceType.Knight));
						pawnPromotion.Add(new ChessMove(pos, endPos, ChessMoveType.PawnPromote, ChessPieceType.Bishop));
					}

				
			}
			return pawnPromotion;
		}
		private List<ChessMove> CastleQueenSideMoves(int byPlayer) {

			List<ChessMove> castleMoves = new List<ChessMove>();
			BoardPosition startPos, endPos;
			if (byPlayer == 1) {

				startPos = new BoardPosition(7, 4);
				endPos = new BoardPosition(7, 2);
				castleMoves.Add(new ChessMove(startPos, endPos, ChessMoveType.CastleQueenSide));
					
			}
			else {

				startPos = new BoardPosition(0, 4);
				endPos = new BoardPosition(0, 2);
				castleMoves.Add(new ChessMove(startPos, endPos, ChessMoveType.CastleQueenSide));

			}
			return castleMoves;
		}
		private List<ChessMove> CastleKingSideMoves(int byPlayer) {

			List<ChessMove> castleMoves = new List<ChessMove>();
			BoardPosition startPos, endPos;
			if (byPlayer == 1) {

				startPos = new BoardPosition(7, 4);
				endPos = new BoardPosition(7, 6);
				castleMoves.Add(new ChessMove(startPos, endPos, ChessMoveType.CastleKingSide));

			}
			else {

				startPos = new BoardPosition(0, 4);
				endPos = new BoardPosition(0, 6);
				castleMoves.Add(new ChessMove(startPos, endPos, ChessMoveType.CastleKingSide));

			}
			return castleMoves;
		}
		private List<BoardDirection> PawnAttackDirections(int player) {

			List<BoardDirection> pawnAttackDirections = new List<BoardDirection>();

			if (player == 1) {

				pawnAttackDirections.Add(new BoardDirection(-1, -1));
				pawnAttackDirections.Add(new BoardDirection(-1, 1));
			}
			else {

				pawnAttackDirections.Add(new BoardDirection(1, -1));
				pawnAttackDirections.Add(new BoardDirection(1, 1));
			}
			return pawnAttackDirections;
		}
		private GameAdvantage Advantage() {

			return new GameAdvantage(MBoardAdvantage > 0 ? 1 : MBoardAdvantage < 0 ? 2 : 0, Math.Abs(MBoardAdvantage));
		}
		private void ChangeAdvantage(ChessPiece piece, int capturedOrPlaced) {
			
			//1 = piece was captured, -1 = piece was placed on board
			if (piece.PieceType == ChessPieceType.Pawn) {

				if (piece.Player == 1 && capturedOrPlaced == 1){

					MBoardAdvantage -= pawnValue;
				}
				else if (piece.Player == 1 && capturedOrPlaced == -1) {

					MBoardAdvantage += pawnValue;
				}
				else if (piece.Player == 2 && capturedOrPlaced == 1) {

					MBoardAdvantage += pawnValue;
				}
				else if (piece.Player == 2 && capturedOrPlaced == -1) {

					MBoardAdvantage -= pawnValue;
				}
			   }
			else if (piece.PieceType == ChessPieceType.Knight || piece.PieceType == ChessPieceType.Bishop) {

				if (piece.Player == 1 && capturedOrPlaced == 1) {

					MBoardAdvantage -= knightBishopValue;
				}
				else if (piece.Player == 1 && capturedOrPlaced == -1) {

					MBoardAdvantage += knightBishopValue;
				}
				else if (piece.Player == 2 && capturedOrPlaced == 1) {

					MBoardAdvantage += knightBishopValue;
				}
				else if (piece.Player == 2 && capturedOrPlaced == -1) {

					MBoardAdvantage -= knightBishopValue;
				}
			}
			else if (piece.PieceType == ChessPieceType.Rook) {

				if (piece.Player == 1 && capturedOrPlaced == 1) {

					MBoardAdvantage -= rookValue;
				}
				else if (piece.Player == 1 && capturedOrPlaced == -1) {

					MBoardAdvantage += rookValue;
				}
				else if (piece.Player == 2 && capturedOrPlaced == 1) {

					MBoardAdvantage += rookValue;
				}
				else if (piece.Player == 2 && capturedOrPlaced == -1) {

					MBoardAdvantage -= rookValue;
				}
			}
			else if (piece.PieceType == ChessPieceType.Queen) {

				if (piece.Player == 1 && capturedOrPlaced == 1) {

					MBoardAdvantage -= queenValue;
				}
				else if (piece.Player == 1 && capturedOrPlaced == -1) {

					MBoardAdvantage += queenValue;
				}
				else if (piece.Player == 2 && capturedOrPlaced == 1) {

					MBoardAdvantage += queenValue;
				}
				else if (piece.Player == 2 && capturedOrPlaced == -1) {

					MBoardAdvantage -= queenValue;
				}
			}
			
		}
		
		#endregion

		#region Explicit IGameBoard implementations.
		IEnumerable<IGameMove> IGameBoard.GetPossibleMoves() {
			return GetPossibleMoves();
		}
		void IGameBoard.ApplyMove(IGameMove m) {
			ApplyMove(m as ChessMove);
		}
		IReadOnlyList<IGameMove> IGameBoard.MoveHistory => mMoveHistory;

		private long weight;
		public long BoardWeight
		{
			get
			{
				weight = CurrentPlayer == 1 ? CurrentAdvantage.Advantage : -CurrentAdvantage.Advantage;

				foreach (BoardPosition pos in BoardPosition.GetRectangularPositions(BoardSize, BoardSize))
				{
					ChessPiece currentPiece = GetPieceAtPosition(pos);

					if (currentPiece.PieceType == ChessPieceType.Pawn)
					{
						int originalRow = currentPiece.Player == 1 ? 6 : 1;
						int rowChange = originalRow - pos.Row;
						weight += rowChange;
					}
					int oppositePlayer = currentPiece.Player == 1 ? 2 : 1;
					if (PositionIsAttacked(pos, oppositePlayer))
					{
						int valueChange = 0;
						if (currentPiece.PieceType == ChessPieceType.Knight || currentPiece.PieceType == ChessPieceType.Bishop)
						{
							valueChange = 1;
						}
						else if (currentPiece.PieceType == ChessPieceType.Rook)
						{
							valueChange = 2;
						}
						else if (currentPiece.PieceType == ChessPieceType.Queen)
						{
							valueChange = 5;
						}
						else if (currentPiece.PieceType == ChessPieceType.King)
						{
							valueChange = 4;
						}
						if (CurrentPlayer == 1)
						{
							weight += valueChange;
						}
						else
						{
							weight -= valueChange;
						}
					}
					bool threatened = false;
					BoardPosition newPos;
					if (currentPiece.PieceType == ChessPieceType.Knight)
					{
						foreach (BoardDirection knightDir in KnightDirections)
						{
							newPos = pos.Translate(knightDir);

							if (PositionInBounds(newPos) && !PositionIsEmpty(newPos) && !PositionIsEnemy(newPos, currentPiece.Player)
								&& GetPieceAtPosition(newPos).PieceType == ChessPieceType.Knight || GetPieceAtPosition(newPos).PieceType == ChessPieceType.Bishop)
							{
								threatened = true;
							}
						}
					}
					else if (currentPiece.PieceType == ChessPieceType.Pawn)
					{
						foreach (BoardDirection pawnDir in PawnAttackDirections(currentPiece.Player))
						{
							newPos = pos.Translate(pawnDir);

							if (PositionInBounds(newPos) && !PositionIsEmpty(newPos) && !PositionIsEnemy(newPos, currentPiece.Player)
								&& GetPieceAtPosition(newPos).PieceType == ChessPieceType.Knight || GetPieceAtPosition(newPos).PieceType == ChessPieceType.Bishop)
							{
								threatened = true;
							}
						}
					}
					else if (currentPiece.PieceType == ChessPieceType.Rook)
					{
						foreach (BoardDirection rookDir in RookDirections)
						{
							newPos = pos;
							while (PositionInBounds(newPos) && PositionIsEmpty(newPos)) {

								newPos = newPos.Translate(rookDir);
								if (!PositionIsEmpty(newPos) && !PositionIsEnemy(newPos, currentPiece.Player)
								&& GetPieceAtPosition(newPos).PieceType == ChessPieceType.Knight || GetPieceAtPosition(newPos).PieceType == ChessPieceType.Bishop)
								{
									threatened = true;
									break;
								}
							}
						}
					}
					else if (currentPiece.PieceType == ChessPieceType.Bishop)
					{
						foreach (BoardDirection bishopDir in BishopDirections)
						{
							newPos = pos;
							while (PositionInBounds(newPos) && PositionIsEmpty(newPos)) {

								newPos = newPos.Translate(bishopDir);
								if (!PositionIsEmpty(newPos) && !PositionIsEnemy(newPos, currentPiece.Player)
								&& GetPieceAtPosition(newPos).PieceType == ChessPieceType.Knight || GetPieceAtPosition(newPos).PieceType == ChessPieceType.Bishop)
								{
									threatened = true;
									break;
								}
							}
						}
					}
					else if (currentPiece.PieceType == ChessPieceType.Queen)
					{
						foreach (BoardDirection queenDir in BoardDirection.CardinalDirections)
						{
							newPos = pos;
							while (PositionInBounds(newPos) && PositionIsEmpty(newPos)) {

								newPos = newPos.Translate(queenDir);
								if (!PositionIsEmpty(newPos) && !PositionIsEnemy(newPos, currentPiece.Player)
								&& GetPieceAtPosition(newPos).PieceType == ChessPieceType.Knight || GetPieceAtPosition(newPos).PieceType == ChessPieceType.Bishop)
								{
									threatened = true;
									break;
								}
							}
						}
					}
					else if (currentPiece.PieceType == ChessPieceType.King)
					{
						foreach (BoardDirection kingDir in BoardDirection.CardinalDirections)
						{
							newPos = pos.Translate(kingDir);
							if (PositionInBounds(newPos) && !PositionIsEmpty(newPos) && !PositionIsEnemy(newPos, currentPiece.Player)
								&& GetPieceAtPosition(newPos).PieceType == ChessPieceType.Knight || GetPieceAtPosition(newPos).PieceType == ChessPieceType.Bishop)
							{
								threatened = true;
							}
						}
					}
					if (threatened == true)
					{
						if (currentPiece.Player == 1)
						{
							weight++;
						}
						else
						{
							weight--;
						}
					}
				} 
				return weight;
			}
			private set { weight = value; }
		}
        public BoardPosition MRightBlackRook { get; set; } = new BoardPosition(0, 7);
        public int MBoardAdvantage { get; set; } = 0;
		#endregion

		// Constructst a new chess board with all pieces at their starting position.
		//shifts each piece to the left 4 bits and adds the piece next to it to represent two pieces as one byte
		public ChessBoard() {

			mChessBoard[28] = (mWhiteRook << 4) + mWhiteKnight;
			mChessBoard[29] = (mWhiteBishop << 4) + mWhiteQueen;
			mChessBoard[30] = (mWhiteKing << 4) + mWhiteBishop;
			mChessBoard[31] = (mWhiteKnight << 4) + mWhiteRook;

			for (int i = 24; i < 28; i++) {

				mChessBoard[i] = (mWhitePawn << 4) + mWhitePawn;
			}
			for (int i = 8; i < 24; i++) {

				mChessBoard[i] = mEmptySpace;
			}
			for (int i = 4; i < 8; i++) {

				mChessBoard[i] = (mBlackPawn << 4) + mBlackPawn;
			}

			mChessBoard[0] = (mBlackRook << 4) + mBlackKnight;
			mChessBoard[1] = (mBlackBishop << 4) + mBlackQueen;
			mChessBoard[2] = (mBlackKing << 4) + mBlackBishop;
			mChessBoard[3] = (mBlackKnight << 4) + mBlackRook;

			CurrentPlayer = 1;
			whiteKingPos = new BoardPosition(7, 4);
			blackKingPos = new BoardPosition(0, 4);
			whiteLeftRookPos = new BoardPosition(7, 0);
			blackLeftRookPos = new BoardPosition(0, 0);
			whiteRightRookPos = new BoardPosition(7, 7);
			blackRightRookPos = new BoardPosition(0, 7);
		}

		public ChessBoard(IEnumerable<Tuple<BoardPosition, ChessPiece>> startingPositions)
			: this() {
			var king1 = startingPositions.Where(t => t.Item2.Player == 1 && t.Item2.PieceType == ChessPieceType.King);
			var king2 = startingPositions.Where(t => t.Item2.Player == 2 && t.Item2.PieceType == ChessPieceType.King);
			if (king1.Count() != 1 || king2.Count() != 1) {
				throw new ArgumentException("A chess board must have a single king for each player");
			}

			foreach (var position in BoardPosition.GetRectangularPositions(8, 8)) {
				SetPieceAtPosition(position, ChessPiece.Empty);
			}

			int[] values = { 0, 0 };
			foreach (var pos in startingPositions) {
				SetPieceAtPosition(pos.Item1, pos.Item2);
				// TODO: you must calculate the overall advantage for this board, in terms of the pieces
				// that the board has started with. "pos.Item2" will give you the chess piece being placed
				// on this particular position.
				ChangeAdvantage(pos.Item2, -1);
			}
		}
	}
}
