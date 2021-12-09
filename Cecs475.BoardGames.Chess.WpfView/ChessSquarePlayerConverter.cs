using Cecs475.BoardGames.Chess.Model;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace CECS475.BoardGames.Chess.WpfView
{
    public class ChessSquarePlayerConverter : IValueConverter {

		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			ChessPiece piece = (ChessPiece)value;

			if (piece.Player == 0)
			{
				return null;
			}
			else if (piece.Player == 1)
			{
				if (piece.PieceType.Equals(ChessPieceType.Pawn))
				{
					return new BitmapImage(new Uri("/CECS475.BoardGames.Chess.WpfView;component/Resources/white pawn.png", UriKind.Relative));
				}
				else if (piece.PieceType.Equals(ChessPieceType.Rook))
				{
					return new BitmapImage(new Uri("/CECS475.BoardGames.Chess.WpfView;component/Resources/white rook.png", UriKind.Relative));
				}
				else if (piece.PieceType.Equals(ChessPieceType.Knight))
				{
					return new BitmapImage(new Uri("/CECS475.BoardGames.Chess.WpfView;component/Resources/white knight.png", UriKind.Relative));
				}
				else if (piece.PieceType.Equals(ChessPieceType.Bishop))
				{
					return new BitmapImage(new Uri("/CECS475.BoardGames.Chess.WpfView;component/Resources/white bishop.png", UriKind.Relative));
				}
				else if (piece.PieceType.Equals(ChessPieceType.Queen))
				{
					return new BitmapImage(new Uri("/CECS475.BoardGames.Chess.WpfView;component/Resources/white queen.png", UriKind.Relative));
				}
				return new BitmapImage(new Uri("/CECS475.BoardGames.Chess.WpfView;component/Resources/white king.png", UriKind.Relative));
			}
			else
			{
				if (piece.PieceType.Equals(ChessPieceType.Pawn))
				{
					return new BitmapImage(new Uri("/CECS475.BoardGames.Chess.WpfView;component/Resources/black pawn.png", UriKind.Relative));
				}
				else if (piece.PieceType.Equals(ChessPieceType.Rook))
				{
					return new BitmapImage(new Uri("/CECS475.BoardGames.Chess.WpfView;component/Resources/black rook.png", UriKind.Relative));
				}
				else if (piece.PieceType.Equals(ChessPieceType.Knight))
				{
					return new BitmapImage(new Uri("/CECS475.BoardGames.Chess.WpfView;component/Resources/black knight.png", UriKind.Relative));
				}
				else if (piece.PieceType.Equals(ChessPieceType.Bishop))
				{
					return new BitmapImage(new Uri("/CECS475.BoardGames.Chess.WpfView;component/Resources/black bishop.png", UriKind.Relative));
				}
				else if (piece.PieceType.Equals(ChessPieceType.Queen))
				{
					return new BitmapImage(new Uri("/CECS475.BoardGames.Chess.WpfView;component/Resources/black queen.png", UriKind.Relative));
				}
				return new BitmapImage(new Uri("/CECS475.BoardGames.Chess.WpfView;component/Resources/black king.png", UriKind.Relative));
			}
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}
