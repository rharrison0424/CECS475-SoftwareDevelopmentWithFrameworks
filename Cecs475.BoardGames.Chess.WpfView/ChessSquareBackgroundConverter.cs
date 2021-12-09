using Cecs475.BoardGames.Model;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;

namespace CECS475.BoardGames.Chess.WpfView
{
    public class ChessSquareBackgroundConverter : IMultiValueConverter {

		private static SolidColorBrush SELECTED_BRUSH = Brushes.Red;
		private static SolidColorBrush CHECK_BRUSH = Brushes.Yellow;
		private static SolidColorBrush HOVER_BRUSH = Brushes.LightGreen;
		private static SolidColorBrush EVEN_BRUSH = Brushes.SandyBrown;
		private static SolidColorBrush ODD_BRUSH = Brushes.BlanchedAlmond;
		private static SolidColorBrush CHECKMATE_BRUSH = Brushes.Black;

		public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
		{
			BoardPosition pos = (BoardPosition)values[0];
			bool isHighlighted = (bool)values[1];
			bool isSelected = (bool)values[2];
			bool isInCheck = (bool)values[3];
			bool isInCheckmate = (bool)values[4];

			// Hovered squares have a specific color.
			if (isHighlighted)
			{
				return HOVER_BRUSH;
			}
			
			if (isSelected)
			{
				return SELECTED_BRUSH;
			}
			
			if (isInCheck)
			{
				return CHECK_BRUSH;
			}
			if (isInCheckmate)
			{
				return CHECKMATE_BRUSH;
			}
			if (pos.Col % 2 == 0)
			{
				return EVEN_BRUSH;
			}
			return ODD_BRUSH;
		}

		public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}
