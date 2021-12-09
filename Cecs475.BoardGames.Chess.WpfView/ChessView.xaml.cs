using Cecs475.BoardGames.Chess.Model;
using Cecs475.BoardGames.WpfView;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace CECS475.BoardGames.Chess.WpfView
{
    /// <summary>
    /// Interaction logic for ChessView.xaml
    /// </summary>
    public partial class ChessView : UserControl, IWpfGameView
    {
        public ChessView()
        {
            InitializeComponent();
        }

        public ChessViewModel ChessViewModel => FindResource("vm") as ChessViewModel;

        public Control ViewControl => this;

        public IGameViewModel ViewModel => ChessViewModel;

        private async void Border_MouseClick(object sender, MouseEventArgs e)
        {
            if (!IsEnabled)
            {
                return;
            }
            Border b = sender as Border;
            var square = b.DataContext as ChessSquare;
            var vm = FindResource("vm") as ChessViewModel;

            if (vm.SelectedSquare == null && square.Player == vm.CurrentPlayer && vm.StartMoves.Contains(square.Position))
            {
                vm.SelectedSquare = square;
                square.IsSelected = true;
            }
            else if (vm.SelectedSquare != null)
            {
                foreach (var move in vm.PossMoves.Where(x => x.StartPosition == vm.SelectedSquare.Position))
                {
                    if (move.EndPosition.Equals(square.Position))
                    {
                        if (move.PromotionPiece != ChessPieceType.Empty)
                        {
                            var pawnPromotionWindow = new PawnPromotion(vm, move.StartPosition, move.EndPosition);
                            pawnPromotionWindow.Show();
                            break;
                        }
                        else
                        {
                            IsEnabled = false;
                            vm.UndoEnabler = false;
                            await vm.ApplyMove(move.StartPosition, move.EndPosition, ChessPieceType.Empty);
                            IsEnabled = true;
                            vm.UndoEnabler = true;
                            break;
                        }
                    }
                }
                vm.SelectedSquare.IsSelected = false;
                vm.SelectedSquare = null;
            }
        }
        private void Border_MouseEnter(object sender, MouseEventArgs e)
        {
            if (!IsEnabled)
            {
                return;
            }
            Border b = sender as Border;
            var square = b.DataContext as ChessSquare;
            var vm = FindResource("vm") as ChessViewModel;
            if (vm.StartMoves.Contains(square.Position))
            {
                if (vm.SelectedSquare != null)
                {
                    square.IsHighlighted = false;
                }
                else
                {
                    square.IsHighlighted = true;
                }
            }
            if (vm.SelectedSquare != null)
            {
                foreach (var move in vm.PossMoves.Where(x => x.StartPosition == vm.SelectedSquare.Position))
                {
                    if (move.EndPosition.Equals(square.Position)) {

                        square.IsHighlighted = true;
                    }
                }
            }
              
        }

        private void Border_MouseLeave(object sender, MouseEventArgs e)
        {
            if (!IsEnabled)
            {
                return;
            }
            Border b = sender as Border;
            var square = b.DataContext as ChessSquare;
            square.IsHighlighted = false;
        }

    }
}
