namespace ChessDroid.Services
{
    public static class Chess960Service
    {
        private static readonly Random _rng = new();

        public static int GetRandomPosition() => _rng.Next(960);

        // Returns back-rank piece chars for White, index 0 = a-file … 7 = h-file.
        // Algorithm: https://en.wikipedia.org/wiki/Chess960_numbering_scheme
        public static char[] GetBackRank(int position)
        {
            if (position < 0 || position > 959) throw new ArgumentOutOfRangeException(nameof(position));

            var sq = new char[8]; // '\0' = empty
            int n  = position;

            // Step A — light-squared bishop (files b,d,f,h = indices 1,3,5,7)
            sq[(n % 4) * 2 + 1] = 'B'; n /= 4;

            // Step B — dark-squared bishop (files a,c,e,g = indices 0,2,4,6)
            sq[(n % 4) * 2] = 'B'; n /= 4;

            // Step C — queen in the nth empty slot (0-based)
            PlaceNth(sq, n % 6, 'Q'); n /= 6;

            // Step D — both knights relative to the same 5 remaining empty slots (simultaneous)
            int[] k1 = { 0, 0, 0, 0, 1, 1, 1, 2, 2, 3 };
            int[] k2 = { 1, 2, 3, 4, 2, 3, 4, 3, 4, 4 };
            var e5 = Enumerable.Range(0, 8).Where(i => sq[i] == '\0').ToList();
            sq[e5[k1[n]]] = 'N';
            sq[e5[k2[n]]] = 'N';

            // Step E — remaining 3 slots → Rook, King, Rook (left-to-right = queenside R, K, kingside R)
            PlaceNth(sq, 0, 'R');
            PlaceNth(sq, 0, 'K');
            PlaceNth(sq, 0, 'R');

            return sq;
        }

        // Returns the full FEN for a Chess960 starting position (Shredder castling rights).
        public static string GetStartFen(int position)
        {
            var w = GetBackRank(position);
            string whitePieces = new(w);

            // Shredder-FEN castling: file letter of each rook (uppercase = white, lowercase = black),
            // listed in ascending file order so the queenside letter comes first.
            string castlingWhite = string.Concat(
                Enumerable.Range(0, 8).Where(f => w[f] == 'R').Select(f => (char)('A' + f)));
            string castling = castlingWhite + castlingWhite.ToLower();

            return $"{whitePieces.ToLower()}/pppppppp/8/8/8/8/PPPPPPPP/{whitePieces} w {castling} - 0 1";
        }

        // Places piece p at the nth empty slot (0-based) in sq.
        private static void PlaceNth(char[] sq, int n, char p)
        {
            int count = 0;
            for (int i = 0; i < sq.Length; i++)
            {
                if (sq[i] == '\0')
                {
                    if (count == n) { sq[i] = p; return; }
                    count++;
                }
            }
        }
    }
}
