using ChessDroid.Models;

namespace ChessDroid
{
    public class BoardVisualizer : Form
    {
        private const int SQUARE_SIZE = 60;
        private const int BOARD_SIZE = 8;
        private Panel boardPanel = new Panel();
        private ChessBoard board;
        private string templatePath;

        public BoardVisualizer(ChessBoard detectedBoard, string templatesPath)
        {
            board = detectedBoard;
            templatePath = templatesPath;

            InitializeForm();
            DrawBoard();
        }

        private void InitializeForm()
        {
            this.Text = "Detected Board Position";
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(45, 45, 45);
            this.ShowIcon = false;
            this.TopMost = true;

            // Create main panel
            boardPanel = new Panel
            {
                Location = new Point(20, 20),
                Size = new Size(SQUARE_SIZE * BOARD_SIZE, SQUARE_SIZE * BOARD_SIZE),
                BorderStyle = BorderStyle.FixedSingle
            };

            this.Controls.Add(boardPanel);

            // Add FEN label
            var fenLabel = new Label
            {
                Location = new Point(20, SQUARE_SIZE * BOARD_SIZE + 30),
                Size = new Size(SQUARE_SIZE * BOARD_SIZE, 40),
                Text = $"FEN: {board.ToFEN()}",
                ForeColor = Color.White,
                Font = new Font("Consolas", 9),
                AutoSize = false
            };
            this.Controls.Add(fenLabel);

            // Add close button
            var closeButton = new Button
            {
                Text = "Close",
                Location = new Point(20 + SQUARE_SIZE * BOARD_SIZE - 80, SQUARE_SIZE * BOARD_SIZE + 80),
                Size = new Size(80, 30),
                BackColor = Color.FromArgb(70, 70, 70),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            closeButton.Click += (s, e) => this.Close();
            this.Controls.Add(closeButton);

            this.ClientSize = new Size(SQUARE_SIZE * BOARD_SIZE + 40, SQUARE_SIZE * BOARD_SIZE + 130);
        }

        private void DrawBoard()
        {
            boardPanel.Controls.Clear();

            for (int row = 0; row < BOARD_SIZE; row++)
            {
                for (int col = 0; col < BOARD_SIZE; col++)
                {
                    // Create square
                    var square = new Panel
                    {
                        Location = new Point(col * SQUARE_SIZE, row * SQUARE_SIZE),
                        Size = new Size(SQUARE_SIZE, SQUARE_SIZE),
                        BackColor = (row + col) % 2 == 0 ? Color.FromArgb(240, 217, 181) : Color.FromArgb(181, 136, 99)
                    };

                    // Add coordinate labels on edges
                    if (col == 0)
                    {
                        var rankLabel = new Label
                        {
                            Text = (8 - row).ToString(),
                            Location = new Point(2, 2),
                            Size = new Size(15, 15),
                            Font = new Font("Arial", 8, FontStyle.Bold),
                            ForeColor = (row + col) % 2 == 0 ? Color.FromArgb(181, 136, 99) : Color.FromArgb(240, 217, 181),
                            BackColor = Color.Transparent
                        };
                        square.Controls.Add(rankLabel);
                    }

                    if (row == 7)
                    {
                        var fileLabel = new Label
                        {
                            Text = ((char)('a' + col)).ToString(),
                            Location = new Point(SQUARE_SIZE - 15, SQUARE_SIZE - 17),
                            Size = new Size(15, 15),
                            Font = new Font("Arial", 8, FontStyle.Bold),
                            ForeColor = (row + col) % 2 == 0 ? Color.FromArgb(181, 136, 99) : Color.FromArgb(240, 217, 181),
                            BackColor = Color.Transparent
                        };
                        square.Controls.Add(fileLabel);
                    }

                    // Get piece at this position
                    char piece = board.GetPiece(row, col);
                    if (piece != '.')
                    {
                        // Try to load piece image
                        string pieceImagePath = GetPieceImagePath(piece);
                        if (!string.IsNullOrEmpty(pieceImagePath) && System.IO.File.Exists(pieceImagePath))
                        {
                            try
                            {
                                var pictureBox = new PictureBox
                                {
                                    Location = new Point(0, 0),
                                    Size = new Size(SQUARE_SIZE, SQUARE_SIZE),
                                    SizeMode = PictureBoxSizeMode.Zoom,
                                    Image = Image.FromFile(pieceImagePath),
                                    BackColor = Color.Transparent
                                };
                                square.Controls.Add(pictureBox);
                            }
                            catch
                            {
                                // If image load fails, show text representation
                                AddPieceText(square, piece);
                            }
                        }
                        else
                        {
                            // No image found, show text representation
                            AddPieceText(square, piece);
                        }
                    }

                    boardPanel.Controls.Add(square);
                }
            }
        }

        private void AddPieceText(Panel square, char piece)
        {
            var pieceLabel = new Label
            {
                Text = GetPieceSymbol(piece),
                Location = new Point(0, 0),
                Size = new Size(SQUARE_SIZE, SQUARE_SIZE),
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Arial", 24, FontStyle.Bold),
                ForeColor = char.IsUpper(piece) ? Color.White : Color.Black,
                BackColor = Color.Transparent
            };
            square.Controls.Add(pieceLabel);
        }

        private string GetPieceSymbol(char piece)
        {
            // Unicode chess symbols
            return piece switch
            {
                'K' => "♔",
                'Q' => "♕",
                'R' => "♖",
                'B' => "♗",
                'N' => "♘",
                'P' => "♙",
                'k' => "♚",
                'q' => "♛",
                'r' => "♜",
                'b' => "♝",
                'n' => "♞",
                'p' => "♟",
                _ => ""
            };
        }

        private string GetPieceImagePath(char piece)
        {
            // Map piece character to image file
            // piece: K/Q/R/B/N/P (white uppercase), k/q/r/b/n/p (black lowercase)
            string color = char.IsUpper(piece) ? "w" : "b";
            char pieceType = char.ToUpper(piece);

            // Try different template paths
            string[] possiblePaths = new string[]
            {
                System.IO.Path.Combine(templatePath, "Lichess", $"{color}{pieceType}.png"),
                System.IO.Path.Combine(templatePath, "Chess.com", $"{color}{pieceType}.png"),
                System.IO.Path.Combine(templatePath, $"{color}{pieceType}_light.png"),
                System.IO.Path.Combine(templatePath, $"{color}{pieceType}_dark.png")
            };

            foreach (string path in possiblePaths)
            {
                if (System.IO.File.Exists(path))
                {
                    return path;
                }
            }

            return "";
        }
    }
}